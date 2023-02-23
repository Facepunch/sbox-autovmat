using Sandbox;
using Editor;
using System.IO;
using System.Collections.Generic;

namespace Facepunch.Tools;

[Tool( "Autovmat", "texture", "Create materials from a group of textures easily." )]
public class Autovmat : DockWindow
{
	private class Material
	{
		public string Directory { get; set; }
		public string RelativePath { get; set; }
		public string Name { get; set; }
		public string Albedo { get; set; }
		public string Normal { get; set; }
		public string Roughness { get; set; }
		public string Metalness { get; set; }
		public string AmbientOcclusion { get; set; }

		public bool HasAlbedo => !string.IsNullOrEmpty( Albedo );
		public bool HasNormal => !string.IsNullOrEmpty( Normal );
		public bool HasRoughness => !string.IsNullOrEmpty( Roughness );
		public bool HasMetalness => !string.IsNullOrEmpty( Metalness );
		public bool HasAmbientOcclusion => !string.IsNullOrEmpty( AmbientOcclusion );
		public bool HasEverything => HasAlbedo && HasNormal && HasRoughness && HasMetalness && HasAmbientOcclusion;
		public bool IsSpecular => HasMetalness;

		public string GetMaterialString()
		{
			return MaterialTemplate.String
				.Replace( "{Albedo}", Albedo )
				.Replace( "{Normal}", Normal )
				.Replace( "{Roughness}", Roughness )
				.Replace( "{Metalness}", Metalness )
				.Replace( "{AmbientOcclusion}", AmbientOcclusion )
				.Replace( "{Specular}", IsSpecular ? "1" : "0" )
				.Replace( "{UseMetalness}", HasMetalness ? "1" : "0" );
		}
	}

	private enum TextureType
	{
		None,
		Albedo,
		Normal,
		Roughness,
		Metalness,
		AmbientOcclusion
	}

	private class Settings
	{
		[Description( "File name suffix for albedo textures." )]
		public string Albedo { get; set; } = "_color";

		[Description( "File name suffix for normal textures." )]
		public string Normal { get; set; } = "_normal";

		[Description( "File name suffix for roughness textures." )]
		public string Roughness { get; set; } = "_rough";

		[Description( "File name suffix for metalness textures." )]
		public string Metalness { get; set; } = "_metal";

		[Description( "File name suffix for ambient occlusion textures." )]
		public string AmbientOcclusion { get; set; } = "_ao";

		[Description( "Complain if we're missing any texture?" )]
		public bool ComplainOnMissing { get; set; } = true;

		[Description( "Should we recurse directories to find more textures?" )]
		public bool RecurseDirectories { get; set; } = false;
	}

	private SettingsView PropertiesView { get; set; }
	private Settings CurrentSettings { get; set; }
	private Dictionary<string,Material> Materials { get; set; }
	private List<string> Textures { get; set; }
	private Option ConvertOption { get; set; }
	private TexturesView TexturesView { get; set; }
	private ToolBar ToolBar { get; set; }

	public Autovmat()
	{
		CurrentSettings = new();
		Materials = new();
		Textures = new();
		DeleteOnClose = true;
		Title = "Autovmat";
		Size = new Vector2( 800f, 800f );

		CreateUI();
		Show();
	}

	protected override void OnPaint()
	{
		base.OnPaint();
	}

	private void BuildMenuBar()
	{
		var menu = MenuBar.AddMenu( "File" );
		menu.AddOption( "Open Folder", "folder_open", Open );
		menu.AddOption( "Quit", "disabled_by_default", Close );
	}

	private void CreateToolBar()
	{
		ToolBar = new ToolBar( this, "AutovmatToolbar" );
		
		AddToolBar( ToolBar, ToolbarPosition.Top );

		var openFolder = ToolBar.AddOption( "Open Folder", "common/open.png", Open );
		openFolder.StatusText = "Open Folder";

		ConvertOption = ToolBar.AddOption( "Convert", "check_circle_outline", () => Convert() );
		ConvertOption.StatusText = "Convert";
		ConvertOption.Enabled = Materials.Count > 0;
	}

	private void Convert()
	{
		foreach ( var kv in Materials )
		{
			var material = kv.Value;
			var filePath = Path.Combine( material.Directory, $"{material.Name}.vmat" );

			File.WriteAllText( filePath, material.GetMaterialString() );
			Log.Info( $"✅ Successfully saved {material.Name}.vmat to {material.Directory}." );

			var asset = AssetSystem.RegisterFile( filePath );
			asset.Compile( true );

			Log.Info( $"✅ Successfully compiled {material.Name}.vmat_c." );
		}
	}

	private string FindRootPath( string directory )
	{
		if ( File.Exists( Path.Combine( directory, ".addon" ) ) )
		{
			return directory;
		}

		var parent = Directory.GetParent( directory );

		if ( parent == null || !parent.Exists )
			return null;

		return FindRootPath( parent.FullName );
	}

