using System;
using System.Collections.Generic;

namespace Sgry.Azuki
{
	internal class CrlfRangeList : ILineRangeList
	{
		readonly TextBuffer _Buffer;

		internal CrlfRangeList( TextBuffer buf )
		{
			_Buffer = buf;
		}

		public ILineRange this[ int lineIndex ]
		{
			get
			{
				if( lineIndex < 0 || _Buffer.Lines.Count < lineIndex )
					throw new ArgumentOutOfRangeException();

				var range = _Buffer.GetLineRange( lineIndex, true );
				return new LineRange( _Buffer, range.Begin, range.End, lineIndex );
			}
		}

		public int Count
		{
			get{ return _Buffer.LHI.Count; }
		}
	}
}
