namespace Sgry.Azuki
{
	public interface ILineRangeList
	{
		ILineRange this[ int lineIndex ]
		{
			get;
		}

		int Count
		{
			get;
		}
	}
}
