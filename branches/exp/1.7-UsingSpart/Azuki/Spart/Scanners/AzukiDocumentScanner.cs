// Copyright (c) 2012 Suguru YAMAMOTO.
// License: zlib/libpng license.
using System;
using Sgry.Azuki;
using Spart.Parsers;

namespace Spart.Scanners
{
	/// <summary>
	/// Scanner to scan Azuki document for Spart library.
	/// </summary>
	class AzukiDocumentScanner : IScanner
	{
		Document _Document;
		int _Offset;
		int _End;
		IFilter _Filter;

		public AzukiDocumentScanner( Document doc, int offset, int atMostOffset )
		{
			_Document = doc;
			_Offset = offset;
			_End = atMostOffset;
			_Filter = null;
		}

		public object Source
		{
			get{ return _Document; }
		}

		public bool AtEnd
		{
			get{ return (_End <= _Offset); }
		}

		public bool Read()
		{
			if( AtEnd )
				throw new InvalidOperationException( "Scanner already"
					+ " reached to the end." );

			_Offset++;
			return !AtEnd;
		}

		public char Peek()
		{
			if( AtEnd )
				return '\0';

			if( Filter == null )
			{
				return _Document.GetCharAt( _Offset );
			}
			else
			{
				return Filter.Filter( _Document.GetCharAt(_Offset) );
			}
		}

		public long Offset
		{
			get{ return (long)_Offset; }
			set
			{
				if( Int32.MaxValue < value )
					throw new NotSupportedException(
						"An offset greater than the maximum of 32 bit signed"
						+ " integer is not acceptable." );

				_Offset = (int)value;
			}
		}

		public void Seek(long offset)
		{
			if( Int32.MaxValue < offset )
				throw new NotSupportedException(
					"An offset greater than the maximum of 32 bit signed "
					+ "integer is not acceptable." );
			if( offset < 0 || _End < offset )
				throw new ArgumentOutOfRangeException( "offset" );

			_Offset = (int)offset;
		}

		public string Substring(long offset, int length)
		{
			if( Int32.MaxValue < offset )
				throw new NotSupportedException(
					"An offset greater than the maximum of 32 bit signed"
					+ " integer is not acceptable." );
			if( offset < 0 || _End < offset )
				throw new ArgumentOutOfRangeException( "offset" );
			if( length < 0 )
				throw new ArgumentOutOfRangeException( "length" );

			int begin = (int)offset;
			int end = Math.Min( (int)offset + length, _Document.Length );

			string str = _Document.GetTextInRange( begin, end );
			if( Filter != null )
			{
				return Filter.Filter( str );
			}
			else
			{
				return str;
			}
		}

		public IFilter Filter
		{
			get{ return _Filter; }
			set{ _Filter = value; }
		}
	}
}