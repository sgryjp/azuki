// file: Actions.cs
// brief: Actions for Azuki engine.
//=========================================================
using System;
using System.Drawing;
using System.Text;
using Sgry.Azuki.Utils;
using Debug = System.Diagnostics.Debug;

namespace Sgry.Azuki
{
	/// <summary>
	/// Common interface of actions of Azuki engine
	/// </summary>
	public delegate void ActionProc( IUserInterface ui );

	/// <summary>
	/// A static class containing predefined actions for Azuki.
	/// </summary>
	public static partial class Actions
	{
		#region Delete
		/// <summary>
		/// Deletes one character before caret if nothing was selected, otherwise delete selection.
		/// </summary>
		public static void BackSpace( IUserInterface ui )
		{
			var view = (IViewInternal)ui.View;
			var doc = ui.Document;

			// do nothing if the document is read-only
			if( doc.IsReadOnly )
			{
				Plat.Inst.MessageBeep();
				return;
			}

			// switch logic according to selection state
			if( doc.RectSelectRanges != null )
			{
				//--- case of rectangle selection ---
				using( doc.BeginUndo() )
					doc.DeleteRectSelectText();
				view.Invalidate();
			}
			else if( view.AnchorIndex != view.CaretIndex )
			{
				//--- case of normal selection ---
				doc.Replace( String.Empty );
			}
			else
			{
				//--- case of no selection ---
				int delLen;
				int caret = view.CaretIndex;

				// if the caret is at document head, there is no chars to delete
				if( caret <= 0 )
				{
					Plat.Inst.MessageBeep();
					return;
				}

				if( ui.UnindentsWithBackspace
					&& 0 <= caret-1
					&& (caret == doc.Length || TextUtil.IsEolChar(doc[caret]))
					&& 0 <= " \r\x3000".IndexOf(doc[caret-1]) )
				{
					// Remove whitespace characters to previous tab-stop
					var caretPos = view.GetVirtualPos( caret );
					int x;
					int nextX = 0;
					do
					{
						x = nextX;
						nextX = view.NextTabStopX( x );
					}
					while( nextX < caretPos.X );
					int newLineEnd = view.GetCharIndex( new Point(x, caretPos.Y) );
					delLen = 0;
					for( int i=caret-1; newLineEnd <= i; i-- )
					{
						if( " \r\x3000".IndexOf(doc[i]) < 0 )
							break;
						delLen++;
					}
				}
				else
				{
					// Avoid dividing an undividable sequences such as CR-LF, surrogate pairs,
					// variation sequences. But if it's a combining character sequence (which isn't a
					// variation sequence), delete just one character.
					int prevIndex = doc.PrevGraphemeClusterIndex( caret );
					if( 0 <= prevIndex && 1 < caret - prevIndex
						&& doc.IsCombiningCharacter(prevIndex+1)
						&& !doc.IsVariationSelector(prevIndex+1) )
					{
						delLen = 1;
					}
					else
					{
						delLen = caret - prevIndex;
					}
				}

				// delete char(s).
				doc.Replace( String.Empty, caret-delLen, caret );
			}

			// update desired column
			if( ui.UsesStickyCaret == false )
			{
				view.SetDesiredColumn();
			}
			view.ScrollToCaret();
		}
		
		/// <summary>
		/// Deletes one word before caret if nothing was selected, otherwise delete selection.
		/// </summary>
		public static void BackSpaceWord( IUserInterface ui )
		{
			var view = ui.View;
			var doc = ui.Document;

			// do nothing if the document is read-only
			if( doc.IsReadOnly )
			{
				Plat.Inst.MessageBeep();
				return;
			}

			// switch logic according to selection state
			if( doc.RectSelectRanges != null )
			{
				//--- case of rectangle selection ---
				using( doc.BeginUndo() )
					doc.DeleteRectSelectText();
				view.Invalidate();
			}
			else if( view.AnchorIndex != view.CaretIndex )
			{
				//--- case of normal selection ---
				doc.Replace( String.Empty );
			}
			else
			{
				//--- case of no selection ---
				
				// if the caret is at document head, there is no chars to delete
				if( view.CaretIndex <= 0 )
				{
					Plat.Inst.MessageBeep();
					return;
				}

				// delete between previous word start position and the caret position
				int prevWordIndex = CaretMoveLogic.Calc_PrevWord( view );
				doc.Replace( String.Empty, prevWordIndex, view.CaretIndex );
			}

			// update desired column
			if( ui.UsesStickyCaret == false )
			{
				view.SetDesiredColumn();
			}
			view.ScrollToCaret();
		}
		
