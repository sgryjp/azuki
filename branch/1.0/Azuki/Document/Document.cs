// file: Document.cs
// brief: Document of Azuki engine.
// author: YAMAMOTO Suguru
// encoding: UTF-8
// update: 2008-07-20
//=========================================================
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Color = System.Drawing.Color;
using Debug = System.Diagnostics.Debug;

namespace Sgry.Azuki
{
	/// <summary>
	/// The document of the Azuki editor engine.
	/// </summary>
	public class Document : IEnumerable
	{
		#region Fields
		TextBuffer _Buffer = new TextBuffer();
		SplitArray<int> _LHI = new SplitArray<int>( 64, 32 ); // line head indexes
		EditHistory _History = new EditHistory();
		ColorScheme _ColorScheme = ColorScheme.Default;
		int _CaretIndex = 0;
		int _AnchorIndex = 0;
		bool _IsRecordingHistory = true;
		string _EolCode = "\r\n";
		bool _IsReadOnly = false;
		IHighlighter _Highlighter;
		#endregion

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public Document()
		{
			// initialize LHI
			_LHI.Clear();
			_LHI.Add( 0 );
			
			_Highlighter = new DummyHighlighter();
		}
		#endregion

		#region Selection
		/// <summary>
		/// Gets index of where the caret is at (in char-index).
		/// </summary>
		public int CaretIndex
		{
			get{ return _CaretIndex; }
		}

		/// <summary>
		/// Gets caret location by logical line/column index.
		/// </summary>
		/// <param name="lineIndex">line index of where the caret is at</param>
		/// <param name="columnIndex">column index of where the caret is at</param>
		public void GetCaretIndex( out int lineIndex, out int columnIndex )
		{
			GetLineColumnIndexFromCharIndex( _CaretIndex, out lineIndex, out columnIndex );
		}

		/// <summary>
		/// Sets caret location by logical line/column index.
		/// Note that calling this method will release selection.
		/// </summary>
		/// <param name="lineIndex">new line index of where the caret is at</param>
		/// <param name="columnIndex">new column index of where the caret is at</param>
		public void SetCaretIndex( int lineIndex, int columnIndex )
		{
			int caretIndex = LineLogic.GetCharIndexFromLineColumnIndex( _Buffer, _LHI, lineIndex, columnIndex );
			SetSelection( caretIndex, caretIndex );
		}

		/// <summary>
		/// Gets index of the position where the selection starts (in char-index).
		/// </summary>
		public int AnchorIndex
		{
			get{ return _AnchorIndex; }
		}

		/// <summary>
		/// Sets selection range.
		/// Note that if given index is at middle of a surrogate pair,
		/// selection range will be automatically expanded to avoid dividing the pair.
		/// </summary>
		/// <param name="anchor">new index of the selection anchor</param>
		/// <param name="caret">new index of the caret</param>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was invalid.</exception>
		public void SetSelection( int anchor, int caret )
		{
			if( anchor < 0 || _Buffer.Count < anchor
				|| caret < 0 || _Buffer.Count < caret )
				throw new ArgumentOutOfRangeException( "'anchor' or 'caret'", "Invalid line index was given (anchor:"+anchor+", caret:"+caret+")." );
			
			int oldAnchor, oldCaret;

			// if given parameters change nothing, do nothing
			if( _AnchorIndex == anchor && _CaretIndex == caret )
			{
				return;
			}

			// ensure that given index is not in middle of the surrogate pairs
			Utl.ConstrainIndex( _Buffer, ref anchor, ref caret );

			// get anchor/caret position in new text content
			oldAnchor = _AnchorIndex;
			oldCaret = _CaretIndex;

			// apply new selection
			_AnchorIndex = anchor;
			_CaretIndex = caret;

			// invoke event
			InvokeSelectionChanged( oldAnchor, oldCaret );
		}

