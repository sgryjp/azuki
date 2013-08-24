// file: SelectionManager.cs
// brief: Internal class to manage text selection range.
// author: YAMAMOTO Suguru
//=========================================================
using System;
using System.Diagnostics;
using System.Drawing;

namespace Sgry.Azuki
{
	/// <summary>
	/// Internal class to manage text selection range.
	/// </summary>
	class SelectionManager
	{
		#region Fields
		Document _Document;
		int _CaretIndex = 0;
		int _AnchorIndex = 0;
		int _OriginalAnchorIndex = -1;
		int _LineSelectionAnchor1 = -1;
		int _LineSelectionAnchor2 = -1; // temporary variable holding selection anchor on expanding line selection backward
		int[] _RectSelectRanges = null;
		TextDataType _SelectionMode = TextDataType.Normal;
		#endregion

		#region Init / Dispose
		public SelectionManager( Document doc )
		{
			Debug.Assert( doc != null );
			_Document = doc;
		}
		#endregion

		#region Selection State
		/// <summary>
		/// Gets or sets current position of the caret.
		/// </summary>
		public int CaretIndex
		{
			get{ return _CaretIndex; }
			set
			{
				Debug.Assert( 0 <= value && value <= _Document.Length, "invalid value ("+value+") was set to SelectionManager.CaretIndex (Document.Length:"+_Document.Length+")" );
				_CaretIndex = value;
			}
		}

		/// <summary>
		/// Gets or sets current position of selection anchor.
		/// </summary>
		public int AnchorIndex
		{
			get{ return _AnchorIndex; }
			set
			{
				Debug.Assert( 0 <= value && value <= _Document.Length, "invalid value ("+value+") was set to SelectionManager.AnchorIndex (Document.Length:"+_Document.Length+")" );
				_OriginalAnchorIndex = -1;
				_AnchorIndex = value;
			}
		}

		/// <summary>
		/// Gets originally set position of selection anchor.
		/// </summary>
		public int OriginalAnchorIndex
		{
			get
			{
				if( 0 <= _OriginalAnchorIndex )
					return _OriginalAnchorIndex;
				else
					return _AnchorIndex;
			}
		}

		public TextDataType SelectionMode
		{
			get{ return _SelectionMode; }
			set
			{
				bool changed = (_SelectionMode != value);
				_SelectionMode = value;
				if( changed )
				{
					_Document.InvokeSelectionModeChanged();
				}
			}
		}

		public int[] RectSelectRanges
		{
			get{ return _RectSelectRanges; }
			set{ _RectSelectRanges = value; }
		}

		public IRange GetSelection()
		{
			var range = new Range();

			if( _AnchorIndex < _CaretIndex )
			{
				range.Begin = _AnchorIndex;
				range.End = _CaretIndex;
			}
			else
			{
				range.Begin = _CaretIndex;
				range.End = _AnchorIndex;
			}

			DebugUtl.Assert( 0 <= range.Begin && range.Begin <= _Document.Length,
							 "range.Begin:{0}, Length:{1}", range.Begin, _Document.Length );
			DebugUtl.Assert( 0 <= range.End && range.End <= _Document.Length, "range.End:{0},"
							 + " Length:{1}",range.End, _Document.Length );
			DebugUtl.Assert( range.Begin <= range.End, "range.Begin:{0}, range.End:{1}",
							 range.Begin, range.End );

			return range;
		}

		public void SetSelection( int anchor, int caret, IViewInternal view )
		{
			Debug.Assert( 0 <= anchor && anchor <= _Document.Length, "parameter 'anchor' out of range (anchor:"+anchor+", Document.Length:"+_Document.Length+")" );
			Debug.Assert( 0 <= caret && caret <= _Document.Length, "parameter 'caret' out of range (anchor:"+anchor+", Document.Length:"+_Document.Length+")" );
			Debug.Assert( _SelectionMode == TextDataType.Normal || view != null );

			// ensure that document can be divided at given index
			TextUtil.ConstrainIndex( _Document.InternalBuffer, ref anchor, ref caret );

			// set selection
			if( SelectionMode == TextDataType.Rectangle )
			{
				ClearLineSelectionData();
				_OriginalAnchorIndex = -1;
				SetSelection_Rect( anchor, caret, view );
			}
			else if( SelectionMode == TextDataType.Line )
			{
				ClearRectSelectionData();
				_OriginalAnchorIndex = -1;
				SetSelection_Line( anchor, caret, view );
			}
			else if( SelectionMode == TextDataType.Words )
			{
				ClearLineSelectionData();
				ClearRectSelectionData();
				SetSelection_Words( anchor, caret );
			}
			else
			{
				ClearLineSelectionData();
				ClearRectSelectionData();
				_OriginalAnchorIndex = -1;
				SetSelection_Normal( anchor, caret );
			}
		}

		/// <summary>
		/// Distinguishes whether specified index is in selection or not.
		/// </summary>
		public bool IsInSelection( int index )
		{
			int begin, end;

			if( _Document.RectSelectRanges != null )
			{
				// is in rectangular selection mode.
				for( int i=0; i<_Document.RectSelectRanges.Length; i+=2 )
				{
					begin = _Document.RectSelectRanges[i];
					end = _Document.RectSelectRanges[i+1];
					if( begin <= index && index < end )
					{
						return true;
					}
				}

				return false;
			}
			else
			{
				// is not in rectangular selection mode.
				_Document.GetSelection( out begin, out end );
				return (begin <= index && index < end);
			}
		}
		#endregion