	private void Open()
	{
		var fileDialog = new FileDialog( null );
		fileDialog.Title = $"Open Folder";
		fileDialog.SetFindDirectory();
		fileDialog.SetModeOpen();

		if ( !fileDialog.Execute() )
			return;

		Materials.Clear();
		Textures.Clear();

		var rootPath = FindRootPath( fileDialog.Directory );

		if ( string.IsNullOrEmpty( rootPath ) )
		{
			Log.Error( "❌ You must select a folder that exists within a project with a .addon file!" );
			return;
		}

		AddFromDirectory( rootPath, fileDialog.Directory, CurrentSettings.RecurseDirectories );

		foreach ( var kv in Materials )
		{
			var material = kv.Value;
			var pathWithName = Path.Combine( material.RelativePath, material.Name );

			if ( !material.HasAlbedo )
				Log.Warning( $"No albedo ({CurrentSettings.Albedo}) texture found for {pathWithName}" );
			if ( !material.HasNormal )
				Log.Warning( $"No normal ({CurrentSettings.Normal}) texture found for {pathWithName}" );
			if ( !material.HasRoughness )
				Log.Warning( $"No roughness ({CurrentSettings.Roughness}) texture found for {pathWithName}" );
			if ( !material.HasMetalness )
				Log.Warning( $"No metalness ({CurrentSettings.Metalness}) texture found for {pathWithName}" );
			if ( !material.HasAmbientOcclusion )
				Log.Warning( $"No ambient occlusion ({CurrentSettings.AmbientOcclusion}) texture found for {pathWithName}" );

			if ( !CurrentSettings.ComplainOnMissing || material.HasEverything )
				Log.Info( $"✅ Prepared {pathWithName}.vmat ready for conversion." );
			else
				Log.Info( $"⚠️ Prepared {pathWithName}.vmat ready for conversion with some missing textures." );
		}

		ConvertOption.Enabled = Materials.Count > 0;

		TexturesView?.AddTextures( Textures );
	}

	private void AddFromDirectory( string rootPath, string directory, bool recurseDirectories )
	{
		var files = Directory.EnumerateFiles( directory, "*" );

		foreach ( var fileName in files )
		{
			var extension = Path.GetExtension( fileName ).ToLower();

			if ( extension == ".png" || extension == ".jpg" || extension == ".tga" || extension == ".jpeg" )
			{
				var fileNameWithoutExtension = Path.GetFileNameWithoutExtension( fileName ).ToLower();
				var materialName = RemoveSuffixes( fileNameWithoutExtension );
				var type = GetTextureType( fileNameWithoutExtension );
				if ( type == TextureType.None ) continue;

				var materialPath = Path.Combine( directory, materialName );

				if ( !Materials.TryGetValue( materialPath, out var material ) )
				{
					material = new Material
					{
						Name = materialName,
						Directory = directory,
						RelativePath = Path.GetRelativePath( rootPath, directory )
					};

					Materials[materialPath] = material;
				}

				var relativeFileName = Path.GetRelativePath( rootPath, fileName );

				if ( type == TextureType.Albedo )
					material.Albedo = relativeFileName;
				else if ( type == TextureType.Normal )
					material.Normal = relativeFileName;
				else if ( type == TextureType.Roughness )
					material.Roughness = relativeFileName;
				else if ( type == TextureType.Metalness )
					material.Metalness = relativeFileName;
				else if ( type == TextureType.AmbientOcclusion )
					material.AmbientOcclusion = relativeFileName;

				Textures.Add( fileName );
			}
		}

		if ( recurseDirectories )
		{
			var directories = Directory.EnumerateDirectories( directory );

			foreach ( var childDirectory in directories )
			{
				AddFromDirectory( rootPath, childDirectory, recurseDirectories );
			}
		}
	}

	private string RemoveSuffixes( string fileName )
	{
		fileName = fileName.Replace( CurrentSettings.Albedo.ToLower(), "" );
		fileName = fileName.Replace( CurrentSettings.Normal.ToLower(), "" );
		fileName = fileName.Replace( CurrentSettings.Roughness.ToLower(), "" );
		fileName = fileName.Replace( CurrentSettings.Metalness.ToLower(), "" );
		fileName = fileName.Replace( CurrentSettings.AmbientOcclusion.ToLower(), "" );
		return fileName;
	}

	private TextureType GetTextureType( string fileName )
	{
		if ( fileName.Contains( CurrentSettings.Albedo.ToLower() ) )
		{
			return TextureType.Albedo;
		}

		if ( fileName.Contains( CurrentSettings.Normal.ToLower() ) )
		{
			return TextureType.Normal;
		}

		if ( fileName.Contains( CurrentSettings.Roughness.ToLower() ) )
		{
			return TextureType.Roughness;
		}

		if ( fileName.Contains( CurrentSettings.Metalness.ToLower() ) )
		{
			return TextureType.Metalness;
		}

		if ( fileName.Contains( CurrentSettings.AmbientOcclusion.ToLower() ) )
		{
			return TextureType.AmbientOcclusion;
		}

		return TextureType.None;
	}

	private void CreateUI()
	{
		CreateToolBar();
		BuildMenuBar();

		DockManager.RegisterDockType( "Console", "text_snippet", null, false );

		/*
		TexturesView = new TexturesView( this );
		TexturesView.SetLayout( LayoutMode.TopToBottom );
		*/

		PropertiesView = new SettingsView( this );
		PropertiesView.Target = CurrentSettings;

		var properties = DockManager.DockProperty.HideCloseButton | DockManager.DockProperty.HideOnClose | DockManager.DockProperty.DisallowFloatWindow;

		if ( TexturesView is not null )
		{
			DockManager.AddDock( null, PropertiesView, DockArea.Left, properties, 0.3f );
			DockManager.AddDock( PropertiesView, TexturesView, DockArea.Right, properties, 0.7f );
		}
		else
		{
			DockManager.AddDock( null, PropertiesView, DockArea.Left, properties, 0.4f );
		}

		var console = TypeLibrary.Create( "ConsoleWidget", typeof( Widget ), new[] { this } ) as Widget;

		if ( TexturesView is null )
			DockManager.AddDock( PropertiesView, console, DockArea.Right, properties, 0.6f );
		else
			DockManager.AddDock( TexturesView, console, DockArea.Bottom, properties, 0.25f );

		TexturesView?.AddTextures( Textures );
	}

	[Event.Hotload]
	private void OnHotload()
	{
		RemoveToolBar( ToolBar );

		DockManager.Clear();
		MenuBar.Clear();

		CreateUI();
	}
}