		/// <summary>
		/// Gets range of current selection.
		/// Note that this method does not return [anchor, caret) pair but [begin, end) pair.
		/// </summary>
		/// <param name="begin">index of where the selection begins.</param>
		/// <param name="end">index of where the selection ends (selection do not includes the char at this index).</param>
		public void GetSelection( out int begin, out int end )
		{
			if( _AnchorIndex < _CaretIndex )
			{
				begin = _AnchorIndex;
				end = _CaretIndex;
			}
			else
			{
				begin = _CaretIndex;
				end = _AnchorIndex;
			}
		}
		#endregion

		#region Content Access
		internal TextBuffer InternalBuffer
		{
			get{ return _Buffer; }
		}

		/// <summary>
		/// Gets or sets currently inputted text.
		/// </summary>
		public string Text
		{
			get
			{
				if( _Buffer.Count == 0 )
					return String.Empty;

				char[] text = new char[ _Buffer.Count ];
				_Buffer.GetRange( 0, _Buffer.Count, ref text );
				return new String( text );
			}
			set
			{
				Replace( value, 0, this.Length );
				SetSelection( 0, 0 );
			}
		}

		/// <summary>
		/// Gets a character at specified index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was invalid.</exception>
		public char GetCharAt( int index )
		{
			if( index < 0 || _Buffer.Count <= index )
				throw new ArgumentOutOfRangeException( "index", "Invalid index was given (index:"+index+", this.Length:"+Length+")." );

			return _Buffer[ index ];
		}

		/// <summary>
		/// Gets currently inputted character's count.
		/// Note that a surrogate pair will be counted as two.
		/// </summary>
		public int Length
		{
			get{ return _Buffer.Count; }
		}

		/// <summary>
		/// Gets number of the logical line.
		/// </summary>
		public int LineCount
		{
			get{ return _LHI.Count; }
		}

		/// <summary>
		/// Gets length of the logical line.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was invalid.</exception>
		public int GetLineLength( int lineIndex )
		{
			if( lineIndex < 0 || _LHI.Count <= lineIndex )
				throw new ArgumentOutOfRangeException( "lineIndex", "Invalid line index was given (lineIndex:"+lineIndex+", this.LineCount:"+LineCount+")." );

			int begin, end;

			// get line range
			LineLogic.GetLineRange( _Buffer, _LHI, lineIndex, out begin, out end );

			// return length
			return end - begin;
		}

		/// <summary>
		/// Gets content of the logical line.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was invalid.</exception>
		public string GetLineContent( int lineIndex )
		{
			if( lineIndex < 0 || _LHI.Count <= lineIndex )
				throw new ArgumentOutOfRangeException( "lineIndex", "Invalid line index was given (lineIndex:"+lineIndex+", this.LineCount:"+LineCount+")." );

			int begin, end;
			char[] lineContent;

			// prepare buffer to store line content
			LineLogic.GetLineRange( _Buffer, _LHI, lineIndex, out begin, out end );
			if( end <= begin )
			{
				return String.Empty;
			}
			lineContent = new char[ end-begin ];

			// copy line content
			_Buffer.GetRange( begin, end, ref lineContent );

			return new String( lineContent );
		}

		/// <summary>
		/// Gets content of the logical line without trimming EOL code.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was invalid.</exception>
		public string GetLineContentWithEolCode( int lineIndex )
		{
			int begin, end;
			char[] lineContent;

			if( lineIndex < 0 || _LHI.Count <= lineIndex )
				throw new ArgumentOutOfRangeException( "lineIndex", "Invalid line index was given (lineIndex:"+lineIndex+", this.LineCount:"+LineCount+")." );

			// prepare buffer to store line content
			LineLogic.GetLineRangeWithEol( _Buffer, _LHI, lineIndex, out begin, out end );
			if( end <= begin )
			{
				return String.Empty;
			}
			lineContent = new char[ end-begin ];
			
			// copy line content
			_Buffer.GetRange( begin, end, ref lineContent );

			return new String( lineContent );
		}

