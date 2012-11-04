// Spart License (zlib/png)
// 
// 
// Copyright (c) 2003 Jonathan de Halleux
// 
// This software is provided 'as-is', without any express or implied warranty. 
// In no event will the authors be held liable for any damages arising from 
// the use of this software.
// 
// Permission is granted to anyone to use this software for any purpose, 
// including commercial applications, and to alter it and redistribute it 
// freely, subject to the following restrictions:
// 
// 1. The origin of this software must not be misrepresented; you must not 
// claim that you wrote the original software. If you use this software in a 
// product, an acknowledgment in the product documentation would be 
// appreciated but is not required.
// 
// 2. Altered source versions must be plainly marked as such, and must not be 
// misrepresented as being the original software.
// 
// 3. This notice may not be removed or altered from any source distribution.
// 
// Author: Jonathan de Halleux
using System;

namespace Spart.Scanners
{
	/// <summary>
	/// A to lower input filter
	/// </summary>
	class ToLowerFilter : IFilter
	{
		/// <summary>
		/// Converts s to lower string
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public String Filter(String s)
		{
			return s.ToLower();
		}

		/// <summary>
		/// Converts i to lower i
		/// </summary>
		/// <param name="i"></param>
		/// <returns></returns>
		public Char Filter(Char i)
		{
			return (Convert.ToString(i).ToLower())[0];
		}
	}
}
