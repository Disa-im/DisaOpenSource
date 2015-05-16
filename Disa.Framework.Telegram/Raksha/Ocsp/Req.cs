using System;
using System.Collections;
using System.IO;

using Raksha.Asn1;
using Raksha.Asn1.Ocsp;
using Raksha.Asn1.X509;
using Raksha.X509;

namespace Raksha.Ocsp
{
	public class Req
		: X509ExtensionBase
	{
		private Request req;

		public Req(
			Request req)
		{
			this.req = req;
		}

		public CertificateID GetCertID()
		{
			return new CertificateID(req.ReqCert);
		}

		public X509Extensions SingleRequestExtensions
		{
			get { return req.SingleRequestExtensions; }
		}

		protected override X509Extensions GetX509Extensions()
		{
			return SingleRequestExtensions;
		}
	}
}
