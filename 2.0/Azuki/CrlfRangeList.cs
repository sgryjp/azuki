using System;
using System.Collections.Generic;
using System.Text;

namespace Sgry.Azuki
{
	internal class CrlfRangeList : IRangeList
	{
		readonly TextBuffer _Buffer;
		readonly IList<int> _Lhi;
		readonly GapBuffer<LineDirtyState> _Lds;

		internal CrlfRangeList( TextBuffer buf, IList<int> lhi, GapBuffer<LineDirtyState> lds )
		{
			_Buffer = buf;
			_Lhi = lhi;
			_Lds = lds;
		}

		public Range this[ int lineIndex ]
		{
			get
			{
				if( lineIndex <= 0 )
					throw new ArgumentOutOfRangeException();

				return _Buffer.GetLineRange( lineIndex, true );
			}
		}

		public int Count
		{
			get{ return _Lhi.Count; }
		}
	}
}
