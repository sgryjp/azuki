using System;
using System.Diagnostics;

namespace Sgry.Azuki
{
	/// <summary>
	/// Range of a text line, excluding its EOL code.
	/// </summary>
	internal class LineRange : Range, ILineRange
	{
		/// <exception cref="ArgumentException"/>
		/// <exception cref="ArgumentNullException"/>
		/// <exception cref="ArgumentOutOfRangeException"/>
		internal LineRange( TextBuffer buf, int begin, int end, int lineIndex )
			: base( buf, begin, end )
		{
			if( buf == null )
				throw new ArgumentNullException( "buf" );
			if( lineIndex < 0 )
				throw new ArgumentOutOfRangeException( "lineIndex", "Parameter 'lineIndex' must"
													  + " not be null." );
			if( buf.Lines.Count <= lineIndex )
				throw new ArgumentOutOfRangeException( "lineIndex", "Parameter 'lineIndex' was"
													   + " too large. (lineIndex:" + lineIndex
													   + ", LineCount:" + buf.Lines.Count + ")" );

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
				Debug.Assert( TextBuffer != null );

				var buf = TextBuffer;
				var begin = End;
				var end = (LineIndex+1 < buf.Lines.Count) ? buf.Lines[LineIndex+1].Begin
														  : buf.Count;
				return buf.GetText( new Range(begin, end) );
			}
		}

		public DirtyState DirtyState
		{
			get
			{
				Debug.Assert( TextBuffer != null );

				var buf = TextBuffer;
				return (LineIndex < buf.LDS.Count) ? buf.LDS[ LineIndex ]
												   : DirtyState.Clean;
			}
			set
			{
				Debug.Assert( TextBuffer != null );
				Debug.Assert( 0 <= LineIndex );
				Debug.Assert( LineIndex <= TextBuffer.LDS.Count );

				if( LineIndex < TextBuffer.LDS.Count )
					TextBuffer.LDS[LineIndex] = value;
			}
		}
	}
}
