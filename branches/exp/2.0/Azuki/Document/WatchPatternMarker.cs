﻿// file: WatchPatternMarker.cs
// brief: a singleton class which marks up watching text patterns in document.
// author: YAMAMOTO Suguru
// update: 2011-08-20
//=========================================================
using System;
using System.Text.RegularExpressions;
using Debug = System.Diagnostics.Debug;

namespace Sgry.Azuki
{
	/// <summary>
	/// Parser to mark up specific watching text patterns in Azuki document.
	/// </summary>
	class WatchPatternMarker
	{
		Document _Document;
		int _LastDrawnLogicalLineIndex;

		#region Init / Dispose
		public WatchPatternMarker( Document doc )
		{
			_Document = doc;
			_LastDrawnLogicalLineIndex = 0;
		}
		#endregion

		#region Event handlers
		public void HandleContentChanged( object sender, ContentChangedEventArgs e )
		{
			UiImpl ui = (UiImpl)sender;
			Document doc = _Document;
			int lineIndex;
			int lineHead, lineEnd;
			bool shouldBeRedrawn;

			Debug.Assert( ui.Document == _Document );

			// update marking in this line
			lineIndex = doc.GetLineIndexFromCharIndex( e.Index );
			shouldBeRedrawn = MarkOneLine( doc, lineIndex, true );
			if( shouldBeRedrawn )
			{
				// update entire graphic of the logical line
				// if marking bits associated with any character was changed
				lineHead = doc.GetLineHeadIndex( lineIndex );
				lineEnd = lineHead + doc.GetLineRange( lineIndex ).Length;
				ui.View.Invalidate( lineHead, lineEnd );
			}
		}

		public void UI_LineDrawing( object sender, LineDrawEventArgs e )
		{
			IUserInterface ui = (IUserInterface)sender;
			Debug.Assert( ui.Document == _Document );

			// Mark up all URIs in the logical line
			int scrernLineHeadIndex = ui.View.GetLineHeadIndex( e.LineIndex );
			int logicalLineIndex = ui.Document.GetLineIndexFromCharIndex( scrernLineHeadIndex );
			if( logicalLineIndex == _LastDrawnLogicalLineIndex )
			{
				// Skip marking already marked line
				// (more optimization can be done though)
				return;
			}
			_LastDrawnLogicalLineIndex = logicalLineIndex;

			e.ShouldBeRedrawn = MarkOneLine( _Document,
											 logicalLineIndex,
											 true );
		}
		#endregion

		#region Marking logic
		/// <summary>
		/// Marks patterns in a logical line.
		/// </summary>
		/// <returns>Whether specified line should be redrawn or not.</returns>
		bool MarkOneLine( Document doc, int logicalLineIndex, bool marks )
		{
			Debug.Assert( doc != null );
			Debug.Assert( 0 <= logicalLineIndex && logicalLineIndex <= doc.Lines.Count,
				"logicalLineIndex is out of valid range. (value:"+logicalLineIndex+", doc.Lines.Count:"+doc.Lines.Count+")" );

			int lineHead;
			string line;
			MatchCollection matches;
			int count = 0;
			int lastMarkedIndex;

			if( logicalLineIndex == doc.Lines.Count )
				return false;

			lineHead = doc.GetLineHeadIndex( logicalLineIndex );
			line = doc.GetLineContent( logicalLineIndex );
			lastMarkedIndex = lineHead;
			foreach( WatchPattern wp in doc.WatchPatterns )
			{
				// do nothing if invalid pattern was set
				if( wp.Pattern == null
					|| wp.Pattern.ToString() == String.Empty )
				{
					continue;
				}

				// mark all matched parts
				matches = wp.Pattern.Matches( line );
				foreach( Match match in matches )
				{
					// skip if length of this matched part is zero
					// (ex. length of matching result of regular expression '^' is zero.)
					if( match.Length <= 0 )
						continue;

					// unmark before the part
					count += doc.Unmark( lastMarkedIndex,
										 lineHead + match.Index,
										 wp.MarkingID ) ? 1 : 0;

					// mark the part
					count += doc.Mark( lineHead + match.Index,
									   lineHead + match.Index + match.Length,
									   wp.MarkingID ) ? 1 : 0;

					// remember lastly marked position
					lastMarkedIndex = lineHead + match.Index + match.Length;
				}

				// unmark remaining part of the line
				count += doc.Unmark( lastMarkedIndex,
									 lineHead + line.Length,
									 wp.MarkingID ) ? 1 : 0;
			}

			return (0 < count);
		}
		#endregion
	}
}
