namespace Sgry.Azuki
{
	public interface ILineRange : IRange
	{
		string EolCode{ get; }

		DirtyState DirtyState
		{
			get; set;
		}
	}
}
