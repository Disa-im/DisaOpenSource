// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CMSProcessableInputStream.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using Raksha.Utilities.IO;

namespace Raksha.Cms
{
    public class CmsProcessableInputStream : CmsProcessable, ICmsReadable
    {
        private readonly Stream _input;
        private bool _used;

        public CmsProcessableInputStream(Stream input)
        {
            _input = input;
        }

        public void Write(Stream output)
        {
            CheckSingleUsage();

            Streams.PipeAll(_input, output);
            _input.Dispose();
        }

        public object GetContent()
        {
            return GetInputStream();
        }

        public Stream GetInputStream()
        {
            CheckSingleUsage();

            return _input;
        }

        private void CheckSingleUsage()
        {
            lock (this)
            {
                if (_used)
                {
                    throw new InvalidOperationException("CmsProcessableInputStream can only be used once");
                }

                _used = true;
            }
        }
    }
}
