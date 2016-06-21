using System;
using Raksha.Crypto.Parameters;

namespace Raksha.Crypto.Engines
{
	/// <remarks>A class that provides a basic DESede (or Triple DES) engine.</remarks>
    public class DesEdeEngine
		: DesEngine
    {
        private int[] workingKey1, workingKey2, workingKey3;
        private bool forEncryption;

		/**
		* initialise a DESede cipher.
		*
		* @param forEncryption whether or not we are for encryption.
		* @param parameters the parameters required to set up the cipher.
		* @exception ArgumentException if the parameters argument is
		* inappropriate.
		*/
		public override void Init(
			bool				forEncryption,
			ICipherParameters	parameters)
		{
			if (!(parameters is KeyParameter))
			{
				throw new ArgumentException("invalid parameter passed to DESede init - " + parameters.GetType().ToString());
			}

			byte[] keyMaster = ((KeyParameter)parameters).GetKey();

			this.forEncryption = forEncryption;

			byte[] key1 = new byte[8];
			Array.Copy(keyMaster, 0, key1, 0, key1.Length);
			workingKey1 = GenerateWorkingKey(forEncryption, key1);

			byte[] key2 = new byte[8];
			Array.Copy(keyMaster, 8, key2, 0, key2.Length);
			workingKey2 = GenerateWorkingKey(!forEncryption, key2);

			if (keyMaster.Length == 24)
			{
				byte[] key3 = new byte[8];
				Array.Copy(keyMaster, 16, key3, 0, key3.Length);
				workingKey3 = GenerateWorkingKey(forEncryption, key3);
			}
			else        // 16 byte key
			{
				workingKey3 = workingKey1;
			}
		}

		public override string AlgorithmName
        {
            get { return "DESede"; }
        }

		public override int GetBlockSize()
        {
            return BLOCK_SIZE;
        }

		public override int ProcessBlock(
            byte[]	input,
            int		inOff,
            byte[]	output,
            int		outOff)
        {
            if (workingKey1 == null)
                throw new InvalidOperationException("DESede engine not initialised");
            if ((inOff + BLOCK_SIZE) > input.Length)
                throw new DataLengthException("input buffer too short");
            if ((outOff + BLOCK_SIZE) > output.Length)
                throw new DataLengthException("output buffer too short");

			byte[] temp = new byte[BLOCK_SIZE];

			if (forEncryption)
            {
                DesFunc(workingKey1, input, inOff, temp, 0);
                DesFunc(workingKey2, temp, 0, temp, 0);
                DesFunc(workingKey3, temp, 0, output, outOff);
            }
            else
            {
                DesFunc(workingKey3, input, inOff, temp, 0);
                DesFunc(workingKey2, temp, 0, temp, 0);
                DesFunc(workingKey1, temp, 0, output, outOff);
            }

			return BLOCK_SIZE;
        }

		public override void Reset()
        {
        }
    }
}
