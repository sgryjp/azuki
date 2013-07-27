namespace Sgry.Azuki
{
	public interface ILineRange : IRange
	{
		LineDirtyState LineDirtyState
		{
			get; set;
		}
	}
}
