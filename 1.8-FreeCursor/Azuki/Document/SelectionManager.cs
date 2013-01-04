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
		Selections _Selections;
		int? _DesiredAnchor;
		TextDataType _SelectionMode = TextDataType.Normal;
		#endregion

		public int? DesiredAnchor
		{
			get{ return _DesiredAnchor; }
			set{ _DesiredAnchor = value; }
		}

		public TextDataType SelectionMode
		{
			get{ return _SelectionMode; }
			set{ _SelectionMode = value; }
		}

		#region Init / Dispose
		public SelectionManager( Document doc )
		{
			Debug.Assert( doc != null );
			_Document = doc;
			_Selections = new Selections();
		}
		#endregion

		#region Selection State
		public Selections Selections
		{
			get{ return _Selections; }
		}

		public void SetSelection( int anchor, int caret,
								  IView view, TextDataType mode )
		{
			Debug.Assert( 0 <= anchor && anchor <= _Document.Length,
						  "Parameter 'anchor' out of range (anchor:" + anchor
						  + ", Document.Length:" + _Document.Length + ")" );
			Debug.Assert( 0 <= caret && caret <= _Document.Length,
						  "Parameter 'caret' out of range (anchor:" + anchor
						  + ", Document.Length:" + _Document.Length + ")" );

			// ensure that document can be divided at given index
			Document.Utl.ConstrainIndex( _Document, ref anchor, ref caret );

			// set selection
			_Selections.LastRangeIndex = 0;
			if( mode == TextDataType.Rectangle )
			{
				SetSelection_Rect( anchor, caret, view );
			}
			else if( mode == TextDataType.Line )
			{
				SetSelection_Line( anchor, caret, view );
			}
			else if( mode == TextDataType.Words )
			{
				SetSelection_Words( anchor, caret );
			}
			else
			{
				SetSelection_Normal( anchor, caret );
			}
		}

		/// <summary>
		/// Distinguishes whether specified index is in selection or not.
		/// </summary>
		public bool IsInSelection( int index )
		{
			foreach( Range r in _Selections.Ranges )
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
			Range range = new Range( 0, 0 );
			if( anchor <= caret )
			{
				int toLineIndex = view.GetLineIndexFromCharIndex( caret );
				if( toLineIndex+1 < view.LineCount-1 )
					range.To = view.GetLineHeadIndex( toLineIndex+1 );
				else
					range.To = view.Document.Length;

				range.From = view.GetLineHeadIndexFromCharIndex( anchor );
			}
			else
			{
				range.To = view.GetLineHeadIndexFromCharIndex( caret );

				int fromLineIndex = view.GetLineIndexFromCharIndex( anchor );
				if( fromLineIndex+1 < view.LineCount-1 )
					range.From = view.GetLineHeadIndex( fromLineIndex+1 );
				else
					range.From = view.Document.Length;
			}

			SetSelection_Normal( range.From, range.To );
		}

		void SetSelection_Words( int anchor, int caret )
		{
			int waBegin, waEnd; // wa = Word at Anchor
			int wcBegin, wcEnd; // wc = Word at Caret

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
