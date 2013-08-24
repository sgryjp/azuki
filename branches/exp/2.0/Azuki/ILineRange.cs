namespace Sgry.Azuki
{
	/// <summary>
	/// Range of a text line.
	/// </summary>
	public interface ILineRange : IRange
	{
		/// <summary>
		/// Gets index of the line in a text buffer which contains the line.
		/// </summary>
		int LineIndex{ get; }

		/// <summary>
		/// Gets EOL code which terminates this line.
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
