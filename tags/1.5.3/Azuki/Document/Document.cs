// file: Document.cs
// brief: Document of Azuki engine.
// author: YAMAMOTO Suguru
// update: 2010-03-17
//=========================================================
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Color = System.Drawing.Color;
using Regex = System.Text.RegularExpressions.Regex;
using RegexOptions = System.Text.RegularExpressions.RegexOptions;
using Debug = System.Diagnostics.Debug;

namespace Sgry.Azuki
{
	using Highlighter;

	/// <summary>
	/// The document of the Azuki editor engine.
	/// </summary>
	public class Document : IEnumerable
	{
		#region Fields
		TextBuffer _Buffer = new TextBuffer( 4096, 1024 );
		SplitArray<int> _LHI = new SplitArray<int>( 64 ); // line head indexes
		SplitArray<LineDirtyState> _LDS = new SplitArray<LineDirtyState>( 64 ); // line dirty states
		EditHistory _History = new EditHistory();
		int _CaretIndex = 0;
		int _AnchorIndex = 0;
		bool _IsRecordingHistory = true;
		string _EolCode = "\r\n";
		bool _IsReadOnly = false;
		bool _IsDirty = false;
		IHighlighter _Highlighter = null;
		ViewParam _ViewParam = new ViewParam();
		DateTime _LastModifiedTime = DateTime.Now;
		int _LineSelectionAnchor = -1;
		int[] _RectSelectRanges = null;
		object _Tag = null;
		static readonly char[] _PairBracketTable = new char[]{
			'(', ')', '{', '}', '[', ']', '<', '>',
			'\xff08', '\xff09', // full-width parenthesis
			'\xff5b', '\xff5d', // full-width curly bracket
			'\xff3b', '\xff3d', // full-width square bracket
			'\xff1c', '\xff1e', // full-width less/greater than sign
			'\x3008', '\x3009', // CJK angle bracket
			'\x300a', '\x300b', // CJK double angle bracket
			'\x300c', '\x300d', // CJK corner bracket
			'\x300e', '\x300f', // CJK white corner bracket
			'\x3010', '\x3011', // CJK black lenticular bracket
			'\x3016', '\x3017', // CJK white lenticular bracket
			'\x3014', '\x3015', // CJK tortoise shell bracket
			'\x3018', '\x3019', // CJK white tortoise shell bracket
			'\x301a', '\x301b', // CJK white square bracket
			'\xff62', '\xff63' // half-width CJK corner bracket
		};
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

			// initialize LDS
			_LDS.Clear();
			_LDS.Add( 0 );
		}
		#endregion

		#region States
		/// <summary>
		/// Gets or sets the flag that is true if there are any unsaved modifications.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Dirty flag is the flag that is true if there are any unsaved modifications.
		/// Although any changes occured in Azuki sets this flag true automatically,
		/// setting this flag back to false must be done by user manually.
		/// Application is responsible to do so after saving content.
		/// </para>
		/// </remarks>
		public bool IsDirty
		{
			get{ return _IsDirty; }
			set
			{
				bool valueChanged = (_IsDirty != value);
				
				// apply value
				_IsDirty = value;

				// 'clean' up dirty state of modified lines
				if( _IsDirty == false )
				{
					for( int i=0; i<_LDS.Count; i++ )
					{
						if( _LDS[i] == LineDirtyState.Dirty )
						{
							_LDS[i] = LineDirtyState.Cleaned;
						}
					}
				}

				// invoke event
				if( valueChanged )
				{
					InvokeDirtyStateChanged();
				}
			}
		}

