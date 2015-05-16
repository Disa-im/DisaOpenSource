// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NaccacheSternEngine.cs">
//     Copyright (c) 2014 Alexander Logger.
//     Copyright (c) 2000 - 2013 The Legion of the Bouncy Castle Inc. (http://www.bouncycastle.org).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Diagnostics;
using BigMath;
using Raksha.Crypto.Parameters;
using Raksha.Math;
using Raksha.Utilities;

namespace Raksha.Crypto.Engines
{
    /// <summary>
    ///     NaccacheStern Engine.
    /// </summary>
    /// <remarks>
    ///     For details on this cipher, please see http://www.gemplus.com/smart/rd/publications/pdf/NS98pkcs.pdf
    /// </remarks>
    public class NaccacheSternEngine : IAsymmetricBlockCipher
    {
        private bool _debug;
        private bool _forEncryption;

        private NaccacheSternKeyParameters _key;

        private IList[] _lookup;

        public bool ShowDebug
        {
            get { return _debug; }
            set { _debug = value; }
        }

        public string AlgorithmName
        {
            get { return "NaccacheStern"; }
        }

        /**
        * Initializes this algorithm. Must be called before all other Functions.
        *
        * @see org.bouncycastle.crypto.AsymmetricBlockCipher#init(bool,
        *      org.bouncycastle.crypto.CipherParameters)
        */

        public void Init(bool forEncryption, ICipherParameters parameters)
        {
            _forEncryption = forEncryption;

            if (parameters is ParametersWithRandom)
            {
                parameters = ((ParametersWithRandom) parameters).Parameters;
            }

            _key = (NaccacheSternKeyParameters) parameters;

            // construct lookup table for faster decryption if necessary
            if (!_forEncryption)
            {
                if (_debug)
                {
                    Debug.WriteLine("Constructing lookup Array");
                }
                var priv = (NaccacheSternPrivateKeyParameters) _key;
                IList primes = priv.SmallPrimesList;
                _lookup = new IList[primes.Count];
                for (int i = 0; i < primes.Count; i++)
                {
                    var actualPrime = (BigInteger) primes[i];
                    int actualPrimeValue = actualPrime.IntValue;

                    _lookup[i] = Platform.CreateArrayList(actualPrimeValue);
                    _lookup[i].Add(BigInteger.One);

                    if (_debug)
                    {
                        Debug.WriteLine("Constructing lookup ArrayList for " + actualPrimeValue);
                    }

                    BigInteger accJ = BigInteger.Zero;

                    for (int j = 1; j < actualPrimeValue; j++)
                    {
//						BigInteger bigJ = BigInteger.ValueOf(j);
//						accJ = priv.PhiN.Multiply(bigJ);
                        accJ = accJ.Add(priv.PhiN);
                        BigInteger comp = accJ.Divide(actualPrime);
                        _lookup[i].Add(priv.G.ModPow(comp, priv.Modulus));
                    }
                }
            }
        }

        /**
        * Returns the input block size of this algorithm.
        *
        * @see org.bouncycastle.crypto.AsymmetricBlockCipher#GetInputBlockSize()
        */

        public int GetInputBlockSize()
        {
            if (_forEncryption)
            {
                // We can only encrypt values up to lowerSigmaBound
                return (_key.LowerSigmaBound + 7)/8 - 1;
            }
            // We pad to modulus-size bytes for easier decryption.
//				return key.Modulus.ToByteArray().Length;
            return _key.Modulus.BitLength/8 + 1;
        }

        /**
        * Returns the output block size of this algorithm.
        *
        * @see org.bouncycastle.crypto.AsymmetricBlockCipher#GetOutputBlockSize()
        */

        public int GetOutputBlockSize()
        {
            if (_forEncryption)
            {
                // encrypted Data is always padded up to modulus size
//				return key.Modulus.ToByteArray().Length;
                return _key.Modulus.BitLength/8 + 1;
            }
            // decrypted Data has upper limit lowerSigmaBound
            return (_key.LowerSigmaBound + 7)/8 - 1;
        }

        /**
        * Process a single Block using the Naccache-Stern algorithm.
        *
        * @see org.bouncycastle.crypto.AsymmetricBlockCipher#ProcessBlock(byte[],
        *      int, int)
        */