		/// <summary>
		/// Gets text in the range [begin, end).
		/// Note that if given index is at middle of a surrogate pair,
		/// given range will be automatically expanded to avoid dividing the pair.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified range was invalid.</exception>
		public string GetTextInRange( int begin, int end )
		{
			if( end < 0 || _Buffer.Count < end )
				throw new ArgumentOutOfRangeException( "end", "Invalid index was given (end:"+end+", this.Length:"+Length+")." );
			if( begin < 0 || end < begin )
				throw new ArgumentOutOfRangeException( "begin", "Invalid index was given (begin:"+begin+", end:"+end+", this.Length:"+Length+")." );

			if( begin == end )
			{
				return String.Empty;
			}

			// constrain indexes to avoid dividing surrogate pair
			Utl.ConstrainIndex( _Buffer, ref begin, ref end );
			
			// retrieve a part of the content
			char[] buf = new char[end - begin];
			_Buffer.GetRange( begin, end, ref buf );
			return new String( buf );
		}

		/// <summary>
		/// Gets text in the range [ (fromLineIndex, fromColumnIndex), (toLineIndex, toColumnIndex) ).
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified range was invalid.</exception>
		public string GetTextInRange( int beginLineIndex, int beginColumnIndex, int endLineIndex, int endColumnIndex )
		{
			if( endLineIndex < 0 || _LHI.Count <= endLineIndex )
				throw new ArgumentOutOfRangeException( "endLineIndex", "Invalid index was given (endLineIndex:"+endLineIndex+", this.Length:"+Length+")." );
			if( beginLineIndex < 0 || endLineIndex < beginLineIndex )
				throw new ArgumentOutOfRangeException( "beginLineIndex", "Invalid index was given (beginLineIndex:"+beginLineIndex+", endLineIndex:"+endLineIndex+")." );
			if( endColumnIndex < 0 )
				throw new ArgumentOutOfRangeException( "endColumnIndex", "Invalid index was given (endColumnIndex:"+endColumnIndex+")." );
			if( beginColumnIndex < 0 )
				throw new ArgumentOutOfRangeException( "beginColumnIndex", "Invalid index was given (beginColumnIndex:"+beginColumnIndex+")." );

			int begin, end;

			// if the specified range is empty, return empty string
			if( beginLineIndex == endLineIndex && beginColumnIndex == endColumnIndex )
			{
				return String.Empty;
			}

			// prepare buffer
			begin = _LHI[beginLineIndex] + beginColumnIndex;
			end = _LHI[endLineIndex] + endColumnIndex;
			if( _Buffer.Count < end )
			{
				throw new ArgumentOutOfRangeException( "?", "Invalid index was given (calculated end:"+end+", this.Length:"+Length+")." );
			}
			if( end <= begin )
			{
				throw new ArgumentOutOfRangeException( "?", String.Format("Invalid index was given (calculated range:[{4}, {5}) / beginLineIndex:{0}, beginColumnIndex:{1}, endLineIndex:{2}, endColumnIndex:{3}", beginLineIndex, beginColumnIndex, endLineIndex, endColumnIndex, begin, end) );
			}

			// copy content part
			return GetTextInRange( begin, end );
		}

		/// <summary>
		/// Gets class of the character at given index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was invalid.</exception>
		public CharClass GetCharClass( int index )
		{
			if( Length <= index )
				throw new ArgumentOutOfRangeException( "index", "Invalid index was given (index:"+index+", Length:"+Length+")." );

			Debug.Assert( _Buffer.GetCharClassAt(index) != CharClass.Selection, "char at index "+index+" has invalid char class." );
			return _Buffer.GetCharClassAt( index );
		}

		/// <summary>
		/// Sets class of the character at given index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was invalid.</exception>
		public void SetCharClass( int index, CharClass klass )
		{
			if( Length <= index )
				throw new ArgumentOutOfRangeException( "index", "Invalid index was given (index:"+index+", Length:"+Length+")." );

			_Buffer.SetCharClassAt( index, klass );
		}
		#endregion

		#region Content Modification
		/// <summary>
		/// Replaces current selection.
		/// </summary>
		public void Replace( string text )
		{
			int begin, end;

			GetSelection( out begin, out end );

			Replace( text, begin, end );
		}

