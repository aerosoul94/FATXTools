using System;
using System.Collections.Generic;
using System.Text;

namespace FATX.Analyzers.Signatures
{
    class LiveSignature : FileSignature
    {
        private string LiveMagic = "LIVE";

        public LiveSignature(Volume volume, long offset)
            : base(volume, offset)
        {

        }

        public override bool Test()
        {
            byte[] magic = ReadBytes(4);
            if (System.Text.Encoding.UTF8.GetString(magic) == LiveMagic)
            {
                return true;
            }

            return false;
        }

        public override void Parse()
        {
            // does nothing for now
        }
    }
}
