namespace Sgry.Azuki.Highlighter
{
	/// <summary>
	/// Highlighter for Ruby language based on keyword matching.
	/// </summary>
	class RubyHighlighter : KeywordHighlighter
	{
		public RubyHighlighter()
		{
			AddKeywordSet( new[] {
				"alias", "and", "begin", "BEGIN", "break", "case", "class",
				"def", "defined", "do", "else", "elsif", "end", "END", "ensure",
				"false", "for", "if", "in", "module", "next", "nil", "not",
				"or", "redo", "rescue", "retry", "return", "self", "super",
				"then", "true", "undef", "unless", "until", "when", "while", "yield"
			}, CharClass.Keyword );

			AddEnclosure( "'", "'", CharClass.String, '\\' );
			AddEnclosure( "\"", "\"", CharClass.String, '\\' );
			AddEnclosure( "=begin", "=end", CharClass.DocComment, '\0' );
			AddLineHighlight( "#", CharClass.Comment );
		}
	}
}
