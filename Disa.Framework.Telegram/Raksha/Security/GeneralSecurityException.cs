using System;

namespace Raksha.Security
{
	public class GeneralSecurityException
		: Exception
	{
		public GeneralSecurityException()
			: base()
		{
		}

		public GeneralSecurityException(
			string message)
			: base(message)
		{
		}

		public GeneralSecurityException(
			string		message,
			Exception	exception)
			: base(message, exception)
		{
		}
	}
}
