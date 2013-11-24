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
	public struct CharData
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
		public int Length
		{
			get{ return TextUtil.NextGraphemeClusterIndex(_Buffer, Index) - Index; }
		}

		/// <summary>
		/// Gets classification of the character at the position.
		/// </summary>
		public CharClass Class
		{
			get{ return _Buffer.GetCharClassAt(Index); }
		}
		#endregion

		#region Operation
		/// <summary>
		/// Gets an UTF-16 character value at the position.
		/// </summary>
		public char ToChar()
		{
			return _Buffer[Index];
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
		public override string ToString()
		{
			var buf = new StringBuilder(4);
			var length = Length;
			for( int i=0; i<length; i++ )
			{
				buf.Append( _Buffer[_Index + i] );
			}
			return buf.ToString();
		}
		#endregion

		#region Behavior as an object
		/// <summary>
		/// Gets an UTF-16 value at the position. This is a synonym of ToChar() method.
		/// </summary>
		/// <seealso cref="ToChar"/>
		public static explicit operator Char( CharData cd )
		{
			return cd.ToChar();
		}

		/// <summary>
		/// Gets a sequence of UTF-16 characters which consists a grapheme cluster at the position.
		/// This is a synonym of ToString() method.
		/// </summary>
		/// <seealso cref="ToString"/>
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
			CharData cd;
			for( int i=_Range.Begin; i<_Range.End; i+=cd.Length )
			{
				cd = new CharData( _Buffer, i );
				yield return cd;
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

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
