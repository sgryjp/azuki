using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Sgry.Azuki
{
	internal class LineRangeList : ILineRangeList
	{
		protected readonly Document _Document;

		internal LineRangeList( Document buf )
		{
			_Document = buf;
		}

		/// <exception cref="ArgumentOutOfRangeException"/>
		public ILineRange AtOffset( int charIndex )
		{
			if( charIndex < 0 || _Document.Length < charIndex )
				throw new ArgumentOutOfRangeException( "charIndex", charIndex, "Invalid index was"
													   + " given. (charIndex:" + charIndex
													   + ", Count:" + Count + ")." );

			return this[ _Document.GetLineColumnPos(charIndex).Line ];
		}

		/// <exception cref="ArgumentOutOfRangeException"/>
		public virtual ILineRange this[ int lineIndex ]
		{
			get
			{
				var buf = _Document.Buffer;
				if( lineIndex < 0 || buf.GetLineCount() < lineIndex )
					throw new ArgumentOutOfRangeException();

				var range = buf.GetLineRange( lineIndex, false );
				Debug.Assert( range.End == buf.Count
							  || buf[range.End] == '\r'
							  || buf[range.End] == '\n' );
				return new LineRange( _Document, range.Begin, range.End, lineIndex );
			}
		}

		public int Count
		{
			get{ return _Document.Buffer.GetLineCount(); }
		}

		#region IEnumerable
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
