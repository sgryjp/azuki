using System;
using System.Collections.Generic;

namespace Sgry.Azuki
{
	/// <summary>
	/// Collection of text ranges.
	/// </summary>
	public interface IRangeList : IEnumerable<IRange>
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
