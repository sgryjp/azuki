// file: UriMarker.cs
// brief: a singleton class which marks URIs up in document.
//=========================================================
using System;
using System.Text;
using UnicodeCategory = System.Globalization.UnicodeCategory;

namespace Sgry.Azuki
{
	/// <summary>
	/// Parser to mark URIs up in Azuki document.
	/// </summary>
	class UriMarker
	{
		#region Fields
		static UriMarker _Inst = null;
		static readonly DefaultWordProc _WordProc = new DefaultWordProc();
		static string[] _SchemeTriggers = { "file://",
											"ftp://",
											"http://",
											"https://",
											"mailto:" };
		#endregion

		#region Static members
		/// <summary>
		/// Gets or sets list of URI scheme to trigger URI parsing;
		/// "http://" for instance.
		/// </summary>
		public static string[] Schemes
		{
			get{ return _SchemeTriggers; }
			set{ _SchemeTriggers = value; }
		}

		/// <summary>
		/// Initializes static members.
		/// </summary>
		static UriMarker()
		{
			_WordProc.EnableCharacterHanging = false;
			_WordProc.EnableEolHanging = false;
			_WordProc.EnableLineEndRestriction = false;
			_WordProc.EnableLineHeadRestriction = false;
			_WordProc.EnableWordWrap = false;
		}

		/// <summary>
		/// Gets the singleton instance of UriMarker.
		/// </summary>
		public static UriMarker Inst
		{
			get
			{
				if( _Inst == null )
					_Inst = new UriMarker();
				return _Inst;
			}
		}
		#endregion

		private UriMarker()
		{}

		#region Event handlers
		public void HandleContentChanged( object sender, ContentChangedEventArgs e )
		{
			var ui = (UiImpl)sender;
			var doc = ui.Document;

			if( doc.MarksUri == false )
				return;

			// Update marking in this line
			var lineIndex = doc.Lines.AtOffset( e.Index ).LineIndex;
			var shouldBeRedrawn = MarkOrUnmarkOneLine( doc, lineIndex, true );
			if( shouldBeRedrawn )
			{
				// Update entire graphic of the logical line if marking bits associated with any
				// character was changed
				ui.View.Invalidate( doc.Lines[lineIndex] );
			}
		}

		public void UI_LineDrawing( object sender, LineDrawEventArgs e )
		{
			var ui = (IUserInterface)sender;

			// Even if the URI marking is disabled, scanning procedure must be done because
			// characters marked as URI already must be unmarked after disabling URI marking.
			/*DO_NOT -->
			if( doc.MarksUri == false )
				return;
			<-- DO_NOT*/

			// Mark up all URIs in the logical line
			int screenLineHeadIndex = ui.View.Lines[ e.LineIndex ].Begin;
			int logicalLineIndex = ui.Document.Lines.AtOffset( screenLineHeadIndex ).LineIndex;
			e.ShouldBeRedrawn = MarkOrUnmarkOneLine( ui.Document, logicalLineIndex, ui.MarksUri );
		}
		#endregion

		#region Marking logic
		/// <summary>
		/// Marks URIs in a logical line.
		/// </summary>
		/// <returns>Whether specified line should be redrawn or not.</returns>
		bool MarkOrUnmarkOneLine( Document doc, int logicalLineIndex, bool marks )
		{
			DebugUtl.Assert( doc != null );
			DebugUtl.Assert( 0 <= logicalLineIndex );
			DebugUtl.Assert( logicalLineIndex < doc.Lines.Count );

			int changeCount = 0;

			// Do nothing if the document is empty.
			if( doc.Length == 0 )
				return false;

			// Prepare scanning
			var line = doc.Lines[ logicalLineIndex ];
			if( line.IsEmpty )
			{
				return false; // empty line
			}

			// Scan and mark all URIs in the line
			var lastMarkedIndex = line.Begin;
			var seekIndex = line.Begin;
			while( 0 <= seekIndex && seekIndex < line.End )
			{
				// Mark URI if one starts from here
				if( SchemeStartsFromHere(doc, seekIndex) )
				{
					bool isMailAddress;
					int uriEnd = GetUriEnd( doc, seekIndex, out isMailAddress );
					if( 0 < uriEnd )
					{
						// Clear marking before this URI part
						if( lastMarkedIndex < seekIndex )
						{
							if( doc.Unmark(lastMarkedIndex, seekIndex, Marking.Uri) )
								changeCount++;
						}

						// Mark the URI part
						if( marks )
						{
							if( doc.Mark(seekIndex, uriEnd, Marking.Uri) )
								changeCount++;
						}
						else
						{
							if( doc.Unmark(seekIndex, uriEnd, Marking.Uri) )
								changeCount++;
						}

						// Update seek position
						lastMarkedIndex = uriEnd;
						seekIndex = uriEnd;
						if( doc.Length <= seekIndex )
						{
							DebugUtl.Assert( seekIndex == doc.Length );
							break;
						}
					}
				}

				// Skip to next word
				seekIndex = _WordProc.NextWordStart( doc, seekIndex+1 );
			}

			// Clear marking of remaining characters
			if( lastMarkedIndex < line.End )
			{
				if( doc.Unmark(lastMarkedIndex, line.End, Marking.Uri) )
					changeCount++;
			}

			return (0 < changeCount);
		}

