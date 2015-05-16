using System;
using System.Collections;
using System.Globalization;
using Raksha.Asn1;
using Raksha.Asn1.Kisa;
using Raksha.Asn1.Nist;
using Raksha.Asn1.Ntt;
using Raksha.Asn1.Pkcs;
using Raksha.Crypto;
using Raksha.Crypto.Engines;
using Raksha.Utilities;

namespace Raksha.Security
{
	/// <remarks>
	///  Utility class for creating IWrapper objects from their names/Oids
	/// </remarks>
	public sealed class WrapperUtilities
	{
		private enum WrapAlgorithm { AESWRAP, CAMELLIAWRAP, DESEDEWRAP, RC2WRAP, SEEDWRAP,
			DESEDERFC3211WRAP, AESRFC3211WRAP, CAMELLIARFC3211WRAP };

		private WrapperUtilities()
		{
		}

		private static readonly IDictionary algorithms = Platform.CreateHashtable();
        //private static readonly IDictionary oids = Platform.CreateHashtable();

		static WrapperUtilities()
		{
			// Signal to obfuscation tools not to change enum constants
			((WrapAlgorithm)Enums.GetArbitraryValue(typeof(WrapAlgorithm))).ToString();

			algorithms[NistObjectIdentifiers.IdAes128Wrap.Id] = "AESWRAP";
			algorithms[NistObjectIdentifiers.IdAes192Wrap.Id] = "AESWRAP";
			algorithms[NistObjectIdentifiers.IdAes256Wrap.Id] = "AESWRAP";

			algorithms[NttObjectIdentifiers.IdCamellia128Wrap.Id] = "CAMELLIAWRAP";
			algorithms[NttObjectIdentifiers.IdCamellia192Wrap.Id] = "CAMELLIAWRAP";
			algorithms[NttObjectIdentifiers.IdCamellia256Wrap.Id] = "CAMELLIAWRAP";

			algorithms[PkcsObjectIdentifiers.IdAlgCms3DesWrap.Id] = "DESEDEWRAP";

			algorithms[PkcsObjectIdentifiers.IdAlgCmsRC2Wrap.Id] = "RC2WRAP";

			algorithms[KisaObjectIdentifiers.IdNpkiAppCmsSeedWrap.Id] = "SEEDWRAP";
		}

		public static IWrapper GetWrapper(
			DerObjectIdentifier oid)
		{
			return GetWrapper(oid.Id);
		}

		public static IWrapper GetWrapper(
			string algorithm)
		{
			string upper = algorithm.ToUpperInvariant();
			string mechanism = (string) algorithms[upper];

			if (mechanism == null)
			{
				mechanism = upper;
			}

			try
			{
				WrapAlgorithm wrapAlgorithm = (WrapAlgorithm)Enums.GetEnumValue(
					typeof(WrapAlgorithm), mechanism);

				switch (wrapAlgorithm)
				{
					case WrapAlgorithm.AESWRAP:				return new AesWrapEngine();
					case WrapAlgorithm.CAMELLIAWRAP:		return new CamelliaWrapEngine();
					case WrapAlgorithm.DESEDEWRAP:			return new DesEdeWrapEngine();
					case WrapAlgorithm.RC2WRAP:				return new RC2WrapEngine();
					case WrapAlgorithm.SEEDWRAP:			return new SeedWrapEngine();
					case WrapAlgorithm.DESEDERFC3211WRAP:	return new Rfc3211WrapEngine(new DesEdeEngine());
					case WrapAlgorithm.AESRFC3211WRAP:		return new Rfc3211WrapEngine(new AesFastEngine());
					case WrapAlgorithm.CAMELLIARFC3211WRAP:	return new Rfc3211WrapEngine(new CamelliaEngine());
				}
			}
			catch (ArgumentException)
			{
			}

			// Create an IBufferedCipher and use it as IWrapper (via BufferedCipherWrapper)
			IBufferedCipher blockCipher = CipherUtilities.GetCipher(algorithm);

			if (blockCipher != null)
				return new BufferedCipherWrapper(blockCipher);

			throw new SecurityUtilityException("Wrapper " + algorithm + " not recognised.");
		}

		public static string GetAlgorithmName(
			DerObjectIdentifier oid)
		{
			return (string) algorithms[oid.Id];
		}

		private class BufferedCipherWrapper
			: IWrapper
		{
			private readonly IBufferedCipher cipher;
			private bool forWrapping;

			public BufferedCipherWrapper(
				IBufferedCipher cipher)
			{
				this.cipher = cipher;
			}

			public string AlgorithmName
			{
				get { return cipher.AlgorithmName; }
			}

			public void Init(
				bool				forWrapping,
				ICipherParameters	parameters)
			{
				this.forWrapping = forWrapping;

				cipher.Init(forWrapping, parameters);
			}

			public byte[] Wrap(
				byte[]	input,
				int		inOff,
				int		length)
			{
				if (!forWrapping)
					throw new InvalidOperationException("Not initialised for wrapping");

				return cipher.DoFinal(input, inOff, length);
			}

			public byte[] Unwrap(
				byte[]	input,
				int		inOff,
				int		length)
			{
				if (forWrapping)
					throw new InvalidOperationException("Not initialised for Unwrapping");

				return cipher.DoFinal(input, inOff, length);
			}
		}
	}
}
