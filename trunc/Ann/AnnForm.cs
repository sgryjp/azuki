// 2008-11-03
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Sgry.Azuki;
using Sgry.Azuki.Windows;
using Path = System.IO.Path;
using Debug = System.Diagnostics.Debug;

namespace Sgry.Ann
{
	class AnnForm : Form
	{
		#region Fields
		AppLogic _App;
		Dictionary<MenuItem, AnnAction> _MenuMap = new Dictionary<MenuItem,AnnAction>();
		Dictionary<Keys, AnnAction>	_KeyMap = new Dictionary<Keys, AnnAction>();
		#endregion

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public AnnForm( AppLogic app )
		{
			_App = app;
			InitUIComponent();
			InitMenuComponents();
			InitMenuMap();
			InitKeyMap();
			ResetShortcutInMenu();
#			if !PocketPC
			this._Azuki.UseCtrlTabToMoveFocus = false;
			Font = SystemInformation.MenuFont;
#			endif

			// set shortcut-key hook
			_Azuki.KeyDown += HandleKeyAction;
		}
		#endregion

		#region UI Access
		/// <summary>
		/// Gets Azuki control.
		/// </summary>
		public AzukiControl Azuki
		{
			get{ return _Azuki; }
		}

		/// <summary>
		/// Resets form text.
		/// </summary>
		public
#		if !PocketPC
		override
#		endif
		void ResetText()
		{
			Document doc = _App.ActiveDocument;
			base.Text = String.Format( "Ann - {0} [{1}, {2}]",
				Path.GetFileName(doc.DisplayName),
				doc.Encoding.WebName,
				doc.FileType.Name );
		}
		#endregion

		#region Action Mapping
		void HandleMenuAction( object sender, EventArgs e )
		{
			Debug.Assert( sender is MenuItem );

			MenuItem mi = (MenuItem)sender;
			try
			{
				_MenuMap[ mi ]( _App );
			}
			catch( KeyNotFoundException )
			{}
		}

		void HandleKeyAction( object sender, KeyEventArgs e )
		{
			try
			{
				if( _Azuki.GetKeyBind(e.KeyData) == null )
				{
					_KeyMap[ e.KeyData ]( _App );
				}
			}
			catch( KeyNotFoundException )
			{}
		}

		void InitMenuMap()
		{
			_MenuMap[ _MI_File_Open ]		= Actions.OpenDocument;
			_MenuMap[ _MI_File_Save ]		= Actions.SaveDocument;
			_MenuMap[ _MI_File_SaveAs ]		= Actions.SaveDocumentAs;
			_MenuMap[ _MI_File_Close ]		= Actions.CloseDocument;
			_MenuMap[ _MI_File_Exit ]		= Actions.Exit;

			_MenuMap[ _MI_Edit_Cut ]		= Actions.Cut;
			_MenuMap[ _MI_Edit_Copy ]		= Actions.Copy;
			_MenuMap[ _MI_Edit_Paste ]		= Actions.Paste;
			_MenuMap[ _MI_Edit_SelectAll ]	= Actions.SelectAll;
			
			_MenuMap[ _MI_View_ShowSpecialChar ]	= Actions.SelectSpecialCharVisibility;
			_MenuMap[ _MI_View_WrapLines ]			= Actions.ToggleWrapLines;

			_MenuMap[ _MI_Mode_Text ]		= Actions.SetToTextMode;
			_MenuMap[ _MI_Mode_Cpp ]		= Actions.SetToCppMode;
			_MenuMap[ _MI_Mode_CSharp ]		= Actions.SetToCSharpMode;
			_MenuMap[ _MI_Mode_Java ]		= Actions.SetToJavaMode;
			_MenuMap[ _MI_Mode_Ruby ]		= Actions.SetToRubyMode;
			_MenuMap[ _MI_Mode_XML ]		= Actions.SetToXmlMode;

			_MenuMap[ _MI_Window_Next ]		= Actions.ActivateNextDocument;
			_MenuMap[ _MI_Window_Prev ]		= Actions.ActivatePrevDocument;
			_MenuMap[ _MI_Window_List ]		= Actions.ShowDocumentList;
		}

		void InitKeyMap()
		{
			_KeyMap[ Keys.O|Keys.Control ]	= Actions.OpenDocument;
			_KeyMap[ Keys.S|Keys.Control ]	= Actions.SaveDocument;
			_KeyMap[ Keys.S|Keys.Control|Keys.Shift ]	= Actions.SaveDocumentAs;
			_KeyMap[ Keys.W|Keys.Control ]	= Actions.CloseDocument;
			_KeyMap[ Keys.Q|Keys.Control ]	= Actions.Exit;

			_KeyMap[ Keys.C|Keys.Control ]	= Actions.Cut;
			_KeyMap[ Keys.V|Keys.Control ]	= Actions.Copy;
			_KeyMap[ Keys.P|Keys.Control ]	= Actions.Paste;
			_KeyMap[ Keys.A|Keys.Control ]	= Actions.SelectAll;

			_KeyMap[ Keys.PageDown|Keys.Control ]	= Actions.ActivateNextDocument;
			_KeyMap[ Keys.PageUp|Keys.Control ]		= Actions.ActivatePrevDocument;
			_KeyMap[ Keys.D|Keys.Control ]			= Actions.ShowDocumentList;

			_KeyMap[ Keys.Tab|Keys.Control ]			= Actions.ActivateNextDocument;
			_KeyMap[ Keys.Tab|Keys.Control|Keys.Shift ]	= Actions.ActivatePrevDocument;
		}
		#endregion

