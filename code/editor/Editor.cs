using System;
using System.Collections.Generic;
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;

namespace Starfall
{
	[Library]
	public partial class FileTree : Panel
	{
		public FileTree()
		{
			AddClass( "filetree" );
		}
	}


	[Library]
	public class Editor : Panel
	{
		[ClientCmd("sf_openeditor", Help = "Opens the starfall editor")]
		public static void OpenEditor()
		{
			if( Instance is null )
			{
				Instance = new Editor();
			}
			Instance.SetClass( "editoropen", true );
		}

		public static Editor Instance;
		public Editor()
		{
			Instance = this;

			var filebrowser = Add.Panel( "filebrowser" );
			{
				var body = filebrowser.Add.Panel( "body" );
				{
					var tree = body.AddChild<FileTree>();
				}
			}

			var editwindow = Add.Panel( "editwindow" );
			{
				var tabs = editwindow.Add.Panel( "tabs" );
				{
					tabs.Add.Button( "Tools" ).AddClass( "active" );
					tabs.Add.Button( "Utility" );
				}
				var body = editwindow.Add.Panel( "body" );
				{
					var editing = body.Add.TextEntry( "fileedit" );
					{
					}
				}
			}
		}
	}
}