		public int GetUriEnd( Document doc, int startIndex, out bool isMailAddress )
		{
			if( doc == null )
				throw new ArgumentNullException( "doc" );
			if( startIndex < 0 || doc.Length < startIndex )
				throw new ArgumentOutOfRangeException( "startIndex" );

			int index = startIndex;
			char ch;
			var scheme = new StringBuilder( 8 );

			// Prepare parsing
			isMailAddress = false;
			var lineEnd = doc.Lines.AtOffset( startIndex ).End;
			DebugUtl.Assert( lineEnd <= doc.Length );

		//scheme:
			// Parse first character of scheme part
			if( index < lineEnd )
			{
				ch = doc[ index ];
				if( GetUriEnd_ValidChar(ch) == false )
					return -1;
				if( ch == '/' || ch == '?' || ch == '#' || ch == ':' )
					return -1;
				scheme.Append( ch );

				index++;
			}
			else
			{
				return -1;
			}

			// Parse remainings of scheme part
			while( index < lineEnd )
			{
				ch = doc[ index ];
				if( GetUriEnd_ValidChar(ch) == false )
					return -1;
				if( ch == '/' || ch == '?' || ch == '#' )
					return -1;
				if( ch == ':' )
					break;
				scheme.Append( ch );

				index++;
			}
			if( lineEnd <= index )
			{
				return -1;
			}

		//colon:
			// Parse colon part
			DebugUtl.Assert( doc[index] == ':' );
			index++;

			// If scheme is mailto, switch to mail address specific logic
			if( scheme.ToString() == "mailto" )
			{
				isMailAddress = true;
				return GetMailToEnd( doc, index );
			}

		//slash-1:
			// Parse slash part
			if( index < lineEnd )
			{
				ch = doc[ index ];
				if( ch != '/' )
					return -1;

				index++;
			}
			else
			{
				return -1;
			}

		//slash-2:
			// Parse slash part
			if( index < lineEnd )
			{
				ch = doc[ index ];
				if( ch != '/' )
					return -1;

				index++;
			}
			else
			{
				return -1;
			}

		//authority:
			// Parse first character of authority part
			if( index < lineEnd )
			{
				ch = doc[ index ];
				if( GetUriEnd_ValidChar(ch) == false )
					return -1;

				index++;
			}
			else
			{
				return -1;
			}

			// Parse remainings of authority part
			while( index < lineEnd )
			{
				ch = doc[ index ];
				if( GetUriEnd_ValidChar(ch) == false )
					return index;
				if( ch == '/' )
					break; //goto path;
				if( ch == '?' )
					goto query;
				if( ch == '#' )
					goto fragment;

				index++;
			}

		//path:
			// Parse path part
			while( index < lineEnd )
			{
				ch = doc[ index ];
				if( GetUriEnd_ValidChar(ch) == false )
					return index;
				if( ch == '?' )
					break; //goto query;
				if( ch == '#' )
					goto fragment;

				index++;
			}

		query:
			// Parse query part
			while( index < lineEnd )
			{
				ch = doc[ index ];
				if( GetUriEnd_ValidChar(ch) == false )
					return index;
				if( ch == '#' )
					break; //goto fragment;

				index++;
			}

		fragment:
			// Parse fragment part
			while( index < lineEnd )
			{
				ch = doc[ index ];
				if( GetUriEnd_ValidChar(ch) == false )
					return index;
				 
				index++;
			}

			return index;
		}

