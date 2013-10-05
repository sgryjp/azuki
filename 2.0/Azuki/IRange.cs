using System;
using System.Collections.Generic;

namespace Sgry.Azuki
{
	/// <summary>
	/// Represents a range to describe where a portion of text begins from and where it ends.
	/// </summary>
	public interface IRange
	{
		/// <summary>
		/// Gets or sets beginning position of this range.
		/// </summary>
		int Begin { get; set; }

		/// <summary>
		/// Gets or sets ending position of this range.
		/// </summary>
		int End { get; set; }

		/// <summary>
		/// Gets number of UTF-16 characters in this range.
		/// </summary>
		int Length { get; }

		/// <summary>
		/// Gets text in this range.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property extracts a substring from an associated text buffer. The substring
		/// extracted is cached so that subsequent access won't hurt performance.
		/// </para>
		/// </remarks>
		/// <exception cref="InvalidOperationException">
		/// The range is not associatd with a specific text buffer.
		/// </exception>
		string Text { get; }

		/// <summary>
		/// Gets whether this range is an empty (zero-length) range or not.
		/// </summary>
		bool IsEmpty { get; }

		/// <summary>
		/// Calculates intersection of another range and this range.
		/// </summary>
		/// <exception cref="ArgumentNullException"/>
		Range Intersect( IRange another );

		/// <summary>
		/// Gets information of the character at specified index in this range.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Through this property, you can get a CharData object which describes the character at
		/// the position. The actual value of the character can be retrieved from the CharData
		/// object along with other information about it.
		/// </para>
		/// </remarks>
		/// <seealso cref="CharData"/>
		/// <exception cref="ArgumentOutOfRangeException"/>
		CharData this[ int index ] { get; }

		/// <summary>
		/// Gets a collection of characters (grapheme clusters) in this range.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property gets an enumerator object which enumerates characters in the range.
		/// Strictly, the enumerator object does not enumerates UTF-16 characters but enumerates
		/// grapheme clusters.
		/// </para>
		/// <para>
		/// If you want to enumerate UTF-16 characters, use <see cref="RawChars"/> instead.
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// foreach( var line in document.Lines )
		///     foreach( var ch in line.Chars )
		///         if( 1 &lt; ch.Length )
		///             MessageBox.Show( "Found a character which is expressed with multiple UTF-16"
		///                              + " characters: [" + ch.ToString() + "]" );
		/// </code>
		/// </example>
		/// <seealso cref="CharData"/>
		/// <seealso cref="RawChars"/>
		/// <exception cref="InvalidOperationException">
		/// The range is not associatd with a specific text buffer.
		/// </exception>
		IEnumerable<CharData> Chars { get; }

		/// <summary>
		/// Gets a collection of UTF-16 characters in this range.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property gets an object which enumerates characters in the range character by
		/// character. Since the enumerator object does not avoid positioning in a middle of a 
		/// grapheme cluster, it can enumerate every UTF-16 characters.
		/// </para>
		/// <para>
		/// If you want to enumerate grapheme clusters, use <see cref="Chars"/> instead.
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// foreach( var line in document.RawLines )
		///     foreach( var ch in line.RawChars )
		///         if( ch.Char == 0x00 )
		///             MessageBox.Show( "Found a null-character at index " + ch.Index + "." );
		/// </code>
		/// </example>
		/// <seealso cref="CharData"/>
		/// <seealso cref="Chars"/>
		/// <exception cref="InvalidOperationException">
		/// The range is not associatd with a specific text buffer.
		/// </exception>
		IEnumerable<CharData> RawChars { get; }
	}
}