		/// <summary>
		/// Replaces specified range [begin, end) of the content into the given string.
		/// </summary>
		/// <param name="text">specified range will be replaced with this text</param>
		/// <param name="begin">begin index of the range to be replaced</param>
		/// <param name="end">end index of the range to be replaced</param>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was invalid.</exception>
		public void Replace( string text, int begin, int end )
		{
			if( begin < 0 || _Buffer.Count < begin )
				throw new ArgumentOutOfRangeException( "begin", "Invalid index was given (begin:"+begin+", this.Length:"+Length+")." );
			if( end < begin || _Buffer.Count < end )
				throw new ArgumentOutOfRangeException( "end", "Invalid index was given (end:"+begin+", this.Length:"+Length+")." );
			if( text == null )
				throw new ArgumentNullException( "text" );

			if( _IsReadOnly )
				return;

			string oldText = String.Empty;
			int oldAnchor, anchorDelta;
			int oldCaret, caretDelta;
			EditAction undo;

			// keep copy of the part which will be deleted by this replacement
			if( begin < end )
			{
				char[] oldChars = new char[ end-begin ];
				_Buffer.GetRange( begin, end, ref oldChars );
				oldText = new String( oldChars );
			}

			// keep copy of old caret/anchor index
			oldAnchor = _AnchorIndex;
			oldCaret = _CaretIndex;

			// delete target range
			if( begin < end )
			{
				LineLogic.LHI_Delete( _LHI, _Buffer, begin, end );
				_Buffer.Delete( begin, end );
				if( begin < _CaretIndex )
				{
					_CaretIndex -= end - begin;
					if( _CaretIndex < begin )
						_CaretIndex = begin;
				}
				if( begin < _AnchorIndex )
				{
					_AnchorIndex -= end - begin;
					if( _AnchorIndex < begin )
						_AnchorIndex = begin;
				}
			}

			// then, insert text
			if( 0 < text.Length )
			{
				LineLogic.LHI_Insert( _LHI, _Buffer, text, begin );
				_Buffer.Insert( begin, text.ToCharArray() );
				if( begin <= _CaretIndex )
				{
					_CaretIndex += text.Length;
					if( _Buffer.Count < _CaretIndex ) // _Buffer.Count? really? isn't this "end"?
						_CaretIndex = _Buffer.Count;
				}
				if( begin <= _AnchorIndex )
				{
					_AnchorIndex += text.Length;
					if( _Buffer.Count < _AnchorIndex )
						_AnchorIndex = _Buffer.Count;
				}
			}

			// calc diff of anchor/caret between old and new positions
			anchorDelta = _AnchorIndex - oldAnchor;
			caretDelta = _CaretIndex - oldCaret;

			// calc anchor/caret index in current text
			oldAnchor += anchorDelta;
			oldCaret += caretDelta;

			// stack UNDO history
			if( _IsRecordingHistory )
			{
				undo = new EditAction( this, begin, oldText, text );
				_History.Add( undo );
			}

			Debug.Assert( begin <= Length );

			// cast event
			InvokeContentChanged( begin, oldText, text );
			InvokeSelectionChanged( oldAnchor, oldCaret );
		}
		#endregion

		#region Edit Actions and Modes
		/// <summary>
		/// Executes UNDO.
		/// </summary>
		public void Undo()
		{
			if( CanUndo )
			{
				EditAction action = _History.GetUndoAction();
				action.Undo();
			}
		}

		/// <summary>
		/// Gets whether an available undo action exists or not.
		/// </summary>
		public bool CanUndo
		{
			get{ return _History.CanUndo; }
		}

		/// <summary>
		/// Clears all stacked undo actions.
		/// </summary>
		public void ClearHistory()
		{
			_History.Clear();
		}

		/// <summary>
		/// Executes REDO.
		/// </summary>
		public void Redo()
		{
			if( CanRedo )
			{
				EditAction action = _History.GetRedoAction();
				action.Redo();
			}
		}

		/// <summary>
		/// Gets whether an available REDO action exists or not.
		/// </summary>
		public bool CanRedo
		{
			get{ return _History.CanRedo; }
		}

