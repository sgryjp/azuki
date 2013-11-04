namespace Sgry.Azuki.Highlighter
{
	class PythonHighlighter : KeywordHighlighter
	{
		public PythonHighlighter()
		{
			AddKeywordSet( new[] {
				"and", "as", "assert", "break", "continue", "del", "elif",
				"else", "except", "finally", "for",
				"from", "global", "if", "import", "in", "lambda",
				"nonlocal", "not", "or", "pass", "raise", "return",
				"try", "while", "with" },
				CharClass.Keyword );

			AddKeywordSet( new[] { "False", "None", "True" },
						   CharClass.Keyword2 );

			AddLineHighlight( "#", CharClass.Comment );

			AddRegex( @"(class)\s+(\w+)\(",
					  new[]{CharClass.Keyword, CharClass.Class} );

			AddRegex( @"(def)\s+(\w+)\(",
					  new[]{CharClass.Keyword, CharClass.Function} );

			AddEnclosure( "r\"\"\"", "\"\"\"", CharClass.String, true );
			AddEnclosure( "R\"\"\"", "\"\"\"", CharClass.String, true );
			AddEnclosure( "\"\"\"", "\"\"\"", CharClass.String, true );

			AddEnclosure( "r'''", "'''", CharClass.String, true );
			AddEnclosure( "R'''", "'''", CharClass.String, true );
			AddEnclosure( "'''", "'''", CharClass.String, true );

			AddEnclosure( "r\"", "\"", CharClass.String, false, '\\' );
			AddEnclosure( "R\"", "\"", CharClass.String, false, '\\' );
			AddEnclosure( "\"", "\"", CharClass.String, false, '\\' );

			AddEnclosure( "r'", "'", CharClass.String, false, '\\' );
			AddEnclosure( "R'", "'", CharClass.String, false, '\\' );
			AddEnclosure( "'", "'", CharClass.String, false, '\\' );
		}
	}
}