		/// <summary>
		/// Gets dirty state of specified line.
		/// </summary>
		/// <param name="lineIndex">Index of the line that to get dirty state of.</param>
		/// <returns>Dirty state of the specified line.</returns>
		/// <remarks>
		/// <para>
		/// This method gets dirty state of specified line.
		/// Dirty state of lines will changed as below.
		/// </para>
		/// <list type="bullet">
		///		<item>
		///		If a line was not modified yet, the dirty state of the line is
		///		<see cref="Sgry.Azuki.LineDirtyState">LineDirtyState</see>.Clean.
		///		</item>
		///		<item>
		///		If a line was modified, its dirty state will be changed to
		///		<see cref="Sgry.Azuki.LineDirtyState">LineDirtyState</see>.Dirty
		///		</item>
		///		<item>
		///		Setting false to
		///		<see cref="Sgry.Azuki.Document.IsDirty">Document.IsDirty</see>
		///		property will set all states of modified lines to
		///		<see cref="Sgry.Azuki.LineDirtyState">LineDirtyState</see>.Cleaned.
		///		</item>
		///		<item>
		///		Calling
		///		<see cref="Sgry.Azuki.Document.ClearHistory">Document.ClearHistory</see>
		///		to reset all states of lines to
		///		<see cref="Sgry.Azuki.LineDirtyState">LineDirtyState</see>.Clean.
		///		</item>
		/// </list>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.LineDirtyState">LineDirtyState enum</seealso>
		/// <seealso cref="Sgry.Azuki.Document.IsDirty">Document.IsDirty property</seealso>
		/// <seealso cref="Sgry.Azuki.Document.ClearHistory">Document.ClearHistory method</seealso>
		public LineDirtyState GetLineDirtyState( int lineIndex )
		{
			Debug.Assert( lineIndex <= _LDS.Count );
			Debug.Assert( _LDS.Count == _LHI.Count );
			if( lineIndex < 0 || LineCount < lineIndex )
				throw new ArgumentOutOfRangeException( "lineIndex", "lineIndex param is "+lineIndex+" but must be positive and equal to or less than "+LineCount );

			if( _LDS.Count <= lineIndex )
			{
				return LineDirtyState.Clean;
			}
			return _LDS[lineIndex];
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

		/// <summary>
		/// Gets whether an available UNDO action exists or not.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property gets whether one or more UNDOable action exists or not.
		/// </para>
		/// <para>
		/// To execute UNDO, use <see cref="Sgry.Azuki.Document.Undo">Undo</see> method.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.Document.Undo">Document.Undo method</seealso>
		public bool CanUndo
		{
			get{ return _History.CanUndo; }
		}

		/// <summary>
		/// Gets whether an available REDO action exists or not.
		/// </summary>
		public bool CanRedo
		{
			get{ return _History.CanRedo; }
		}

		/// <summary>
		/// Gets or sets the size of the internal buffer.
		/// </summary>
		public int Capacity
		{
			get{ return _Buffer.Capacity; }
			set{ _Buffer.Capacity = value; }
		}

		/// <summary>
		/// Gets the time when this document was last modified.
		/// </summary>
		public DateTime LastModifiedTime
		{
			get{ return _LastModifiedTime; }
		}

		/// <summary>
		/// Gets view specific parameters associated with this document.
		/// </summary>
		/// <remarks>
		/// <para>
		/// There are some parameters that are dependent on each document
		/// but are not parameters about document content.
		/// This property contains such parameters.
		/// </para>
		/// </remarks>
		internal ViewParam ViewParam
		{
			get{ return _ViewParam; }
		}
		#endregion

		#region Selection
		/// <summary>
		/// Gets index of where the caret is at (in char-index).
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property gets the index of the 'caret;' the text insertion point.
		/// </para>
		/// <para>
		/// In Azuki, selection always exists and is expressed by the range from anchor index to caret index.
		/// If there is nothing selected, it means that both anchor index and caret index is set to same value.
		/// </para>
		/// <para>
		/// To set value of anchor or caret, use
		/// <see cref="Sgry.Azuki.Document.SetSelection">Document.SetSelection</see> method.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.Document.AnchorIndex">Document.AnchorIndex Property</seealso>
		/// <seealso cref="Sgry.Azuki.Document.SetSelection">Document.SetSelection Method</seealso>
		public int CaretIndex
		{
			get{ return _CaretIndex; }
		}

		/// <summary>
		/// Gets index of the position where the selection starts (in char-index).
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property gets the index of the 'selection anchor;' where the selection starts.
		/// </para>
		/// <para>
		/// In Azuki, selection always exists and is expressed by the range from anchor index to caret index.
		/// If there is nothing selected, it means that both anchor index and caret index is set to same value.
		/// </para>
		/// <para>
		/// To set value of anchor or caret, use
		/// <see cref="Sgry.Azuki.Document.SetSelection">Document.SetSelection</see> method.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.Document.CaretIndex">Document.CaretIndex Property</seealso>
		/// <seealso cref="Sgry.Azuki.Document.SetSelection">Document.SetSelection Method</seealso>
		public int AnchorIndex
		{
			get{ return _AnchorIndex; }
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
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		public void SetCaretIndex( int lineIndex, int columnIndex )
		{
			if( lineIndex < 0 || columnIndex < 0 )
				throw new ArgumentOutOfRangeException( "lineIndex or columnIndex", "index must not be negative value. (lineIndex:"+lineIndex+", columnIndex:"+columnIndex+")" );
			if( _LHI.Count <= lineIndex )
				throw new ArgumentOutOfRangeException( "lineIndex", "too large line index was given (given:"+lineIndex+", actual line count:"+_LHI.Count+")" );
			
			int caretIndex = LineLogic.GetCharIndexFromLineColumnIndex( _Buffer, _LHI, lineIndex, columnIndex );
			SetSelection( caretIndex, caretIndex );
		}

		/// <summary>
		/// Sets selection range.
		/// </summary>
		/// <param name="anchor">new index of the selection anchor</param>
		/// <param name="caret">new index of the caret</param>
		/// <exception cref="System.ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		/// <remarks>
		/// This method sets selection range and invokes
		/// <see cref="Sgry.Azuki.Document.SelectionChanged">Document.SelectionChanged</see> event.
		/// Note that if given index is at middle of a surrogate pair,
		/// selection range will be automatically expanded to avoid dividing the pair.
		/// </remarks>
		public void SetSelection( int anchor, int caret )
		{
			if( anchor < 0 || _Buffer.Count < anchor
				|| caret < 0 || _Buffer.Count < caret )
			{
				throw new ArgumentOutOfRangeException( "'anchor' or 'caret'", "Invalid line index was given (anchor:"+anchor+", caret:"+caret+")." );
			}
			
			SetSelection_Impl( anchor, caret, true );
		}

		internal void SetSelection_Impl( int anchor, int caret, bool clearsSpecialSelection )
		{
			int oldAnchor, oldCaret;
			int[] oldRectSelectRanges = null;

			// clear rectangle selection if specified
			oldRectSelectRanges = _RectSelectRanges;
			if( clearsSpecialSelection )
			{
				_RectSelectRanges = null;
				LineSelectionAnchor = -1;
			}

			// if given parameters change nothing, do nothing
			if( _AnchorIndex == anchor && _CaretIndex == caret )
			{
				// but on executing rectangle selection with mouse,
				// slight movement that does not change the selection in the line under the mouse cursor
				// might change selection in other lines which is not under the mouse cursor.
				// so invoke event only if it is rectangle selection mode.
				if( _RectSelectRanges != null )
				{
					InvokeSelectionChanged( AnchorIndex, CaretIndex, _RectSelectRanges, false );
				}
				return;
			}

			// ensure that given index is not in middle of a surrogate pair or a CR-LF pair
			Utl.ConstrainIndex( _Buffer, ref anchor, ref caret );

			// get anchor/caret position in new text content
			oldAnchor = _AnchorIndex;
			oldCaret = _CaretIndex;

			// apply new selection
			_AnchorIndex = anchor;
			_CaretIndex = caret;

			// invoke event
			if( oldRectSelectRanges != null )
			{
				InvokeSelectionChanged( oldAnchor, oldCaret, oldRectSelectRanges, false );
			}
			else
			{
				InvokeSelectionChanged( oldAnchor, oldCaret, oldRectSelectRanges, false );
			}
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

		/// <summary>
		/// Gets or sets text ranges selected by rectangle selection.
		/// </summary>
		/// <remarks>
		/// <para>
		/// (This property is basically for internal use only
		/// so using this is not recommended.)
		/// </para>
		/// <para>
		/// The value of this method is an array of text indexes
		/// that is consisted with beginning index of first text range (row),
		/// ending index of first text range,
		/// beginning index of second text range,
		/// ending index of second text range and so on.
		/// </para>
		/// </remarks>
		public int[] RectSelectRanges
		{
			get{ return _RectSelectRanges; }
			set{ _RectSelectRanges = value; }
		}

		/// <summary>
		/// Gets or sets the char-index where the line selection has been started; or -1 if line selection is not executing.
		/// </summary>
		internal int LineSelectionAnchor
		{
			get{ return _LineSelectionAnchor; }
			set{ _LineSelectionAnchor = value; }
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
		/// <remarks>
		/// Getting text content through this property
		/// will copy all characters from internal buffer
		/// to a string object and returns it.
		/// </remarks>
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
				if( value == null )
					value = String.Empty;

				Replace( value, 0, this.Length );
				SetSelection( 0, 0 );
			}
		}

		/// <summary>
		/// Gets a character at specified index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		public char GetCharAt( int index )
		{
			if( index < 0 || _Buffer.Count <= index )
				throw new ArgumentOutOfRangeException( "index", "Invalid index was given (index:"+index+", this.Length:"+Length+")." );

			return _Buffer.GetAt( index );
		}

		/// <summary>
		/// Gets a word at specified index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		public string GetWordAt( int index )
		{
			if( index < 0 || _Buffer.Count < index ) // index can be equal to char-count
				throw new ArgumentOutOfRangeException( "index", "Invalid index was given (index:"+index+", this.Length:"+Length+")." );

			int begin, end;

			WordLogic.GetWordAt( this, index, out begin, out end );
			if( begin < 0 || end < 0 || end <= begin )
			{
				return String.Empty;
			}

			return GetTextInRange( begin, end );
		}

		/// <summary>
		/// Gets number of characters currently held in this document.
		/// Note that a surrogate pair will be counted as two characters.
		/// </summary>
		/// <remarks>
		/// This property is the number of characters currently held in this document.
		/// Since Azuki stores characters in form of UTF-16,
		/// surrogate pairs will not be counted as "1 character" in this property.
		/// </remarks>
		public int Length
		{
			get{ return _Buffer.Count; }
		}

		/// <summary>
		/// Gets number of the logical lines.
		/// </summary>
		/// <remarks>
		/// Through this property,
		/// number of the logical lines in this document can be retrieved.
		/// "Logical line" here means a string simply separated by EOL codes.
		/// and differs from "physical line" (a text line drawn as a graphc).
		/// To retrieve count of the logical lines,
		/// use <see cref="Sgry.Azuki.IView.LineCount">IView.LineCount</see> or
		/// <see cref="Sgry.Azuki.IUserInterface.LineCount">
		/// IUserInterface.LineCount</see> instead.
		/// </remarks>
		public int LineCount
		{
			get{ return _LHI.Count; }
		}

		/// <summary>
		/// Gets length of the logical line.
		/// </summary>
		/// <param name="lineIndex">Index of the line of which to get the length.</param>
		/// <returns>Length of the specified line in character count.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
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
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
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
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		public string GetLineContentWithEolCode( int lineIndex )
		{
			if( lineIndex < 0 || _LHI.Count <= lineIndex )
				throw new ArgumentOutOfRangeException( "lineIndex", "Invalid line index was given (lineIndex:"+lineIndex+", this.LineCount:"+LineCount+")." );

			int begin, end;
			char[] lineContent;

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
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
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
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
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

			// copy the portion of content
			return GetTextInRange( begin, end );
		}

		/// <summary>
		/// Gets class of the character at given index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		public CharClass GetCharClass( int index )
		{
			if( Length <= index )
				throw new ArgumentOutOfRangeException( "index", "Invalid index was given (index:"+index+", Length:"+Length+")." );

			return _Buffer.GetCharClassAt( index );
		}

		/// <summary>
		/// Sets class of the character at given index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		public void SetCharClass( int index, CharClass klass )
		{
			if( Length <= index )
				throw new ArgumentOutOfRangeException( "index", "Invalid index was given (index:"+index+", Length:"+Length+")." );

			_Buffer.SetCharClassAt( index, klass );
		}

		/// <summary>
		/// Replaces current selection.
		/// </summary>
		/// <exception cref="ArgumentNullException">Parameter text is null.</exception>
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
		/// <exception cref="ArgumentNullException">Parameter text is null.</exception>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		public void Replace( string text, int begin, int end )
		{
			Debug.Assert( _LHI.Count == _LDS.Count, "LHI.Count("+_LHI.Count+") is not LDS.Count("+_LDS.Count+")" );
			if( begin < 0 || _Buffer.Count < begin )
				throw new ArgumentOutOfRangeException( "begin", "Invalid index was given (begin:"+begin+", this.Length:"+Length+")." );
			if( end < begin || _Buffer.Count < end )
				throw new ArgumentOutOfRangeException( "end", "Invalid index was given (end:"+begin+", this.Length:"+Length+")." );
			if( text == null )
				throw new ArgumentNullException( "text" );

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
				// manage line head indexes and delete content
				LineLogic.LHI_Delete( _LHI, _LDS, _Buffer, begin, end );
				_Buffer.Delete( begin, end );

				// manage caret/anchor index
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
				// manage line head indexes and insert content
				LineLogic.LHI_Insert( _LHI, _LDS, _Buffer, text, begin );
				_Buffer.Insert( begin, text.ToCharArray() );

				// manage caret/anchor index
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

			// stack UNDO history
			if( _IsRecordingHistory )
			{
				undo = new EditAction( this, begin, oldText, text, oldAnchor, oldCaret );
				_History.Add( undo );
			}
			_LastModifiedTime = DateTime.Now;

			// convert anchor/caret index in current text
			oldAnchor += anchorDelta;
			oldCaret += caretDelta;

			Debug.Assert( begin <= Length );
			Debug.Assert( _LHI.Count == _LDS.Count, "LHI.Count("+_LHI.Count+") is not LMF.Count("+_LDS.Count+")" );

			// cast event
			IsDirty = true;
			InvokeContentChanged( begin, oldText, text );
			InvokeSelectionChanged( oldAnchor, oldCaret, null, true );
		}
		#endregion

		#region Editing Behavior
		/// <summary>
		/// Begins grouping up editing actions into a single UNDO action.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Call of this method creates a new group of actions in UNDO history
		/// and collect modification to this document until call of
		/// <see cref="Sgry.Azuki.Document.EndUndo">EndUndo method</see>.
		/// </para>
		/// <para>
		/// If no actions has been executed between call of BeginUndo and EndUndo,
		/// an UNDO action which do nothing will be stored in UNDO history.
		/// After call of this method, this method does nothing until EndUndo method was called
		/// so calling this method multiple times in a row happens nothing.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.Document.EndUndo">Document.EndUndo method</seealso>
		public void BeginUndo()
		{
			_History.BeginUndo();
		}

		/// <summary>
		/// Ends grouping up editing actions.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Call of this method stops grouping up editing actions.
		/// After call of this method,
		/// this method does nothing until
		/// <see cref="Sgry.Azuki.Document.BeginUndo">BeginUndo</see>.
		/// method was called.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.Document.BeginUndo">Document.BeginUndo method</seealso>
		public void EndUndo()
		{
			_History.EndUndo();
		}

		/// <summary>
		/// Executes UNDO.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This method restores the modification lastly done to this document.
		/// If there is no UNDOable action, this method will do nothing.
		/// </para>
		/// <para>
		/// To get whether any UNDOable action exists or not,
		/// use <see cref="Sgry.Azuki.Document.CanUndo">CanUndo</see> property.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.Document.CanUndo">Document.CanUndo property</seealso>
		public void Undo()
		{
			if( CanUndo )
			{
				EditAction action = _History.GetUndoAction();
				action.Undo();
			}
		}

		/// <summary>
		/// Clears all stacked undo actions.
		/// </summary>
		public void ClearHistory()
		{
			_History.Clear();
			_IsDirty = false;
			for( int i=0; i<_LDS.Count; i++ )
			{
				_LDS[i] = LineDirtyState.Clean;
			}
		}

		/// <summary>
		/// Executes REDO.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This method 'replays' the lastly UNDOed action if available.
		/// If there is no REDOable action, this method will do nothing.
		/// </para>
		/// </remarks>
		public void Redo()
		{
			if( CanRedo )
			{
				EditAction action = _History.GetRedoAction();
				action.Redo();
			}
		}

		/// <summary>
		/// Gets or sets default EOL Code of this document.
		/// </summary>
		/// <remarks>
		/// This value will be used when an Enter key was pressed,
		/// but setting this property itself does nothing to the content.
		/// </remarks>
		/// <exception cref="InvalidOperationException">Specified EOL code is not supported.</exception>
		public string EolCode
		{
			get{ return _EolCode; }
			set
			{
				if( value != "\r\n" && value != "\r" && value != "\n" )
					throw new InvalidOperationException( "unsupported type of EOL code was set." );
				_EolCode = value;
			}
		}
		#endregion

		#region Index Conversion
		/// <summary>
		/// Gets the index of the first char in the logical line.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
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
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		public int GetLineHeadIndexFromCharIndex( int charIndex )
		{
			if( charIndex < 0 || _Buffer.Count < charIndex ) // charIndex can be char-count
				throw new ArgumentOutOfRangeException( "charIndex", "Invalid index was given (charIndex:"+charIndex+", this.Length:"+Length+")." );

			return LineLogic.GetLineHeadIndexFromCharIndex( _Buffer, _LHI, charIndex );
		}

		/// <summary>
		/// Calculates logical line index from char-index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		public int GetLineIndexFromCharIndex( int charIndex )
		{
			if( charIndex < 0 || _Buffer.Count < charIndex ) // charIndex can be char-count
				throw new ArgumentOutOfRangeException( "charIndex", "Invalid index was given (charIndex:"+charIndex+", this.Length:"+Length+")." );

			return LineLogic.GetLineIndexFromCharIndex( _LHI, charIndex );
		}

		/// <summary>
		/// Calculates logical line/column index from char-index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		public void GetLineColumnIndexFromCharIndex( int charIndex, out int lineIndex, out int columnIndex )
		{
			if( charIndex < 0 || _Buffer.Count < charIndex ) // charIndex can be char-index
				throw new ArgumentOutOfRangeException( "charIndex", "Invalid index was given (charIndex:"+charIndex+", this.Length:"+Length+")." );

			LineLogic.GetLineColumnIndexFromCharIndex( _Buffer, _LHI, charIndex, out lineIndex, out columnIndex );
		}

		/// <summary>
		/// Calculates char-index from logical line/column index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		public int GetCharIndexFromLineColumnIndex( int lineIndex, int columnIndex )
		{
			if( lineIndex < 0 || _LHI.Count <= lineIndex )
				throw new ArgumentOutOfRangeException( "lineIndex", "Invalid index was given (lineIndex:"+lineIndex+", this.LineCount:"+LineCount+")." );
			if( columnIndex < 0 )
				throw new ArgumentOutOfRangeException( "columnIndex", "Invalid index was given (columnIndex:"+columnIndex+")." );

			int index;

			index = LineLogic.GetCharIndexFromLineColumnIndex( _Buffer, _LHI, lineIndex, columnIndex );
			if( _Buffer.Count < index )
			{
				// strict validation of column index is only done in debug build (for performance)
				// but exceeding buffer size may crash application so checks only that problem
				index = _Buffer.Count;
			}

			return index;
		}
		#endregion

		#region Text Search
		/// <summary>
		/// Finds a text pattern.
		/// </summary>
		/// <param name="value">The String to find.</param>
		/// <param name="startIndex">The search starting position.</param>
		/// <returns>Search result object if found, otherwise null if not found.</returns>
		/// <exception cref="ArgumentNullException">
		/// Parameter <paramref name="value"/> is null.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Parameter <paramref name="startIndex"/> is greater than character count in this document.
		/// </exception>
		/// <remarks>
		/// <para>
		/// This method finds the first occurrence of the pattern for the range of
		/// [<paramref name="startIndex"/>, EOD) where EOD means the end-of-document.
		/// The text matching process continues for the document end
		/// and does not stop at line ends nor null-characters.
		/// If the search range should end before EOD,
		/// use <see cref="Sgry.Azuki.Document.FindNext(string, int, int)">
		/// other overload method</see>.
		/// </para>
		/// <para>
		/// This method searches the text pattern case-sensitively.
		/// If the matching should be case-insensitively,
		/// use <see cref="Sgry.Azuki.Document.FindNext(string, int, bool)">
		/// other overload method</see>.
		/// </para>
		/// <para>
		/// If parameter <paramref name="value"/> is an empty string,
		/// search result will be the range of
		/// [<paramref name="startIndex"/>, <paramref name="startIndex"/>).
		/// </para>
		/// </remarks>
		public SearchResult FindNext( string value, int startIndex )
		{
			return FindNext( value, startIndex, _Buffer.Count, true );
		}

		/// <summary>
		/// Finds a text pattern.
		/// </summary>
		/// <param name="value">The String to find.</param>
		/// <param name="begin">The search starting position.</param>
		/// <param name="end">The search terminating position.</param>
		/// <returns>Search result object if found, otherwise null if not found.</returns>
		/// <exception cref="ArgumentNullException">
		/// Parameter <paramref name="value"/> is null.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Parameter <paramref name="begin"/> or <paramref name="end"/> is
		/// out of valid range.
		/// </exception>
		/// <remarks>
		/// <para>
		/// This method finds the first occurrence of the pattern in the range of
		/// [<paramref name="begin"/>, <paramref name="end"/>).
		/// The text matching process continues for the document end
		/// and does not stop at line ends nor null-characters.
		/// </para>
		/// <para>
		/// This method searches the text pattern case-sensitively.
		/// If the matching should be case-insensitively,
		/// use <see cref="Sgry.Azuki.Document.FindNext(string, int, int, bool)">
		/// other overload method</see>.
		/// </para>
		/// <para>
		/// If parameter <paramref name="value"/> is an empty string,
		/// search result will be the range of
		/// [<paramref name="startIndex"/>, <paramref name="startIndex"/>).
		/// </para>
		/// </remarks>
		public SearchResult FindNext( string value, int begin, int end )
		{
			return FindNext( value, begin, end, true );
		}

		/// <summary>
		/// Finds a text pattern.
		/// </summary>
		/// <param name="value">The String to find.</param>
		/// <param name="startIndex">The search starting position.</param>
		/// <param name="matchCase">Whether the search should be case-sensitive or not.</param>
		/// <returns>Search result object if found, otherwise null if not found.</returns>
		/// <exception cref="ArgumentNullException">
		/// Parameter <paramref name="value"/> is null.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Parameter <paramref name="startIndex"/> is greater than character count in this document.
		/// </exception>
		/// <remarks>
		/// <para>
		/// This method finds the first occurrence of the pattern for the range of
		/// [<paramref name="startIndex"/>, EOD) where EOD means the end-of-document.
		/// The text matching process continues for the document end
		/// and does not stop at line ends nor null-characters.
		/// If the search range should end before EOD,
		/// use <see cref="Sgry.Azuki.Document.FindNext(string, int, int, bool)">
		/// other overload method</see>.
		/// </para>
		/// <para>
		/// If <paramref name="matchCase"/> is true,
		/// the text pattern will be matched case-sensitively
		/// otherwise case will be ignored.
		/// </para>
		/// <para>
		/// If parameter <paramref name="value"/> is an empty string,
		/// search result will be the range of
		/// [<paramref name="startIndex"/>, <paramref name="startIndex"/>).
		/// </para>
		/// </remarks>
		public SearchResult FindNext( string value, int startIndex, bool matchCase )
		{
			return FindNext( value, startIndex, _Buffer.Count, matchCase );
		}

		/// <summary>
		/// Finds a text pattern.
		/// </summary>
		/// <param name="value">The string to find.</param>
		/// <param name="begin">The search starting position.</param>
		/// <param name="end">The search terminating position.</param>
		/// <param name="matchCase">Whether the search should be case-sensitive or not.</param>
		/// <returns>Search result object if found, otherwise null if not found.</returns>
		/// <exception cref="ArgumentNullException">
		/// Parameter <paramref name="value"/> is null.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Parameter <paramref name="begin"/> or <paramref name="end"/> is
		/// out of valid range.
		/// </exception>
		/// <remarks>
		/// <para>
		/// This method finds the first occurrence of the pattern in the range of
		/// [<paramref name="begin"/>, <paramref name="end"/>).
		/// The text matching process continues for the index specified by <paramref name="end"/> parameter
		/// and does not stop at line ends nor null-characters.
		/// </para>
		/// <para>
		/// If <paramref name="matchCase"/> is true,
		/// the text pattern will be matched case-sensitively
		/// otherwise case will be ignored.
		/// </para>
		/// <para>
		/// If parameter <paramref name="value"/> is an empty string,
		/// search result will be the range of [<paramref name="begin"/>, <paramref name="begin"/>).
		/// </para>
		/// </remarks>
		public SearchResult FindNext( string value, int begin, int end, bool matchCase )
		{
			if( begin < 0 )
				throw new ArgumentOutOfRangeException( "begin", "parameter begin must be a positive integer. (begin:"+begin+")" );
			if( end < begin )
				throw new ArgumentOutOfRangeException( "end", "parameter end must be greater than parameter begin. (begin:"+begin+", end:"+end+")" );
			if( _Buffer.Count < end )
				throw new ArgumentOutOfRangeException( "end", "end must not be greater than character count. (end:"+end+", this.Length:"+_Buffer.Count+")" );
			if( value == null )
				throw new ArgumentNullException( "value" );

			return _Buffer.FindNext( value, begin, end, matchCase );
		}

		/// <summary>
		/// Finds a text pattern by regular expression.
		/// </summary>
		/// <param name="regex">A Regex object expressing the text pattern.</param>
		/// <param name="startIndex">The search starting position.</param>
		/// <returns>Search result object if found, otherwise null if not found.</returns>
		/// <exception cref="ArgumentException">
		/// Parameter <paramref name="regex"/> is a Regex object with RegexOptions.RightToLeft option.
		/// </exception>
		/// <exception cref="ArgumentNullException">
		/// Parameter <paramref name="regex"/> is null.
		/// </exception>
		/// <remarks>
		/// <para>
		/// This method finds a text pattern
		/// expressed by a regular expression in the range of
		/// [<paramref name="startIndex"/>, EOD) where EOD means the end-of-document.
		/// The text matching process continues for the index
		/// specified with the <paramref name="end"/> parameter
		/// and does not stop at line ends nor null-characters.
		/// If the search range should end before EOD,
		/// use <see cref="Sgry.Azuki.Document.FindNext(Regex, int, int)">
		/// other overload method</see>.
		/// </para>
		/// <para>
		/// <see cref="System.Text.RegularExpressions.RegexOptions.RightToLeft">
		/// RegexOptions.RightToLeft</see> option MUST NOT be set to
		/// the Regex object given as parameter <paramref name="regex"/>
		/// otherwise an ArgumentException will be thrown.
		/// </para>
		/// <para>
		/// If an empty string was used for a regular expression pattern,
		/// search result will be the range of [<paramref name="startIndex"/>, <paramref name="startIndex"/>).
		/// The text matching process continues for the end of document
		/// and does not stop at line ends nor null-characters.
		/// </para>
		/// </remarks>
		public SearchResult FindNext( Regex regex, int startIndex )
		{
			return FindNext( regex, startIndex, _Buffer.Count );
		}

		/// <summary>
		/// Finds a text pattern by regular expression.
		/// </summary>
		/// <param name="regex">A Regex object expressing the text pattern to find.</param>
		/// <param name="begin">The begin index of the search range.</param>
		/// <param name="end">The end index of the search range.</param>
		/// <returns>Search result object if found, otherwise null if not found.</returns>
		/// <exception cref="ArgumentException">
		/// Parameter <paramref name="regex"/> is a Regex object with RegexOptions.RightToLeft option.
		/// </exception>
		/// <exception cref="ArgumentNullException">
		/// Parameter <paramref name="regex"/> is null.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Parameter <paramref name="begin"/> or <paramref name="end"/> is out of valid range.
		/// </exception>
		/// <remarks>
		/// <para>
		/// This method finds the first ocurrence of a pattern
		/// expressed by a regular expression in the range of
		/// [<paramref name="begin"/>, <paramref name="end"/>).
		/// The text matching process continues for the index
		/// specified with the <paramref name="end"/> parameter
		/// and does not stop at line ends nor null-characters.
		/// </para>
		/// <para>
		/// <see cref="System.Text.RegularExpressions.RegexOptions.RightToLeft">
		/// RegexOptions.RightToLeft</see> option MUST NOT be set to
		/// the Regex object given as parameter <paramref name="regex"/>
		/// otherwise an ArgumentException will be thrown.
		/// </para>
		/// <para>
		/// If an empty string was used for a regular expression pattern,
		/// search result will be the range of [<paramref name="begin"/>, <paramref name="begin"/>).
		/// </para>
		/// </remarks>
		public SearchResult FindNext( Regex regex, int begin, int end )
		{
			if( begin < 0 )
				throw new ArgumentOutOfRangeException( "begin", "parameter begin must be a positive integer. (begin:"+begin+")" );
			if( end < begin )
				throw new ArgumentOutOfRangeException( "end", "parameter end must be greater than parameter begin. (begin:"+begin+", end:"+end+")" );
			if( _Buffer.Count < end )
				throw new ArgumentOutOfRangeException( "end", "end must not be greater than character count. (end:"+end+", this.Length:"+_Buffer.Count+")" );
			if( regex == null )
				throw new ArgumentNullException( "regex" );
			if( (regex.Options & RegexOptions.RightToLeft) != 0 )
				throw new ArgumentException( "RegexOptions.RightToLeft option must not be set to the object 'regex'." );

			return _Buffer.FindNext( regex, begin, end );
		}

		/// <summary>
		/// Finds a text pattern backward.
		/// </summary>
		/// <param name="value">The string to find.</param>
		/// <param name="startIndex">The search starting position.</param>
		/// <returns>Search result object if found, otherwise null if not found.</returns>
		/// <exception cref="ArgumentNullException">
		/// Parameter <paramref name="value"/> is null.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Parameter <paramref name="startIndex"/> is out of range.
		/// </exception>
		/// <remarks>
		/// <para>
		/// This method finds the last occurrence of the pattern in the range
		/// of [0, <paramref name="startIndex"/>).
		/// The text matching process continues for the document head
		/// and does not stop at line ends nor null-characters.
		/// If the search range should end before document head,
		/// use <see cref="Sgry.Azuki.Document.FindPrev(string, int, int)">
		/// other overload method</see>.
		/// </para>
		/// <para>
		/// This method searches the text pattern case-sensitively.
		/// If the matching should be case-insensitively,
		/// use <see cref="Sgry.Azuki.Document.FindPrev(string, int, bool)">
		/// other overload method</see>.
		/// </para>
		/// <para>
		/// If parameter <paramref name="value"/> is an empty string,
		/// search result will be the range of [<paramref name="startIndex"/>, <paramref name="startIndex"/>).
		/// </para>
		/// </remarks>
		public SearchResult FindPrev( string value, int startIndex )
		{
			return FindPrev( value, 0, startIndex, true );
		}

		/// <summary>
		/// Finds a text pattern backward.
		/// </summary>
		/// <param name="value">The string to find.</param>
		/// <param name="startIndex">The search starting position.</param>
		/// <param name="matchCase">Whether the search should be case-sensitive or not.</param>
		/// <returns>Search result object if found, otherwise null if not found.</returns>
		/// <exception cref="ArgumentNullException">
		/// Parameter <paramref name="value"/> is null.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Parameter <paramref name="startIndex"/> is out of range.
		/// </exception>
		/// <remarks>
		/// <para>
		/// This method finds the last occurrence of the pattern in the range
		/// of [0, <paramref name="startIndex"/>).
		/// The text matching process continues for the document head
		/// and does not stop at line ends nor null-characters.
		/// If the search range should end before document head,
		/// use <see cref="Sgry.Azuki.Document.FindPrev(string, int, int)">
		/// other overload method</see>.
		/// </para>
		/// <para>
		/// If <paramref name="matchCase"/> is true,
		/// the text pattern will be matched case-sensitively
		/// otherwise case will be ignored.
		/// </para>
		/// <para>
		/// If parameter <paramref name="value"/> is an empty string,
		/// search result will be the range of [<paramref name="startIndex"/>, <paramref name="startIndex"/>).
		/// </para>
		/// </remarks>
		public SearchResult FindPrev( string value, int startIndex, bool matchCase )
		{
			return FindPrev( value, 0, startIndex, matchCase );
		}

		/// <summary>
		/// Finds a text pattern backward.
		/// </summary>
		/// <param name="value">The string to find.</param>
		/// <param name="begin">The begin index of the search range.</param>
		/// <param name="end">The end index of the search range.</param>
		/// <returns>Search result object if found, otherwise null if not found.</returns>
		/// <exception cref="ArgumentNullException">
		/// Parameter <paramref name="value"/> is null.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Parameter <paramref name="begin"/> or <paramref name="end"/> is out of range.
		/// </exception>
		/// <remarks>
		/// <para>
		/// This method finds the last occurrence of the pattern in the range
		/// of [<paramref name="begin"/>, <paramref name="end"/>).
		/// The text matching process continues for the document head
		/// and does not stop at line ends nor null-characters.
		/// If the search range should end before document head,
		/// use <see cref="Sgry.Azuki.Document.FindPrev(string, int, int)">
		/// other overload method</see>.
		/// </para>
		/// <para>
		/// This method searches the text pattern case-sensitively.
		/// If the matching should be case-insensitively,
		/// use <see cref="Sgry.Azuki.Document.FindPrev(string, int, int, bool)">
		/// other overload method</see>.
		/// </para>
		/// <para>
		/// If parameter <paramref name="value"/> is an empty string,
		/// search result will be the range of [<paramref name="startIndex"/>, <paramref name="startIndex"/>).
		/// </para>
		/// </remarks>
		public SearchResult FindPrev( string value, int begin, int end )
		{
			return FindPrev( value, begin, end, true );
		}

		/// <summary>
		/// Finds a text pattern backward.
		/// </summary>
		/// <param name="value">The string to find.</param>
		/// <param name="begin">The begin index of the search range.</param>
		/// <param name="end">The end index of the search range.</param>
		/// <param name="matchCase">Whether the search should be case-sensitive or not.</param>
		/// <returns>Search result object if found, otherwise null if not found.</returns>
		/// <exception cref="ArgumentNullException">
		/// Parameter <paramref name="value"/> is null.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Parameter <paramref name="begin"/> or <paramref name="end"/> is out of valid range.
		/// </exception>
		/// <remarks>
		/// <para>
		/// This method finds the last occurrence of the pattern in the range of
		/// [<paramref name="begin"/>, <paramref name="end"/>).
		/// The text matching process continues for the index specified by <paramref name="begin"/> parameter
		/// and does not stop at line ends nor null-characters.
		/// </para>
		/// <para>
		/// If <paramref name="matchCase"/> is true,
		/// the text pattern will be matched case-sensitively
		/// otherwise case will be ignored.
		/// </para>
		/// <para>
		/// If parameter <paramref name="value"/> is an empty string,
		/// search result will be a range of [<paramref name="end"/>, <paramref name="end"/>).
		/// </para>
		/// </remarks>
		public SearchResult FindPrev( string value, int begin, int end, bool matchCase )
		{
			if( begin < 0 )
				throw new ArgumentOutOfRangeException( "begin", "parameter begin must be a positive integer. (begin:"+begin+")" );
			if( end < begin )
				throw new ArgumentOutOfRangeException( "end", "parameter end must be greater than parameter begin. (begin:"+begin+", end:"+end+")" );
			if( _Buffer.Count < end )
				throw new ArgumentOutOfRangeException( "end", "end must not be greater than character count. (end:"+end+", this.Length:"+_Buffer.Count+")" );
			if( value == null )
				throw new ArgumentNullException( "value" );

			return _Buffer.FindPrev( value, begin, end, matchCase );
		}

		/// <summary>
		/// Finds a text pattern backward by regular expression.
		/// </summary>
		/// <param name="regex">A Regex object expressing the text pattern.</param>
		/// <param name="startIndex">The search starting position.</param>
		/// <returns>Search result object if found, otherwise null if not found.</returns>
		/// <exception cref="ArgumentException">
		/// Parameter <paramref name="regex"/> is a Regex object without RegexOptions.RightToLeft option.
		/// </exception>
		/// <exception cref="ArgumentNullException">
		/// Parameter <paramref name="regex"/> is null.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Parameter <paramref name="startIndex"/> is out of valid range.
		/// </exception>
		/// <remarks>
		/// <para>
		/// This method finds the last occurrence of a pattern
		/// expressed by a regular expression in the range of
		/// [0, <paramref name="startIndex"/>).
		/// The text matching process continues for the document head
		/// and does not stop at line ends nor null-characters.
		/// If the search range should end before EOD,
		/// use <see cref="Sgry.Azuki.Document.FindPrev(Regex, int, int)">
		/// other overload method</see>.
		/// </para>
		/// <para>
		/// <see cref="System.Text.RegularExpressions.RegexOptions.RightToLeft">
		/// RegexOptions.RightToLeft</see> option MUST be set to
		/// the Regex object given as parameter <paramref name="regex"/>
		/// otherwise an ArgumentException will be thrown.
		/// </para>
		/// <para>
		/// If an empty string was used for a regular expression pattern,
		/// search result will be a range of [<paramref name="end"/>, <paramref name="end"/>).
		/// </para>
		/// </remarks>
		public SearchResult FindPrev( Regex regex, int startIndex )
		{
			return FindPrev( regex, 0, startIndex );
		}

		/// <summary>
		/// Finds a text pattern backward by regular expression.
		/// </summary>
		/// <param name="regex">A Regex object expressing the text pattern.</param>
		/// <param name="begin">The begin index of the search range.</param>
		/// <param name="end">The end index of the search range.</param>
		/// <returns>Search result object if found, otherwise null if not found.</returns>
		/// <exception cref="ArgumentException">
		/// Parameter <paramref name="regex"/> is a Regex object without RegexOptions.RightToLeft option.
		/// </exception>
		/// <exception cref="ArgumentNullException">
		/// Parameter <paramref name="regex"/> is null.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Parameter <paramref name="begin"/> or <paramref name="end"/> is out of valid range.
		/// </exception>
		/// <remarks>
		/// <para>
		/// This method finds the last occurrence of a pattern
		/// expressed by a regular expression in the range of
		/// [<paramref name="begin"/>, <paramref name="end"/>).
		/// The text matching process continues for the index
		/// specified with the <paramref name="begin"/> parameter
		/// and does not stop at line ends nor null-characters.
		/// </para>
		/// <para>
		/// <see cref="System.Text.RegularExpressions.RegexOptions.RightToLeft">
		/// RegexOptions.RightToLeft</see> option MUST be set to
		/// the Regex object given as parameter <paramref name="regex"/>
		/// otherwise an ArgumentException will be thrown.
		/// </para>
		/// <para>
		/// If an empty string was used for a regular expression pattern,
		/// search result will be a range of [<paramref name="end"/>, <paramref name="end"/>).
		/// </para>
		/// </remarks>
		public SearchResult FindPrev( Regex regex, int begin, int end )
		{
			if( begin < 0 )
				throw new ArgumentOutOfRangeException( "begin", "parameter begin must be a positive integer. (begin:"+begin+")" );
			if( end < begin )
				throw new ArgumentOutOfRangeException( "end", "parameter end must be greater than parameter begin. (begin:"+begin+", end:"+end+")" );
			if( _Buffer.Count < end )
				throw new ArgumentOutOfRangeException( "end", "end must not be greater than character count. (end:"+end+", this.Length:"+_Buffer.Count+")" );
			if( regex == null )
				throw new ArgumentNullException( "regex" );
			if( (regex.Options & RegexOptions.RightToLeft) == 0 )
				throw new ArgumentException( "RegexOptions.RightToLeft option must be set to the object 'regex'." );

			return _Buffer.FindPrev( regex, begin, end );
		}

		/// <summary>
		/// Finds matched bracket from specified index.
		/// </summary>
		/// <param name="index">The index to start searching matched bracket.</param>
		/// <returns>Index of the matched bracket if found. Otherwise -1.</returns>
		/// <remarks>
		/// <para>
		/// This method searches the matched bracket from specified index.
		/// If the character at specified index was not a sort of bracket,
		/// this method returns -1.
		/// </para>
		/// </remarks>
		public int FindMatchedBracket( int index )
		{
			if( index < 0 || Length < index )
				throw new ArgumentOutOfRangeException( "index" );

			char bracket, pairBracket;
			bool isOpenBracket = false;
			int depth;

			// if given index is the end position,
			// there is no char at the index so search must be fail
			if( Length == index )
			{
				return -1;
			}

			// get the bracket and its pair
			bracket = this[index];
			pairBracket = '\0';
			for( int i=0; i<_PairBracketTable.Length; i++ )
			{
				if( bracket == _PairBracketTable[i] )
				{
					if( (i % 2) == 0 )
					{
						// found bracket is an opener. get paired closer
						pairBracket = _PairBracketTable[i+1];
						isOpenBracket = true;
					}
					else
					{
						// found bracket is an closer. get paired opener
						pairBracket = _PairBracketTable[i-1];
						isOpenBracket = false;
					}
					break;
				}
			}
			if( pairBracket == '\0' )
			{
				return -1; // not a bracket.
			}

			// search matched one
			depth = 0;
			if( isOpenBracket )
			{
				for( int i=index; i<this.Length; i++ )
				{
					// if it is in comment or something that is not a part of "content," ignore it
					if( Utl.ShouldBeIgnoredGrammatically(this, i) )
						continue;

					if( this[i] == bracket )
					{
						// found an opener again. increment depth count
						depth++;
					}
					else if( this[i] == pairBracket )
					{
						// found an closer. decrement depth count
						depth--;
						if( depth == 0 )
						{
							return i; // depth count reset by this char; this is the pair
						}
					}
				}
			}
			else
			{
				// search matched one
				for( int i=index; 0<=i; i-- )
				{
					// if it is in comment or something that is not a part of "content," ignore it
					if( Utl.ShouldBeIgnoredGrammatically(this, i) )
						continue;

					if( this[i] == bracket )
					{
						// found an closer again. increment depth count
						depth++;
					}
					else if( this[i] == pairBracket )
					{
						// found an opener. decrement depth count
						depth--;
						if( depth == 0 )
						{
							return i; // depth count reset by this char; this is the pair
						}
					}
				}
			}

			// not found
			return -1;
		}
		#endregion

		#region Highlighter
		/// <summary>
		/// Gets or sets highlighter object to highlight currently active document
		/// or null to disable highlighting.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property gets or sets highlighter for this document.
		/// </para>
		/// <para>
		/// Highlighter objects are used to highlight syntax of documents.
		/// They implements
		/// <see cref="Sgry.Azuki.Highlighter.IHighlighter">IHighlighter</see>
		/// interface and called
		/// <see cref="Sgry.Azuki.Highlighter.IHighlighter.Highlight(Sgry.Azuki.Document, ref int, ref int)">Highlight</see>
		/// method every time slightly after user input stopped to execute own highlighting logic.
		/// If null was set to this property, highlighting feature will be disabled.
		/// </para>
		/// <para>
		/// Azuki provides some built-in highlighters. See
		/// <see cref="Sgry.Azuki.Highlighter.Highlighters">Highlighter.Highlighters</see>
		/// class members.
		/// </para>
		/// <para>
		/// User can create and use custom highlighter object.
		/// If you want to create a keyword-based highlighter,
		/// you can extend
		/// <see cref="Sgry.Azuki.Highlighter.KeywordHighlighter">KeywordHighlighter</see>.
		/// If you want ot create not a keyword based one,
		/// create a class which implements
		/// <see cref="Sgry.Azuki.Highlighter.IHighlighter">IHighlighter</see>
		/// and write your own highlighting logic.
		/// </para>
		/// <para>
		/// Note that setting new value to this property will not invalidate graphics.
		/// To update graphic, set value via IUserInterface.Highlighter.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.IUserInterface.Highlighter">IUserInterface.Highlighter</seealso>
		public IHighlighter Highlighter
		{
			get{ return _Highlighter; }
			set
			{
				// clear all highlight information
				_Buffer.ClearCharClasses();

				// associate with new highlighter object and highlight whole content
				_Highlighter = value;
				if( _Highlighter != null )
				{
					_Highlighter.Highlight( this );
				}
			}
		}

		/// <summary>
		/// Gets whether the character at specified index
		/// is just a character data without meaning on grammer.
		/// </summary>
		/// <param name="index">The index of the character to examine.</param>
		/// <returns>Whether the character is part of a character data or not.</returns>
		/// <remarks>
		/// <para>
		/// This method gets whether the character at specified index
		/// is just a character data without meaning on grammer.
		/// 'Character data' here is text data
		/// that is treated as plain text data on grammer
		/// like characters in comment, string literal etc.
		/// </para>
		/// </remarks>
		public bool IsCDATA( int index )
		{
			CharClass klass;

			klass = GetCharClass( index );
			return ( klass == CharClass.AttributeValue
					|| klass == CharClass.CDataSection
					|| klass == CharClass.Character
					|| klass == CharClass.Comment
					|| klass == CharClass.DocComment
					|| klass == CharClass.Regex
					|| klass == CharClass.String
				);
		}
		#endregion

		#region Events
		/// <summary>
		/// Occurs when the selection was changed.
		/// </summary>
		public event SelectionChangedEventHandler SelectionChanged;
		void InvokeSelectionChanged( int oldAnchor, int oldCaret, int[] oldRectSelectRanges, bool byContentChanged )
		{
#			if DEBUG
			Debug.Assert( 0 <= oldAnchor );
			Debug.Assert( 0 <= oldCaret );
			if( oldRectSelectRanges != null )
			{
				Debug.Assert( oldRectSelectRanges.Length % 2 == 0 );
			}
#			endif

			if( SelectionChanged != null )
			{
				SelectionChanged(
						this,
						new SelectionChangedEventArgs(oldAnchor, oldCaret, oldRectSelectRanges, byContentChanged)
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
			Debug.Assert( 0 <= index );
			Debug.Assert( oldText != null );
			Debug.Assert( newText != null );

			if( ContentChanged != null )
				ContentChanged( this, new ContentChangedEventArgs(index, oldText, newText) );
		}

		/// <summary>
		/// Occurs when IsDirty property has changed.
		/// </summary>
		public event EventHandler DirtyStateChanged;
		void InvokeDirtyStateChanged()
		{
			if( DirtyStateChanged != null )
				DirtyStateChanged( this, EventArgs.Empty );
		}
		#endregion

		#region Utilities
		/// <summary>
		/// Gets or sets an object associated with this document.
		/// </summary>
		public object Tag
		{
			get{ return _Tag; }
			set{ _Tag = value; }
		}

		/// <summary>
		/// Gets line content enumerator.
		/// </summary>
		public IEnumerator GetEnumerator()
		{
			return _Buffer.GetEnumerator();
		}

		/// <summary>
		/// Gets estimated memory size used by this document.
		/// </summary>
		public int MemoryUsage
		{
			get
			{
				int usage = 0;
				usage += _Buffer.Capacity * ( sizeof(char) + sizeof(CharClass) );
				usage += _LHI.Capacity * sizeof(int);
				usage += _History.MemoryUsage;
				return usage;
			}
		}

		/// <summary>
		/// Gets one character at given index.
		/// </summary>
		public char this[ int index ]
		{
			get{ return _Buffer[index]; }
		}

		/// <summary>
		/// Determines whether text can be divided by given index or not.
		/// </summary>
		public static bool IsDividableIndex( string text, int index )
		{
			if( 0 < index && text[index-1] == '\r'
				&& index < text.Length && text[index] == '\n' )
			{
				return false;
			}
			if( 0 < index && IsHighSurrogate(text[index-1])
				&& index < text.Length && IsLowSurrogate(text[index]) )
			{
				return false;
			}

			return true;
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
			public static bool ShouldBeIgnoredGrammatically( Document doc, int index )
			{
				CharClass klass = doc.GetCharClass( index );
				if( klass == CharClass.CDataSection
					|| klass == CharClass.Character
					|| klass == CharClass.Comment
					|| klass == CharClass.DocComment
					|| klass == CharClass.Regex
					|| klass == CharClass.String )
				{
					return true;
				}

				return false;
			}

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

	#region Types for Events
	/// <summary>
	/// Event handler for SelectionChanged event.
	/// </summary>
	public delegate void SelectionChangedEventHandler( object sender, SelectionChangedEventArgs e );

	/// <summary>
	/// Event information about selection change.
	/// </summary>
	public class SelectionChangedEventArgs : EventArgs
	{
		int _OldAnchor;
		int _OldCaret;
		int[] _OldRectSelectRanges;
		bool _ByContentChanged;

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public SelectionChangedEventArgs( int anchorIndex, int caretIndex, int[] oldRectSelectRanges, bool byContentChanged )
		{
			_OldAnchor = anchorIndex;
			_OldCaret = caretIndex;
			_OldRectSelectRanges = oldRectSelectRanges;
			_ByContentChanged = byContentChanged;
		}

		/// <summary>
		/// Anchor index (in current text) of the previous selection.
		/// </summary>
		public int OldAnchor
		{
			get{ return _OldAnchor; }
		}

		/// <summary>
		/// Caret index (in current text) of the previous selection.
		/// </summary>
		public int OldCaret
		{
			get{ return _OldCaret; }
		}

		/// <summary>
		/// Text ranges selected by previous rectangle selection (indexes are valid in current text.)
		/// </summary>
		public int[] OldRectSelectRanges
		{
			get{ return _OldRectSelectRanges; }
		}

		/// <summary>
		/// This value will be true if this event has been occured because the document was modified.
		/// </summary>
		public bool ByContentChanged
		{
			get{ return _ByContentChanged; }
		}
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
}