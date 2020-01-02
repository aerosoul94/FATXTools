using System;
using System.Collections.Generic;
using System.Text;

namespace FATX.Signatures
{
    class XEXSignature : FileSignature
    {
        private const string XEX1Signature = "XEX1";
        private const string XEX2Signature = "XEX2";

        public XEXSignature(Volume volume, long offset)
            : base(volume, offset)
        {

        }

        public override bool Test()
        {
            byte[] magic = this.ReadBytes(4);
            if (System.Text.Encoding.UTF8.GetString(magic) == XEX2Signature)
            {
                return true;
            }

            return false;
        }

        public override void Parse()
        {
            _fileName = "XEXSignature";
        }
    }
}
