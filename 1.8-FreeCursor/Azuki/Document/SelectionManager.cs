// file: SelectionManager.cs
// brief: Internal class to manage text selection range.
//=========================================================
using System;
using System.Collections.Generic;
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
		int _OriginalAnchorIndex = -1;
		int _LineSelectionAnchor1 = -1;
		int _LineSelectionAnchor2 = -1; // temporal variable holding selection anchor on expanding line selection backward
		Selections _Selections;
internal TextDataType _LastSelectionMode = TextDataType.Normal; // just remembering how last selection was made
		#endregion

		#region Init / Dispose
		public SelectionManager( Document doc )
		{
			Debug.Assert( doc != null );
			_Document = doc;
			_Selections = new Selections();
		}
		#endregion

		#region Selection State
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
					return _Selections.Anchor;
			}
		}

		public Selections Selections
		{
			get{ return _Selections; }
		}

		public void SetSelection( int anchor, int caret,
								  IView view, TextDataType mode )
		{
			Debug.Assert( 0 <= anchor && anchor <= _Document.Length,
						  "parameter 'anchor' out of range (anchor:" + anchor
						  + ", Document.Length:" + _Document.Length + ")" );
			Debug.Assert( 0 <= caret && caret <= _Document.Length,
						  "parameter 'caret' out of range (anchor:" + anchor
						  + ", Document.Length:" + _Document.Length + ")" );

			// ensure that document can be divided at given index
			Document.Utl.ConstrainIndex( _Document, ref anchor, ref caret );

			// set selection
			_Selections.LastRangeIndex = 0;
			if( mode == TextDataType.Rectangle )
			{
				ClearLineSelectionData();
				_OriginalAnchorIndex = -1;
				SetSelection_Rect( anchor, caret, view );
				_LastSelectionMode = TextDataType.Rectangle;
			}
			else if( mode == TextDataType.Line )
			{
				_OriginalAnchorIndex = -1;
				SetSelection_Line( anchor, caret, view );
				_LastSelectionMode = TextDataType.Line;
			}
			else if( mode == TextDataType.Words )
			{
				ClearLineSelectionData();
				SetSelection_Words( anchor, caret );
				_LastSelectionMode = TextDataType.Words;
			}
			else
			{
				ClearLineSelectionData();
				_OriginalAnchorIndex = -1;
				SetSelection_Normal( anchor, caret );
				_LastSelectionMode = TextDataType.Normal;
			}
		}

		/// <summary>
		/// Distinguishes whether specified index is in selection or not.
		/// </summary>
		public bool IsInSelection( int index )
		{
			foreach( Range r in _Document.Selections )
				if( r.Begin <= index && index <= r.End )
					return true;

			return false;
		}
		#endregion

		#region Internal Logic
		void SetSelection_Rect( int anchor, int caret, IView view )
		{
			// Calculate ranges to be selected newly
			Point anchorPos = view.GetVirPosFromIndex( anchor );
			Point caretPos = view.GetVirPosFromIndex( caret );
			Range[] ranges = MakeRectSelectRanges(
					view,
					Utl.MakeRectFromTwoPoints(anchorPos, caretPos),
					(anchorPos.X < caretPos.X)
				);

			// Apply new selection ranges
			Selections oldSelections = _Selections.Clone();
			_Selections.Ranges.Clear();
			_Selections.Ranges.AddRange( ranges );
			if( 0 < ranges.Length )
			{
				if( ranges[0].Begin <= caret && caret <= ranges[0].End ) // [*]
					_Selections.LastRangeIndex = 0;
				else
					_Selections.LastRangeIndex = ranges.Length - 1;
			}

			// set selection
			_Document.InvokeSelectionChanged( oldSelections,
											  false );
		}

		void SetSelection_Line( int anchor, int caret, IView view )
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
				anchor = view.GetLineHeadIndex( fromLineIndex );
				if( fromLineIndex+1 < view.LineCount )
				{
					caret = view.GetLineHeadIndex( fromLineIndex + 1 );
				}
				else
				{
					caret = _Document.Length;
				}
				_LineSelectionAnchor1 = anchor;
				_LineSelectionAnchor2 = anchor;
			}
			else if( _LineSelectionAnchor1 < caret )
			{
				//-- selecting to the line (or after) where selection started --
				// select between head of the starting line and the end of the destination line
				anchor = view.GetLineHeadIndexFromCharIndex( _LineSelectionAnchor1 );
				if( Document.Utl.IsLineHead(_Document, view, caret) == false )
				{
					toLineIndex = view.GetLineIndexFromCharIndex( caret );
					if( toLineIndex+1 < view.LineCount )
					{
						caret = view.GetLineHeadIndex( toLineIndex + 1 );
					}
					else
					{
						caret = _Document.Length;
					}
				}
			}
			else// if( caret < LineSelectionAnchor )
			{
				//-- selecting to foregoing lines where selection started --
				// select between head of the destination line and end of the starting line
				int anchorLineIndex;

				caret = view.GetLineHeadIndex( toLineIndex );
				anchorLineIndex = view.GetLineIndexFromCharIndex( _LineSelectionAnchor1 );
				if( anchorLineIndex+1 < view.LineCount )
				{
					anchor = view.GetLineHeadIndex( anchorLineIndex + 1 );
				}
				else
				{
					anchor = _Document.Length;
				}
				//DO_NOT//_LineSelectionAnchor1 = anchor;
				_LineSelectionAnchor2 = anchor;
			}

			// apply new selection
			SetSelection_Normal( anchor, caret );
		}

		void SetSelection_Words( int anchor, int caret )
		{
			int waBegin, waEnd; // wa = Word at Anchor
			int wcBegin, wcEnd; // wc = Word at Caret

			// remember original position of anchor 
			_OriginalAnchorIndex = anchor;

			// ensure both selection boundaries are on word boundary
			_Document.GetWordAt( anchor, out waBegin, out waEnd );
			_Document.GetWordAt( caret, out wcBegin, out wcEnd );
			if( anchor <= caret )
			{
				anchor = waBegin;
				caret = wcEnd;
			}
			else
			{
				caret = wcBegin;
				anchor = waEnd;
			}

			// select normally
			SetSelection_Normal( anchor, caret );

		}

		void SetSelection_Normal( int anchor, int caret )
		{
			Selections oldSelections = _Selections.Clone();
			_Selections.Set( new Range(anchor, caret) );
			_Document.InvokeSelectionChanged( oldSelections,
											  false );
		}

		void ClearLineSelectionData()
		{
			_LineSelectionAnchor1 = -1;
			_LineSelectionAnchor2 = -1;
		}
		#endregion

		#region Utilities
		/// <summary>
		/// Calculates and returns text ranges that will be selected by
		/// specified rectangle.
		/// </summary>
		Range[] MakeRectSelectRanges( IView view, Rectangle selRect, bool leftToRight )
		{
			List<Range> selections = new List<Range>();
			Point leftPos = new Point();
			Point rightPos = new Point();
			int y;
			int selRectBottom;

			// Sanitize invalid coordinate values
			selRectBottom = selRect.Bottom;
			if( selRect.Bottom < 0 )
				selRectBottom = 0;

			// Calculate new selection range in each line
			leftPos.X = selRect.Left;
			rightPos.X = selRect.Right;
			y = selRect.Top - (selRect.Top % view.LineSpacing);
			while( y <= selRectBottom )
			{
				// Calculate ranges of substring which is covered by the rectangle
				leftPos.Y = rightPos.Y = y;
				int leftIndex = view.GetIndexFromVirPos( leftPos );
				int rightIndex = view.GetIndexFromVirPos( rightPos );
				if( 1 < selections.Count
					&& selections[selections.Count-1].End == rightIndex )
				{
					break; // reached EOF
				}
				Debug.Assert( view.Document.IsNotDividableIndex(leftIndex) == false );
				Debug.Assert( view.Document.IsNotDividableIndex(rightIndex) == false );

				// Append it to an array
				if( leftToRight )
					selections.Add( new Range(leftIndex, rightIndex) );
				else
					selections.Add( new Range(rightIndex, leftIndex) );

				// Go to next line
				y += view.LineSpacing;
			}

			return selections.ToArray();
		}

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
