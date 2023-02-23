using Editor;
using System;
using System.Collections.Generic;

namespace Facepunch.Tools;

public class TexturesView : Widget
{
	private Widget Canvas { get; set; }

	public class TextureWidget : Widget
	{
		public string Texture { get; set; }

		public TextureWidget( Widget parent ) : base( parent )
		{
			Size = new Vector2( 128f, 128f );
		}

		protected override void OnPaint()
		{
			Paint.Draw( LocalRect, Texture );
			base.OnPaint();
		}
	}

	public TexturesView( Widget parent ) : base( parent )
	{
		WindowTitle = "Textures";
		Name = "Textures";

		SetWindowIcon( "texture" );
		SetLayout( LayoutMode.TopToBottom );

		Canvas = new Widget( this );
		Canvas.SetLayout( LayoutMode.RightToLeft );

		var scroller = new ScrollArea( this )
		{
			Canvas = Canvas
		};

		Layout.Add( scroller );
	}

	public void AddTextures( List<string> textures )
	{
		Canvas.DestroyChildren();

		foreach ( var texture in textures )
		{
			var w = new TextureWidget( Canvas );
			w.Texture = texture;
			Canvas.Layout.Add( w );
		}
	}
}
