using System;
using System.Diagnostics;

namespace Sgry.Azuki
{
	/// <summary>
	/// Range of a text line, excluding its EOL code.
	/// </summary>
	internal class LineRange : Range, ILineRange
	{
		internal LineRange( TextBuffer buf, int begin, int end, int lineIndex )
			: base( buf, begin, end )
		{
			LineIndex = lineIndex;
		}

		public int LineIndex
		{
			get;
			private set;
		}

		public virtual string EolCode
		{
			get
			{
				int begin = End;
				int end = (LineIndex+1 < _Buffer.Lines.Count) ? _Buffer.Lines[LineIndex+1].Begin
															  : _Buffer.Count;
				return _Buffer.GetText( new Range(begin, end) );
			}
		}

		public DirtyState DirtyState
		{
			get
			{
				if( LineIndex < 0 )
					throw new InvalidOperationException( "The line index is out of valid range."
														 + " (lineIndex:" + LineIndex+ ", Line"
														 + " count:" + _Buffer.Lines.Count + ")" );

				return (LineIndex < _Buffer.LDS.Count) ? _Buffer.LDS[ LineIndex ]
														: DirtyState.Clean;
			}
			set
			{
				Debug.Assert( 0 <= LineIndex );
				Debug.Assert( LineIndex <= _Buffer.LDS.Count );

				if( LineIndex < _Buffer.LDS.Count )
					_Buffer.LDS[LineIndex] = value;
			}
		}
	}
}
