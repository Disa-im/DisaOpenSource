using System;

namespace Raksha.Ocsp
{
	public class OcspException
		: Exception
	{
		public OcspException()
		{
		}

		public OcspException(
			string message)
			: base(message)
		{
		}

		public OcspException(
			string		message,
			Exception	e)
			: base(message, e)
		{
		}
	}
}
