using System;
using System.Collections.Generic;
using System.Text;

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
