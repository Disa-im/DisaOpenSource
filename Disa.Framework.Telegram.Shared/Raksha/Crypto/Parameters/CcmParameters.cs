using System;

namespace Raksha.Crypto.Parameters
{
    public class CcmParameters
        : AeadParameters 
    {
		/**
		 * Base constructor.
		 * 
		 * @param key key to be used by underlying cipher
		 * @param macSize macSize in bits
		 * @param nonce nonce to be used
		 * @param associatedText associated text, if any
		 */
		public CcmParameters(
			KeyParameter	key,
			int				macSize,
			byte[]			nonce,
			byte[]			associatedText)
			: base(key, macSize, nonce, associatedText)
		{
		}
	}
}
