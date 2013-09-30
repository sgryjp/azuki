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
		static Antialias _TextRenderingMode = Antialias.Default;

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
