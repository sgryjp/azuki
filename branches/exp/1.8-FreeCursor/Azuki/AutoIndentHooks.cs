// file: AutoIndentLogic.cs
// brief: Logic around auto-indentation.
//=========================================================
using System;
using System.Text;
using Point = System.Drawing.Point;

namespace Sgry.Azuki
{
	/// <summary>
	/// Hook delegate called every time a character was inserted.
	/// </summary>
	/// <param name="ui">User interface object such as AzukiControl.</param>
	/// <param name="ch">Character about to be inserted.</param>
	/// <returns>
	/// Whether this hook delegate successfully executed or not.
	/// If true, Azuki itself will input nothing.
	/// </returns>
	public delegate bool AutoIndentHook( IUserInterface ui, char ch );

	/// <summary>
	/// Static class containing hook delegates for auto-indentation.
	/// </summary>
	/// <seealso cref="Sgry.Azuki.WinForms.AzukiControl.AutoIndentHook">
	/// AzukiControl.AutoIndentHook property
	/// </seealso>
	public static class AutoIndentHooks
	{
		/// <summary>
		/// Hook delegate to execute basic auto-indentation; indent same amount
		/// of spaces as the previous line.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This member is a hook delegate to execute auto-indentation. This
		/// delegate just copies previous indentation characters on making a
		/// new line.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.WinForms.AzukiControl.AutoIndentHook">
		/// AzukiControl.AutoIndentHook property
		/// </seealso>
		public static readonly AutoIndentHook
			GenericHook = delegate( IUserInterface ui, char ch )
		{
			Document doc = ui.Document;
			StringBuilder str = new StringBuilder();
			int lineHead;

			// do nothing if Azuki is in single line mode
			if( ui.IsSingleLineMode )
			{
				return false;
			}

			// if EOL code was detected, perform indentation
			if( LineLogic.IsEolChar(ch) )
			{
				str.Append( doc.EolCode );

				// get indent chars
				lineHead = doc.GetLineHeadIndexFromCharIndex( doc.CaretIndex );
				for( int i=lineHead; i<doc.CaretIndex; i++ )
				{
					if( doc[i] == ' ' || doc[i] == '\t' || doc[i] == '\x3000' )
						str.Append( doc[i] );
					else
						break;
				}

				// replace selection
				doc.BeginUndo();
				ui.Delete( doc.Selections );
				doc.Replace( str.ToString(), doc.CaretIndex, doc.CaretIndex );
				doc.EndUndo();

				return true;
			}

			return false;
		};

		/// <summary>
		/// Hook delegate to execute auto-indentation for C styled source code.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This member is a hook delegate to execute auto-indentation for C
		/// styled source code. Here 'C style' means that curly brackets are
		/// used to enclose each logical block.
		/// </para>
		/// <para>
		/// Note that if user hits the Enter key on a line that ends with a
		/// closing curly bracket (<c> } </c>), newly generated line will be
		/// indented one more level by inserting additional indent characters.
		///	The additional indent characters will be chosen according to the
		///	value of
		///	<see cref="Sgry.Azuki.WinForms.AzukiControl.UsesTabForIndent">
		///	AzukiControl.UsesTabForIndent</see> property.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.WinForms.AzukiControl.AutoIndentHook">
		/// AzukiControl.AutoIndentHook property
		/// </seealso>
		/// <seealso cref="Sgry.Azuki.WinForms.AzukiControl.UsesTabForIndent">
		/// AzukiControl.UsesTabForIndent property
		/// </seealso>
		public static readonly AutoIndentHook
			CHook = delegate( IUserInterface ui, char ch )
		{
			if( LineLogic.IsEolChar(ch) ) // Enter key
			{
				return CHook_OnEnter( ui, ch );
			}
			else if( ch == '}' )
			{
				return CHook_OnOpenBracket( ui, ch );
			}

			return false;
		};

