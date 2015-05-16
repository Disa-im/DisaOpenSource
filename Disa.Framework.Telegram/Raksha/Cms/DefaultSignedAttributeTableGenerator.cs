using System;
using System.Collections;
using Raksha.Asn1;
using Raksha.Asn1.Cms;
using Raksha.Utilities;

namespace Raksha.Cms
{
	/**
	 * Default signed attributes generator.
	 */
	public class DefaultSignedAttributeTableGenerator
		: CmsAttributeTableGenerator
	{
		private readonly IDictionary table;

		/**
		 * Initialise to use all defaults
		 */
		public DefaultSignedAttributeTableGenerator()
		{
			table = Platform.CreateHashtable();
		}

		/**
		 * Initialise with some extra attributes or overrides.
		 *
		 * @param attributeTable initial attribute table to use.
		 */
		public DefaultSignedAttributeTableGenerator(
			AttributeTable attributeTable)
		{
			if (attributeTable != null)
			{
				table = attributeTable.ToDictionary();
			}
			else
			{
				table = Platform.CreateHashtable();
			}
		}

#if SILVERLIGHT || PCL
		/**
		 * Create a standard attribute table from the passed in parameters - this will
		 * normally include contentType, signingTime, and messageDigest. If the constructor
		 * using an AttributeTable was used, entries in it for contentType, signingTime, and
		 * messageDigest will override the generated ones.
		 *
		 * @param parameters source parameters for table generation.
		 *
		 * @return a filled in Hashtable of attributes.
		 */
		protected virtual IDictionary CreateStandardAttributeTable(
			IDictionary parameters)
		{
            IDictionary std = Platform.CreateHashtable(table);
            DoCreateStandardAttributeTable(parameters, std);
            return std;
		}
#else
        /**
		 * Create a standard attribute table from the passed in parameters - this will
		 * normally include contentType, signingTime, and messageDigest. If the constructor
		 * using an AttributeTable was used, entries in it for contentType, signingTime, and
		 * messageDigest will override the generated ones.
		 *
		 * @param parameters source parameters for table generation.
		 *
		 * @return a filled in Hashtable of attributes.
		 */
		protected virtual Hashtable CreateStandardAttributeTable(
			IDictionary parameters)
		{
            Hashtable std = new Hashtable(table);
            DoCreateStandardAttributeTable(parameters, std);
			return std;
		}
#endif

        private void DoCreateStandardAttributeTable(IDictionary parameters, IDictionary std)
        {
            // contentType will be absent if we're trying to generate a counter signature.
            if (parameters.Contains(CmsAttributeTableParameter.ContentType))
            {
                if (!std.Contains(CmsAttributes.ContentType))
                {
                    DerObjectIdentifier contentType = (DerObjectIdentifier)
                        parameters[CmsAttributeTableParameter.ContentType];
                    Asn1.Cms.Attribute attr = new Asn1.Cms.Attribute(CmsAttributes.ContentType,
                        new DerSet(contentType));
                    std[attr.AttrType] = attr;
                }
            }

            if (!std.Contains(CmsAttributes.SigningTime))
            {
                Asn1.Cms.Attribute attr = new Asn1.Cms.Attribute(CmsAttributes.SigningTime,
                    new DerSet(new Time(DateTime.UtcNow)));
                std[attr.AttrType] = attr;
            }

            if (!std.Contains(CmsAttributes.MessageDigest))
            {
                byte[] messageDigest = (byte[])parameters[CmsAttributeTableParameter.Digest];
                Asn1.Cms.Attribute attr = new Asn1.Cms.Attribute(CmsAttributes.MessageDigest,
                    new DerSet(new DerOctetString(messageDigest)));
                std[attr.AttrType] = attr;
            }
        }

        /**
		 * @param parameters source parameters
		 * @return the populated attribute table
		 */
		public virtual AttributeTable GetAttributes(
			IDictionary parameters)
		{
            IDictionary table = CreateStandardAttributeTable(parameters);
			return new AttributeTable(table);
		}
	}
}
