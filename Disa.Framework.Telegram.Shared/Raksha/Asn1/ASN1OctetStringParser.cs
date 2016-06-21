using System.IO;

namespace Raksha.Asn1
{
	public interface Asn1OctetStringParser
		: IAsn1Convertible
	{
		Stream GetOctetStream();
	}
}
