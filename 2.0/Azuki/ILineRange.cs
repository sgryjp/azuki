namespace Sgry.Azuki
{
	public interface ILineRange : IRange
	{
		DirtyState DirtyState
		{
			get; set;
		}
	}
}
