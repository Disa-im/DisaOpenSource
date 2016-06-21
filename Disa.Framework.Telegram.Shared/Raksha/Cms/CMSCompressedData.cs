// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CMSCompressedData.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.IO;
using Raksha.Asn1;
using Raksha.Asn1.Cms;
using Raksha.Utilities.Zlib;

namespace Raksha.Cms
{
    /// <summary>
    ///     Containing class for an CMS Compressed Data object.
    /// </summary>
    public class CmsCompressedData
    {
        internal readonly ContentInfo ContentInfoInternal;

        public CmsCompressedData(byte[] compressedData) : this(CmsUtilities.ReadContentInfo(compressedData))
        {
        }

        public CmsCompressedData(Stream compressedDataStream) : this(CmsUtilities.ReadContentInfo(compressedDataStream))
        {
        }

        public CmsCompressedData(ContentInfo contentInfo)
        {
            ContentInfoInternal = contentInfo;
        }

        public ContentInfo ContentInfo
        {
            get { return ContentInfoInternal; }
        }

        /**
		 * Return the uncompressed content.
		 *
		 * @return the uncompressed content
		 * @throws CmsException if there is an exception uncompressing the data.
		 */

        public byte[] GetContent()
        {
            CompressedData comData = CompressedData.GetInstance(ContentInfoInternal.Content);
            ContentInfo content = comData.EncapContentInfo;

            var bytes = (Asn1OctetString) content.Content;
            using (var zIn = new ZInputStream(bytes.GetOctetStream()))
            {
                try
                {
                    return CmsUtilities.StreamToByteArray(zIn);
                }
                catch (IOException e)
                {
                    throw new CmsException("exception reading compressed stream.", e);
                }
            }
        }

        /**
	     * Return the uncompressed content, throwing an exception if the data size
	     * is greater than the passed in limit. If the content is exceeded getCause()
	     * on the CMSException will contain a StreamOverflowException
	     *
	     * @param limit maximum number of bytes to read
	     * @return the content read
	     * @throws CMSException if there is an exception uncompressing the data.
	     */

        public byte[] GetContent(int limit)
        {
            CompressedData comData = CompressedData.GetInstance(ContentInfoInternal.Content);
            ContentInfo content = comData.EncapContentInfo;

            var bytes = (Asn1OctetString) content.Content;

            var zIn = new ZInputStream(new MemoryStream(bytes.GetOctets(), false));

            try
            {
                return CmsUtilities.StreamToByteArray(zIn, limit);
            }
            catch (IOException e)
            {
                throw new CmsException("exception reading compressed stream.", e);
            }
        }

        /**
        * return the ASN.1 encoded representation of this object.
        */

        public byte[] GetEncoded()
        {
            return ContentInfoInternal.GetEncoded();
        }
    }
}
