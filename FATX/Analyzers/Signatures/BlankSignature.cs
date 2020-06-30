using FATX.FileSystem;
using System;

namespace FATX.Analyzers.Signatures.Blank
{
    class BlankSignature : Signatures.FileSignature
    {
        public BlankSignature(Volume volume, long offset)
            : base(volume, offset)
        {

        }

        public override void Parse()
        {
            throw new NotImplementedException("Should not be calling GenericSignature.Parse()");
        }

        public override bool Test()
        {
            throw new NotImplementedException("Should not be calling GenericSignature.Test()");
        }
    }
}
