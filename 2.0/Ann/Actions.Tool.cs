namespace Sgry.Ann
{
	static partial class Actions
	{
		/// <summary>
		/// Set editing mode to Text mode.
		/// </summary>
		public static void SetToTextMode( AppLogic app )
		{
			app.SetFileType( app.ActiveDocument, FileType.TextFileType );
		}

		/// <summary>
		/// Set editing mode to LaTeX mode.
		/// </summary>
		public static void SetToLatexMode( AppLogic app )
		{
			app.SetFileType( app.ActiveDocument, FileType.LatexFileType );
		}

		/// <summary>
		/// Set editing mode to batch file mode.
		/// </summary>
		public static void SetToBatchFileMode( AppLogic app )
		{
			app.SetFileType( app.ActiveDocument, FileType.BatchFileType );
		}

		/// <summary>
		/// Set editing mode to C/C++ mode.
		/// </summary>
		public static void SetToCppMode( AppLogic app )
		{
			app.SetFileType( app.ActiveDocument, FileType.CppFileType );
		}

		/// <summary>
		/// Set editing mode to C# mode.
		/// </summary>
		public static void SetToCSharpMode( AppLogic app )
		{
			app.SetFileType( app.ActiveDocument, FileType.CSharpFileType );
		}

		/// <summary>
		/// Set editing mode to Diff/Patch file mode.
		/// </summary>
		public static void SetToDiffMode( AppLogic app )
		{
			app.SetFileType( app.ActiveDocument, FileType.DiffFileType );
		}

		/// <summary>
		/// Set editing mode to Java mode.
		/// </summary>
		public static void SetToJavaMode( AppLogic app )
		{
			app.SetFileType( app.ActiveDocument, FileType.JavaFileType );
		}

		/// <summary>
		/// Set editing mode to Python mode.
		/// </summary>
		public static void SetToPythonMode( AppLogic app )
		{
			app.SetFileType( app.ActiveDocument, FileType.PythonFileType );
		}

		/// <summary>
		/// Set editing mode to Ruby mode.
		/// </summary>
		public static void SetToRubyMode( AppLogic app )
		{
			app.SetFileType( app.ActiveDocument, FileType.RubyFileType );
		}

		/// <summary>
		/// Set editing mode to JavaScript mode.
		/// </summary>
		public static void SetToJavaScriptMode( AppLogic app )
		{
			app.SetFileType( app.ActiveDocument, FileType.JavaScriptFileType );
		}

		/// <summary>
		/// Set editing mode to Ini mode.
		/// </summary>
		public static void SetToIniMode( AppLogic app )
		{
			app.SetFileType( app.ActiveDocument, FileType.IniFileType );
		}

		/// <summary>
		/// Set editing mode to XML mode.
		/// </summary>
		public static void SetToXmlMode( AppLogic app )
		{
			app.SetFileType( app.ActiveDocument, FileType.XmlFileType );
		}
	}
}