        public byte[] ProcessBlock(byte[] inBytes, int inOff, int length)
        {
            if (_key == null)
            {
                throw new InvalidOperationException("NaccacheStern engine not initialised");
            }
            if (length > (GetInputBlockSize() + 1))
            {
                throw new DataLengthException("input too large for Naccache-Stern cipher.\n");
            }

            if (!_forEncryption)
            {
                // At decryption make sure that we receive padded data blocks
                if (length < GetInputBlockSize())
                {
                    throw new InvalidCipherTextException("BlockLength does not match modulus for Naccache-Stern cipher.\n");
                }
            }

            // transform input into BigInteger
            var input = new BigInteger(1, inBytes, inOff, length);

            if (_debug)
            {
                Debug.WriteLine("input as BigInteger: " + input);
            }

            byte[] output;
            if (_forEncryption)
            {
                output = Encrypt(input);
            }
            else
            {
                IList plain = Platform.CreateArrayList();
                var priv = (NaccacheSternPrivateKeyParameters) _key;
                IList primes = priv.SmallPrimesList;
                // Get Chinese Remainders of CipherText
                for (int i = 0; i < primes.Count; i++)
                {
                    BigInteger exp = input.ModPow(priv.PhiN.Divide((BigInteger) primes[i]), priv.Modulus);
                    IList al = _lookup[i];
                    if (_lookup[i].Count != ((BigInteger) primes[i]).IntValue)
                    {
                        if (_debug)
                        {
                            Debug.WriteLine("Prime is " + primes[i] + ", lookup table has size " + al.Count);
                        }
                        throw new InvalidCipherTextException("Error in lookup Array for " + ((BigInteger) primes[i]).IntValue +
                            ": Size mismatch. Expected ArrayList with length " + ((BigInteger) primes[i]).IntValue + " but found ArrayList of length " + _lookup[i].Count);
                    }
                    int lookedup = al.IndexOf(exp);

                    if (lookedup == -1)
                    {
                        if (_debug)
                        {
                            Debug.WriteLine("Actual prime is " + primes[i]);
                            Debug.WriteLine("Decrypted value is " + exp);

                            Debug.WriteLine("LookupList for " + primes[i] + " with size " + _lookup[i].Count + " is: ");
                            for (int j = 0; j < _lookup[i].Count; j++)
                            {
                                Debug.WriteLine(_lookup[i][j]);
                            }
                        }
                        throw new InvalidCipherTextException("Lookup failed");
                    }
                    plain.Add(BigInteger.ValueOf(lookedup));
                }
                BigInteger test = ChineseRemainder(plain, primes);

                // Should not be used as an oracle, so reencrypt output to see
                // if it corresponds to input

                // this breaks probabilisic encryption, so disable it. Anyway, we do
                // use the first n primes for key generation, so it is pretty easy
                // to guess them. But as stated in the paper, this is not a security
                // breach. So we can just work with the correct sigma.

                // if (debug) {
                //      Debug.WriteLine("Decryption is " + test);
                // }
                // if ((key.G.ModPow(test, key.Modulus)).Equals(input)) {
                //      output = test.ToByteArray();
                // } else {
                //      if(debug){
                //          Debug.WriteLine("Engine seems to be used as an oracle,
                //          returning null");
                //      }
                //      output = null;
                // }

                output = test.ToByteArray();
            }

            return output;
        }

        /**
        * Encrypts a BigInteger aka Plaintext with the public key.
        *
        * @param plain
        *            The BigInteger to encrypt
        * @return The byte[] representation of the encrypted BigInteger (i.e.
        *         crypted.toByteArray())
        */

        public byte[] Encrypt(BigInteger plain)
        {
            // Always return modulus size values 0-padded at the beginning
            // 0-padding at the beginning is correctly parsed by BigInteger :)
//			byte[] output = key.Modulus.ToByteArray();
//			Array.Clear(output, 0, output.Length);
            var output = new byte[_key.Modulus.BitLength/8 + 1];

            byte[] tmp = _key.G.ModPow(plain, _key.Modulus).ToByteArray();
            Array.Copy(tmp, 0, output, output.Length - tmp.Length, tmp.Length);
            if (_debug)
            {
                Debug.WriteLine("Encrypted value is:  " + new BigInteger(output));
            }
            return output;
        }

        /**
        * Adds the contents of two encrypted blocks mod sigma
        *
        * @param block1
        *            the first encrypted block
        * @param block2
        *            the second encrypted block
        * @return encrypt((block1 + block2) mod sigma)
        * @throws InvalidCipherTextException
        */