		/// <summary>
		/// Deletes one character after caret if nothing was selected, otherwise delete selection.
		/// </summary>
		public static void Delete( IUserInterface ui )
		{
			var view = ui.View;
			var doc = ui.Document;

			// do nothing if the document is read-only
			if( doc.IsReadOnly )
			{
				Plat.Inst.MessageBeep();
				return;
			}

			// switch logic according to selection state
			if( doc.RectSelectRanges != null )
			{
				//--- case of rectangle selection ---
				using( doc.BeginUndo() )
					doc.DeleteRectSelectText();
				view.Invalidate();
			}
			else if( view.AnchorIndex != view.CaretIndex )
			{
				//--- case of normal selection ---
				doc.Replace( String.Empty );
			}
			else
			{
				//--- case of no selection ---
				int begin = view.CaretIndex;
				int end = begin + 1;

				// if the caret is at document end, there is no chars to delete
				if( doc.Length <= view.CaretIndex )
				{
					Plat.Inst.MessageBeep();
					return;
				}

				// avoid dividing a CR-LF, a surrogate pair,
				// or a combining character sequence
				while( doc.IsUndividableIndex(end) )
					end++;

				// delete char(s).
				doc.Replace( String.Empty, begin, end );
			}

			// update desired column
			if( ui.UsesStickyCaret == false )
			{
				view.SetDesiredColumn();
			}
			view.ScrollToCaret();
		}

		/// <summary>
		/// Deletes one word after caret if nothing was selected, otherwise delete selection.
		/// </summary>
		public static void DeleteWord( IUserInterface ui )
		{
			var view = ui.View;
			var doc = ui.Document;

			// do nothing if the document is read-only
			if( doc.IsReadOnly )
			{
				Plat.Inst.MessageBeep();
				return;
			}

			// switch logic according to selection state
			if( doc.RectSelectRanges != null )
			{
				//--- case of rectangle selection ---
				using( doc.BeginUndo() )
					doc.DeleteRectSelectText();
				view.Invalidate();
			}
			else if( view.AnchorIndex != view.CaretIndex )
			{
				//--- case of normal selection ---
				doc.Replace( String.Empty );
			}
			else
			{
				//--- case of no selection ---
				int nextWordIndex = CaretMoveLogic.Calc_NextWord( view );
				if( nextWordIndex == doc.Length && view.CaretIndex == nextWordIndex )
				{
					Plat.Inst.MessageBeep();
					return;
				}

				// delete char(s).
				doc.Replace( String.Empty, view.CaretIndex, nextWordIndex );
			}

			// update desired column
			if( ui.UsesStickyCaret == false )
			{
				view.SetDesiredColumn();
			}
			view.ScrollToCaret();
		}
		#endregion

		#region Clipboard
		/// <summary>
		/// Cuts currently selected text (copies it to the system clipboard and removes it).
		/// </summary>
		/// <remarks>
		/// <para>
		/// If there are selected characters in a document, the characters will be copied to the
		/// system clipboard and be removed from the document. If nothing is selected,
		/// <see cref="IUserInterface.CopyLineWhenNoSelection"/> property determines the action. If
		/// the property is true, the line where the caret is at will be cut. Otherwise, nothing
		/// happens.
		/// </para>
		/// </remarks>
		/// <seealso cref="IUserInterface.CopyLineWhenNoSelection"/>
		public static void Cut( IUserInterface ui )
		{
			var doc = ui.Document;
			var view = ui.View;

			// do nothing if the document is read-only
			if( doc.IsReadOnly )
			{
				Plat.Inst.MessageBeep();
				return;
			}

			// is there any selection?
			var text = ui.GetSelectedText();
			if( 0 < text.Length )
			{
				// there is selection.
				
				// delete selected text
				if( doc.RectSelectRanges != null )
				{
					using( doc.BeginUndo() )
						doc.DeleteRectSelectText();
					Plat.Inst.SetClipboardText( text, TextDataType.Rectangle );
				}
				else
				{
					doc.Replace( String.Empty );
					Plat.Inst.SetClipboardText( text, TextDataType.Normal );
				}
			}
			else
			{
				// no selection.
				if( !ui.CopyLineWhenNoSelection )
				{
					return; // nothing should be done
				}

				// if the user prefers to use cuting/copying without selection
				// to cut/copy current line, change the begin/end position to line head/end
				var line = doc.RawLines.AtOffset( view.CaretIndex );
				Plat.Inst.SetClipboardText( line.Text, TextDataType.Line );
				doc.Replace( String.Empty, line.Begin, line.End );
			}
			
			if( ui.UsesStickyCaret == false )
			{
				ui.View.SetDesiredColumn();
			}
		}

