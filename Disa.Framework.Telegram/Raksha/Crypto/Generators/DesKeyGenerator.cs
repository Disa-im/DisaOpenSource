using System;
using Raksha.Crypto.Parameters;

namespace Raksha.Crypto.Generators
{
    public class DesKeyGenerator
		: CipherKeyGenerator
    {
		public DesKeyGenerator()
		{
		}

		internal DesKeyGenerator(
			int defaultStrength)
			: base(defaultStrength)
		{
		}

		/**
		* initialise the key generator - if strength is set to zero
		* the key generated will be 64 bits in size, otherwise
		* strength can be 64 or 56 bits (if you don't count the parity bits).
		*
		* @param param the parameters to be used for key generation
		*/
		protected override void engineInit(
			KeyGenerationParameters parameters)
		{
			base.engineInit(parameters);

			if (strength == 0 || strength == (56 / 8))
			{
				strength = DesParameters.DesKeyLength;
			}
			else if (strength != DesParameters.DesKeyLength)
			{
				throw new ArgumentException(
					"DES key must be " + (DesParameters.DesKeyLength * 8) + " bits long.");
			}
		}

		protected override byte[] engineGenerateKey()
        {
            byte[] newKey;

			do
            {
				newKey = random.GenerateSeed(DesParameters.DesKeyLength);
				DesParameters.SetOddParity(newKey);
            }
            while (DesParameters.IsWeakKey(newKey, 0));

			return newKey;
        }
    }
}