        public byte[] AddCryptedBlocks(byte[] block1, byte[] block2)
        {
            // check for correct blocksize
            if (_forEncryption)
            {
                if ((block1.Length > GetOutputBlockSize()) || (block2.Length > GetOutputBlockSize()))
                {
                    throw new InvalidCipherTextException("BlockLength too large for simple addition.\n");
                }
            }
            else
            {
                if ((block1.Length > GetInputBlockSize()) || (block2.Length > GetInputBlockSize()))
                {
                    throw new InvalidCipherTextException("BlockLength too large for simple addition.\n");
                }
            }

            // calculate resulting block
            var m1Crypt = new BigInteger(1, block1);
            var m2Crypt = new BigInteger(1, block2);
            BigInteger m1m2Crypt = m1Crypt.Multiply(m2Crypt);
            m1m2Crypt = m1m2Crypt.Mod(_key.Modulus);
            if (_debug)
            {
                Debug.WriteLine("c(m1) as BigInteger:....... " + m1Crypt);
                Debug.WriteLine("c(m2) as BigInteger:....... " + m2Crypt);
                Debug.WriteLine("c(m1)*c(m2)%n = c(m1+m2)%n: " + m1m2Crypt);
            }

            //byte[] output = key.Modulus.ToByteArray();
            //Array.Clear(output, 0, output.Length);
            var output = new byte[_key.Modulus.BitLength/8 + 1];

            byte[] m1m2CryptBytes = m1m2Crypt.ToByteArray();
            Array.Copy(m1m2CryptBytes, 0, output, output.Length - m1m2CryptBytes.Length, m1m2CryptBytes.Length);

            return output;
        }

        /**
        * Convenience Method for data exchange with the cipher.
        *
        * Determines blocksize and splits data to blocksize.
        *
        * @param data the data to be processed
        * @return the data after it went through the NaccacheSternEngine.
        * @throws InvalidCipherTextException
        */

        public byte[] ProcessData(byte[] data)
        {
            if (_debug)
            {
                Debug.WriteLine(string.Empty);
            }
            if (data.Length > GetInputBlockSize())
            {
                int inBlocksize = GetInputBlockSize();
                int outBlocksize = GetOutputBlockSize();
                if (_debug)
                {
                    Debug.WriteLine("Input blocksize is:  " + inBlocksize + " bytes");
                    Debug.WriteLine("Output blocksize is: " + outBlocksize + " bytes");
                    Debug.WriteLine("Data has length:.... " + data.Length + " bytes");
                }
                int datapos = 0;
                int retpos = 0;
                var retval = new byte[(data.Length/inBlocksize + 1)*outBlocksize];
                while (datapos < data.Length)
                {
                    byte[] tmp;
                    if (datapos + inBlocksize < data.Length)
                    {
                        tmp = ProcessBlock(data, datapos, inBlocksize);
                        datapos += inBlocksize;
                    }
                    else
                    {
                        tmp = ProcessBlock(data, datapos, data.Length - datapos);
                        datapos += data.Length - datapos;
                    }
                    if (_debug)
                    {
                        Debug.WriteLine("new datapos is " + datapos);
                    }
                    if (tmp != null)
                    {
                        tmp.CopyTo(retval, retpos);
                        retpos += tmp.Length;
                    }
                    else
                    {
                        if (_debug)
                        {
                            Debug.WriteLine("cipher returned null");
                        }
                        throw new InvalidCipherTextException("cipher returned null");
                    }
                }
                var ret = new byte[retpos];
                Array.Copy(retval, 0, ret, 0, retpos);
                if (_debug)
                {
                    Debug.WriteLine("returning " + ret.Length + " bytes");
                }
                return ret;
            }
            if (_debug)
            {
                Debug.WriteLine("data size is less then input block size, processing directly");
            }
            return ProcessBlock(data, 0, data.Length);
        }

        /**
        * Computes the integer x that is expressed through the given primes and the
        * congruences with the chinese remainder theorem (CRT).
        *
        * @param congruences
        *            the congruences c_i
        * @param primes
        *            the primes p_i
        * @return an integer x for that x % p_i == c_i
        */

        private static BigInteger ChineseRemainder(IList congruences, IList primes)
        {
            BigInteger retval = BigInteger.Zero;
            BigInteger all = BigInteger.One;
            for (int i = 0; i < primes.Count; i++)
            {
                all = all.Multiply((BigInteger) primes[i]);
            }
            for (int i = 0; i < primes.Count; i++)
            {
                var a = (BigInteger) primes[i];
                BigInteger b = all.Divide(a);
                BigInteger b2 = b.ModInverse(a);
                BigInteger tmp = b.Multiply(b2);
                tmp = tmp.Multiply((BigInteger) congruences[i]);
                retval = retval.Add(tmp);
            }

            return retval.Mod(all);
        }
    }
}