		#region Internal Logic
		void SetSelection_Rect( int anchor, int caret, IView view )
		{
			// calculate graphical position of both anchor and new caret
			Point anchorPos = view.GetVirtualPos( anchor );
			Point caretPos = view.GetVirtualPos( caret );

			// calculate ranges selected by the rectangle made with the two points
			_RectSelectRanges = view.GetRectSelectRanges(
					Utl.MakeRectFromTwoPoints(anchorPos, caretPos)
				);

			// set selection
			SetSelection_Normal( anchor, caret );
		}

		void SetSelection_Line( int anchor, int caret, IViewInternal view )
		{
			int toLineIndex;

			// get line index of the lines where selection starts and ends
			toLineIndex = view.GetLineIndexFromCharIndex( caret );
			if( _LineSelectionAnchor1 < 0
				|| (anchor != _LineSelectionAnchor1 && anchor != _LineSelectionAnchor2) )
			{
				//-- line selection anchor changed or did not exists --
				// select between head of the line and end of the line
				int fromLineIndex = view.GetLineIndexFromCharIndex( anchor );
				var range = view.RawLines[ fromLineIndex ];
				anchor = range.Begin;
				caret = range.End;
				_LineSelectionAnchor1 = anchor;
				_LineSelectionAnchor2 = anchor;
			}
			else if( _LineSelectionAnchor1 < caret )
			{
				//-- selecting to the line (or after) where selection started --
				// select between head of the starting line and the end of the destination line
				anchor = view.Lines.AtOffset( _LineSelectionAnchor1 ).Begin;
				if( view.IsLineHead(caret) == false )
				{
					toLineIndex = view.GetLineIndexFromCharIndex( caret );
					caret = view.RawLines[ toLineIndex ].End;
				}
			}
			else// if( caret < LineSelectionAnchor )
			{
				//-- selecting to foregoing lines where selection started --
				// select between head of the destination line and end of the starting line
				int anchorLineIndex = view.GetLineIndexFromCharIndex( _LineSelectionAnchor1 );
				caret = view.RawLines[ toLineIndex ].Begin;
				anchor = view.RawLines[ anchorLineIndex ].End;

				//DO_NOT//_LineSelectionAnchor1 = anchor;
				_LineSelectionAnchor2 = anchor;
			}

			// apply new selection
			SetSelection_Normal( anchor, caret );
		}

		void SetSelection_Words( int anchor, int caret )
		{
			// Remember original position of anchor 
			_OriginalAnchorIndex = anchor;

			// Ensure both selection boundaries are on word boundary
			var wordAtAnchorRange = _Document.GetWordRange( anchor );
			var wordAtCaretRange = _Document.GetWordRange( caret );
			if( anchor <= caret )
			{
				anchor = wordAtAnchorRange.Begin;
				caret = wordAtCaretRange.End;
			}
			else
			{
				caret = wordAtCaretRange.Begin;
				anchor = wordAtAnchorRange.End;
			}

			// Select normally
			SetSelection_Normal( anchor, caret );

		}

		void SetSelection_Normal( int anchor, int caret )
		{
			int oldAnchor, oldCaret;
			int[] oldRectSelectRanges = null;

			// if given parameters change nothing, do nothing
			if( _AnchorIndex == anchor && _CaretIndex == caret )
			{
				// but on executing rectangle selection with mouse,
				// slight movement that does not change the selection in the line under the mouse cursor
				// might change selection in other lines which is not under the mouse cursor.
				// so invoke event only if it is rectangle selection mode.
				if( _RectSelectRanges != null )
				{
					_Document.InvokeSelectionChanged( AnchorIndex, CaretIndex, _RectSelectRanges, false );
				}
				return;
			}

			// remember old selection state
			oldAnchor = _AnchorIndex;
			oldCaret = _CaretIndex;
			oldRectSelectRanges = _RectSelectRanges;

			// apply new selection
			_AnchorIndex = anchor;
			_CaretIndex = caret;

			// invoke event
			if( oldRectSelectRanges != null )
			{
				_Document.InvokeSelectionChanged( oldAnchor, oldCaret, oldRectSelectRanges, false );
			}
			else
			{
				_Document.InvokeSelectionChanged( oldAnchor, oldCaret, oldRectSelectRanges, false );
			}
		}

		void ClearRectSelectionData()
		{
			_RectSelectRanges = null;
		}

		void ClearLineSelectionData()
		{
			_LineSelectionAnchor1 = -1;
			_LineSelectionAnchor2 = -1;
		}
		#endregion

		#region Utilities
		static class Utl
		{
			public static Rectangle MakeRectFromTwoPoints( Point pt1, Point pt2 )
			{
				Rectangle rect = new Rectangle();

				// set left and width
				if( pt1.X < pt2.X )
				{
					rect.X = pt1.X;
					rect.Width = pt2.X - pt1.X;
				}
				else
				{
					rect.X = pt2.X;
					rect.Width = pt1.X - pt2.X;
				}

				// set top and height
				if( pt1.Y < pt2.Y )
				{
					rect.Y = pt1.Y;
					rect.Height = pt2.Y - pt1.Y;
				}
				else
				{
					rect.Y = pt2.Y;
					rect.Height = pt1.Y - pt2.Y;
				}

				return rect;
			}
		}
		#endregion
	}
}
