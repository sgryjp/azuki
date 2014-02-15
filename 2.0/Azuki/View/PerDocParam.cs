// file: PerDocParam.cs
// brief: Parameters associated with each document used internally by View.
//=========================================================
using System;

namespace Sgry.Azuki
{
	/// <summary>
	/// Parameters associated with each document used internally by View.
	/// </summary>
	/// <remarks>
	/// This class is a set of parameters that are dependent on each document
	/// but are not parameters about document content
	/// (mainly used for drawing text or user interaction.)
	/// </remarks>
	class PerDocParam
	{
		public WeakReference WeakRef{ get; private set; }

		#region Init / Dispose
		public PerDocParam( Document doc )
		{
			WeakRef = new WeakReference( doc );
			LastFontHashCode = 0;
			LastTextAreaWidth = 0;
			DesiredColumnX = 0;
			SLHI = new GapBuffer<int>( 128, 128 ) { 0 };
		}
		#endregion

		#region View common properties
		int _FirstVisibleLine = 0;
		int _ScrollPosX = 0;
		int _MaxLineNumber = 9999;

		/// <summary>
		/// Gets or sets current X-coordinate of the "desired column."
		/// </summary>
		public int DesiredColumnX { get; set; }

		/// <summary>
		/// Gets or sets index of the line which is displayed at top of the view.
		/// </summary>
		public int FirstVisibleLine
		{
			get{ return _FirstVisibleLine; }
			set
			{
				if( value < 0 )
					throw new ArgumentException( "FirstVisibleLine must be greater than zero (set value: "+value+")" );
				_FirstVisibleLine = value;
			}
		}

		/// <summary>
		/// Gets or sets x-coordinate of the view's origin currently displayed.
		/// </summary>
		public int ScrollPosX
		{
			get{ return _ScrollPosX; }
			set
			{
				if( value < 0 )
					throw new ArgumentException( "ScrollPosX must be greater than zero (set value: "+value+")" );
				_ScrollPosX = value;
			}
		}

		/// <summary>
		/// Gets or sets maximum line number.
		/// </summary>
		public int MaxLineNumber
		{
			get{ return _MaxLineNumber; }
			set{ _MaxLineNumber = value; }
		}
		#endregion

		#region PropView specific parameters
		public int PrevAnchorLine { get; set; }
		public int PrevCaretLine { get; set; }

		/// <summary>
		/// Gets or sets lastly drawn horizontal ruler bar position.
		/// </summary>
		public int PrevHRulerVirX { get; set; }

		#endregion

		#region PropWrapView specific parameters
		DateTime _LastModifiedTime = DateTime.MinValue;

		public GapBuffer<int> SLHI
		{
			get;
			private set;
		}

		public int LastTextAreaWidth { get; set; }

		public int LastFontHashCode { get; set; }

		public DateTime LastModifiedTime
		{
			get{ return _LastModifiedTime; }
			set{ _LastModifiedTime = value; }
		}
		#endregion

		#region UiImpl
		// Wherther the document contains any characters which should be highlighted.
		public bool H_IsInvalid = false;

		// Beginning position of the range to be highlighted.
		public int H_InvalidRangeBegin = Int32.MaxValue;

		// Ending position of the range to be highlighted.
		public int H_InvalidRangeEnd = 0;

		// Beginning position of the range which was already highlighted.
		public int H_ValidRangeBegin = 0;

		// Ending position of the range which was already highlighted.
		public int H_ValidRangeEnd = 0;

		// Index of a matched brackets; Index of a bracket after caret, counterpart of
		// it, a bracket before caret, and counterpart of it.
		public readonly int[] MatchedBracketIndexes = {-1, -1, -1, -1};
		#endregion
	}
}
