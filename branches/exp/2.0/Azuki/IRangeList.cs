namespace Sgry.Azuki
{
	public interface IRangeList
	{
		Range this[ int lineIndex ]
		{
			get;
		}

		int Count
		{
			get;
		}
	}
}