		/// <summary>
		/// Copies currently selected text to clipboard.
		/// </summary>
		/// <remarks>
		/// <para>
		/// If there are selected characters in a document, the characters will be copied to the
		/// system clipboard. If nothing is selected,
		/// <see cref="IUserInterface.CopyLineWhenNoSelection"/> property determines the action. If
		/// the property is true, the line where the caret is at will be copied. Otherwise, nothing
		/// happens.
		/// </para>
		/// </remarks>
		/// <seealso cref="IUserInterface.CopyLineWhenNoSelection"/>
		public static void Copy( IUserInterface ui )
		{
			var doc = ui.Document;
			var view = ui.View;
			
			// is there any selection?
			var text = ui.GetSelectedText();
			if( 0 < text.Length )
			{
				// there is selection.
				// copy selected text.
				if( doc.RectSelectRanges != null )
					Plat.Inst.SetClipboardText( text, TextDataType.Rectangle );
				else
					Plat.Inst.SetClipboardText( text, TextDataType.Normal );
			}
			else
			{
				// no selection.
				if( !ui.CopyLineWhenNoSelection )
				{
					return; // nothing should be done
				}

				// if the user prefers to use cuting/copying without selection
				// to cut/copy current line, change the begin/end position to line head/end
				var line = doc.RawLines.AtOffset( view.CaretIndex );
				Plat.Inst.SetClipboardText( line.Text, TextDataType.Line );
			}
		}

		/// <summary>
		/// Pastes clipboard content at where the caret is at.
		/// </summary>
		public static void Paste( IUserInterface ui )
		{
			var view = ui.View;
			var doc = ui.Document;
			TextDataType dataType;

			// do nothing if the document is read-only
			if( doc.IsReadOnly )
			{
				Plat.Inst.MessageBeep();
				return;
			}

			// get clipboard content
			var clipboardText = Plat.Inst.GetClipboardText( out dataType );
			if( clipboardText == null )
			{
				return;
			}

			// limit the content in a single line if it's in single line mode
			if( ui.IsSingleLineMode )
			{
				int eolIndex = clipboardText.IndexOfAny( "\r\n".ToCharArray() );
				if( 0 <= eolIndex )
				{
					clipboardText = clipboardText.Remove( eolIndex, clipboardText.Length-eolIndex );
				}
			}
			
			using( doc.BeginUndo() )
			{
				int begin, end;
				int insertIndex;

				// delete currently selected text before insertion
				doc.GetSelection( out begin, out end );
				if( doc.RectSelectRanges != null )
				{
					//--- case of rectangle selection ---
					// delete selected text
					doc.DeleteRectSelectText();
					view.Invalidate();
				}
				else if( begin != end )
				{
					//--- case of normal selection ---
					// delete selected text
					doc.Replace( "" );
				}

				// paste according type of the text data
				if( dataType == TextDataType.Rectangle )
				{
					//--- rectangle text data ---
					// Insert every row at same column position
					var insertPos = view.GetVirtualPos( view.CaretIndex );
					var rowBegin = 0;
					var rowEnd = TextUtil.NextLineHead( clipboardText, rowBegin );
					while( 0 <= rowEnd )
					{
						string rowText;

						// get this row content
						if( clipboardText[rowEnd-1] == '\n' )
						{
							if( clipboardText[rowEnd-2] == '\r' )
								rowText = clipboardText.Substring( rowBegin, rowEnd-rowBegin-2 );
							else
								rowText = clipboardText.Substring( rowBegin, rowEnd-rowBegin-1 );
						}
						else if( clipboardText[rowEnd-1] == '\r' )
						{
							rowText = clipboardText.Substring( rowBegin, rowEnd-rowBegin-1 );
						}
						else
						{
							rowText = clipboardText.Substring( rowBegin, rowEnd-rowBegin );
						}

						// pad tabs if needed
						var padding = UiImpl.GetNeededPaddingChars( ui, insertPos, false );

						// insert this row
						insertIndex = view.GetCharIndex( insertPos );
						doc.Replace( padding + rowText, insertIndex, insertIndex );

						// goto next line
						insertPos.Y += ui.LineSpacing;
						rowBegin = rowEnd;
						rowEnd = TextUtil.NextLineHead( clipboardText, rowBegin );
					}
				}
				else
				{
					//--- normal or line text data ---
					// calculate insertion index
					insertIndex = begin;
					if( dataType == TextDataType.Line )
					{
						// make the insertion point to caret line head if it is line data type
						insertIndex = doc.Lines.AtOffset( begin ).Begin;
					}

					// insert
					doc.Replace( clipboardText, insertIndex, insertIndex );
				}
			}

			// move caret
			if( ui.UsesStickyCaret == false )
			{
				view.SetDesiredColumn();
			}
			ui.ScrollToCaret();
		}
		#endregion