		static bool GetUriEnd_ValidChar( char ch )
		{
			if( ch <= 0x7f )
			{
				if( 'a' <= ch && ch <= 'z' || 'A' <= ch && ch <= 'Z' // alpha
					|| '0' <= ch && ch <= '9' // digit
					|| 0 <= "./_-?&=#%~!$*+,:;@\\^|".IndexOf(ch) )
				{
					return true;
				}
				return false;
			}
			else
			{
				var cat = Char.GetUnicodeCategory( ch );
				if( cat == UnicodeCategory.ClosePunctuation
					|| cat == UnicodeCategory.OpenPunctuation
					|| cat == UnicodeCategory.ParagraphSeparator
					|| cat == UnicodeCategory.SpaceSeparator
					|| cat == UnicodeCategory.Format
					|| 0 <= "\x3001\x3002".IndexOf(ch) )
				{
					return false;
				}
				return true;
			}
		}

		public int GetMailToEnd( Document doc, int startIndex )
		{
			if( doc == null )
				throw new ArgumentNullException( "doc" );
			if( startIndex < 0 || doc.Length < startIndex )
				throw new ArgumentOutOfRangeException( "startIndex" );

			int index = startIndex;
			char ch;

			if( doc.Length <= startIndex )
				return -1;

			// Prepare parsing
			int lineEnd = doc.Lines.AtOffset( startIndex ).End;
			DebugUtl.Assert( lineEnd <= doc.Length );

		//local-part:
			if( index < lineEnd )
			{
				ch = doc[index];
				if( GetMailToEnd_IsLocalPartChar(ch) == false )
					return -1;

				index++;
			}
			while( index < lineEnd )
			{
				ch = doc[index];
				if( ch == '@' )
					break;
				if( GetMailToEnd_IsLocalPartChar(ch) == false )
					return -1;

				index++;
			}
			if( lineEnd <= index )
			{
				return -1;
			}

		//at-mark:
			DebugUtl.Assert( doc[index] == '@' );
			index++;

		//domain:
			// Parse first character of domain part
			if( index < lineEnd )
			{
				ch = doc[index];
				if( GetMailToEnd_IsDomainChar(ch) == false )
					return -1;

				index++;
			}
			else
			{
				return -1;
			}

			// Parse remainings of domain part
			while( index < lineEnd )
			{
				ch = doc[index];
				if( GetMailToEnd_IsDomainChar(ch) == false )
					return index;

				index++;
			}

			return index;
		}

		static bool GetMailToEnd_IsLocalPartChar( char ch )
		{
			return ('0' <= ch && ch <= '9')
					|| ('A' <= ch && ch <= 'Z')
					|| 'a' <= ch && ch <= 'z'
					|| 0 <= ".-_!#$%&'*+/=?^`{|}~".IndexOf(ch);
		}

		static bool GetMailToEnd_IsDomainChar( char ch )
		{
			return ('A' <= ch && ch <= 'Z')
				|| ('a' <= ch && ch <= 'z')
				|| ('0' <= ch && ch <= '9')
				|| (0 <= "-.:[]".IndexOf(ch));
		}
		#endregion

		#region Utilities
		bool SchemeStartsFromHere( Document doc, int index )
		{
			DebugUtl.Assert( doc != null );
			DebugUtl.Assert( 0 <= index );
			DebugUtl.Assert( index < doc.Length );

			foreach( var scheme in _SchemeTriggers )
			{
				if( StartsWith(doc, index, scheme) )
					return true;
			}

			return false;
		}

		bool StartsWith( Document doc, int index, string text )
		{
			DebugUtl.Assert( doc != null );
			DebugUtl.Assert( 0 <= index );
			DebugUtl.Assert( index < doc.Length );
			DebugUtl.Assert( text != null );

			for( int i=0; i<text.Length; i++ )
			{
				if( doc.Length <= (index + i)
					|| text[i] != doc[index+i] )
				{
					return false;
				}
			}

			return true;
		}
		#endregion
	}
}
