namespace Sgry.Azuki
{
	/// <summary>
	/// Represents a range to describe where a portion of text begins from and where it ends.
	/// </summary>
	public interface IRange
	{
		int Begin { get; set; }

		int End { get; set; }

		int Length { get; }

		/// <summary>
		/// Extracts text in this range.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property extracts a substring from a text buffer associated with. The substring
		/// extracted is cached so that subsequent access won't hurt performance.
		/// </para>
		/// </remarks>
		string Text{ get; }

		bool IsEmpty { get; }

		Range Intersect( IRange another );
	}
}
