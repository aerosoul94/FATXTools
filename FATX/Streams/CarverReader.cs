using System;
using System.IO;

namespace FATX.Streams
{
    public class CarverReader : EndianReader
    {
        public CarverReader(Stream stream)
            : base(stream)
        {

        }

        public override long Seek(long offset, SeekOrigin origin = SeekOrigin.Begin)
        {
            return BaseStream.Seek(offset, origin);
        }

        public string ReadCString(int terminant = 0)
        {
            string tempString = string.Empty;
            int tempChar = -1;
            bool eof;

            while (!(eof = (BaseStream.Position == BaseStream.Length)) 
                    && (tempChar = ReadByte()) != terminant)
            {
                tempString += Convert.ToChar(tempChar);
                if (eof)
                {
                    tempString += '\0';
                    break;
                }
            }

            return tempString;
        }
    }
}