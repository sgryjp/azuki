using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using Sgry.Azuki;
using Encoding = System.Text.Encoding;

namespace Sgry.Ann
{
	static class AppConfig
	{
		static string _IniFilePath;

		public static FontInfo FontInfo { get; set; }
		public static Size WindowSize { get; set; }
		public static bool WindowMaximized { get; set; }
		public static bool TabPanelEnabled { get; set; }
		public static bool DrawsEolCode { get; set; }
		public static bool DrawsFullWidthSpace { get; set; }
		public static bool DrawsSpace { get; set; }
		public static bool DrawsTab { get; set; }
		public static bool DrawsEofMark { get; set; }
		public static bool HighlightsCurrentLine { get; set; }
		public static bool HighlightsMatchedBracket { get; set; }
		public static bool ShowsLineNumber { get; set; }
		public static bool ShowsHRuler { get; set; }
		public static bool ShowsDirtBar { get; set; }
		public static int TabWidth { get; set; }
		public static int LinePadding { get; set; }
		public static int LeftMargin { get; set; }
		public static int TopMargin { get; set; }
		public static ViewType ViewType { get; set; }
		public static bool UsesTabForIndent { get; set; }
		public static bool ConvertsFullWidthSpaceToSpace { get; set; }
		public static HRulerIndicatorType HRulerIndicatorType { get; set; }
		public static bool ScrollsBeyondLastLine { get; set; }
		public static bool CopyLineWhenNoSelection { get; set; }
		public static int AutoScrollMargin { get; set; }
		public static MruFileList MruFiles { get; private set; }
		public static Ini Ini { get; private set; }

		static AppConfig()
		{
			MruFiles = new MruFileList();
			Ini = new Ini();
		}

		static void Reset()
		{
			FontInfo = new FontInfo( "Courier New", 11, FontStyle.Regular );
			WindowSize = new Size( 360, 400 );
			WindowMaximized = false;
			TabPanelEnabled = false;
			DrawsEolCode = true;
			DrawsFullWidthSpace = true;
			DrawsSpace = true;
			DrawsTab = true;
			DrawsEofMark = false;
			HighlightsCurrentLine = true;
			HighlightsMatchedBracket = true;
			ShowsLineNumber = true;
			ShowsHRuler = false;
			ShowsDirtBar = true;
			TabWidth = 8;
			LinePadding = 1;
			LeftMargin = 1;
			TopMargin = 1;
			ViewType = ViewType.Proportional;
			UsesTabForIndent = true;
			ConvertsFullWidthSpaceToSpace = false;
			HRulerIndicatorType = HRulerIndicatorType.Segment;
			ScrollsBeyondLastLine = true;
			CopyLineWhenNoSelection = true;
			AutoScrollMargin = 1;
			MruFiles.Clear();
			Ini.Clear();
		}

		/// <summary>
		/// Loads application config file.
		/// </summary>
		public static void Load()
		{
			try
			{
				Reset();
				Ini.Load( IniFilePath, Encoding.UTF8 );

				int fontSize = Ini.GetInt( "Default", "FontSize", 1, Int32.MaxValue, FontInfo.Size );
				string fontName = Ini.Get( "Default", "Font", FontInfo.Name );
				int width = Ini.GetInt( "Default", "WindowWidth", 100, Int32.MaxValue, WindowSize.Width );
				int height = Ini.GetInt( "Default", "WindowHeight", 100, Int32.MaxValue, WindowSize.Height );

				FontInfo					= new FontInfo( fontName, fontSize, FontStyle.Regular );
				FontInfo.Antialias			= Ini.Get( "Default", "Antialias", FontInfo.Antialias );
				WindowSize					= new Size( width, height );
				WindowMaximized				= Ini.Get( "Default", "WindowMaximized", WindowMaximized );
				TabPanelEnabled				= Ini.Get( "Default", "TabPanelEnabled", TabPanelEnabled );
				DrawsEolCode				= Ini.Get( "Default", "DrawsEolCode", DrawsEolCode );
				DrawsFullWidthSpace			= Ini.Get( "Default", "DrawsFullWidthSpace", DrawsFullWidthSpace );
				DrawsSpace					= Ini.Get( "Default", "DrawsSpace", DrawsSpace );
				DrawsTab					= Ini.Get( "Default", "DrawsTab", DrawsTab );
				DrawsEofMark				= Ini.Get( "Default", "DrawsEofMark", DrawsEofMark );
				HighlightsCurrentLine		= Ini.Get( "Default", "HighlightsCurrentLine", HighlightsCurrentLine );
				HighlightsMatchedBracket	= Ini.Get( "Default", "HighlightsMatchedBracket", HighlightsMatchedBracket );
				ShowsLineNumber				= Ini.Get( "Default", "ShowsLineNumber", ShowsLineNumber );
				ShowsHRuler					= Ini.Get( "Default", "ShowsHRuler", ShowsHRuler );
				ShowsDirtBar				= Ini.Get( "Default", "ShowsDirtBar", ShowsDirtBar );
				TabWidth					= Ini.GetInt( "Default", "TabWidth", 0, 100, TabWidth );
				LinePadding					= Ini.GetInt( "Default", "LinePadding", 1, 100, LinePadding );
				LeftMargin					= Ini.GetInt( "Default", "LeftMargin", 0, 100, LeftMargin );
				TopMargin					= Ini.GetInt( "Default", "TopMargin", 0, 100, TopMargin );
				ViewType					= Ini.Get( "Default", "ViewType", ViewType );
				UsesTabForIndent			= Ini.Get( "Default", "UsesTabForIndent", UsesTabForIndent );
				ConvertsFullWidthSpaceToSpace = Ini.Get( "Default", "ConvertsFullWidthSpaceToSpace", ConvertsFullWidthSpaceToSpace );
				HRulerIndicatorType			= Ini.Get( "Default", "HRulerIndicatorType", HRulerIndicatorType );
				ScrollsBeyondLastLine		= Ini.Get( "Default", "ScrollsBeyondLastLine", ScrollsBeyondLastLine );
				CopyLineWhenNoSelection		= Ini.Get( "Default", "CopyLineWhenNoSelection", CopyLineWhenNoSelection );
				AutoScrollMargin			= Ini.Get( "Default", "AutoScrollMargin", AutoScrollMargin );
				MruFiles.Load( Ini.Get("Default", "Mru", "") );
			}
			catch
			{}
		}

