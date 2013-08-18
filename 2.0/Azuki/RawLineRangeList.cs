using System;
using System.Diagnostics;

namespace Sgry.Azuki
{
	internal class RawLineRangeList : LineRangeList
	{
		internal RawLineRangeList( TextBuffer buf )
			: base( buf )
		{}

		public override ILineRange this[ int lineIndex ]
		{
			get
			{
				if( lineIndex < 0 || _Buffer.Lines.Count < lineIndex )
					throw new ArgumentOutOfRangeException();

				var range = _Buffer.GetLineRange( lineIndex, true );
				Debug.Assert( range.End == _Buffer.Count
							  || (0 < range.End && _Buffer[range.End-1] == '\r')
							  || (0 < range.End && _Buffer[range.End-1] == '\n') );
				return new RawLineRange( _Buffer, range.Begin, range.End, lineIndex );
			}
		}
	}
}
