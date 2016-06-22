// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SecureRandom.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using Raksha.Crypto.Digests;
using Raksha.Crypto.Prng;

namespace Raksha.Security
{
    public class SecureRandom : Random
    {
        // Note: all objects of this class should be deriving their random data from
        // a single generator appropriate to the digest being used.
        private static readonly IRandomGenerator Sha1Generator = new DigestRandomGenerator(new Sha1Digest());
        private static readonly IRandomGenerator Sha256Generator = new DigestRandomGenerator(new Sha256Digest());

        private static readonly SecureRandom[] master = {null};
        private static readonly double DoubleScale = System.Math.Pow(2.0, 64.0);

        protected readonly IRandomGenerator Generator;

        public SecureRandom() : this(Sha1Generator)
        {
            SetSeed(GetSeed(8));
        }

        public SecureRandom(byte[] inSeed) : this(Sha1Generator)
        {
            SetSeed(inSeed);
        }

        /// <summary>Use the specified instance of IRandomGenerator as random source.</summary>
        /// <remarks>
        ///     This constructor performs no seeding of either the <c>IRandomGenerator</c> or the
        ///     constructed <c>SecureRandom</c>. It is the responsibility of the client to provide
        ///     proper seed material as necessary/appropriate for the given <c>IRandomGenerator</c>
        ///     implementation.
        /// </remarks>
        /// <param name="generator">The source to generate all random bytes from.</param>
        public SecureRandom(IRandomGenerator generator) : base(0)
        {
            this.Generator = generator;
        }

        private static SecureRandom Master
        {
            get
            {
                if (master[0] == null)
                {
                    IRandomGenerator gen = Sha256Generator;
                    gen = new ReversedWindowGenerator(gen, 32);
                    SecureRandom sr = master[0] = new SecureRandom(gen);

                    sr.SetSeed(DateTime.Now.Ticks);
                    sr.SetSeed(new ThreadedSeedGenerator().GenerateSeed(24, true));
                    sr.GenerateSeed(1 + sr.Next(32));
                }

                return master[0];
            }
        }

        public static SecureRandom GetInstance(string algorithm)
        {
            // TODO Compared to JDK, we don't auto-seed if the client forgets - problem?

            // TODO Support all digests more generally, by stripping PRNG and calling DigestUtilities?
            string drgName = algorithm.ToUpperInvariant();

            IRandomGenerator drg = null;
            if (drgName == "SHA1PRNG")
            {
                drg = Sha1Generator;
            }
            else if (drgName == "SHA256PRNG")
            {
                drg = Sha256Generator;
            }

            if (drg != null)
            {
                return new SecureRandom(drg);
            }

            throw new ArgumentException("Unrecognised PRNG algorithm: " + algorithm, "algorithm");
        }

        public static byte[] GetSeed(int length)
        {
            return Master.GenerateSeed(length);
        }

        public virtual byte[] GenerateSeed(int length)
        {
            SetSeed(DateTime.Now.Ticks);

            var rv = new byte[length];
            NextBytes(rv);
            return rv;
        }

        public virtual void SetSeed(byte[] inSeed)
        {
            Generator.AddSeedMaterial(inSeed);
        }

        public virtual void SetSeed(long seed)
        {
            Generator.AddSeedMaterial(seed);
        }

        public override int Next()
        {
            for (;;)
            {
                int i = NextInt() & int.MaxValue;

                if (i != int.MaxValue)
                {
                    return i;
                }
            }
        }

        public override int Next(int maxValue)
        {
            if (maxValue < 2)
            {
                if (maxValue < 0)
                {
                    throw new ArgumentOutOfRangeException("maxValue < 0");
                }

                return 0;
            }

            // Test whether maxValue is a power of 2
            if ((maxValue & -maxValue) == maxValue)
            {
                int val = NextInt() & int.MaxValue;
                long lr = (maxValue*(long) val) >> 31;
                return (int) lr;
            }

            int bits, result;
            do
            {
                bits = NextInt() & int.MaxValue;
                result = bits%maxValue;
            } while (bits - result + (maxValue - 1) < 0); // Ignore results near overflow

            return result;
        }

        public override int Next(int minValue, int maxValue)
        {
            if (maxValue <= minValue)
            {
                if (maxValue == minValue)
                {
                    return minValue;
                }

                throw new ArgumentException("maxValue cannot be less than minValue");
            }

            int diff = maxValue - minValue;
            if (diff > 0)
            {
                return minValue + Next(diff);
            }

            for (;;)
            {
                int i = NextInt();

                if (i >= minValue && i < maxValue)
                {
                    return i;
                }
            }
        }

        public override void NextBytes(byte[] buffer)
        {
            Generator.NextBytes(buffer);
        }

        public virtual void NextBytes(byte[] buffer, int start, int length)
        {
            Generator.NextBytes(buffer, start, length);
        }

        public override double NextDouble()
        {
            return Convert.ToDouble((ulong) NextLong())/DoubleScale;
        }

        public virtual int NextInt()
        {
            var intBytes = new byte[4];
            NextBytes(intBytes);

            int result = 0;
            for (int i = 0; i < 4; i++)
            {
                result = (result << 8) + (intBytes[i] & 0xff);
            }

            return result;
        }

        public virtual long NextLong()
        {
            return ((long) (uint) NextInt() << 32) | (uint) NextInt();
        }
    }
}
