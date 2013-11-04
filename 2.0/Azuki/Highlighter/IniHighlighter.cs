namespace Sgry.Azuki.Highlighter
{
	class IniHighlighter : KeywordHighlighter
	{
		public IniHighlighter()
		{
			AddRegex( @"^\s*(\[[^\]]+\])",
					  false,
					  new[]{ CharClass.Heading1 } );
			AddRegex( @"^\s*([^=]+)\s*[=:]",
					  false,
					  new[]{ CharClass.Property } );
			AddRegex( @"^\s*([;#!].*)",
					  false,
					  new[]{ CharClass.Comment } );
			HighlightsNumericLiterals = false;
		}
	}
}
