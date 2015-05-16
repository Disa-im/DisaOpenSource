using System;



namespace Raksha.Bcpg.Sig
{
    /**
    * packet giving signature creation time.
    */
    public class Exportable
        : SignatureSubpacket
    {
        private static byte[] BooleanToByteArray(bool val)
        {
            byte[]    data = new byte[1];

            if (val)
            {
                data[0] = 1;
                return data;
            }
            else
            {
                return data;
            }
        }

        public Exportable(
            bool    critical,
            byte[]     data)
            : base(SignatureSubpacketTag.Exportable, critical, data)
        {
        }

        public Exportable(
            bool    critical,
            bool    isExportable)
            : base(SignatureSubpacketTag.Exportable, critical, BooleanToByteArray(isExportable))
        {
        }

        public bool IsExportable()
        {
            return data[0] != 0;
        }
    }
}
