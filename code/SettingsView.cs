using Editor;
using System;

namespace Facepunch.Tools;

public class SettingsView : Widget
{
	public object Target
	{
		get => Sheet.Target;
		set => Sheet.Target = value;
	}

	private readonly PropertySheet Sheet;

	public Action PropertyUpdated { get; set; }

	public SettingsView( Widget parent ) : base( parent )
	{
		WindowTitle = "Settings";
		Name = "Settings";

		SetWindowIcon( "settings" );
		SetLayout( LayoutMode.TopToBottom );

		Sheet = new PropertySheet( this );
		Sheet.PropertyUpdated += () => PropertyUpdated?.Invoke();

		var scroller = new ScrollArea( this )
		{
			Canvas = Sheet
		};

		Layout.Add( scroller );
	}
}
