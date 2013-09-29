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
		static bool _UseTextForEofMark = true;
		static Antialias _TextRenderingMode = Antialias.Default;

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
