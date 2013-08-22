using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Sgry.Azuki
{
	internal class LineRangeList : ILineRangeList
	{
		protected readonly TextBuffer _Buffer;

		internal LineRangeList( TextBuffer buf )
		{
			_Buffer = buf;
		}

		/// <exception cref="ArgumentOutOfRangeException"/>
		public ILineRange AtOffset( int charIndex )
		{
			if( charIndex < 0 || _Buffer.Count < charIndex )
				throw new ArgumentOutOfRangeException( "charIndex", charIndex, "Invalid index was"
													   + " given. (charIndex:" + charIndex
													   + ", Count:" + Count + ")." );

			return this[ _Buffer.GetTextPosition(charIndex).Line ];
		}

		/// <exception cref="ArgumentOutOfRangeException"/>
		public virtual ILineRange this[ int lineIndex ]
		{
			get
			{
				if( lineIndex < 0 || _Buffer.Lines.Count < lineIndex )
					throw new ArgumentOutOfRangeException();

				var range = _Buffer.GetLineRange( lineIndex, false );
				Debug.Assert( range.End == _Buffer.Count
							  || _Buffer[range.End] == '\r'
							  || _Buffer[range.End] == '\n' );
				return new LineRange( _Buffer, range.Begin, range.End, lineIndex );
			}
		}

		/// <exception cref="ArgumentOutOfRangeException"/>
		IRange IRangeList.this[int lineIndex]
		{
			get{ return this[lineIndex]; }
		}

		public int Count
		{
			get{ return _Buffer.LHI.Count; }
		}

		#region IEnumerator
		public IEnumerator<ILineRange> GetEnumerator()
		{
			for( int i=0; i<Count; i++ )
				yield return this[i];
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		#endregion
	}
}
