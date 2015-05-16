using System;
using System.IO;

using Raksha.Asn1;
using Raksha.Asn1.X509;
using Raksha.Crypto;
using Raksha.Crypto.Parameters;
using Raksha.Utilities.Date;
using Raksha.Utilities.IO;
using Raksha.Security;

namespace Raksha.Cms
{
	public class CmsAuthenticatedGenerator
		: CmsEnvelopedGenerator
	{
		/**
		* base constructor
		*/
		public CmsAuthenticatedGenerator()
		{
		}

		/**
		* constructor allowing specific source of randomness
		*
		* @param rand instance of SecureRandom to use
		*/
		public CmsAuthenticatedGenerator(
			SecureRandom rand)
			: base(rand)
		{
		}
	}
}
