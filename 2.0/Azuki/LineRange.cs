using System;
using System.Diagnostics;

namespace Sgry.Azuki
{
	/// <summary>
	/// Range of a text line, excluding its EOL code.
	/// </summary>
	internal class LineRange : Range, ILineRange
	{
		protected int _LineIndex;

		internal LineRange( TextBuffer buf, int begin, int end, int lineIndex )
			: base( buf, begin, end )
		{
			_LineIndex = lineIndex;
		}

		/// <summary>
		/// Gets content of this line, without EOL code.
		/// </summary>
		public override string Text
		{
			get{ return base.Text; } // Just to change documentation comment...
		}

		/// <summary>
		/// Gets EOL code which terminates this line.
		/// </summary>
		public virtual string EolCode
		{
			get
			{
				int begin = End;
				int end = (_LineIndex+1 < _Buffer.Lines.Count) ? _Buffer.Lines[_LineIndex+1].Begin
															   : _Buffer.Count;
				return _Buffer.GetText( new Range(begin, end) );
			}
		}

		public DirtyState DirtyState
		{
			get
			{
				if( _LineIndex < 0 )
					throw new InvalidOperationException( "The line index is out of valid range."
														 + " (lineIndex:" + _LineIndex+ ", Line"
														 + " count:" + _Buffer.Lines.Count + ")" );

				return (_LineIndex < _Buffer.LDS.Count) ? _Buffer.LDS[ _LineIndex ]
														: DirtyState.Clean;
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
