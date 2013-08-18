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

		public ILineRange AtOffset( int charIndex )
		{
			if( charIndex < 0 || _Buffer.Count < charIndex )
				throw new ArgumentOutOfRangeException( "charIndex", charIndex, "Invalid index was"
													   + " given. (charIndex:" + charIndex
													   + ", Count:" + Count + ")." );

			return this[ _Buffer.GetTextPosition(charIndex).Line ];
		}

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

		public int Count
		{
			get{ return _Buffer.LHI.Count; }
		}

		#region IEnumerator
		public IEnumerator<ILineRange> GetEnumerator()
		{
			return new Enumerator(this);
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return new Enumerator(this);
		}

		class Enumerator : IEnumerator<ILineRange>
		{
			LineRangeList _list;
			int _index = -1;

			public Enumerator( LineRangeList list )
			{
				_list = list;
			}

			public ILineRange Current
			{
				get{ return _list[_index]; }
			}

			public void Dispose()
			{}

			object System.Collections.IEnumerator.Current
			{
				get{ return _list[_index]; }
			}

			public bool MoveNext()
			{
				if( _list.Count <= _index+1 )
					return false;

				_index++;
				return true;
			}

			public void Reset()
			{
				_index = -1;
			}
		}
		#endregion
	}
}
