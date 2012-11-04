// This source file was written by Suguru YAMAMOTO as part of Azuki library.
using System;
using System.Collections.Generic;
using System.Text;
using Spart.Scanners;

namespace Spart.Parsers.Primitives
{
	/// <summary>
	/// Matches one of the a sorted string list.
	/// Significantly faster than combining Prims.Str with Ops.Choice.
	/// </summary>
	class KeywordParser : TerminalParser
	{
		string[]	_Keywords;
		int			_MaxLength;
		string		_ProhibitedFollowingChars;

		public KeywordParser( string prohibitedFollowingChars,
							  params string[] keywords )
		{
			_ProhibitedFollowingChars = prohibitedFollowingChars;
			_Keywords = keywords;
			_MaxLength = 0;
			foreach( string keyword in keywords )
			{
				if( _MaxLength < keyword.Length )
				{
					_MaxLength = keyword.Length;
				}
			}
		}

		protected override ParserMatch ParseMain( IScanner scanner )
		{
			ParserMatch match;
			long orgOffset = scanner.Offset;
			StringBuilder buf = new StringBuilder( 32 );
			string text;

			// Get enough number of characters to match against keywords
			long offset = orgOffset;
			for( int i=0; i<_MaxLength && !scanner.AtEnd; i++ )
			{
				buf.Append( scanner.Peek() );
				scanner.Seek( ++offset );
			}
			text = buf.ToString();

			// Check whether one of the keywords starts from the parsing point
			int index = Array.BinarySearch( _Keywords,
										    text,
											new SimpleComparer() );
			if( index < 0 )
			{
				index = (~index) - 1;
				if( index < 0
					|| text.StartsWith(_Keywords[index]) == false )
				{
					match = ParserMatch.CreateFailureMatch( scanner,
															orgOffset );
					scanner.Seek( orgOffset );
					return match;
				}
			}
			string keyword = _Keywords[ index ];

			// It's not a keyword if a following character is not a delimiter
			scanner.Seek( orgOffset + keyword.Length );
			if( 0 <= _ProhibitedFollowingChars.IndexOf(scanner.Peek()) )
			{
				match = ParserMatch.CreateFailureMatch( scanner,
														orgOffset );
				scanner.Seek( orgOffset );
				return match;
			}

			// if we arrive at this point, we have a match
			match = ParserMatch.CreateSuccessfulMatch( scanner,
													   orgOffset,
													   keyword.Length );
			scanner.Seek( orgOffset + keyword.Length );

			return match;
		}

		class SimpleComparer : IComparer<string>
		{
			public int Compare( string x, string y )
			{
				int diff = 0;
				for( int i=0; i<x.Length && i<y.Length; i++ )
				{
					diff += x[i] - y[i];
					if( diff != 0 )
						return diff;
				}
				if( x.Length < y.Length )
					diff = -1;
				else if( x.Length > y.Length )
					diff = 1;
				
				return diff;
			}
		}
	}
}
