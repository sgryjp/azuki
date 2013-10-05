using System;
using System.Text;
using Path = System.IO.Path;
using Regex = System.Text.RegularExpressions.Regex;

namespace Sgry.Ann
{
	class Document : Azuki.Document
	{
		#region Fields
		string _FilePath;
		string _DisplayName = null;
		#endregion

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public Document()
		{
			FileType = FileType.TextFileType;
			Encoding = Encoding.Default;
			base.MarksUri = true;
			base.WatchPatterns.Register( 0, new Regex("") );
		}
		#endregion

		#region Properties
		/// <summary>
		/// Gets or sets name for display.
		/// </summary>
		public string DisplayName
		{
			get
			{
				if( _FilePath != null )
				{
					return Path.GetFileName( _FilePath );
				}
				else
				{
					return _DisplayName;
				}
			}
			set
			{
				_DisplayName = value;
			}
		}

		/// <summary>
		/// Gets name for display with flags.
		/// </summary>
		public string DisplayNameWithFlags
		{
			get
			{
				if( IsDirty )
					return DisplayName + '*';
				else
					return DisplayName;
			}
		}

		/// <summary>
		/// Gets associated file type object.
		/// </summary>
		public FileType FileType
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets the file path associated with this document.
		/// </summary>
		public string FilePath
		{
			get{ return _FilePath; }
			set
			{
				// Once file path was associated with a document, set display name to the file name
				// and lock it
				_DisplayName = Path.GetFileName( value );
				_FilePath = value;
			}
		}

		/// <summary>
		/// Gets or sets encoding of the document file.
		/// </summary>
		public Encoding Encoding
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets whether a BOM should be used on saving the document.
		/// </summary>
		public bool WithBom
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets time this document was saved.
		/// </summary>
		public DateTime LastSavedTime
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets searching text pattern.
		/// </summary>
		public Regex SearchingPattern
		{
			get{ return WatchPatterns[0].Pattern; }
			set{ WatchPatterns.Register(0, value); }
		}
		#endregion

		#region Utilities
		/// <summary>
		/// Gets display name of this document.
		/// </summary>
		public override string ToString()
		{
			return DisplayNameWithFlags;
		}
		#endregion
	}
}