		/// <summary>
		/// Gets or sets EOL Code used in this document.
		/// Note that setting this property do nothing to the content.
		/// This is provided for other classes to determine EOL code to be used;
		/// for example, choosing EOL code to be input with Enter key,
		/// setting/getting by engine's client for any usage.
		/// </summary>
		public string EolCode
		{
			get{ return _EolCode; }
			set
			{
				if( value != "\r\n" && value != "\r" && value != "\n" )
					throw new InvalidOperationException( "invalid EOL code was set." );
				_EolCode = value;
			}
		}

		/// <summary>
		/// Gets or sets whether this document is recording edit actions or not.
		/// </summary>
		public bool IsRecordingHistory
		{
			get{ return _IsRecordingHistory; }
			set{ _IsRecordingHistory = value; }
		}

		/// <summary>
		/// Gets or sets whether this document is read-only or not.
		/// </summary>
		public bool IsReadOnly
		{
			get{ return _IsReadOnly; }
			set{ _IsReadOnly = value; }
		}
		#endregion

		#region Index Conversion
		/// <summary>
		/// Gets the index of the first char in the logical line.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was invalid.</exception>
		public int GetLineHeadIndex( int lineIndex )
		{
			if( lineIndex < 0 || _LHI.Count <= lineIndex )
				throw new ArgumentOutOfRangeException( "lineIndex", "Invalid index was given (lineIndex:"+lineIndex+", this.LineCount:"+LineCount+")." );

			return _LHI[ lineIndex ];
		}

		/// <summary>
		/// Gets the index of the first char in the logical line
		/// which contains the specified char-index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was invalid.</exception>
		public int GetLineHeadIndexFromCharIndex( int charIndex )
		{
			if( charIndex < 0 || _Buffer.Count < charIndex )
				throw new ArgumentOutOfRangeException( "charIndex", "Invalid index was given (charIndex:"+charIndex+", this.Length:"+Length+")." );

			//return LineLogic.GetLineHeadIndexFromCharIndex( _Buffer, _LHI, charIndex );
			return LineLogic.GetLineHeadIndexFromCharIndex( _Buffer, _LHI, charIndex );
		}

		/// <summary>
		/// Calculates logical line index from char-index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was invalid.</exception>
		public int GetLineIndexFromCharIndex( int charIndex )
		{
			if( charIndex < 0 || _Buffer.Count < charIndex )
				throw new ArgumentOutOfRangeException( "charIndex", "Invalid index was given (charIndex:"+charIndex+", this.Length:"+Length+")." );

			return LineLogic.GetLineIndexFromCharIndex( _LHI, charIndex );
		}

		/// <summary>
		/// Calculates logical line/column index from char-index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was invalid.</exception>
		public void GetLineColumnIndexFromCharIndex( int charIndex, out int lineIndex, out int columnIndex )
		{
			if( charIndex < 0 || _Buffer.Count < charIndex )
				throw new ArgumentOutOfRangeException( "charIndex", "Invalid index was given (charIndex:"+charIndex+", this.Length:"+Length+")." );

			//LineLogic.GetLineColumnIndexFromCharIndex( _Buffer, _LHI, charIndex, out lineIndex, out columnIndex );
			LineLogic.GetLineColumnIndexFromCharIndex( _Buffer, _LHI, charIndex, out lineIndex, out columnIndex );
		}

		/// <summary>
		/// Calculates char-index from logical line/column index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was invalid.</exception>
		public int GetCharIndexFromLineColumnIndex( int lineIndex, int columnIndex )
		{
			if( lineIndex < 0 || _LHI.Count <= lineIndex )
				throw new ArgumentOutOfRangeException( "lineIndex", "Invalid index was given (lineIndex:"+lineIndex+", this.LineCount:"+LineCount+")." );
			if( columnIndex < 0 )
				throw new ArgumentOutOfRangeException( "columnIndex", "Invalid index was given (columnIndex:"+columnIndex+")." );

			//return LineLogic.GetCharIndexFromLineColumnIndex( _Buffer, _LHI, lineIndex, columnIndex );
			return LineLogic.GetCharIndexFromLineColumnIndex( _Buffer, _LHI, lineIndex, columnIndex );
		}
		#endregion

