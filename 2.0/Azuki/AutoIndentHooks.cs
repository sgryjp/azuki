// file: AutoIndentLogic.cs
// brief: Logic around auto-indentation.
//=========================================================
using System;
using System.Text;
using Sgry.Azuki.Utils;

namespace Sgry.Azuki
{
	/// <summary>
	/// Hook delegate called every time a character was inserted.
	/// </summary>
	/// <param name="ui">User interface object such as AzukiControl.</param>
	/// <param name="ch">The character to be inserted.</param>
	/// <returns>
	/// Whether the hook handles input successfully or not.
	/// </returns>
	/// <remarks>
	///   <para>
	///   AutoIndentHook is the type of delegate which is used to override (hook and change)
	///   Azuki's built-in input handling logic. If a hook of this type was installed, it is called
	///   every time a character is inserted and if it returned true, Azuki suppresses built-in
	///   input handling logic.
	///   </para>
	/// </remarks>
	/// <seealso cref="IUserInterface.AutoIndentHook"/>
	/// <seealso cref="AutoIndentHooks"/>
	public delegate bool AutoIndentHook( IUserInterface ui, char ch );

	/// <summary>
	/// Static class containing built-in hook delegates for auto-indentation.
	/// </summary>
	/// <seealso cref="IUserInterface.AutoIndentHook"/>
	/// <seealso cref="WinForms.AzukiControl.AutoIndentHook"/>
	public static class AutoIndentHooks
	{
		/// <summary>
		/// Basic auto-indent hook.
		/// </summary>
		/// <remarks>
		///   <para>
		///   This hook executes most basic auto-indentation; it just copies previous indentation
		///   characters every time the user creates a new line.
		///   </para>
		/// </remarks>
		public static readonly AutoIndentHook GenericHook = delegate( IUserInterface ui, char ch )
		{
			// Do nothing if Azuki is in single line mode
			if( ui.IsSingleLineMode )
				return false;

			var doc = ui.Document;
			var view = ui.View;

			// Perform indentation if an EOL code was inserted
			if( ch.IsEolChar() )
			{
				var str = new StringBuilder( doc.EolCode );

				// Get indent chars
				var lineHead = doc.Lines.AtOffset( view.CaretIndex ).Begin;
				for( int i=lineHead; i<view.CaretIndex; i++ )
				{
					var c = doc[i];
					if( c == ' ' || c == '\t' || c == '\x3000' )
						str.Append( c );
					else
						break;
				}

				// Replace selection
				var newCaretIndex = Math.Min( view.AnchorIndex, view.CaretIndex ) + str.Length;
				doc.Replace( str.ToString() );
				doc.SetSelection( newCaretIndex, newCaretIndex );

				return true;
			}

			return false;
		};

