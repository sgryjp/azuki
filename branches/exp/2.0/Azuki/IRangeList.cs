namespace Sgry.Azuki
{
	public interface IRangeList
	{
		/// <summary>
		/// Gets a range at a specifed index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException"/>
		IRange this[ int lineIndex ]
		{
			get;
		}

		/// <summary>
		/// Gets a number of ranges in this list.
		/// </summary>
		int Count
		{
			get;
		}
	}
}