		#region Highlighter and Character classes
		/// <summary>
		/// Gets or sets highlighter for this document.
		/// Note that setting null will disable highlighting.
		/// </summary>
		public IHighlighter Highlighter
		{
			get{ return _Highlighter; }
			set
			{
				if( value == null )
					value = new DummyHighlighter();

				// associate with new highlighter object and highlight whole content
				_Highlighter = value;
				_Highlighter.Highlight( this );
			}
		}

		/// <summary>
		/// Color scheme for this document.
		/// </summary>
		public ColorScheme ColorScheme
		{
			get{ return _ColorScheme; }
			set
			{
				if( value == null )
					throw new InvalidOperationException( "ColorScheme must not be null." );
				_ColorScheme = value;
			}
		}

		/// <summary>
		/// Registers a new character class used in this document.
		/// </summary>
		/// <param name="klass">New character class to be registered.</param>
		/// <param name="fore">Foreground color to draw characters of this class.</param>
		public void RegisterCharClass( CharClass klass, Color fore )
		{
			if( klass.Id < 10 )
				throw new ArgumentException( "ID of a user defined char-class must be larger than 10." );
			_ColorScheme[klass] = new ColorPair( fore, _ColorScheme.BackColor );
		}

		/// <summary>
		/// Registers a new character class used in this document.
		/// </summary>
		/// <param name="klass">New character class to be registered.</param>
		/// <param name="fore">Foreground color to draw characters of this class.</param>
		/// <param name="back">Background color to draw characters of this class.</param>
		public void RegisterCharClass( CharClass klass, Color fore, Color back )
		{
			if( klass.Id < 10 )
				throw new ArgumentException( "ID of a user defined char-class must be larger than 10." );
			_ColorScheme[klass] = new ColorPair( fore, _ColorScheme.BackColor );
		}
		#endregion

		#region Events
		/// <summary>
		/// Occurs when the selection was changed.
		/// </summary>
		public event SelectionChangedEventHandler SelectionChanged;
		void InvokeSelectionChanged( int oldAnchor, int oldCaret )
		{
			if( SelectionChanged != null )
			{
				SelectionChanged(
						this,
						new SelectionChangedEventArgs(oldAnchor, oldCaret)
					);
			}
		}

		/// <summary>
		/// Occurs when the document content was changed.
		/// ContentChangedEventArgs contains the old (replaced) text,
		/// new text, and index indicating the replacement occured.
		/// </summary>
		public event ContentChangedEventHandler ContentChanged;
		void InvokeContentChanged( int index, string oldText, string newText )
		{
			if( ContentChanged != null )
				ContentChanged( this, new ContentChangedEventArgs(index, oldText, newText) );
		}
		#endregion

		#region Utilities
		/// <summary>
		/// Gets line content enumerator.
		/// </summary>
		public IEnumerator GetEnumerator()
		{
			return new DocumentLineEnumerator( this );
		}

		/// <summary>
		/// Gets one character at given index.
		/// </summary>
		public char this[ int index ]
		{
			get{ return _Buffer[index]; }
		}

		/// <summary>
		/// Determines whether given char is a high surrogate or not.
		/// </summary>
		public static bool IsHighSurrogate( char ch )
		{
			return (0xd800 <= ch && ch <= 0xdbff);
		}

		/// <summary>
		/// Determines whether given char is a low surrogate or not.
		/// </summary>
		public static bool IsLowSurrogate( char ch )
		{
			return (0xdc00 <= ch && ch <= 0xdfff);
		}