		#region Undo / Reduo
		/// <summary>
		/// Undos an action.
		/// </summary>
		public static void Undo( IUserInterface ui )
		{
			var view = (IViewInternal)ui.View;
			if( view.Document.CanUndo == false
				|| view.Document.IsReadOnly )
			{
				return;
			}

			// undo
			view.Document.Undo();
			if( ui.UsesStickyCaret == false )
			{
				view.SetDesiredColumn();
			}
			view.ScrollToCaret();

			// redraw graphic of dirt-bar
			if( ui.ShowsDirtBar )
			{
				view.Invalidate( view.DirtBarRect );
			}
		}

		/// <summary>
		/// Redos an action.
		/// </summary>
		public static void Redo( IUserInterface ui )
		{
			var view = ui.View;
			var doc = ui.Document;
			if( doc.CanRedo == false
				|| doc.IsReadOnly )
			{
				return;
			}

			// redo
			doc.Redo();
			if( ui.UsesStickyCaret == false )
			{
				view.SetDesiredColumn();
			}
			view.ScrollToCaret();
		}
		#endregion

		#region Misc.
		/// <summary>
		/// Toggles overwrite mode.
		/// </summary>
		public static void ToggleOverwriteMode( IUserInterface ui )
		{
			ui.IsOverwriteMode = !ui.IsOverwriteMode;
		}

		/// <summary>
		/// Toggles overwrite mode.
		/// </summary>
		public static void ToggleRectSelectMode( IUserInterface ui )
		{
			if( ui.SelectionMode != TextDataType.Rectangle )
				ui.SelectionMode = TextDataType.Rectangle;
			else
				ui.SelectionMode = TextDataType.Normal;
		}
		
		/// <summary>
		/// Scrolls down one line.
		/// </summary>
		public static void ScrollDown( IUserInterface ui )
		{
			ui.View.Scroll( 1 );
		}
		
		/// <summary>
		/// Scrolls up one line.
		/// </summary>
		public static void ScrollUp( IUserInterface ui )
		{
			ui.View.Scroll( -1 );
		}

		/// <summary>
		/// Refreshes view and force to redraw text area.
		/// </summary>
		public static void Refresh( IUserInterface ui )
		{
			ui.View.Invalidate();
		}
		#endregion

		#region Line editing
		/// <summary>
		/// Inserts a new line above the caret.
		/// </summary>
		public static void BreakPreviousLine( IUserInterface ui )
		{
			var view = ui.View;
			var doc = ui.Document;

			if( doc.IsReadOnly || ui.IsSingleLineMode )
				return;

			// Insert an EOL code at the beginning of the previous line
			var caretLine = view.Lines.AtOffset( view.CaretIndex );
			int insIndex = (0 == caretLine.LineIndex) ? 0
													  : view.Lines[ caretLine.LineIndex-1 ].End;
			doc.SetSelection( insIndex, insIndex );
			ui.HandleTextInput( "\n" );
			ui.ScrollToCaret();
		}

		/// <summary>
		/// Inserts a new line below the caret.
		/// </summary>
		public static void BreakNextLine( IUserInterface ui )
		{
			var view = ui.View;
			var doc = ui.Document;

			if( doc.IsReadOnly || ui.IsSingleLineMode )
				return;

			// Insert an EOL code at the end of current line
			var caretLine = view.Lines.AtOffset( view.CaretIndex );
			doc.SetSelection( caretLine.End, caretLine.End );
			ui.HandleTextInput( "\n" );
			ui.ScrollToCaret();
		}

