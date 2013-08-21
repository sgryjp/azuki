using System;
using System.Collections.Generic;

namespace Sgry.Azuki
{
	/// <summary>
	/// An interface to extract ranges of lines.
	/// </summary>
	public interface ILineRangeList : IEnumerable<ILineRange>
	{
		/// <summary>
		/// Gets a range of a line at specifed index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException"/>
		ILineRange this[ int lineIndex ]
		{
			get;
		}

		/// <summary>
		/// Gets a range which contains a character at specified offset.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException"/>
		ILineRange AtOffset( int charIndex );

		/// <summary>
		/// Gets number of lines.
		/// </summary>
		int Count
		{
			get;
		}
	}
}
