// file: IHighlighter.cs
// brief: Interface of syntax highlighter objects for Azuki.
//=========================================================
using System;

namespace Sgry.Azuki.Highlighter
{
	/// <summary>
	/// Interface of syntax highlighter objects for Azuki.
	/// </summary>
	/// <remarks>
	///   <para>
	///   This interface is commonly used by syntax highlighter objects. If a highlighter object is
	///   set for a document, <see cref="IHighlighter.Highlight"/> method will be called on every
	///   time slightly after the user stopped editing. Since the method is called with parameters
	///   indicating where to begin highlighting and where to end highlighting, highlighting will
	///   not process entire document.
	///   </para>
	///   <para>
	///   If you implement this interface, note that the document to be highlighted can be
	///   retrieved through an IRange object which will be given to
	///   <see cref="IHighlighter.Highlight"/> method.
	///   </para>
	/// </remarks>
	public interface IHighlighter
	{
		/// <summary>
		/// Highlights a part of a document.
		/// </summary>
		/// <returns>The range of text highlighted.</returns>
		/// <exception cref="InvalidOperationException">
		///   No valid object was set to Document property.
		/// </exception>
		IRange Highlight( IRange dirtyRange );

		/// <summary>
		/// Gets or sets whether this highlighter supports hook mechanism or not.
		/// </summary>
		/// <remarks>
		///   <para>
		///   This property gets or sets whether this highlighter object supports hook mechanism or
		///   not. If it is supported, hook procedure can be installed through 
		///   <see cref="IHighlighter.HookProc"/> property.
		///   </para>
		/// </remarks>
		/// <seealso cref="IHighlighter.HookProc"/>
		bool CanUseHook
		{
			get;
		}

		/// <summary>
		/// Gets or sets a hook procedure.
		/// </summary>
		/// <remarks>
		///   <para>
		///   This property gets or sets a hook procedure to override highlighting logic of this
		///   syntax highlighter.
		///   </para>
		///   <para>
		///   Hook mechanism is provided to override original syntax highlighter's behavior.
		///   Technically, a hook is a delegate object of type <see cref="HighlightHook"/> and is
		///   called each time before a token is highlighted by the syntax highlighter. If it
		///   returns true, syntax highlighter will skip highlighting the token. This means that
		///   hook procedures can highlight each token differently
		///   </para>
		///   <para>
		///   One of the typical usage of hook mechanism is changing character class for specific
		///   keywords to meet application specific needs. Another typical usage is to extend
		///   highlighting logic of highlighters. For example, built-in C/C++ highlighter uses a
		///   hook procedure to allow highlighting preprocessor macros whose '#' and keyword are
		///   separated with spaces. Note that hooks are just hooks; it can change so little of
		///   original behavior.
		///   </para>
		///   <para>
		///   Hook mechanism is not required to be implemented. An IHighlighter implementation
		///   which does not support it MUST throw a NotSupportedException on setting a value to
		///   this property, and CanHook property MUST always be False.
		///   </para>
		/// </remarks>
		/// <exception cref="NotSupportedException">
		///   This highlighter does not support hook mechanism.
		/// </exception>
		/// <seealso cref="IHighlighter.CanUseHook"/>
		/// <seealso cref="HighlightHook"/>
		HighlightHook HookProc
		{
			get; set;
		}
	}

	/// <summary>
	/// The type of the hook to override default procedure to highlight a token.
	/// </summary>
	/// <param name="doc">The document to be highlighted.</param>
	/// <param name="token">The substring to be highlighted.</param>
	/// <param name="index">The index of where the token is at.</param>
	/// <param name="klass">
	///   The character class which the token is to be classified as, by the highlighter.
	/// </param>
	/// <returns>
	///   Returns true if default behavior of the highlighter should be suppressed, otherwise
	///   returns false.
	/// </returns>
	/// <seealso cref="IHighlighter.HookProc"/>
	public delegate bool HighlightHook( Document doc, string token, int index, CharClass klass );
}
