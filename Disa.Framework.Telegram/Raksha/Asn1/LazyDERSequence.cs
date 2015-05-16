using System;
using System.Collections;
using System.Diagnostics;

namespace Raksha.Asn1
{
	internal class LazyDerSequence
		: DerSequence
	{
		private byte[]	encoded;
		private bool	parsed = false;

		internal LazyDerSequence(
			byte[] encoded)
		{
			this.encoded = encoded;
		}

		private void Parse()
		{
			lock (this)
			{
				if (!parsed)
				{
					Asn1InputStream e = new LazyAsn1InputStream(encoded);

					Asn1Object o;
					while ((o = e.ReadObject()) != null)
					{
						AddObject(o);
					}

					encoded = null;
					parsed = true;
				}
			}
		}

		public override Asn1Encodable this[int index]
		{
			get
			{
				Parse();

				return base[index];
			}
		}

		public override IEnumerator GetEnumerator()
		{
			Parse();

			return base.GetEnumerator();
		}

		public override int Count
		{
			get
			{
				Parse();

				return base.Count;
			}
		}

		internal override void Encode(
			DerOutputStream derOut)
		{
			lock (this)
			{
				if (parsed)
				{
					base.Encode(derOut);
				}
				else
				{
					derOut.WriteEncoded(Asn1Tags.Sequence | Asn1Tags.Constructed, encoded);
				}
			}
		}
	}
}
