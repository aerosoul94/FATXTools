using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

using FATX.Streams;

namespace FATX.Analyzers
{
    public class SignatureMatcher
    {
        IFileSignature[] _allSignatures;

        int _blockSize;
        ByteOrder _byteOrder;
        ScannerStream _scanner;
        CarverReader _reader;

        public SignatureMatcher(Stream searchArea, ByteOrder byteOrder, int blockSize)
        {
            _allSignatures = AppDomain.CurrentDomain.GetAssemblies()
               .SelectMany(t => t.GetTypes())
               .Where(c => typeof(IFileSignature).IsAssignableFrom(c) && c.IsClass)
               .Select(s => (IFileSignature)Activator.CreateInstance(s)).ToArray();

            _blockSize = blockSize;
            _byteOrder = byteOrder;
            _scanner = new ScannerStream(searchArea, blockSize);
            _reader = new CarverReader(_scanner);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Match(out CarvedFile carvedFile)
        {
            carvedFile = null;

            foreach (var signature in _allSignatures)
            {
                _scanner.Seek(0, SeekOrigin.Begin);
                _reader.ByteOrder = _byteOrder;

                try
                {
                    if (signature.Test(_reader))
                    {
                        carvedFile = new CarvedFile(_scanner.Offset, signature.Name);

                        signature.Parse(_reader, carvedFile);

                        Console.WriteLine($"Found {signature.Name} at 0x{_scanner.Offset:X}.");

                        break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exception thrown while parsing {signature.Name} at {_scanner.Offset:X}: {e.Message}");
                    Console.WriteLine(e.StackTrace);
                }
            }

            _scanner.Shift(_blockSize);
        }
    }
}
