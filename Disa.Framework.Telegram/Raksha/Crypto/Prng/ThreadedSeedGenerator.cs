// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ThreadedSeedGenerator.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Raksha.Crypto.Prng
{
    /**
     * A thread based seed generator - one source of randomness.
     * <p>
     * Based on an idea from Marcus Lippert.
     * </p>
     */

    public class ThreadedSeedGenerator
    {
        /**
         * Generate seed bytes. Set fast to false for best quality.
         * <p>
         * If fast is set to true, the code should be round about 8 times faster when
         * generating a long sequence of random bytes. 20 bytes of random values using
         * the fast mode take less than half a second on a Nokia e70. If fast is set to false,
         * it takes round about 2500 ms.
         * </p>
         * @param numBytes the number of bytes to generate
         * @param fast true if fast mode should be used
         */

        public byte[] GenerateSeed(int numBytes, bool fast)
        {
            return new SeedGenerator().GenerateSeed(numBytes, fast);
        }

        private class SeedGenerator
        {
            private volatile int _counter;
            private volatile bool _stop;

            private void Run()
            {
                while (!_stop)
                {
                    _counter++;
                }
            }

            public byte[] GenerateSeed(int numBytes, bool fast)
            {
                _counter = 0;
                _stop = false;

                var result = new byte[numBytes];
                int last = 0;
                int end = fast ? numBytes : numBytes*8;

                Task.Run(() => Run());

                for (int i = 0; i < end; i++)
                {
                    while (_counter == last)
                    {
                        SpinWait.SpinUntil(() => false, 1);
                    }

                    last = _counter;

                    if (fast)
                    {
                        result[i] = (byte) last;
                    }
                    else
                    {
                        int bytepos = i/8;
                        result[bytepos] = (byte) ((result[bytepos] << 1) | (last & 1));
                    }
                }

                _stop = true;

                return result;
            }
        }
    }
}