		internal class Utl
		{
			public static void ConstrainIndex( TextBuffer buf, ref int anchor, ref int caret )
			{
				if( anchor < caret )
				{
					if( IsLowSurrogate(buf[anchor]) )
						anchor--;
					else if( buf[anchor] == '\n'
						&& 0 <= anchor-1 && buf[anchor-1] == '\r' )
						anchor--;
					if( caret < buf.Count && IsLowSurrogate(buf[caret]) )
						caret++;
					else if( caret < buf.Count && buf[caret] == '\n'
						&& 0 <= caret-1 && buf[caret-1] == '\r' )
						caret++;
				}
				else if( caret < anchor )
				{
					if( IsLowSurrogate(buf[caret]) )
						caret--;
					else if( buf[caret] == '\n'
						&& 0 < caret && buf[caret-1] == '\r' )
						caret--;
					if( anchor < buf.Count && IsLowSurrogate(buf[anchor]) )
						anchor++;
					else if( anchor < buf.Count && buf[anchor] == '\n'
						&& 0 <= anchor-1 && buf[anchor-1] == '\r' )
						anchor++;
				}
				else// if( anchor == caret )
				{
					if( anchor < buf.Count )
					{
						if( IsLowSurrogate(buf[anchor])
							|| (buf[anchor] == '\n' && 0 < anchor && buf[anchor-1] == '\r') )
						{
							anchor--;
							caret--;
						}
					}
				}
			}

		}
		#endregion
	}

	#region Events for Documents
	/// <summary>
	/// Event handler for SelectionChanged event.
	/// </summary>
	public delegate void SelectionChangedEventHandler( object sender, SelectionChangedEventArgs e );

	/// <summary>
	/// Event information about selection change.
	/// </summary>
	public class SelectionChangedEventArgs : EventArgs
	{
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public SelectionChangedEventArgs( int anchorIndex, int caretIndex )
		{
			OldAnchor = anchorIndex;
			OldCaret = caretIndex;
		}

		/// <summary>
		/// Anchor index (in current text) of the previous selection.
		/// </summary>
		public int OldAnchor;

		/// <summary>
		/// Offset from new anchor index to old anchor index
		/// (anchor index in old text content can be calculated by "OldAnchorIndex - AnchorDelta").
		/// </summary>
		public int AnchorDelta;

		/// <summary>
		/// Caret index (in current text) of the previous selection.
		/// </summary>
		public int OldCaret;

		/// <summary>
		/// Offset from new caret index to old caret index
		/// (caret index in old text content can be calculated by "OldCaretIndex - CaretDelta").
		/// </summary>
		public int CaretDelta;
	}

	/// <summary>
	/// Event handler for ContentChanged event.
	/// </summary>
	public delegate void ContentChangedEventHandler( object sender, ContentChangedEventArgs e );

	/// <summary>
	/// Event information about content change.
	/// </summary>
	public class ContentChangedEventArgs : EventArgs
	{
		int _Index;
		string _OldText, _NewText;

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public ContentChangedEventArgs( int index, string oldText, string newText )
		{
			_Index = index;
			_OldText = oldText;
			_NewText = newText;
		}

		/// <summary>
		/// Gets index of the position where the replacement occured.
		/// </summary>
		public int Index
		{
			get{ return _Index; }
		}

		/// <summary>
		/// Gets replaced text.
		/// </summary>
		public string OldText
		{
			get{ return _OldText; }
		}

		/// <summary>
		/// Gets newly inserted text.
		/// </summary>
		public string NewText
		{
			get{ return _NewText; }
		}
	}
	#endregion

	#region Line Enumerator
	/// <summary>
	/// Line enumerator for Document of Azuki.
	/// </summary>
	public class DocumentLineEnumerator : IEnumerator
	{
		Document _Doc;
		int _LineIndex = -1;

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public DocumentLineEnumerator( Document doc )
		{
			_Doc = doc;
		}

		/// <summary>
		/// Disposes resources.
		/// </summary>
		public void Dispose()
		{}
		#endregion

		#region IEnumerator Interface
		/// <summary>
		/// Moves this enumerator to the next line.
		/// </summary>
		public bool MoveNext()
		{
			if( _Doc.LineCount <= _LineIndex+1 )
				return false;

			_LineIndex++;
			return true;
		}

		/// <summary>
		/// Resets location of this enumerator.
		/// </summary>
		public void Reset()
		{
			_LineIndex = -1;
		}
		
		/// <summary>
		/// Retrieves the line content where this enumerator indicates. (System.String)
		/// </summary>
		public object Current
		{
			get{ return _Doc.GetLineContent(_LineIndex); }
		}
		#endregion
	}
	#endregion
}
