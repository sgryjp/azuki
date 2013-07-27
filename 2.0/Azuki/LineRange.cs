using System;
using System.Diagnostics;

namespace Sgry.Azuki
{
	internal class LineRange : Range, ILineRange
	{
		int _LineIndex;

		internal LineRange( TextBuffer buf, int begin, int end, int lineIndex )
			: base( buf, begin, end )
		{
			_LineIndex = lineIndex;
		}

		public LineDirtyState LineDirtyState
		{
			get
			{
				if( _LineIndex < 0 )
					throw new InvalidOperationException( "The line index is out of valid range."
														 + " (lineIndex:" + _LineIndex+ ", Line"
														 + " count:" + _Buffer.Lines.Count + ")" );

				return (_LineIndex < _Buffer.LDS.Count) ? _Buffer.LDS[ _LineIndex ]
														: LineDirtyState.Clean;
			}
			set
			{
				Debug.Assert( 0 <= _LineIndex );
				Debug.Assert( _LineIndex <= _Buffer.LDS.Count );

				if( _LineIndex < _Buffer.LDS.Count )
					_Buffer.LDS[_LineIndex] = value;
			}
		}
	}
}