		/// <summary>
		/// Increase indentation level of selected lines.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This action indents all selected lines at once. The character(s) to
		/// be used for indentation will be a tab (U+0009) if
		/// <see cref="Sgry.Azuki.IUserInterface.UsesTabForIndent">
		/// IUserInterface.UsesTabForIndent</see> property is true, otherwise
		/// a sequence of space characters (U+0020). The number of space
		/// characters which will be used for indentation is determined by <see
		/// cref="Sgry.Azuki.IUserInterface.TabWidth">IUserInterface.TabWidth
		/// </see> property value.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.IUserInterface.UsesTabForIndent">
		/// IUserInterface.UsesTabForIndent property
		/// </seealso>
		/// <seealso cref="Sgry.Azuki.IUserInterface.TabWidth">
		/// IUserInterface.TabWidth property
		/// </seealso>
		public static void BlockIndent( IUserInterface ui )
		{
			var doc = ui.Document;
			string indentChars;
			int beginL, endL;

			// if read-only document, do nothing
			if( doc.IsReadOnly )
			{
				return;
			}

			// get range of the selected lines
			doc.GetSelectedLineRange( out beginL, out endL );

			// prepare indent character
			indentChars = ui.UsesTabForIndent ? "\t"
											  : new String( ' ', ui.TabWidth );

			// indent each lines
			using( doc.BeginUndo() )
			{
				for( int i=beginL; i<endL; i++ )
				{
					var line = doc.Lines[i];
					if( 0 < line.Length )
						doc.Replace( indentChars, line.Begin, line.Begin );
				}
			}

			// select whole range
			doc.SetSelection( doc.Lines[beginL].Begin,
							  doc.RawLines[ Math.Max(0, endL-1) ].End );
		}

		/// <summary>
		/// Decrease indentation level of selected lines.
		/// </summary>
		public static void BlockUnIndent( IUserInterface ui )
		{
			var view = ui.View;
			var doc = ui.Document;
			int beginL, endL;

			// if read-only document, do nothing
			if( doc.IsReadOnly )
			{
				return;
			}
			
			// get range of the selected lines
			doc.GetSelectedLineRange( out beginL, out endL );

			// unindent each lines
			using( doc.BeginUndo() )
			{
				for( int i=beginL; i<endL; i++ )
				{
					int lineHead = doc.Lines[i].Begin;
					if( doc.Length <= lineHead )
					{
						// no more chars available. exit.
						break;
					}
					else if( doc[lineHead] == '\t' )
					{
						// there is a tab. remove it
						doc.Replace( String.Empty, lineHead, lineHead+1 );
					}
					else if( doc[lineHead] == ' ' )
					{
						// there is a space.
						// remove them until the count reaches to the tab-width
						int n = 0;
						while( doc[lineHead] == ' ' && n < view.TabWidth )
						{
							doc.Replace( String.Empty, lineHead, lineHead+1 );
							n++;
						}
					}
				}
			}

			// select whole range
			doc.SetSelection( doc.Lines[beginL].Begin,
							  doc.RawLines[ Math.Max(0, endL-1) ].End );
		}
		#endregion

		#region Blank Operation
		/// <summary>
		/// Removes spaces at the end of each selected lines.
		/// </summary>
		public static void TrimTrailingSpace( IUserInterface ui )
		{
			Debug.Assert( ui != null );
			Debug.Assert( ui.Document != null );

			int beginL, endL;
			var doc = ui.Document;

			// Determine target lines
			doc.GetSelectedLineRange( out beginL, out endL );

			// Trim
			using( doc.BeginUndo() )
			{
				for( int i=beginL; i<endL; i++ )
				{
					var line = doc.Lines[i];
					int index = line.End;
					while( line.Begin <= index-1 && Char.IsWhiteSpace(doc[index-1]) )
					{
						index--;
					}
					if( index < line.End )
					{
						doc.Replace( "", index, line.End );
					}
				}
			}
		}