		/// <summary>
		/// Auto-indent hook for C styled source code.
		/// </summary>
		/// <remarks>
		///   <para>
		///   This hook delegate provides a special indentation logic for C styled source code.
		///   Here 'C style' means that curly brackets are used to enclose each logical block, such
		///   as C++, Java, C#, and so on.
		///   </para>
		///   <para>
		///   The differences between this and generic auto-indentation are below:
		///   </para>
		///   <list type="bullet">
		///     <item>
		///     Pressing Enter key increases indentation level if the line was terminated with a
		///     closing curly bracket (<c> } </c>)
		///     </item>
		///     <item>
		///     Inserting an opening curly bracket (<c> { </c>) decreases indentation level if the
		///     line was consisted only with whitespace characters.
		///     </item>
		///   </list>
		///   <para>
		///	  Note that the characters to be used to create indentation will be chosen according to
		///	  the value of <see cref="IUserInterface.UsesTabForIndent"/> property.
		///   </para>
		/// </remarks>
		public static readonly AutoIndentHook CHook = delegate( IUserInterface ui, char ch )
		{
			// Do nothing if it's in single line mode
			if( ui.IsSingleLineMode )
				return false;

			var view = ui.View;
			var doc = ui.Document;
			int selBegin, selEnd;

			doc.GetSelection( out selBegin, out selEnd );
			var line = doc.Lines.AtOffset( selBegin );

			if( ch.IsEolChar() ) // Enter key
			{
				bool extraPaddingNeeded = false;
				var indentChars = new StringBuilder( doc.EolCode, 64 );

				// If the line is empty, do nothing
				if( line.IsEmpty )
					return false;

				// Get indent chars
				for( int i=line.Begin; i<selBegin; i++ )
				{
					if( doc[i] == ' ' || doc[i] == '\t' )
						indentChars.Append( doc[i] );
					else
						break;
				}

				// Expand target range to include trailing whitespaces
				for( int i=selEnd; i<line.End; i++ )
				{
					if( doc[i] == ' ' || doc[i] == '\t' || doc[i] == '\x3000' )
						selEnd++;
					else
						break;
				}

				// Determine whether extra padding is needed?
				if( FindPairedBracket_Backward(doc, selBegin, line.Begin, '}', '{') != -1
					&& IndexOf(doc, '}', selBegin, line.End) == -1 )
				{
					extraPaddingNeeded = true;
				}

				// Replace selection
				var newCaretIndex = Math.Min( view.AnchorIndex, selBegin ) + indentChars.Length;
				doc.Replace( indentChars.ToString(), selBegin, selEnd );

				// Insert extra indentation if there is an '{' without pair before caret and is no
				// '}' after caret
				if( extraPaddingNeeded )
				{
					var pos = view.GetVirtualPos( newCaretIndex );
					pos.X += view.TabWidthInPx;
					var extraPadding = UiImpl.GetNeededPaddingChars( ui, pos, true );
					doc.Replace( extraPadding, newCaretIndex, newCaretIndex );
					newCaretIndex += extraPadding.Length;
				}

				// Set new caret position
				doc.SetSelection( newCaretIndex, newCaretIndex );

				return true;
			}
			else if( ch == '}' )
			{
				// Ensure this line contains white spaces only
				for( int i=line.Begin; i<line.End; i++ )
				{
					if( doc.IsEolChar(i) )
						break;
					else if( doc[i] != ' ' && doc[i] != '\t' )
						return false;
				}

				// Find the paired open bracket
				var pairIndex = FindPairedBracket_Backward( doc, selBegin, 0, '}', '{' );
				if( pairIndex == -1 )
				{
					return false; // no pair exists. nothing to do
				}

				// Get indent chars from a line containing the pair
				var indentChars = new StringBuilder( 64 );
				var pairedLine = doc.Lines.AtOffset( pairIndex );
				for( int i=pairedLine.Begin; i<pairedLine.End; i++ )
				{
					if( doc[i] == ' ' || doc[i] == '\t' )
						indentChars.Append( doc[i] );
					else
						break;
				}

				// Replace indent chars of current line
				indentChars.Append( '}' );
				doc.Replace( indentChars.ToString(), line.Begin, selBegin );

				return true;
			}

			return false;
		};

