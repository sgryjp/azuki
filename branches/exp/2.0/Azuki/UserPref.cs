// file: UserPref.cs
// brief: User preferences that affects all Azuki instances.
//=========================================================
using System;
using System.Text;

namespace Sgry.Azuki
{
	/// <summary>
	/// User preferences.
	/// </summary>
	/// <remarks>
	/// <para>
	/// UserPref class is a collection of fields which customizes Azuki's behavior.
	/// All items customizable with this class affects all Azuki instances.
	/// </para>
	/// </remarks>
	public static class UserPref
	{
		static bool _CopyLineWhenNoSelection = true;
		static int _AutoScrollMargin = 1;
		static bool _UseTextForEofMark = true;
		static Antialias _TextRenderingMode = Antialias.Default;

		/// <summary>
		/// If true, cut/copy action targets the current line if nothing selected.
		/// </summary>
		/// <remarks>
		/// <para>
		/// If this property is set true,
		/// then copy action without any selection will copy
		/// the line itself which the caret is on.
		/// Note that this case copies &quot;a line&quot;
		/// and the copied data will be slightly different from
		/// mere text data containing all character sequence of that line.
		/// If a line was copied by this case,
		/// pasting it when the caret is at middle of a line
		/// will insert the copied line before the current line.
		/// </para>
		/// <para>
		/// This property affects both cut and copy action.
		/// </para>
		/// </remarks>
		public static bool CopyLineWhenNoSelection
		{
			get{ return _CopyLineWhenNoSelection; }
			set{ _CopyLineWhenNoSelection = value; }
		}

		/// <summary>
		/// Gets or sets how many lines	are kept visible on moving caret by keyboard.
		/// </summary>
		/// <remarks>
		/// <para>
		/// When user moves caret with keyboard, Azuki automatically scrolls to ensure the caret
		/// always be in screen. This property determines how many lines the caret is distant at
		/// most from top or bottom of the window.
		/// </para>
		/// </remarks>
		public static int AutoScrollMargin
		{
			get{ return _AutoScrollMargin; }
			set
			{
				if( value < 0 )
					value = 0;
				_AutoScrollMargin = value;
			}
		}

		/// <summary>
		/// If true, Azuki draws EOF mark as text "[EOF]".
		/// </summary>
		public static bool UseTextForEofMark
		{
			get{ return _UseTextForEofMark; }
			set{ _UseTextForEofMark = value; }
		}

		/// <summary>
		/// Gets or sets how Azuki anti-aliases text.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property determines the anti-aliase method that Azuki uses
		/// on rendering text.
		/// </para>
		/// </remarks>
		public static Antialias Antialias
		{
			get{ return _TextRenderingMode; }
			set{ _TextRenderingMode = value; }
		}
	}
}
