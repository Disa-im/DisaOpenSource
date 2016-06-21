using System;
using System.IO;

namespace Raksha.Cms
{
    public class CmsStreamException
        : IOException
    {
		public CmsStreamException()
		{
		}

		public CmsStreamException(
			string name)
			: base(name)
        {
        }

		public CmsStreamException(
			string		name,
			Exception	e)
			: base(name, e)
        {
        }
    }
}
