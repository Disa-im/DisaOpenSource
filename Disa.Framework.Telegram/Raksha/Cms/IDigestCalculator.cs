using System;

namespace Raksha.Cms
{
	internal interface IDigestCalculator
	{
		byte[] GetDigest();
	}
}
