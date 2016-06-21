using System;
using Raksha.Crypto.Parameters;

namespace Raksha.Crypto.Engines
{
    /**
    * Serpent is a 128-bit 32-round block cipher with variable key lengths,
    * including 128, 192 and 256 bit keys conjectured to be at least as
    * secure as three-key triple-DES.
    * <p>
    * Serpent was designed by Ross Anderson, Eli Biham and Lars Knudsen as a
    * candidate algorithm for the NIST AES Quest.>
	* </p>
    * <p>
    * For full details see the <a href="http://www.cl.cam.ac.uk/~rja14/serpent.html">The Serpent home page</a>
	* </p>
    */
    public class SerpentEngine
		: IBlockCipher
    {
        private const int    BLOCK_SIZE = 16;

        static readonly int ROUNDS = 32;
        static readonly int PHI    = unchecked((int)0x9E3779B9);       // (Sqrt(5) - 1) * 2**31

        private bool        encrypting;
        private int[]          wKey;

        private int           X0, X1, X2, X3;    // registers

        /**
        * initialise a Serpent cipher.
        *
        * @param forEncryption whether or not we are for encryption.
        * @param parameters the parameters required to set up the cipher.
        * @exception ArgumentException if the parameters argument is
        * inappropriate.
        */
        public void Init(
            bool				forEncryption,
            ICipherParameters	parameters)
        {
            if (!(parameters is KeyParameter))
				throw new ArgumentException("invalid parameter passed to Serpent init - " + parameters.GetType().ToString());

			this.encrypting = forEncryption;
            this.wKey = MakeWorkingKey(((KeyParameter)parameters).GetKey());
        }

		public string AlgorithmName
		{
			get { return "Serpent"; }
		}

		public bool IsPartialBlockOkay
		{
			get { return false; }
		}

		public int GetBlockSize()
        {
            return BLOCK_SIZE;
        }

        /**
        * Process one block of input from the array in and write it to
        * the out array.
        *
        * @param in the array containing the input data.
        * @param inOff offset into the in array the data starts at.
        * @param out the array the output data will be copied into.
        * @param outOff the offset into the out array the output will start at.
        * @exception DataLengthException if there isn't enough data in in, or
        * space in out.
        * @exception InvalidOperationException if the cipher isn't initialised.
        * @return the number of bytes processed and produced.
        */
        public  int ProcessBlock(
            byte[]  input,
            int     inOff,
            byte[]  output,
            int     outOff)
        {
            if (wKey == null)
                throw new InvalidOperationException("Serpent not initialised");
            if ((inOff + BLOCK_SIZE) > input.Length)
                throw new DataLengthException("input buffer too short");
            if ((outOff + BLOCK_SIZE) > output.Length)
                throw new DataLengthException("output buffer too short");

			if (encrypting)
            {
                EncryptBlock(input, inOff, output, outOff);
            }
            else
            {
                DecryptBlock(input, inOff, output, outOff);
            }

            return BLOCK_SIZE;
        }

        public void Reset()
        {
        }

        /**
        * Expand a user-supplied key material into a session key.
        *
        * @param key  The user-key bytes (multiples of 4) to use.
        * @exception ArgumentException
        */
        private int[] MakeWorkingKey(
            byte[] key)
        {
            //
            // pad key to 256 bits
            //
            int[]   kPad = new int[16];
            int     off = 0;
            int     length = 0;

            for (off = key.Length - 4; off > 0; off -= 4)
            {
                kPad[length++] = BytesToWord(key, off);
            }

            if (off == 0)
            {
                kPad[length++] = BytesToWord(key, 0);
                if (length < 8)
                {
                    kPad[length] = 1;
                }
            }
            else
            {
                throw new ArgumentException("key must be a multiple of 4 bytes");
            }

            //
            // expand the padded key up to 33 x 128 bits of key material
            //
            int amount = (ROUNDS + 1) * 4;
            int[] w = new int[amount];

            //
            // compute w0 to w7 from w-8 to w-1
            //
            for (int i = 8; i < 16; i++)
            {
                kPad[i] = RotateLeft(kPad[i - 8] ^ kPad[i - 5] ^ kPad[i - 3] ^ kPad[i - 1] ^ PHI ^ (i - 8), 11);
            }

            Array.Copy(kPad, 8, w, 0, 8);

            //
            // compute w8 to w136
            //
            for (int i = 8; i < amount; i++)
            {
                w[i] = RotateLeft(w[i - 8] ^ w[i - 5] ^ w[i - 3] ^ w[i - 1] ^ PHI ^ i, 11);
            }

            //
            // create the working keys by processing w with the Sbox and IP
            //
            Sb3(w[0], w[1], w[2], w[3]);
            w[0] = X0; w[1] = X1; w[2] = X2; w[3] = X3;
            Sb2(w[4], w[5], w[6], w[7]);
            w[4] = X0; w[5] = X1; w[6] = X2; w[7] = X3;
            Sb1(w[8], w[9], w[10], w[11]);
            w[8] = X0; w[9] = X1; w[10] = X2; w[11] = X3;
            Sb0(w[12], w[13], w[14], w[15]);
            w[12] = X0; w[13] = X1; w[14] = X2; w[15] = X3;
            Sb7(w[16], w[17], w[18], w[19]);
            w[16] = X0; w[17] = X1; w[18] = X2; w[19] = X3;
            Sb6(w[20], w[21], w[22], w[23]);
            w[20] = X0; w[21] = X1; w[22] = X2; w[23] = X3;
            Sb5(w[24], w[25], w[26], w[27]);
            w[24] = X0; w[25] = X1; w[26] = X2; w[27] = X3;
            Sb4(w[28], w[29], w[30], w[31]);
            w[28] = X0; w[29] = X1; w[30] = X2; w[31] = X3;
            Sb3(w[32], w[33], w[34], w[35]);
            w[32] = X0; w[33] = X1; w[34] = X2; w[35] = X3;
            Sb2(w[36], w[37], w[38], w[39]);
            w[36] = X0; w[37] = X1; w[38] = X2; w[39] = X3;
            Sb1(w[40], w[41], w[42], w[43]);
            w[40] = X0; w[41] = X1; w[42] = X2; w[43] = X3;
            Sb0(w[44], w[45], w[46], w[47]);
            w[44] = X0; w[45] = X1; w[46] = X2; w[47] = X3;
            Sb7(w[48], w[49], w[50], w[51]);
            w[48] = X0; w[49] = X1; w[50] = X2; w[51] = X3;
            Sb6(w[52], w[53], w[54], w[55]);
            w[52] = X0; w[53] = X1; w[54] = X2; w[55] = X3;
            Sb5(w[56], w[57], w[58], w[59]);
            w[56] = X0; w[57] = X1; w[58] = X2; w[59] = X3;
            Sb4(w[60], w[61], w[62], w[63]);
            w[60] = X0; w[61] = X1; w[62] = X2; w[63] = X3;
            Sb3(w[64], w[65], w[66], w[67]);
            w[64] = X0; w[65] = X1; w[66] = X2; w[67] = X3;
            Sb2(w[68], w[69], w[70], w[71]);
            w[68] = X0; w[69] = X1; w[70] = X2; w[71] = X3;
            Sb1(w[72], w[73], w[74], w[75]);
            w[72] = X0; w[73] = X1; w[74] = X2; w[75] = X3;
            Sb0(w[76], w[77], w[78], w[79]);
            w[76] = X0; w[77] = X1; w[78] = X2; w[79] = X3;
            Sb7(w[80], w[81], w[82], w[83]);
            w[80] = X0; w[81] = X1; w[82] = X2; w[83] = X3;
            Sb6(w[84], w[85], w[86], w[87]);
            w[84] = X0; w[85] = X1; w[86] = X2; w[87] = X3;
            Sb5(w[88], w[89], w[90], w[91]);
            w[88] = X0; w[89] = X1; w[90] = X2; w[91] = X3;
            Sb4(w[92], w[93], w[94], w[95]);
            w[92] = X0; w[93] = X1; w[94] = X2; w[95] = X3;
            Sb3(w[96], w[97], w[98], w[99]);
            w[96] = X0; w[97] = X1; w[98] = X2; w[99] = X3;
            Sb2(w[100], w[101], w[102], w[103]);
            w[100] = X0; w[101] = X1; w[102] = X2; w[103] = X3;
            Sb1(w[104], w[105], w[106], w[107]);
            w[104] = X0; w[105] = X1; w[106] = X2; w[107] = X3;
            Sb0(w[108], w[109], w[110], w[111]);
            w[108] = X0; w[109] = X1; w[110] = X2; w[111] = X3;
            Sb7(w[112], w[113], w[114], w[115]);
            w[112] = X0; w[113] = X1; w[114] = X2; w[115] = X3;
            Sb6(w[116], w[117], w[118], w[119]);
            w[116] = X0; w[117] = X1; w[118] = X2; w[119] = X3;
            Sb5(w[120], w[121], w[122], w[123]);
            w[120] = X0; w[121] = X1; w[122] = X2; w[123] = X3;
            Sb4(w[124], w[125], w[126], w[127]);
            w[124] = X0; w[125] = X1; w[126] = X2; w[127] = X3;
            Sb3(w[128], w[129], w[130], w[131]);
            w[128] = X0; w[129] = X1; w[130] = X2; w[131] = X3;

            return w;
        }

        private int RotateLeft(
            int     x,
            int     bits)
        {
            return ((x << bits) | (int) ((uint)x >> (32 - bits)));
        }

        private int RotateRight(
            int     x,
            int     bits)
        {
            return ( (int)((uint)x >> bits) | (x << (32 - bits)));
        }

        private int BytesToWord(
            byte[]  src,
            int     srcOff)
        {
            return (((src[srcOff] & 0xff) << 24) | ((src[srcOff + 1] & 0xff) <<  16) |
            ((src[srcOff + 2] & 0xff) << 8) | ((src[srcOff + 3] & 0xff)));
        }

        private void WordToBytes(
            int     word,
            byte[]  dst,
            int     dstOff)
        {
            dst[dstOff + 3] = (byte)(word);
            dst[dstOff + 2] = (byte)((uint)word >> 8);
            dst[dstOff + 1] = (byte)((uint)word >> 16);
            dst[dstOff]     = (byte)((uint)word >> 24);
        }

        /**
        * Encrypt one block of plaintext.
        *
        * @param in the array containing the input data.
        * @param inOff offset into the in array the data starts at.
        * @param out the array the output data will be copied into.
        * @param outOff the offset into the out array the output will start at.
        */
        private void EncryptBlock(
            byte[]  input,
            int     inOff,
            byte[]  outBytes,
            int     outOff)
        {
            X3 = BytesToWord(input, inOff);
            X2 = BytesToWord(input, inOff + 4);
            X1 = BytesToWord(input, inOff + 8);
            X0 = BytesToWord(input, inOff + 12);

            Sb0(wKey[0] ^ X0, wKey[1] ^ X1, wKey[2] ^ X2, wKey[3] ^ X3); LT();
            Sb1(wKey[4] ^ X0, wKey[5] ^ X1, wKey[6] ^ X2, wKey[7] ^ X3); LT();
            Sb2(wKey[8] ^ X0, wKey[9] ^ X1, wKey[10] ^ X2, wKey[11] ^ X3); LT();
            Sb3(wKey[12] ^ X0, wKey[13] ^ X1, wKey[14] ^ X2, wKey[15] ^ X3); LT();
            Sb4(wKey[16] ^ X0, wKey[17] ^ X1, wKey[18] ^ X2, wKey[19] ^ X3); LT();
            Sb5(wKey[20] ^ X0, wKey[21] ^ X1, wKey[22] ^ X2, wKey[23] ^ X3); LT();
            Sb6(wKey[24] ^ X0, wKey[25] ^ X1, wKey[26] ^ X2, wKey[27] ^ X3); LT();
            Sb7(wKey[28] ^ X0, wKey[29] ^ X1, wKey[30] ^ X2, wKey[31] ^ X3); LT();
            Sb0(wKey[32] ^ X0, wKey[33] ^ X1, wKey[34] ^ X2, wKey[35] ^ X3); LT();
            Sb1(wKey[36] ^ X0, wKey[37] ^ X1, wKey[38] ^ X2, wKey[39] ^ X3); LT();
            Sb2(wKey[40] ^ X0, wKey[41] ^ X1, wKey[42] ^ X2, wKey[43] ^ X3); LT();
            Sb3(wKey[44] ^ X0, wKey[45] ^ X1, wKey[46] ^ X2, wKey[47] ^ X3); LT();
            Sb4(wKey[48] ^ X0, wKey[49] ^ X1, wKey[50] ^ X2, wKey[51] ^ X3); LT();
            Sb5(wKey[52] ^ X0, wKey[53] ^ X1, wKey[54] ^ X2, wKey[55] ^ X3); LT();
            Sb6(wKey[56] ^ X0, wKey[57] ^ X1, wKey[58] ^ X2, wKey[59] ^ X3); LT();
            Sb7(wKey[60] ^ X0, wKey[61] ^ X1, wKey[62] ^ X2, wKey[63] ^ X3); LT();
            Sb0(wKey[64] ^ X0, wKey[65] ^ X1, wKey[66] ^ X2, wKey[67] ^ X3); LT();
            Sb1(wKey[68] ^ X0, wKey[69] ^ X1, wKey[70] ^ X2, wKey[71] ^ X3); LT();
            Sb2(wKey[72] ^ X0, wKey[73] ^ X1, wKey[74] ^ X2, wKey[75] ^ X3); LT();
            Sb3(wKey[76] ^ X0, wKey[77] ^ X1, wKey[78] ^ X2, wKey[79] ^ X3); LT();
            Sb4(wKey[80] ^ X0, wKey[81] ^ X1, wKey[82] ^ X2, wKey[83] ^ X3); LT();
            Sb5(wKey[84] ^ X0, wKey[85] ^ X1, wKey[86] ^ X2, wKey[87] ^ X3); LT();
            Sb6(wKey[88] ^ X0, wKey[89] ^ X1, wKey[90] ^ X2, wKey[91] ^ X3); LT();
            Sb7(wKey[92] ^ X0, wKey[93] ^ X1, wKey[94] ^ X2, wKey[95] ^ X3); LT();
            Sb0(wKey[96] ^ X0, wKey[97] ^ X1, wKey[98] ^ X2, wKey[99] ^ X3); LT();
            Sb1(wKey[100] ^ X0, wKey[101] ^ X1, wKey[102] ^ X2, wKey[103] ^ X3); LT();
            Sb2(wKey[104] ^ X0, wKey[105] ^ X1, wKey[106] ^ X2, wKey[107] ^ X3); LT();
            Sb3(wKey[108] ^ X0, wKey[109] ^ X1, wKey[110] ^ X2, wKey[111] ^ X3); LT();
            Sb4(wKey[112] ^ X0, wKey[113] ^ X1, wKey[114] ^ X2, wKey[115] ^ X3); LT();
            Sb5(wKey[116] ^ X0, wKey[117] ^ X1, wKey[118] ^ X2, wKey[119] ^ X3); LT();
            Sb6(wKey[120] ^ X0, wKey[121] ^ X1, wKey[122] ^ X2, wKey[123] ^ X3); LT();
            Sb7(wKey[124] ^ X0, wKey[125] ^ X1, wKey[126] ^ X2, wKey[127] ^ X3);

            WordToBytes(wKey[131] ^ X3, outBytes, outOff);
            WordToBytes(wKey[130] ^ X2, outBytes, outOff + 4);
            WordToBytes(wKey[129] ^ X1, outBytes, outOff + 8);
            WordToBytes(wKey[128] ^ X0, outBytes, outOff + 12);
        }

        /**
        * Decrypt one block of ciphertext.
        *
        * @param in the array containing the input data.
        * @param inOff offset into the in array the data starts at.
        * @param out the array the output data will be copied into.
        * @param outOff the offset into the out array the output will start at.
        */
        private void DecryptBlock(
            byte[]  input,
            int     inOff,
            byte[]  outBytes,
            int     outOff)
        {
            X3 = wKey[131] ^ BytesToWord(input, inOff);
            X2 = wKey[130] ^ BytesToWord(input, inOff + 4);
            X1 = wKey[129] ^ BytesToWord(input, inOff + 8);
            X0 = wKey[128] ^ BytesToWord(input, inOff + 12);

            Ib7(X0, X1, X2, X3);
            X0 ^= wKey[124]; X1 ^= wKey[125]; X2 ^= wKey[126]; X3 ^= wKey[127];
            InverseLT(); Ib6(X0, X1, X2, X3);
            X0 ^= wKey[120]; X1 ^= wKey[121]; X2 ^= wKey[122]; X3 ^= wKey[123];
            InverseLT(); Ib5(X0, X1, X2, X3);
            X0 ^= wKey[116]; X1 ^= wKey[117]; X2 ^= wKey[118]; X3 ^= wKey[119];
            InverseLT(); Ib4(X0, X1, X2, X3);
            X0 ^= wKey[112]; X1 ^= wKey[113]; X2 ^= wKey[114]; X3 ^= wKey[115];
            InverseLT(); Ib3(X0, X1, X2, X3);
            X0 ^= wKey[108]; X1 ^= wKey[109]; X2 ^= wKey[110]; X3 ^= wKey[111];
            InverseLT(); Ib2(X0, X1, X2, X3);
            X0 ^= wKey[104]; X1 ^= wKey[105]; X2 ^= wKey[106]; X3 ^= wKey[107];
            InverseLT(); Ib1(X0, X1, X2, X3);
            X0 ^= wKey[100]; X1 ^= wKey[101]; X2 ^= wKey[102]; X3 ^= wKey[103];
            InverseLT(); Ib0(X0, X1, X2, X3);
            X0 ^= wKey[96]; X1 ^= wKey[97]; X2 ^= wKey[98]; X3 ^= wKey[99];
            InverseLT(); Ib7(X0, X1, X2, X3);
            X0 ^= wKey[92]; X1 ^= wKey[93]; X2 ^= wKey[94]; X3 ^= wKey[95];
            InverseLT(); Ib6(X0, X1, X2, X3);
            X0 ^= wKey[88]; X1 ^= wKey[89]; X2 ^= wKey[90]; X3 ^= wKey[91];
            InverseLT(); Ib5(X0, X1, X2, X3);
            X0 ^= wKey[84]; X1 ^= wKey[85]; X2 ^= wKey[86]; X3 ^= wKey[87];
            InverseLT(); Ib4(X0, X1, X2, X3);
            X0 ^= wKey[80]; X1 ^= wKey[81]; X2 ^= wKey[82]; X3 ^= wKey[83];
            InverseLT(); Ib3(X0, X1, X2, X3);
            X0 ^= wKey[76]; X1 ^= wKey[77]; X2 ^= wKey[78]; X3 ^= wKey[79];
            InverseLT(); Ib2(X0, X1, X2, X3);
            X0 ^= wKey[72]; X1 ^= wKey[73]; X2 ^= wKey[74]; X3 ^= wKey[75];
            InverseLT(); Ib1(X0, X1, X2, X3);
            X0 ^= wKey[68]; X1 ^= wKey[69]; X2 ^= wKey[70]; X3 ^= wKey[71];
            InverseLT(); Ib0(X0, X1, X2, X3);
            X0 ^= wKey[64]; X1 ^= wKey[65]; X2 ^= wKey[66]; X3 ^= wKey[67];
            InverseLT(); Ib7(X0, X1, X2, X3);
            X0 ^= wKey[60]; X1 ^= wKey[61]; X2 ^= wKey[62]; X3 ^= wKey[63];
            InverseLT(); Ib6(X0, X1, X2, X3);
            X0 ^= wKey[56]; X1 ^= wKey[57]; X2 ^= wKey[58]; X3 ^= wKey[59];
            InverseLT(); Ib5(X0, X1, X2, X3);
            X0 ^= wKey[52]; X1 ^= wKey[53]; X2 ^= wKey[54]; X3 ^= wKey[55];
            InverseLT(); Ib4(X0, X1, X2, X3);
            X0 ^= wKey[48]; X1 ^= wKey[49]; X2 ^= wKey[50]; X3 ^= wKey[51];
            InverseLT(); Ib3(X0, X1, X2, X3);
            X0 ^= wKey[44]; X1 ^= wKey[45]; X2 ^= wKey[46]; X3 ^= wKey[47];
            InverseLT(); Ib2(X0, X1, X2, X3);
            X0 ^= wKey[40]; X1 ^= wKey[41]; X2 ^= wKey[42]; X3 ^= wKey[43];
            InverseLT(); Ib1(X0, X1, X2, X3);
            X0 ^= wKey[36]; X1 ^= wKey[37]; X2 ^= wKey[38]; X3 ^= wKey[39];
            InverseLT(); Ib0(X0, X1, X2, X3);
            X0 ^= wKey[32]; X1 ^= wKey[33]; X2 ^= wKey[34]; X3 ^= wKey[35];
            InverseLT(); Ib7(X0, X1, X2, X3);
            X0 ^= wKey[28]; X1 ^= wKey[29]; X2 ^= wKey[30]; X3 ^= wKey[31];
            InverseLT(); Ib6(X0, X1, X2, X3);
            X0 ^= wKey[24]; X1 ^= wKey[25]; X2 ^= wKey[26]; X3 ^= wKey[27];
            InverseLT(); Ib5(X0, X1, X2, X3);
            X0 ^= wKey[20]; X1 ^= wKey[21]; X2 ^= wKey[22]; X3 ^= wKey[23];
            InverseLT(); Ib4(X0, X1, X2, X3);
            X0 ^= wKey[16]; X1 ^= wKey[17]; X2 ^= wKey[18]; X3 ^= wKey[19];
            InverseLT(); Ib3(X0, X1, X2, X3);
            X0 ^= wKey[12]; X1 ^= wKey[13]; X2 ^= wKey[14]; X3 ^= wKey[15];
            InverseLT(); Ib2(X0, X1, X2, X3);
            X0 ^= wKey[8]; X1 ^= wKey[9]; X2 ^= wKey[10]; X3 ^= wKey[11];
            InverseLT(); Ib1(X0, X1, X2, X3);
            X0 ^= wKey[4]; X1 ^= wKey[5]; X2 ^= wKey[6]; X3 ^= wKey[7];
            InverseLT(); Ib0(X0, X1, X2, X3);

            WordToBytes(X3 ^ wKey[3], outBytes, outOff);
            WordToBytes(X2 ^ wKey[2], outBytes, outOff + 4);
            WordToBytes(X1 ^ wKey[1], outBytes, outOff + 8);
            WordToBytes(X0 ^ wKey[0], outBytes, outOff + 12);
        }

        /*
        * The sboxes below are based on the work of Brian Gladman and
        * Sam Simpson, whose original notice appears below.
        * <p>
        * For further details see:
        *      http://fp.gladman.plus.com/cryptography_technology/serpent/
		* </p>
        */

        /* Partially optimised Serpent S Box bool functions derived  */
        /* using a recursive descent analyser but without a full search */
        /* of all subtrees. This set of S boxes is the result of work    */
        /* by Sam Simpson and Brian Gladman using the spare time on a    */
        /* cluster of high capacity servers to search for S boxes with    */
        /* this customised search engine. There are now an average of    */
        /* 15.375 terms    per S box.                                        */
        /*                                                              */
        /* Copyright:   Dr B. R Gladman (gladman@seven77.demon.co.uk)   */
        /*                and Sam Simpson (s.simpson@mia.co.uk)            */
        /*              17th December 1998                                */
        /*                                                              */
        /* We hereby give permission for information in this file to be */
        /* used freely subject only to acknowledgement of its origin.    */

        /**
        * S0 - { 3, 8,15, 1,10, 6, 5,11,14,13, 4, 2, 7, 0, 9,12 } - 15 terms.
        */
        private void Sb0(int a, int b, int c, int d)
        {
            int    t1 = a ^ d;
            int    t3 = c ^ t1;
            int    t4 = b ^ t3;
            X3 = (a & d) ^ t4;
            int    t7 = a ^ (b & t1);
            X2 = t4 ^ (c | t7);
            int    t12 = X3 & (t3 ^ t7);
            X1 = (~t3) ^ t12;
            X0 = t12 ^ (~t7);
        }

        /**
        * InvSO - {13, 3,11, 0,10, 6, 5,12, 1,14, 4, 7,15, 9, 8, 2 } - 15 terms.
        */
        private void Ib0(int a, int b, int c, int d)
        {
            int    t1 = ~a;
            int    t2 = a ^ b;
            int    t4 = d ^ (t1 | t2);
            int    t5 = c ^ t4;
            X2 = t2 ^ t5;
            int    t8 = t1 ^ (d & t2);
            X1 = t4 ^ (X2 & t8);
            X3 = (a & t4) ^ (t5 | X1);
            X0 = X3 ^ (t5 ^ t8);
        }

        /**
        * S1 - {15,12, 2, 7, 9, 0, 5,10, 1,11,14, 8, 6,13, 3, 4 } - 14 terms.
        */
        private void Sb1(int a, int b, int c, int d)
        {
            int    t2 = b ^ (~a);
            int    t5 = c ^ (a | t2);
            X2 = d ^ t5;
            int    t7 = b ^ (d | t2);
            int    t8 = t2 ^ X2;
            X3 = t8 ^ (t5 & t7);
            int    t11 = t5 ^ t7;
            X1 = X3 ^ t11;
            X0 = t5 ^ (t8 & t11);
        }

        /**
        * InvS1 - { 5, 8, 2,14,15, 6,12, 3,11, 4, 7, 9, 1,13,10, 0 } - 14 steps.
        */
        private void Ib1(int a, int b, int c, int d)
        {
            int    t1 = b ^ d;
            int    t3 = a ^ (b & t1);
            int    t4 = t1 ^ t3;
            X3 = c ^ t4;
            int    t7 = b ^ (t1 & t3);
            int    t8 = X3 | t7;
            X1 = t3 ^ t8;
            int    t10 = ~X1;
            int    t11 = X3 ^ t7;
            X0 = t10 ^ t11;
            X2 = t4 ^ (t10 | t11);
        }

        /**
        * S2 - { 8, 6, 7, 9, 3,12,10,15,13, 1,14, 4, 0,11, 5, 2 } - 16 terms.
        */
        private void Sb2(int a, int b, int c, int d)
        {
            int    t1 = ~a;
            int    t2 = b ^ d;
            int    t3 = c & t1;
            X0 = t2 ^ t3;
            int    t5 = c ^ t1;
            int    t6 = c ^ X0;
            int    t7 = b & t6;
            X3 = t5 ^ t7;
            X2 = a ^ ((d | t7) & (X0 | t5));
            X1 = (t2 ^ X3) ^ (X2 ^ (d | t1));
        }

        /**
        * InvS2 - {12, 9,15, 4,11,14, 1, 2, 0, 3, 6,13, 5, 8,10, 7 } - 16 steps.
        */
        private void Ib2(int a, int b, int c, int d)
        {
            int    t1 = b ^ d;
            int    t2 = ~t1;
            int    t3 = a ^ c;
            int    t4 = c ^ t1;
            int    t5 = b & t4;
            X0 = t3 ^ t5;
            int    t7 = a | t2;
            int    t8 = d ^ t7;
            int    t9 = t3 | t8;
            X3 = t1 ^ t9;
            int    t11 = ~t4;
            int    t12 = X0 | X3;
            X1 = t11 ^ t12;
            X2 = (d & t11) ^ (t3 ^ t12);
        }

        /**
        * S3 - { 0,15,11, 8,12, 9, 6, 3,13, 1, 2, 4,10, 7, 5,14 } - 16 terms.
        */
        private void Sb3(int a, int b, int c, int d)
        {
            int    t1 = a ^ b;
            int    t2 = a & c;
            int    t3 = a | d;
            int    t4 = c ^ d;
            int    t5 = t1 & t3;
            int    t6 = t2 | t5;
            X2 = t4 ^ t6;
            int    t8 = b ^ t3;
            int    t9 = t6 ^ t8;
            int    t10 = t4 & t9;
            X0 = t1 ^ t10;
            int    t12 = X2 & X0;
            X1 = t9 ^ t12;
            X3 = (b | d) ^ (t4 ^ t12);
        }

        /**
        * InvS3 - { 0, 9,10, 7,11,14, 6,13, 3, 5,12, 2, 4, 8,15, 1 } - 15 terms
        */
        private void Ib3(int a, int b, int c, int d)
        {
            int    t1 = a | b;
            int    t2 = b ^ c;
            int    t3 = b & t2;
            int    t4 = a ^ t3;
            int    t5 = c ^ t4;
            int    t6 = d | t4;
            X0 = t2 ^ t6;
            int    t8 = t2 | t6;
            int    t9 = d ^ t8;
            X2 = t5 ^ t9;
            int    t11 = t1 ^ t9;
            int    t12 = X0 & t11;
            X3 = t4 ^ t12;
            X1 = X3 ^ (X0 ^ t11);
        }

        /**
        * S4 - { 1,15, 8, 3,12, 0,11, 6, 2, 5, 4,10, 9,14, 7,13 } - 15 terms.
        */
        private void Sb4(int a, int b, int c, int d)
        {
            int    t1 = a ^ d;
            int    t2 = d & t1;
            int    t3 = c ^ t2;
            int    t4 = b | t3;
            X3 = t1 ^ t4;
            int    t6 = ~b;
            int    t7 = t1 | t6;
            X0 = t3 ^ t7;
            int    t9 = a & X0;
            int    t10 = t1 ^ t6;
            int    t11 = t4 & t10;
            X2 = t9 ^ t11;
            X1 = (a ^ t3) ^ (t10 & X2);
        }

        /**
        * InvS4 - { 5, 0, 8, 3,10, 9, 7,14, 2,12,11, 6, 4,15,13, 1 } - 15 terms.
        */
        private void Ib4(int a, int b, int c, int d)
        {
            int    t1 = c | d;
            int    t2 = a & t1;
            int    t3 = b ^ t2;
            int    t4 = a & t3;
            int    t5 = c ^ t4;
            X1 = d ^ t5;
            int    t7 = ~a;
            int    t8 = t5 & X1;
            X3 = t3 ^ t8;
            int    t10 = X1 | t7;
            int    t11 = d ^ t10;
            X0 = X3 ^ t11;
            X2 = (t3 & t11) ^ (X1 ^ t7);
        }

        /**
        * S5 - {15, 5, 2,11, 4,10, 9,12, 0, 3,14, 8,13, 6, 7, 1 } - 16 terms.
        */
        private void Sb5(int a, int b, int c, int d)
        {
            int    t1 = ~a;
            int    t2 = a ^ b;
            int    t3 = a ^ d;
            int    t4 = c ^ t1;
            int    t5 = t2 | t3;
            X0 = t4 ^ t5;
            int    t7 = d & X0;
            int    t8 = t2 ^ X0;
            X1 = t7 ^ t8;
            int    t10 = t1 | X0;
            int    t11 = t2 | t7;
            int    t12 = t3 ^ t10;
            X2 = t11 ^ t12;
            X3 = (b ^ t7) ^ (X1 & t12);
        }

        /**
        * InvS5 - { 8,15, 2, 9, 4, 1,13,14,11, 6, 5, 3, 7,12,10, 0 } - 16 terms.
        */
        private void Ib5(int a, int b, int c, int d)
        {
            int    t1 = ~c;
            int    t2 = b & t1;
            int    t3 = d ^ t2;
            int    t4 = a & t3;
            int    t5 = b ^ t1;
            X3 = t4 ^ t5;
            int    t7 = b | X3;
            int    t8 = a & t7;
            X1 = t3 ^ t8;
            int    t10 = a | d;
            int    t11 = t1 ^ t7;
            X0 = t10 ^ t11;
            X2 = (b & t10) ^ (t4 | (a ^ c));
        }

        /**
        * S6 - { 7, 2,12, 5, 8, 4, 6,11,14, 9, 1,15,13, 3,10, 0 } - 15 terms.
        */
        private void Sb6(int a, int b, int c, int d)
        {
            int    t1 = ~a;
            int    t2 = a ^ d;
            int    t3 = b ^ t2;
            int    t4 = t1 | t2;
            int    t5 = c ^ t4;
            X1 = b ^ t5;
            int    t7 = t2 | X1;
            int    t8 = d ^ t7;
            int    t9 = t5 & t8;
            X2 = t3 ^ t9;
            int    t11 = t5 ^ t8;
            X0 = X2 ^ t11;
            X3 = (~t5) ^ (t3 & t11);
        }

        /**
        * InvS6 - {15,10, 1,13, 5, 3, 6, 0, 4, 9,14, 7, 2,12, 8,11 } - 15 terms.
        */
        private void Ib6(int a, int b, int c, int d)
        {
            int    t1 = ~a;
            int    t2 = a ^ b;
            int    t3 = c ^ t2;
            int    t4 = c | t1;
            int    t5 = d ^ t4;
            X1 = t3 ^ t5;
            int    t7 = t3 & t5;
            int    t8 = t2 ^ t7;
            int    t9 = b | t8;
            X3 = t5 ^ t9;
            int    t11 = b | X3;
            X0 = t8 ^ t11;
            X2 = (d & t1) ^ (t3 ^ t11);
        }

        /**
        * S7 - { 1,13,15, 0,14, 8, 2,11, 7, 4,12,10, 9, 3, 5, 6 } - 16 terms.
        */
        private void Sb7(int a, int b, int c, int d)
        {
            int    t1 = b ^ c;
            int    t2 = c & t1;
            int    t3 = d ^ t2;
            int    t4 = a ^ t3;
            int    t5 = d | t1;
            int    t6 = t4 & t5;
            X1 = b ^ t6;
            int    t8 = t3 | X1;
            int    t9 = a & t4;
            X3 = t1 ^ t9;
            int    t11 = t4 ^ t8;
            int    t12 = X3 & t11;
            X2 = t3 ^ t12;
            X0 = (~t11) ^ (X3 & X2);
        }

        /**
        * InvS7 - { 3, 0, 6,13, 9,14,15, 8, 5,12,11, 7,10, 1, 4, 2 } - 17 terms.
        */
        private void Ib7(int a, int b, int c, int d)
        {
            int t3 = c | (a & b);
            int    t4 = d & (a | b);
            X3 = t3 ^ t4;
            int    t6 = ~d;
            int    t7 = b ^ t4;
            int    t9 = t7 | (X3 ^ t6);
            X1 = a ^ t9;
            X0 = (c ^ t7) ^ (d | X1);
            X2 = (t3 ^ X1) ^ (X0 ^ (a & X3));
        }

        /**
        * Apply the linear transformation to the register set.
        */
        private void LT()
        {
            int x0  = RotateLeft(X0, 13);
            int x2  = RotateLeft(X2, 3);
            int x1  = X1 ^ x0 ^ x2 ;
            int x3  = X3 ^ x2 ^ x0 << 3;

            X1  = RotateLeft(x1, 1);
            X3  = RotateLeft(x3, 7);
            X0  = RotateLeft(x0 ^ X1 ^ X3, 5);
            X2  = RotateLeft(x2 ^ X3 ^ (X1 << 7), 22);
        }

        /**
        * Apply the inverse of the linear transformation to the register set.
        */
        private void InverseLT()
        {
            int x2 = RotateRight(X2, 22) ^ X3 ^ (X1 << 7);
            int x0 = RotateRight(X0, 5) ^ X1 ^ X3;
            int x3 = RotateRight(X3, 7);
            int x1 = RotateRight(X1, 1);
            X3 = x3 ^ x2 ^ x0 << 3;
            X1 = x1 ^ x0 ^ x2;
            X2 = RotateRight(x2, 3);
            X0 = RotateRight(x0, 13);
        }
    }

}
