namespace Sgry.Azuki
{
	/// <summary>
	/// Range of a text line.
	/// </summary>
	public interface ILineRange : IRange
	{
		/// <summary>
		/// Gets index of this line in a text buffer.
		/// </summary>
		int LineIndex{ get; }

		/// <summary>
		/// Gets EOL code which terminates this line. This property will never be null.
		/// </summary>
		string EolCode{ get; }

		/// <summary>
		/// Gets or sets dirty state of this line.
		/// </summary>
		DirtyState DirtyState
		{
			get; set;
		}
	}
}
