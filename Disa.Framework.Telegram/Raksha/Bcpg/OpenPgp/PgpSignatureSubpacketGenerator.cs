using System;
using System.Collections;
using Raksha.Bcpg.Sig;
using Raksha.Utilities;

namespace Raksha.Bcpg.OpenPgp
{
	/// <remarks>Generator for signature subpackets.</remarks>
    public class PgpSignatureSubpacketGenerator
    {
        private IList list = Platform.CreateArrayList();

		public void SetRevocable(
            bool	isCritical,
            bool	isRevocable)
        {
            list.Add(new Revocable(isCritical, isRevocable));
        }

		public void SetExportable(
            bool	isCritical,
            bool	isExportable)
        {
            list.Add(new Exportable(isCritical, isExportable));
        }

		/// <summary>
		/// Add a TrustSignature packet to the signature. The values for depth and trust are largely
		/// installation dependent but there are some guidelines in RFC 4880 - 5.2.3.13.
		/// </summary>
		/// <param name="isCritical">true if the packet is critical.</param>
		/// <param name="depth">depth level.</param>
		/// <param name="trustAmount">trust amount.</param>
		public void SetTrust(
            bool	isCritical,
            int		depth,
            int		trustAmount)
        {
            list.Add(new TrustSignature(isCritical, depth, trustAmount));
        }

		/// <summary>
		/// Set the number of seconds a key is valid for after the time of its creation.
		/// A value of zero means the key never expires.
		/// </summary>
		/// <param name="isCritical">True, if should be treated as critical, false otherwise.</param>
		/// <param name="seconds">The number of seconds the key is valid, or zero if no expiry.</param>
        public void SetKeyExpirationTime(
            bool	isCritical,
            long	seconds)
        {
            list.Add(new KeyExpirationTime(isCritical, seconds));
        }

		/// <summary>
		/// Set the number of seconds a signature is valid for after the time of its creation.
		/// A value of zero means the signature never expires.
		/// </summary>
		/// <param name="isCritical">True, if should be treated as critical, false otherwise.</param>
		/// <param name="seconds">The number of seconds the signature is valid, or zero if no expiry.</param>
        public void SetSignatureExpirationTime(
            bool	isCritical,
            long	seconds)
        {
            list.Add(new SignatureExpirationTime(isCritical, seconds));
        }

		/// <summary>
		/// Set the creation time for the signature.
		/// <p>
		/// Note: this overrides the generation of a creation time when the signature
		/// is generated.</p>
		/// </summary>
		public void SetSignatureCreationTime(
			bool		isCritical,
			DateTime	date)
		{
			list.Add(new SignatureCreationTime(isCritical, date));
		}

		public void SetPreferredHashAlgorithms(
            bool	isCritical,
            int[]	algorithms)
        {
            list.Add(new PreferredAlgorithms(SignatureSubpacketTag.PreferredHashAlgorithms, isCritical, algorithms));
        }

		public void SetPreferredSymmetricAlgorithms(
            bool	isCritical,
            int[]	algorithms)
        {
            list.Add(new PreferredAlgorithms(SignatureSubpacketTag.PreferredSymmetricAlgorithms, isCritical, algorithms));
        }

		public void SetPreferredCompressionAlgorithms(
            bool	isCritical,
            int[]	algorithms)
        {
            list.Add(new PreferredAlgorithms(SignatureSubpacketTag.PreferredCompressionAlgorithms, isCritical, algorithms));
        }

		public void SetKeyFlags(
            bool	isCritical,
            int		flags)
        {
            list.Add(new KeyFlags(isCritical, flags));
        }

		public void SetSignerUserId(
            bool	isCritical,
            string	userId)
        {
            if (userId == null)
                throw new ArgumentNullException("userId");

			list.Add(new SignerUserId(isCritical, userId));
        }

		public void SetEmbeddedSignature(
			bool			isCritical,
			PgpSignature	pgpSignature)
		{
			byte[] sig = pgpSignature.GetEncoded();
			byte[] data;

			// TODO Should be >= ?
			if (sig.Length - 1 > 256)
			{
				data = new byte[sig.Length - 3];
			}
			else
			{
				data = new byte[sig.Length - 2];
			}

			Array.Copy(sig, sig.Length - data.Length, data, 0, data.Length);

			list.Add(new EmbeddedSignature(isCritical, data));
		}

		public void SetPrimaryUserId(
            bool	isCritical,
            bool	isPrimaryUserId)
        {
            list.Add(new PrimaryUserId(isCritical, isPrimaryUserId));
        }

		public void SetNotationData(
			bool	isCritical,
			bool	isHumanReadable,
			string	notationName,
			string	notationValue)
		{
			list.Add(new NotationData(isCritical, isHumanReadable, notationName, notationValue));
		}

		public PgpSignatureSubpacketVector Generate()
        {
            SignatureSubpacket[] a = new SignatureSubpacket[list.Count];
            for (int i = 0; i < list.Count; ++i)
            {
                a[i] = (SignatureSubpacket)list[i];
            }
            return new PgpSignatureSubpacketVector(a);
        }
    }
}
