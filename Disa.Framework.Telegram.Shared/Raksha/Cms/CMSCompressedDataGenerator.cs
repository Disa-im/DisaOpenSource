// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CMSCompressedDataGenerator.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.IO;
using Raksha.Asn1;
using Raksha.Asn1.Cms;
using Raksha.Asn1.X509;
using Raksha.Utilities.Zlib;

namespace Raksha.Cms
{
    /**
    * General class for generating a compressed CMS message.
    * <p>
    * A simple example of usage.</p>
    * <p>
    * <pre>
    *      CMSCompressedDataGenerator fact = new CMSCompressedDataGenerator();
    *      CMSCompressedData data = fact.Generate(content, algorithm);
    * </pre>
    * </p>
    */

    public class CmsCompressedDataGenerator
    {
        public const string ZLib = "1.2.840.113549.1.9.16.3.8";

        /**
        * Generate an object that contains an CMS Compressed Data
        */

        public CmsCompressedData Generate(CmsProcessable content, string compressionOid)
        {
            AlgorithmIdentifier comAlgId;
            Asn1OctetString comOcts;

            try
            {
                using (var bOut = new MemoryStream())
                {
                    using (var zOut = new ZOutputStream(bOut, JZlib.Z_DEFAULT_COMPRESSION))
                    {
                        content.Write(zOut);
                    }

                    comAlgId = new AlgorithmIdentifier(new DerObjectIdentifier(compressionOid));
                    comOcts = new BerOctetString(bOut.ToArray());
                }
            }
            catch (IOException e)
            {
                throw new CmsException("exception encoding data.", e);
            }

            var comContent = new ContentInfo(CmsObjectIdentifiers.Data, comOcts);
            var contentInfo = new ContentInfo(CmsObjectIdentifiers.CompressedData, new CompressedData(comAlgId, comContent));

            return new CmsCompressedData(contentInfo);
        }
    }
}
