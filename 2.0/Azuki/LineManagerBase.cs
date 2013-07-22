using System;
using System.Collections.Generic;
using System.Text;

namespace Sgry.Azuki
{
	internal abstract class LineManagerBase
	{
		protected TextBuffer _Buffer;
		protected GapBuffer<int> _Lhi; // Line Head Indexes

		protected LineManagerBase( TextBuffer buf )
		{
			_Buffer = buf;
			_Lhi = new GapBuffer<int>( (buf.Capacity >> 7), 32 );
		}

		public Range GetLineRange( int lineIndex )
		{
			return GetLineRange( lineIndex, true );
		}

		public abstract Range GetLineRange( int lineIndex, bool printableOnly );
	}
}