		/// <summary>
		/// Removes spaces at the beginning of each selected lines.
		/// </summary>
		public static void TrimLeadingSpace( IUserInterface ui )
		{
			Debug.Assert( ui != null );
			Debug.Assert( ui.Document != null );

			int beginL, endL;
			var doc = ui.Document;

			// Determine target lines
			doc.GetSelectedLineRange( out beginL, out endL );

			// Trim
			using( doc.BeginUndo() )
			{
				for( int i=beginL; i<endL; i++ )
				{
					var line = doc.Lines[i];
					int index = line.Begin;
					while( index < line.End && Char.IsWhiteSpace(doc[index]) )
					{
						index++;
					}
					if( line.Begin < index )
					{
						doc.Replace( "", line.Begin, index );
					}
				}
			}
		}

		/// <summary>
		/// Converts tab characters to space characters.
		/// </summary>
		public static void ConvertTabsToSpaces( IUserInterface ui )
		{
			ForEachSelection( ui, ConvertTabsToSpaces );
		}

		static void ConvertTabsToSpaces( IUserInterface ui,
										 int begin, int end, ref int delta )
		{
			int tabCount = 0;
			var doc = ui.Document;
			var text = new StringBuilder( 1024 );

			for( int i=begin; i<end; i++ )
			{
				if( doc[i] == '\t' )
				{
					tabCount++;
					var spaces = UiImpl.GetTabEquivalentSpaces( ui, i );
					text.Append( spaces );
				}
				else
				{
					text.Append( doc[i] );
				}
			}

			// Replace with current content
			if( 0 < tabCount )
			{
				doc.Replace( text.ToString(), begin, end );
			}

			delta += text.Length - (end - begin);
		}

		/// <summary>
		/// Converts space characters to tab characters.
		/// </summary>
		public static void ConvertSpacesToTabs( IUserInterface ui )
		{
			ForEachSelection( ui, ConvertSpacesToTabs );
		}

		/// <summary>
		/// Convertes sequences of two or more spaces in selected text with
		/// tab characters as much as possible.
		/// </summary>
		static void ConvertSpacesToTabs( IUserInterface ui,
										 int begin, int end,
										 ref int indexDelta )
		{
			var view = (IViewInternal)ui.View;
			var doc = ui.Document;
			var text = new StringBuilder( 1024 );
			int cvtCount = 0;

			for( int i=begin; i<end; i++ )
			{
				int prevCvtCount = cvtCount;
				if( doc[i] == ' ' )
				{
					int x = view.GetVirtualPos( i ).X;
					int nextTabStopX = view.NextTabStopX( x );
					int neededCount = (nextTabStopX - x) / view.SpaceWidthInPx;
					if( 2 <= neededCount && i+neededCount <= end )
					{
						string str = doc.GetText( i, i+neededCount );
						if( str.TrimStart(' ') == "" )
						{
							text.Append( "\t" );
							i += neededCount - 1;
							indexDelta -= neededCount - 1;
							cvtCount++;
						}
					}
				}

				if( prevCvtCount == cvtCount )
				{
					text.Append( doc[i] );
				}
			}

			// Replace with current content
			if( 0 < cvtCount )
			{
				doc.Replace( text.ToString(), begin, end );
			}
		}
		#endregion

		#region Utilities
		delegate void ForEachSelectionPredicate( IUserInterface ui,
												 int begin, int end,
												 ref int indexDelta );

		static void ForEachSelection( IUserInterface ui,
									  ForEachSelectionPredicate predicate )
		{
			Debug.Assert( ui != null );
			Debug.Assert( ui.Document != null );

			var doc = ui.Document;
			int delta = 0;
			int begin, end;

			if( doc.RectSelectRanges != null )
			{
				using( doc.BeginUndo() )
				{
					for( int i=0; i<doc.RectSelectRanges.Length; i+=2 )
					{
						begin = doc.RectSelectRanges[i] + delta;
						end = doc.RectSelectRanges[i+1] + delta;
						predicate( ui, begin, end, ref delta );
					}
				}

				int lastIndex = doc.RectSelectRanges.Length - 1;
				doc.SelectionMode = TextDataType.Rectangle;
				doc.SelectionManager.SetSelection(
						doc.RectSelectRanges[0],
						doc.RectSelectRanges[lastIndex] + delta,
						(IViewInternal)ui.View );
			}
			else
			{
				doc.GetSelection( out begin, out end );
				predicate( ui, begin, end, ref delta );
			}
		}
		#endregion
	}
}
