using System;
using System.Collections.Generic;
using Debug = System.Diagnostics.Debug;

namespace Sgry.Azuki.Highlighter
{
	/// <summary>
	/// A highlighter to highlight XML.
	/// </summary>
	class XmlHighlighter : IHighlighter
	{
		#region Fields
		static readonly string DefaultWordCharSet = null;
		readonly List<Enclosure> _Enclosures = new List<Enclosure>();

		readonly GapBuffer<int> _ReparsePoints = new GapBuffer<int>( 64 );
		int _LastDocumentHash;
		#endregion

		#region Properties
		/// <summary>
		/// Gets or sets whether a highlighter hook procedure can be installed or not.
		/// </summary>
		public bool CanUseHook
		{
			get{ return false; }
		}

		/// <summary>
		/// Gets or sets highlighter hook procedure.
		/// </summary>
		/// <exception cref="NotSupportedException">
		///   This highlighter does not support hook procedure.
		/// </exception>
		public HighlightHook HookProc
		{
			get{ throw new NotSupportedException(); }
			set{ throw new NotSupportedException(); }
		}
		#endregion

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public XmlHighlighter()
		{
			const bool MULTILINE = true;
			const bool CASE_SENSITIVE = false;

			Enclosure doubleQuote = new Enclosure( "\"",
												   "\"",
												   CharClass.String,
												   '\0',
												   MULTILINE,
												   CASE_SENSITIVE );
			_Enclosures.Add( doubleQuote );

			Enclosure singleQuote = new Enclosure( "'",
												   "'",
												   CharClass.String,
												   '\0',
												   MULTILINE,
												   CASE_SENSITIVE );
			_Enclosures.Add( singleQuote );

			Enclosure cdata = new Enclosure( "<![CDATA[",
											 "]]>",
											 CharClass.CDataSection,
											 '\0',
											 MULTILINE,
											 CASE_SENSITIVE );
			_Enclosures.Add( cdata );

			Enclosure comment = new Enclosure( "<!--",
											   "-->",
											   CharClass.Comment,
											   '\0',
											   MULTILINE,
											   CASE_SENSITIVE );
			_Enclosures.Add( comment );
		}
		#endregion

		#region Highlighting Logic
		public IRange Highlight( IRange dirtyRange )
		{
			var doc = dirtyRange.Document;
			if( dirtyRange.Begin < 0 || doc.Length < dirtyRange.Begin )
				throw new ArgumentOutOfRangeException( "dirtyRange", "Begin of 'dirtyRange' is out"
																	 + " of valid range." );
			if( dirtyRange.End < 0 || doc.Length < dirtyRange.End )
				throw new ArgumentOutOfRangeException( "dirtyRange", "End of 'dirtyRange' is out"
																	 + " of valid range." );

			// Refresh cache
			if( _LastDocumentHash != doc.GetHashCode() )
			{
				_ReparsePoints.Clear();
				_LastDocumentHash = doc.GetHashCode();
			}

			// Determine range to highlight
			dirtyRange.Begin = Utl.FindReparsePoint( _ReparsePoints, dirtyRange.Begin );
			dirtyRange.End = Utl.FindReparseEndPoint( doc, dirtyRange.End );

			// seek each tags
			var index = 0;
			while( 0 <= index && index < dirtyRange.End )
			{
				int nextIndex;
				if( Utl.TryHighlight(doc, _Enclosures, index, dirtyRange.End, null, out nextIndex) )
				{
					Utl.EntryReparsePoint( _ReparsePoints, index );
					index = nextIndex;
				}
				else if( doc[index] == '<' )
				{
					Utl.EntryReparsePoint( _ReparsePoints, index );

					// set class for '<'
					doc.SetCharClass( index, CharClass.Delimiter );
					index++;
					if( dirtyRange.End <= index )
					{
						return dirtyRange;
					}

					// if next char is '?' or '/', highlight it too
					var nextCh = doc[ index ];
					if( nextCh == '?' || nextCh == '/' || nextCh == '!' )
					{
						doc.SetCharClass( index, CharClass.Delimiter );
						index++;
						if( dirtyRange.End <= index )
							return dirtyRange;
					}

					// skip whitespaces
					while( Char.IsWhiteSpace(doc[index]) )
					{
						doc.SetCharClass( index, CharClass.Normal );
						index++;
						if( dirtyRange.End <= index )
							return dirtyRange;
					}

					// highlight element name
					nextIndex = Utl.FindNextToken( doc, index, DefaultWordCharSet );
					for( int i=index; i<nextIndex; i++ )
					{
						doc.SetCharClass( i, CharClass.ElementName );
					}
					index = nextIndex;

					// highlight attributes
					while( index < dirtyRange.End && doc[index] != '>' )
					{
						// highlight enclosing part if this token begins a part
						if( Utl.TryHighlight(doc, _Enclosures, index, dirtyRange.End, null, out nextIndex) )
						{
							// successfully highlighted. skip to next.
							index = nextIndex;
							continue;
						}

						// this token is normal class; reset classes and seek to next token
						nextIndex = Utl.FindNextToken( doc, index, DefaultWordCharSet );
						for( int i=index; i<nextIndex; i++ )
						{
							doc.SetCharClass( i, CharClass.Attribute );
						}
						index = nextIndex;
					}

					// highlight '>'
					if( index < dirtyRange.End )
					{
						doc.SetCharClass( index, CharClass.Delimiter );
						if( 1 <= index && doc[index-1] == '/' )
							doc.SetCharClass( index-1, CharClass.Delimiter );
						index++;
					}
				}
				else if( doc[index] == '&' )
				{
					int seekEndIndex;
					bool wasEntity;

					// find end position of this token
					FindEntityEnd( doc, index, out seekEndIndex, out wasEntity );
					Debug.Assert( 0 <= seekEndIndex && seekEndIndex <= doc.Length );

					// highlight this token
					var klass = wasEntity ? CharClass.Entity : CharClass.Normal;
					for( int i=index; i<seekEndIndex; i++ )
					{
						doc.SetCharClass( i, klass );
					}
					index = seekEndIndex;
				}
				else
				{
					// normal character.
					doc.SetCharClass( index, CharClass.Normal );
					index++;
				}
			}

			return dirtyRange;
		}

		static void FindEntityEnd( Document doc, int startIndex,
								   out int endIndex, out bool wasEntity )
		{
			Debug.Assert( startIndex < doc.Length );
			Debug.Assert( doc[startIndex] == '&' );

			endIndex = startIndex + 1;
			while( endIndex < doc.Length )
			{
				char ch = doc[endIndex];

				if( (ch < 'A' || 'Z' < ch)
					&& (ch < 'a' || 'z' < ch)
					&& (ch < '0' || '9' < ch)
					&& (ch != '#') )
				{
					if( ch == ';' )
					{
						endIndex++;
						wasEntity = true;
						return;
					}
					else
					{
						wasEntity = false;
						return;
					}
				}

				endIndex++;
			}

			wasEntity = false;
		}
		#endregion
	}
}
