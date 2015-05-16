using System;
using Raksha.Security;

namespace Raksha.Crypto.Tls
{
	public interface TlsClientContext
	{
		SecureRandom SecureRandom { get; }

		SecurityParameters SecurityParameters { get; }

		object UserObject { get; set; }
	}
}