		#region UI Component Initialization
		void InitUIComponent()
		{
			this._Azuki = new Sgry.Azuki.Windows.AzukiControl();
			this.SuspendLayout();
			// 
			// _Azuki
			// 
			this._Azuki.Dock = DockStyle.Fill;
			this._Azuki.Location = new System.Drawing.Point( 0, 0 );
			this._Azuki.Name = "_Azuki";
			this._Azuki.TabIndex = 0;
			this._Azuki.TabWidth = 8;
			this._Azuki.ViewWidth = 235;
			// 
			// AnnForm
			// 
			this.ClientSize = new System.Drawing.Size( 360, 400 );
			this.Controls.Add( this._Azuki );
			this.Name = "AnnForm";
			this.Text = "Ann";
			this.ResumeLayout( false );
		}

		void InitMenuComponents()
		{
			// construct menu structure
#			if PocketPC
			MenuItem mi_menu = new MenuItem();
			MenuItem mi_save = new MenuItem();
			mi_save.Text = "Save";
			mi_menu.Text = "Menu";
			_MainMenu.MenuItems.Add( mi_save );
			_MainMenu.MenuItems.Add( mi_menu );

			mi_menu.MenuItems.Add( _MI_File );
			mi_menu.MenuItems.Add( _MI_Edit );
			mi_menu.MenuItems.Add( _MI_View );
			mi_menu.MenuItems.Add( _MI_Mode );
			mi_menu.MenuItems.Add( _MI_Window );
			mi_menu.MenuItems.Add( _MI_Help );
#			else
			_MainMenu.MenuItems.Add( _MI_File );
            _MainMenu.MenuItems.Add( _MI_Edit );
            _MainMenu.MenuItems.Add( _MI_View );
			_MainMenu.MenuItems.Add( _MI_Mode );
			_MainMenu.MenuItems.Add( _MI_Window );
            _MainMenu.MenuItems.Add( _MI_Help );
#			endif

			_MI_File.MenuItems.Add( _MI_File_Open );
			_MI_File.MenuItems.Add( _MI_File_Save );
			_MI_File.MenuItems.Add( _MI_File_SaveAs );
            _MI_File.MenuItems.Add( _MI_File_Close );
            _MI_File.MenuItems.Add( _MI_File_Sep1 );
            _MI_File.MenuItems.Add( _MI_File_Exit );

			_MI_Edit.MenuItems.Add( _MI_Edit_Cut );
            _MI_Edit.MenuItems.Add( _MI_Edit_Copy );
            _MI_Edit.MenuItems.Add( _MI_Edit_Paste );
            _MI_Edit.MenuItems.Add( _MI_Edit_Sep1 );
            _MI_Edit.MenuItems.Add( _MI_Edit_SelectAll );

			_MI_View.MenuItems.Add( _MI_View_ShowSpecialChar );
			_MI_View.MenuItems.Add( _MI_View_WrapLines );

			_MI_Mode.MenuItems.Add( _MI_Mode_Text );
			_MI_Mode.MenuItems.Add( _MI_Mode_Cpp );
			_MI_Mode.MenuItems.Add( _MI_Mode_CSharp );
			_MI_Mode.MenuItems.Add( _MI_Mode_Java );	
			_MI_Mode.MenuItems.Add( _MI_Mode_Ruby );	
			_MI_Mode.MenuItems.Add( _MI_Mode_XML );

			_MI_Window.MenuItems.Add( _MI_Window_Next );
			_MI_Window.MenuItems.Add( _MI_Window_Prev );
			_MI_Window.MenuItems.Add( _MI_Window_List );
			_MI_Window.MenuItems.Add( _MI_Window_Sep1 );

			_MI_Help.MenuItems.Add( _MI_Help_About );

			// menu labels
			_MI_File.Text = "&File";
			_MI_File_Open.Text = "&Open...";
			_MI_File_Save.Text = "&Save";
			_MI_File_SaveAs.Text = "Save &as...";
			_MI_File_Close.Text = "&Close";
			_MI_File_Sep1.Text = "-";
			_MI_File_Exit.Text = "E&xit";
			_MI_Edit.Text = "&Edit";
			_MI_Edit_Cut.Text = "Cu&t";
			_MI_Edit_Copy.Text = "&Copy";
			_MI_Edit_Paste.Text = "&Paste";
			_MI_Edit_Sep1.Text = "-";
			_MI_Edit_SelectAll.Text = "Select A&ll";
			_MI_View.Text = "&View";
			_MI_View_ShowSpecialChar.Text = "Show &Special Chars...";
			_MI_View_WrapLines.Text = "&Wrap lines";
			_MI_Mode.Text = "&Mode";
			_MI_Mode_Text.Text = "&Text";
			_MI_Mode_Cpp.Text = "&C/C++";
			_MI_Mode_CSharp.Text = "C&#";
			_MI_Mode_Java.Text = "&Java";
			_MI_Mode_Ruby.Text = "&Ruby";
			_MI_Mode_XML.Text = "&XML";
			_MI_Window.Text = "&Window";
			_MI_Window_Next.Text = "&Next window";
			_MI_Window_Prev.Text = "&Previous window";
			_MI_Window_List.Text = "List &windows...";
			_MI_Window_Sep1.Text = "-";
			_MI_Help.Text = "&Help";
			_MI_Help_About.Text = "&About";

			// bind menu actions
			EventHandler menuActionHandler = new EventHandler( this.HandleMenuAction );
			foreach( MenuItem mi in _MainMenu.MenuItems )
			{
				foreach( MenuItem mi2 in mi.MenuItems )
				{
					foreach( MenuItem mi3 in mi2.MenuItems )
					{
						mi3.Click += menuActionHandler;
					}
					mi2.Click += menuActionHandler;
				}
			}

			// set menu
			Menu = _MainMenu;
		}

