using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Sgry.Azuki
{
	/// <summary>
	/// Represents position of a character in a text buffer and its related information.
	/// </summary>
	public struct CharData : ICloneable
	{
		readonly TextBuffer _Buffer;
		int _Index;

		#region Init / Dispose
		internal CharData( TextBuffer buf, int index )
		{
			Debug.Assert( buf != null );
			Debug.Assert( 0 <= index );
			_Buffer = buf;
			_Index = index;
		}
		#endregion

		#region Properties
		/// <summary>
		/// Gets or sets the index of the character's position in a text buffer.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException"/>
		public int Index
		{
			get{ return _Index; }
			set
			{
				if( value < 0 )
					throw new ArgumentOutOfRangeException();
				_Index = value;
			}
		}

		/// <summary>
		/// Gets number of UTF-16 characters consisting with the character (grapheme cluster.)
		/// </summary>
		/// <exception cref="InvalidOperationException"/>
		public int Length
		{
			get
			{
				if( _Buffer == null )
					throw new InvalidOperationException();
				return (Index < _Buffer.Count)
					? TextUtil.NextGraphemeClusterIndex(_Buffer, Index) - Index
					: 0;
			}
		}

		/// <summary>
		/// Gets classification of the character at the position.
		/// </summary>
		/// <exception cref="InvalidOperationException"/>
		public CharClass Class
		{
			get
			{
				if( _Buffer == null )
					throw new InvalidOperationException();
				return _Buffer.GetCharClassAt(Index);
			}
		}
		#endregion

		#region Operation
		/// <summary>
		/// Gets an UTF-16 character value at the position.
		/// </summary>
		/// <exception cref="InvalidOperationException"/>
		public char ToChar()
		{
			if( _Buffer == null )
				throw new InvalidOperationException();
			return (Index < _Buffer.Count) ? _Buffer[Index]
										   : '\0';
		}

		/// <summary>
		/// Gets a sequence of UTF-16 characters which consists a grapheme cluster at the position.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This method retrieves a sequence of UTF-16 characters which consists a grapheme cluster
		/// at the position. Note that this method assumes that the position of a CharData is not
		/// in a middle of a grapheme cluster. If it was not, the result might be invalid.
		/// </para>
		/// </remarks>
		/// <exception cref="InvalidOperationException"/>
		public override string ToString()
		{
			if( _Buffer == null )
				throw new InvalidOperationException();

			var buf = new StringBuilder(4);
			var length = Length;
			for( int i=0; i<length && i<_Buffer.Count; i++ )
			{
				buf.Append( _Buffer[_Index + i] );
			}
			return buf.ToString();
		}
		#endregion

		#region Iteration
		public static CharData operator ++( CharData cd )
		{
			do
			{
				cd.Index++;
			}
			while( TextUtil.IsNotDividableIndex(cd._Buffer, cd.Index) );
			return cd;
		}

		public static CharData operator --( CharData cd )
		{
			do
			{
				cd.Index--;
			}
			while( TextUtil.IsNotDividableIndex(cd._Buffer, cd.Index) );
			return cd;
		}
		#endregion

		#region Behavior as an object
		public CharData Clone()
		{
			return new CharData( _Buffer, _Index );
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		/// <summary>
		/// Gets an UTF-16 value at the position. This is a synonym of ToChar() method.
		/// </summary>
		/// <seealso cref="ToChar"/>
		/// <exception cref="InvalidOperationException"/>
		public static explicit operator Char( CharData cd )
		{
			return cd.ToChar();
		}

		/// <summary>
		/// Gets a sequence of UTF-16 characters which consists a grapheme cluster at the position.
		/// This is a synonym of ToString() method.
		/// </summary>
		/// <seealso cref="ToString"/>
		/// <exception cref="InvalidOperationException"/>
		public static explicit operator String( CharData cd )
		{
			return cd.ToString();
		}
		#endregion
	}

	/// <summary>
	/// Represents a list of CharData.
	/// </summary>
	public interface ICharDataList : IEnumerable<CharData>
	{
		/// <summary>
		/// Gets an item at the specified index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException"/>
		CharData this[ int index ]
		{
			get;
		}
	}

	#region Utilities
	class CharDataList : ICharDataList
	{
		readonly TextBuffer _Buffer;
		readonly IRange _Range;

		public CharDataList( TextBuffer buffer, IRange range )
		{
			Debug.Assert( buffer != null );
			Debug.Assert( range != null );
			_Buffer = buffer;
			_Range = range;
		}

		public IEnumerator<CharData> GetEnumerator()
		{
			var cd = new CharData( _Buffer, _Range.Begin );
			while( cd.Index < _Range.End )
				yield return (cd++).Clone();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <exception cref="ArgumentOutOfRangeException"/>
		public CharData this[ int index ]
		{
			get
			{
				if( index < 0 || _Buffer.Count <= index )
					throw new ArgumentOutOfRangeException();
				return new CharData(_Buffer, index);
			}
		}
	}

	class RawCharDataList : ICharDataList
	{
		readonly TextBuffer _Buffer;
		readonly IRange _Range;

		public RawCharDataList( TextBuffer buffer, IRange range )
		{
			Debug.Assert( buffer != null );
			Debug.Assert( range != null );
			_Buffer = buffer;
			_Range = range;
		}

		public IEnumerator<CharData> GetEnumerator()
		{
			for( int i=_Range.Begin; i<_Range.End; i++ )
				yield return new CharData( _Buffer, i );
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <exception cref="ArgumentOutOfRangeException"/>
		public CharData this[ int index ]
		{
			get
			{
				if( index < 0 || _Buffer.Count <= index )
					throw new ArgumentOutOfRangeException();
				return new CharData(_Buffer, index);
			}
		}
	}
	#endregion
}
