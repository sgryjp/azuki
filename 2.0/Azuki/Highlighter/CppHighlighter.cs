using System.Collections.Generic;
using Sgry.Azuki.Utils;

namespace Sgry.Azuki.Highlighter
{
	/// <summary>
	/// Highlighter for C/C++ language based on keyword matching.
	/// </summary>
	class CppHighlighter : KeywordHighlighter
	{
		static readonly List<string> MacroKeywords = new List<string>() {
				"define", "elif", "else", "endif", "error",
				"if", "ifdef", "ifndef", "import", "include",
				"line", "pragma", "undef"
			};

		public CppHighlighter()
		{
			AddKeywordSet( new[] {
				"asm", "auto", "bool", "break", "case", "catch", "char",
				"class", "const", "const_cast", "continue", "default",
				"delete", "do", "double", "dynamic_cast", "else",
				"enum", "explicit", "export", "extern", "false",
				"float", "for", "friend", "goto", "if", "inline",
				"int", "long", "mutable", "namespace", "new",
				"operator", "private", "protected", "public", "register",
				"reinterpret_cast", "return", "short",
				"signed", "sizeof", "static", "static_cast",
				"struct", "switch", "template", "this", "throw", "true", "try",
				"typedef", "typeid", "typename", "union", "unsigned", "using",
				"virtual", "void", "volatile", "while"
			}, CharClass.Keyword );

			AddKeywordSet( new[] {
				"__FILE__", "__LINE__", "NULL", "offsetof", "ptrdiff_t", "size_t",
				"u_char", "u_int", "u_long", "u_short", "wchar_t"
			}, CharClass.Keyword2 );

			AddKeywordSet( new[] {
				"BOOL", "BYTE", "DWORD", "DWORD_PTR", "FALSE", "HANDLE", "HRESULT", "HWND",
				"INT_PTR", "LONG_PTR", "LPARAM", "LRESULT", "NULL", "TCHAR", "TRUE",
				"WORD", "WPARAM"
			}, CharClass.Keyword3 );

			AddEnclosure( "'", "'", CharClass.String, false, '\\' );
			AddEnclosure( "\"", "\"", CharClass.String, false, '\\' );
			AddEnclosure( "/*", "*/", CharClass.Comment, true );
			AddLineHighlight( "//", CharClass.Comment );

			HookProc = HighlightPreprocessorMacro;
		}

		bool HighlightPreprocessorMacro( Document doc, string token, int index, CharClass klass )
		{
			// If one previous character is not a space or '#', ignore it
			if( index <= 0 || !doc[index-1].IsOneOf("# \t") )
			{
				return false;
			}

			// If this token is not a macro keyword, ignore it
			if( MacroKeywords.BinarySearch(token) < 0 )
			{
				return false;
			}

			// A suspicious token found.
			// Search for '#' for 32 characters back, and highlight it if found
			for( int i=index-1; 0<=i && index-32<=i; --i )
			{
				if( doc[i] == '#' )
				{
					// Found a sharp so this is a preprocessor macro.
					// Now highlight from the sharp to the end of the keyword
					for( int j=i; j<index+token.Length; j++ )
					{
						doc.SetCharClass( j, CharClass.Macro );
					}
					return true;
				}
				else if( doc[i] != ' ' && doc[i] != '\t' )
				{
					// No sharp characters nor spaces found so this token is not one
					break;
				}
			}

			return false;
		}
	}
}
