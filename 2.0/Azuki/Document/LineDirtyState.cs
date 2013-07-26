// file: LineDirtyState.cs
// brief: Indicator of dirty state of each lines.
//=========================================================

namespace Sgry.Azuki
{
	/// <summary>
	/// Indicates modification state of a text line.
	/// </summary>
	public enum LineDirtyState : byte
	{
		/// <summary>
		/// Not modified yet.
		/// </summary>
		Clean = 0,

		/// <summary>
		/// Modified and not saved yet.
		/// </summary>
		Modified = 1,

		/// <summary>
		/// Not modified since it was saved last time.
		/// </summary>
		Saved = 2
	}
}
