using System;
using System.Collections.Generic;
using Sgry.Azuki;
using Path = System.IO.Path;
using IHighlighter = Sgry.Azuki.Highlighter.IHighlighter;
using Highlighters = Sgry.Azuki.Highlighter.Highlighters;

namespace Sgry.Ann
{
	class FileType
	{
		#region Fields & Constants
		public const string TextFileTypeName = "Text";
		public const string BatchFileTypeName = "Batch";
		public const string CppFileTypeName = "C/C++";
		public const string CSharpFileTypeName = "C#";
		public const string DiffFileTypeName = "Diff";
		public const string IniFileTypeName = "INI";
		public const string JavaFileTypeName = "Java";
		public const string JavaScriptFileTypeName = "JavaScript";
		public const string LatexFileTypeName = "LaTeX";
		public const string PythonFileTypeName = "Python";
		public const string RubyFileTypeName = "Ruby";
		public const string XmlFileTypeName = "XML";
		readonly static Dictionary<string, FileType> _FileTypeMap
			= new Dictionary<string,FileType>();
		#endregion

		private FileType()
		{}

		static FileType()
		{
			_FileTypeMap.Add( "BatchFileType", BatchFileType );
			_FileTypeMap.Add( "CppFileType", CppFileType );
			_FileTypeMap.Add( "CSharpFileType", CSharpFileType );
			_FileTypeMap.Add( "DiffFileType", DiffFileType );
			_FileTypeMap.Add( "IniFileType", IniFileType );
			_FileTypeMap.Add( "JavaFileType", JavaFileType );
			_FileTypeMap.Add( "JavaScriptFileType", JavaScriptFileType );
			_FileTypeMap.Add( "LatexFileType", LatexFileType );
			_FileTypeMap.Add( "PythonFileType", PythonFileType );
			_FileTypeMap.Add( "RubyFileType", RubyFileType );
			_FileTypeMap.Add( "XmlFileType", XmlFileType );
		}

		#region Factory
		/// <summary>
		/// Gets a new Text file type.
		/// </summary>
		public static FileType TextFileType
		{
			get
			{
				return new FileType() { 
					AutoIndentHook = AutoIndentHooks.GenericHook,
					Name = TextFileTypeName
				};
			}
		}

		/// <summary>
		/// Gets a new batch file type.
		/// </summary>
		public static FileType BatchFileType
		{
			get
			{
				return new FileType() {
					Highlighter = Highlighters.BatchFile,
					AutoIndentHook = AutoIndentHooks.GenericHook,
					Name = BatchFileTypeName
				};
			}
		}

		/// <summary>
		/// Gets a new C/C++ file type.
		/// </summary>
		public static FileType CppFileType
		{
			get
			{
				return new FileType() {
					Highlighter = Highlighters.Cpp,
					AutoIndentHook = AutoIndentHooks.CHook,
					Name = CppFileTypeName
				};
			}
		}

		/// <summary>
		/// Gets a new C# file type.
		/// </summary>
		public static FileType CSharpFileType
		{
			get
			{
				return new FileType() {
					Highlighter = Highlighters.CSharp,
					AutoIndentHook = AutoIndentHooks.CHook,
					Name = CSharpFileTypeName
				};
			}
		}

		/// <summary>
		/// Gets a new Diff / Patch file type.
		/// </summary>
		public static FileType DiffFileType
		{
			get
			{
				return new FileType() {
					Highlighter = Highlighters.Diff,
					Name = DiffFileTypeName
				};
			}
		}

		/// <summary>
		/// Gets a new INI file type.
		/// </summary>
		public static FileType IniFileType
		{
			get
			{
				return new FileType() {
					Highlighter = Highlighters.Ini,
					Name = IniFileTypeName
				};
			}
		}

		/// <summary>
		/// Gets a new Java file type.
		/// </summary>
		public static FileType JavaFileType
		{
			get
			{
				return new FileType() {
					Highlighter = Highlighters.Java,
					AutoIndentHook = AutoIndentHooks.CHook,
					Name = JavaFileTypeName
				};
			}
		}

		/// <summary>
		/// Gets a new JavaScript file type.
		/// </summary>
		public static FileType JavaScriptFileType
		{
			get
			{
				return new FileType() {
					Highlighter = Highlighters.JavaScript,
					AutoIndentHook = AutoIndentHooks.CHook,
					Name = JavaScriptFileTypeName
				};
			}
		}

		/// <summary>
		/// Gets a new LaTeX file type.
		/// </summary>
		public static FileType LatexFileType
		{
			get
			{
				return new FileType() {
					Highlighter = Highlighters.Latex,
					AutoIndentHook = AutoIndentHooks.GenericHook,
					Name = LatexFileTypeName
				};
			}
		}

		/// <summary>
		/// Gets a new Python file type.
		/// </summary>
		public static FileType PythonFileType
		{
			get
			{
				return new FileType() {
					Highlighter = Highlighters.Python,
					AutoIndentHook = AutoIndentHooks.GenericHook,
					Name = PythonFileTypeName
				};
			}
		}

		/// <summary>
		/// Gets a new Ruby file type.
		/// </summary>
		public static FileType RubyFileType
		{
			get
			{
				return new FileType() {
					Highlighter = Highlighters.Ruby,
					AutoIndentHook = AutoIndentHooks.GenericHook,
					Name = RubyFileTypeName
				};
			}
		}

		/// <summary>
		/// Gets a new XML file type.
		/// </summary>
		public static FileType XmlFileType
		{
			get
			{
				return new FileType() {
					Highlighter = Highlighters.Xml,
					AutoIndentHook = AutoIndentHooks.GenericHook,
					Name = XmlFileTypeName
				};
			}
		}

		public static FileType GetFileTypeByFileName( string fileName )
		{
			string fileExt = Path.GetExtension( fileName );
			if( fileExt == String.Empty )
			{
				return TextFileType;
			}

			foreach( string sectionName in _FileTypeMap.Keys )
			{
				string extList = AppConfig.Ini.Get( sectionName,
													"Extensions",
													"" );
				foreach( string ext in extList.Split(' ') )
				{
					if( String.Compare(fileExt, ext, true) == 0 )
						return _FileTypeMap[sectionName];
				}
			}

			return TextFileType;
		}
		#endregion

		#region Properties
		/// <summary>
		/// Gets an associated highlighter object.
		/// </summary>
		public IHighlighter Highlighter
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets key-hook procedure for Azuki's auto-indent associated with this file-type.
		/// </summary>
		public AutoIndentHook AutoIndentHook
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the name of the file mode.
		/// </summary>
		public String Name
		{
			get;
			private set;
		}
		#endregion
	}
}
