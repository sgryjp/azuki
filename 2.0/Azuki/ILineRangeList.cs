using System.Collections.Generic;

namespace Sgry.Azuki
{
	public interface ILineRangeList : IEnumerable<ILineRange>
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