		void ResetShortcutInMenu()
		{
			string newText;
			MenuItem mi;

			// find matched pair that both pair has same action from two dictionary
			foreach( KeyValuePair<Keys, AnnAction> keyEntry in _KeyMap )
			{
				foreach( KeyValuePair<MenuItem, AnnAction> menuEntry in _MenuMap )
				{
					// has same action?
					if( keyEntry.Value != menuEntry.Value )
					{
						continue;
					}

					// ok, a pair was found.
					// reset menu text with found shortcut-key.
					mi = menuEntry.Key;
					int tabPos = mi.Text.IndexOf( '\t' );
					if( tabPos == -1 )
					{
						newText = mi.Text + "\t" + Utl.ToString( keyEntry.Key );
					}
					else
					{
						newText = mi.Text.Substring(0, tabPos) + "\t" + Utl.ToString( keyEntry.Key );
					}
					mi.Text = newText;

					break;
				}
			}
		}
		#endregion

		#region UI Components
		MainMenu _MainMenu			= new MainMenu();
		MenuItem _MI_File			= new MenuItem();
		MenuItem _MI_File_Open		= new MenuItem();
		MenuItem _MI_File_Save		= new MenuItem();
		MenuItem _MI_File_SaveAs	= new MenuItem();
		MenuItem _MI_File_Close		= new MenuItem();
		MenuItem _MI_File_Sep1		= new MenuItem();
		MenuItem _MI_File_Exit		= new MenuItem();
		MenuItem _MI_Edit			= new MenuItem();
		MenuItem _MI_Edit_Cut		= new MenuItem();
		MenuItem _MI_Edit_Copy		= new MenuItem();
		MenuItem _MI_Edit_Paste		= new MenuItem();
		MenuItem _MI_Edit_Sep1		= new MenuItem();
		MenuItem _MI_Edit_SelectAll	= new MenuItem();
		MenuItem _MI_View					= new MenuItem();
		MenuItem _MI_View_ShowSpecialChar	= new MenuItem();
		MenuItem _MI_View_WrapLines			= new MenuItem();
		MenuItem _MI_Mode			= new MenuItem();
		MenuItem _MI_Mode_Text		= new MenuItem();
		MenuItem _MI_Mode_Cpp		= new MenuItem();
		MenuItem _MI_Mode_CSharp	= new MenuItem();
		MenuItem _MI_Mode_Java		= new MenuItem();
		MenuItem _MI_Mode_Ruby		= new MenuItem();
		MenuItem _MI_Mode_XML		= new MenuItem();
		MenuItem _MI_Window			= new MenuItem();
		MenuItem _MI_Window_Next	= new MenuItem();
		MenuItem _MI_Window_Prev	= new MenuItem();
		MenuItem _MI_Window_List	= new MenuItem();
		MenuItem _MI_Window_Sep1	= new MenuItem();
		MenuItem _MI_Help			= new MenuItem();
		MenuItem _MI_Help_About		= new MenuItem();
		AzukiControl _Azuki;
		#endregion

		#region Utilities
		static class Utl
		{
			public static string ToString( Keys keyData )
			{
				StringBuilder text = new StringBuilder();

				if( (keyData & Keys.Control) != 0 )
				{
					text.Append( "Ctrl+" );
				}
				if( (keyData & Keys.Alt) != 0 )
				{
					text.Append( "Alt+" );
				}
				if( (keyData & Keys.Shift) != 0 )
				{
					text.Append( "Shift+" );
				}
				text.Append( keyData & (~Keys.Modifiers) );

				return text.ToString();
			}
		}
		#endregion
	}
}