		static bool CHook_OnEnter( IUserInterface ui, char ch )
		{
			Document doc = ui.Document;
			StringBuilder indentChars = new StringBuilder( 256 );
			bool extraPaddingNeeded = false;

			// do nothing if it's in single line mode
			if( ui.IsSingleLineMode )
				return false;

			using( doc.BeginUndo() )
			{
				// Firstly delete selected text
				ui.Delete( doc.Selections );
				int caret = doc.CaretIndex;

				// Quit if the current line is empty
				int lineHead = doc.GetLineHeadIndexFromCharIndex( caret );
				int lineEnd = lineHead + doc.GetLineLengthFromCharIndex(caret);
				if( lineHead == lineEnd )
				{
					return false;
				}

				// Compose character sequence for indentation
				indentChars.Append( doc.EolCode );
				for( int i=lineHead; i<caret; i++ )
				{
					if( Utl.IsIndentChar(doc[i]) )
						indentChars.Append( doc[i] );
					else
						break;
				}

				// Remove following whitespaces
				if( Utl.IsIndentChar(doc[caret]) )
				{
					int begin = caret, end = caret;
					do
					{
						end++;
					}
					while( Utl.IsIndentChar(doc[end]) );

					doc.Replace( "", begin, end );
				}

				// Determine whether extra padding is needed or not
				if( -1 != Utl.FindPairedBracket_Backward(doc,
														 caret, lineHead,
														 '}', '{')
					&& Utl.IndexOf(doc, '}', caret, lineEnd) == -1 )
				{
					extraPaddingNeeded = true;
				}

				// Insert an EOL code and indentation
				doc.Replace( indentChars.ToString(), caret, caret );
				caret += indentChars.Length;

				// If there is a '{' without pair before caret
				// and is no '}' after caret, add indentation
				if( extraPaddingNeeded )
				{
					// make indentation characters
					string extraPadding;
					Point pos = ui.View.GetVirPosFromIndex( caret );
					pos.X += ui.View.TabWidthInPx;
					extraPadding = UiImpl.GetNeededPaddingChars(ui, pos, true);
					doc.Replace( extraPadding, caret, caret );
					caret += extraPadding.Length;
				}

				ui.Select( caret, caret );

				return true;
			}
		}

		static bool CHook_OnOpenBracket( IUserInterface ui, char ch )
		{
			Document doc = ui.Document;
			StringBuilder indentChars = new StringBuilder( 256 );
			int pairIndex, pairLineHead, pairLineEnd;
			int pairLineIndex;

			// Quit if this line contains a non-whitespace character
			int caret = doc.CaretIndex;
			int lineHead = doc.GetLineHeadIndexFromCharIndex( caret );
			int lineEnd = lineHead + doc.GetLineLengthFromCharIndex( caret );
			for( int i=lineHead; i<lineEnd; i++ )
			{
				if( Utl.IsIndentChar(doc[i]) == false )
				{
					return false; // this line contains a non whitespace ch
				}
			}

			// Find the paired open bracket
			pairIndex = Utl.FindPairedBracket_Backward( doc,
														caret, 0,
														'}', '{' );
			if( pairIndex == -1 )
			{
				return false; // No pair exists. Do nothing.
			}

			// Get indent sequence of the line in which the pair exists
			pairLineIndex = ui.GetLineIndexFromCharIndex( pairIndex );
			pairLineHead = ui.GetLineHeadIndex( pairLineIndex );
			pairLineEnd = pairLineHead + ui.GetLineLength( pairLineIndex );
			for( int i=pairLineHead; i<pairLineEnd; i++ )
			{
				if( Utl.IsIndentChar(doc[i]) )
					indentChars.Append( doc[i] );
				else
					break;
			}

			// Replace indent chars of current line
			indentChars.Append( '}' );
			doc.Replace( indentChars.ToString(), lineHead, caret );

			return true;
		}

		#region Utilities
		static class Utl
		{
			public static bool IsIndentChar( char ch )
			{
				return (Char.IsWhiteSpace(ch)
						&& LineLogic.IsEolChar(ch) == false);
			}

			public static int IndexOf( Document doc, char value, int startIndex, int endIndex )
			{
				for( int i=startIndex; i<endIndex; i++ )
				{
					if( doc[i] == value )
					{
						return i;
					}
				}

				return -1;
			}

			public static int LastIndexOf( Document doc, char value, int startIndex, int endIndex )
			{
				for( int i=startIndex-1; endIndex<=i; --i )
				{
					if( doc[i] == value )
						return i;
				}

				return -1;
			}

			public static int FindPairedBracket_Backward( Document doc, int startIndex, int endIndex, char bracket, char pairBracket )
			{
				int depth = 1;

				// seek backward until paired bracket was found
				for( int i=startIndex-1; endIndex<=i; i-- )
				{
					if( doc[i] == bracket && doc.IsCDATA(i) == false )
					{
						// a bracket was found.
						// increase depth count
						depth++;
					}
					else if( doc[i] == pairBracket && doc.IsCDATA(i) == false )
					{
						// a paired bracket was found.
						// decrease count and if the count fell down to zero,
						// return the position.
						depth--;
						if( depth == 0 )
						{
							return i; // found the pair
						}
					}
				}

				// not found
				return -1;
			}
		}
		#endregion
	}
}
