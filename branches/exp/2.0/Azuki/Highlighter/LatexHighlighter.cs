using System;
using Sgry.Azuki.Highlighter.Coco.Latex;

namespace Sgry.Azuki.Highlighter
{
	/// <summary>
	/// A highlighter to highlight LaTeX.
	/// </summary>
	class LatexHighlighter : IHighlighter
	{
		HighlightHook _Hook = null;

		readonly GapBuffer<int> _ReparsePoints = new GapBuffer<int>( 64 );
		int _LastDocumentHash;

		#region Properties
		public bool CanUseHook
		{
			get{ return true; }
		}

		public HighlightHook HookProc
		{
			get{ return _Hook; }
			set{ _Hook = value; }
		}
		#endregion

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

			// set re-highlight range
			dirtyRange.Begin = Utl.FindReparsePoint( _ReparsePoints, dirtyRange.Begin );
			//NO_NEED//dirtyRange.End = something

			// highlight with generated parser
			try
			{
				new Parser( doc, dirtyRange.Begin, dirtyRange.End ) {
					_Hook = _Hook,
					_ReparsePoints = _ReparsePoints
				}.Parse();
			}
			catch( FatalError )
			{}

			return dirtyRange;
		}
	}
}