		/// <summary>
		/// Saves application configuration.
		/// </summary>
		public static void Save()
		{
			try
			{
				Ini.Set( "Default", "FontSize",						FontInfo.Size );
				Ini.Set( "Default", "Font",							FontInfo.Name );
				Ini.Set( "Default", "Antialias",					FontInfo.Antialias );
				Ini.Set( "Default", "WindowWidth",					WindowSize.Width );
				Ini.Set( "Default", "WindowHeight",					WindowSize.Height );
				Ini.Set( "Default", "WindowMaximized",				WindowMaximized );
				Ini.Set( "Default", "TabPanelEnabled",				TabPanelEnabled );
				Ini.Set( "Default", "DrawsEolCode",					DrawsEolCode );
				Ini.Set( "Default", "DrawsFullWidthSpace",			DrawsFullWidthSpace );
				Ini.Set( "Default", "DrawsSpace",					DrawsSpace );
				Ini.Set( "Default", "DrawsTab",						DrawsTab );
				Ini.Set( "Default", "DrawsEofMark",					DrawsEofMark );
				Ini.Set( "Default", "HighlightsCurrentLine",		HighlightsCurrentLine );
				Ini.Set( "Default", "HighlightsMatchedBracket",		HighlightsMatchedBracket );
				Ini.Set( "Default", "ShowsLineNumber",				ShowsLineNumber );
				Ini.Set( "Default", "ShowsHRuler",					ShowsHRuler );
				Ini.Set( "Default", "ShowsDirtBar",					ShowsDirtBar );
				Ini.Set( "Default", "TabWidth",						TabWidth );
				Ini.Set( "Default", "LinePadding",					LinePadding );
				Ini.Set( "Default", "LeftMargin",					LeftMargin );
				Ini.Set( "Default", "TopMargin",					TopMargin );
				Ini.Set( "Default", "ViewType",						ViewType );
				Ini.Set( "Default", "UsesTabForIndent",				UsesTabForIndent );
				Ini.Set( "Default", "ConvertsFullWidthSpaceToSpace",ConvertsFullWidthSpaceToSpace );
				Ini.Set( "Default", "HRulerIndicatorType",			HRulerIndicatorType );
				Ini.Set( "Default", "ScrollsBeyondLastLine",		ScrollsBeyondLastLine );
				Ini.Set( "Default", "CopyLineWhenNoSelection",		CopyLineWhenNoSelection );
				Ini.Set( "Default", "AutoScrollMargin",				AutoScrollMargin );
				Ini.Set( "Default", "Mru",							MruFiles.ToString() );

				Ini.Save( IniFilePath, Encoding.UTF8, "\r\n" );
			}
			catch
			{}
		}

		#region Utilities
		/// <summary>
		/// Gets path of the configuration file.
		/// </summary>
		public static string IniFilePath
		{
			get
			{
				if( _IniFilePath == null )
				{
					Assembly exe = Assembly.GetExecutingAssembly();
					string exePath = exe.GetModules()[0].FullyQualifiedName;
					string exeDirPath = Path.GetDirectoryName( exePath );
					_IniFilePath = Path.Combine( exeDirPath, "Ann.ini" );
				}
				return _IniFilePath;
			}
		}
		#endregion
	}
}
