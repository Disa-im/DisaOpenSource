using System;

namespace Raksha.OpenSsl
{
	public interface IPasswordFinder
	{
		char[] GetPassword();
	}
}
