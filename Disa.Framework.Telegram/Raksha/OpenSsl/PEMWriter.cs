// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PEMWriter.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.IO;
using Raksha.Security;
using Raksha.Utilities.IO.Pem;

namespace Raksha.OpenSsl
{
    /// <summary>
    ///     General purpose writer for OpenSSL PEM objects.
    /// </summary>
    public class PemWriter : Utilities.IO.Pem.PemWriter
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PemWriter" /> class.
        /// </summary>
        /// <param name="writer">The TextWriter object to write the output to.</param>
        public PemWriter(TextWriter writer) : base(writer)
        {
        }

        public void WriteObject(object obj)
        {
            try
            {
                base.WriteObject(new MiscPemGenerator(obj));
            }
            catch (PemGenerationException e)
            {
                if (e.InnerException is IOException)
                {
                    throw e.InnerException;
                }

                throw e;
            }
        }

        public void WriteObject(object obj, string algorithm, char[] password, SecureRandom random)
        {
            base.WriteObject(new MiscPemGenerator(obj, algorithm, password, random));
        }
    }
}
