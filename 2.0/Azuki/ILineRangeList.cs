using System;
using System.Collections.Generic;

namespace Sgry.Azuki
{
	/// <summary>
	/// An interface to extract ranges of lines.
	/// </summary>
	public interface ILineRangeList : IRangeList, IEnumerable<ILineRange>
	{
		/// <summary>
		/// Gets a range of a line at a specifed index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException"/>
		new ILineRange this[ int lineIndex ]
		{
			get;
		}

		/// <summary>
		/// Gets a range of a line which contains a character at a specified char-index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException"/>
		ILineRange AtOffset( int charIndex );
	}
}