		/// <summary>
		/// Auto-indent hook for Python script.
		/// </summary>
		/// <remarks>
		///   <para>
		///   This hook delegate provides a special indentation logic for Python programming
		///   language.
		///   </para>
		///   <list type="bullet">
		///     <item>
		///     Pressing Enter key increases indentation level if the line was terminated with a
		///     colon (<c> : </c>).
		///     </item>
		///     <item>
		///     When the caret is in a middle of a paired parentheses, additional spaces will be
		///     inserted so that the caret's column position will be the same as the previous
		///     opening parenthesis. For example, if there is a code like next:
		///     <pre><code lang="Python">
		///     fruits = ('apple', 'orange')
		///     </code></pre>
		///     and pressing Enter when the caret is at one character ahead of a comma will result:
		///     <pre><code lang="Python">
		///     fruits = ('apple',
		///               'orange')
		///     </code></pre>
		///     </item>
		///   </list>
		///   <para>
		///	  Note that the characters to be used to create indentation will be chosen according to
		///	  the value of <see cref="IUserInterface.UsesTabForIndent"/> property.
		///   </para>
		/// </remarks>
		public static readonly AutoIndentHook PythonHook = delegate( IUserInterface ui, char ch )
		{
			// Do nothing if Azuki is in single line mode
			if( ui.IsSingleLineMode )
			{
				return false;
			}

			if( ch.IsEolChar() )
			{
				var doc = ui.Document;
				var view = (View)ui.View;
				var indentChars = new StringBuilder( doc.EolCode, 128 );

				// First of all, remove selected text
				doc.Replace( "" );

				var line = doc.Lines.AtOffset( doc.CaretIndex );

				// Determine whether an extra padding is needed
				bool levelUp = false;
				{
					for( int i=doc.CaretIndex-1; line.Begin <= i; i-- )
					{
						if( doc[i] == ':' )
						{
							levelUp = true;
							break;
						}
						else if( !doc[i].IsOneOf(" \t") )
						{
							break;
						}
					}
				}

				int lastOpenParenIndex = -1;
				if( !levelUp )
				{
					var openers = new[]{'(', '[', '{'};
					var closers = new[]{')', ']', '}'};
					var levels = new[]{0, 0, 0};
					for( int i=doc.CaretIndex-1; line.Begin <= i; i-- )
					{
						int type = Array.IndexOf( openers, doc[i] );
						if( 0 <= type )
						{
							if( levels[type] == 0 )
							{
								lastOpenParenIndex = i;
								break;
							}
							else
							{
								levels[type]--;
							}
						}
						type = Array.IndexOf( closers, doc[i] );
						if( 0 <= type )
						{
							levels[type]++;
						}
					}
				}

				// Get indent chars
				for( int i=line.Begin; i<doc.CaretIndex; i++ )
				{
					if( doc[i] == ' ' || doc[i] == '\t' || doc[i] == '\x3000' )
						indentChars.Append( doc[i] );
					else
						break;
				}

				// Remove whitespaces just after the caret
				int extraSpaceCount = 0;
				for( int i=doc.CaretIndex; i<line.End; i++ )
				{
					if( doc[i].IsOneOf(" \t") )
						extraSpaceCount++;
					else
						break;
				}

				// Replace selection
				int newCaretIndex = Math.Min( doc.AnchorIndex, doc.CaretIndex ) + indentChars.Length;
				doc.Replace( indentChars.ToString(), doc.CaretIndex, doc.CaretIndex+extraSpaceCount );
				doc.SetSelection( newCaretIndex, newCaretIndex );

				// Add indent level
				if( levelUp )
				{
					var pos = view.GetVirtualPos( doc.CaretIndex );
					pos.X += view.TabWidthInPx;
					indentChars.Length = 0;
					indentChars.Append( UiImpl.GetNeededPaddingChars(ui, pos, true) );
					doc.Replace( indentChars.ToString() );
					newCaretIndex += indentChars.Length;
					doc.SetSelection( newCaretIndex, newCaretIndex );
				}
				else if( 0 <= lastOpenParenIndex )
				{
					int index = lastOpenParenIndex + 1;
					while( index < line.End && (doc[index] == ' ' || doc[index] == '\t' ) )
						index++;
					int caretX = view.GetVirtualPos( doc.CaretIndex ).X;
					int destX = view.GetVirtualPos( index ).X;
					int spaceCount = (destX - caretX) / view.SpaceWidthInPx;
					indentChars.Length = 0;
					for( int i=0; i<spaceCount; i++ )
						indentChars.Append( ' ' );
					doc.Replace( indentChars.ToString() );
					newCaretIndex += indentChars.Length;
					doc.SetSelection( newCaretIndex, newCaretIndex );
				}

				return true;
			}

			return false;
		};

		#region Utilities
		static int IndexOf( Document doc, char value, int startIndex, int endIndex )
		{
			for( int i=startIndex; i<endIndex; i++ )
				if( doc[i] == value )
					return i;
			return -1;
		}

		static int FindPairedBracket_Backward( Document doc, int startIndex, int endIndex,
											   char bracket, char pairBracket )
		{
			int depth = 1;

			// Seek backward until paired bracket was found
			for( int i=startIndex-1; endIndex<=i; i-- )
			{
				if( doc[i] == bracket && doc.IsCDATA(i) == false )
				{
					depth++;
				}
				else if( doc[i] == pairBracket && doc.IsCDATA(i) == false )
				{
					depth--;
					if( depth == 0 )
						return i; // Found
				}
			}

			return -1; // Not found
		}
		#endregion
	}
}
