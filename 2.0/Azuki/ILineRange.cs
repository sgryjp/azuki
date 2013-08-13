namespace Sgry.Azuki
{
	public interface ILineRange : IRange
	{
		string TextWithEolCode{ get; }

		string EolCode{ get; }

		DirtyState DirtyState
		{
			get; set;
		}
	}
}
