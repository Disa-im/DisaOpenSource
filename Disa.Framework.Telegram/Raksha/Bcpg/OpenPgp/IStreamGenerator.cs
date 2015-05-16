using System;

namespace Raksha.Bcpg.OpenPgp
{
	public interface IStreamGenerator : IDisposable
	{
		void Close();
	}
}
