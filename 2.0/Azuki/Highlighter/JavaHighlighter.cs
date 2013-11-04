namespace Sgry.Azuki.Highlighter
{
	class JavaHighlighter : KeywordHighlighter
	{
		public JavaHighlighter()
		{
			AddKeywordSet( new[] {
				"abstract", "assert", "boolean", "break", "byte",
				"case", "catch", "char", "class", "const", "continue",
				"default", "do", "double", "else", "enum", "extends",
				"final", "finally", "float", "for", "goto", "if",
				"implements", "import", "instanceof", "int", "interface",
				"long", "native", "new", "package", "private", "protected",
				"public", "return", "short", "static", "strictfp", "super",
				"switch", "synchronized", "this", "throw", "throws", "transient",
				"try", "void", "volatile", "while"
			}, CharClass.Keyword );

			AddEnclosure( "'", "'", CharClass.String, false, '\\' );
			AddEnclosure( "\"", "\"", CharClass.String, false, '\\' );
			AddEnclosure( "/**", "*/", CharClass.DocComment, true );
			AddEnclosure( "/*", "*/", CharClass.Comment );
			AddLineHighlight( "//", CharClass.Comment );
		}
	}
}
