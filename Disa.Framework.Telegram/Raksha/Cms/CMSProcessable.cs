using System;
using System.IO;

namespace Raksha.Cms
{
    public interface CmsProcessable
    {
		/// <summary>
		/// Generic routine to copy out the data we want processed.
		/// </summary>
		/// <remarks>
		/// This routine may be called multiple times.
		/// </remarks>
        void Write(Stream outStream);

        object GetContent();
    }
}
