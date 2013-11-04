using System;
using System.Collections.Generic;

namespace Sgry.Azuki
{
	/// <summary>
	/// Represents a range to describe where a portion of text begins from and where it ends.
	/// </summary>
	/// <remarks>
	///   <para>
	///   A range is a pair of positions one of which indicates a beginning point of the range and
	///   the other indicates an ending point. A range is denoted as
	///   <code>&quot;[X, Y)&quot;</code> where X is index of the starting position of the range,
	///   and Y is index of the ending. Range includes the character at the beginning position and
	///   DOES NOT include the character at the ending position. For example, let a document's
	///   content is &quot;foobar&quot; and there is a range <code>[3, 5)</code>, the range
	///   includes <code>ba</code>. Note that length of a range can be calculated by subtracting
	///   the beginning index from the ending index.
	/// </para>
	/// </remarks>
	public interface IRange : ICloneable
	{
		/// <summary>
		/// Gets the document which is associated with this range.
		/// </summary>
		Document Document { get; }

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
		/// This property extracts a substring from an associated text buffer.
		/// </para>
		/// <para>
		/// Range objects caches extracted substring until the associated text buffer was modified
		/// so that subsequent read access won't hurt performance.
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
		/// <exception cref="InvalidOperationException">
		///   The range is not associatd with a specific text buffer.
		/// </exception>
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
		///             MessageBox.Show( "Found a character which is expressed with multiple"
		///                              + " UTF-16 characters: [" + ch.ToString() + "]" );
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
