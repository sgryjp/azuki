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
		static int _AutoScrollMargin = 1;
		static bool _UseTextForEofMark = true;
		static Antialias _TextRenderingMode = Antialias.Default;

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
