using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.ComponentModel;
using FATX.Analyzers.Signatures;
using System.Threading;

namespace FATX
{
    public enum FileCarverInterval
    {
        Byte = 0x1,
        Sector = 0x200,
        Page = 0x1000,
        Cluster = 0x4000,
    }

    public class FileCarver
    {
        private readonly Volume _volume;
        private readonly FileCarverInterval _interval;
        private readonly long _length;
        private List<FileSignature> _carvedFiles;
        private long progress;

        public FileCarver(Volume volume, FileCarverInterval interval, long length)
        {
            if (length == 0 || length > volume.FileAreaLength)
            {
                length = volume.Length;
            }

            this._volume = volume;
            this._interval = interval;
            this._length = length;
        }

        public List<FileSignature> GetCarvedFiles()
        {
            return _carvedFiles;
        }

        public long GetProgress()
        {
            return progress / (long)_interval;
        }

        public List<FileSignature> Analyze(CancellationToken ct)
        {
            var allSignatures = from assembly in AppDomain.CurrentDomain.GetAssemblies()
                                from type in assembly.GetTypes()
                                where type.IsSubclassOf(typeof(FileSignature))
                                select type;

            _carvedFiles = new List<FileSignature>();
            var interval = (long)_interval;

            var types = allSignatures.ToList();

            var origByteOrder = _volume.Reader.ByteOrder;

            for (long offset = 0; offset < _length; offset += interval)
            {
                foreach (Type type in types)
                {
                    // too slow
                    FileSignature signature = (FileSignature)Activator.CreateInstance(type, _volume, offset);

                    _volume.Reader.ByteOrder = origByteOrder;

                    _volume.SeekFileArea(offset);
                    bool test = signature.Test();
                    if (test)
                    {
                        try
                        {
                            _volume.SeekFileArea(offset);
                            signature.Parse();
                            _carvedFiles.Add(signature);
                            Console.WriteLine(String.Format("Found {0} at 0x{1:X}.", signature.GetType().Name, offset));
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(String.Format("Exception thrown for {0} at 0x{1:X}: {2}", signature.GetType().Name, offset, e.Message));
                            Console.WriteLine(e.StackTrace);
                        }
                    }
                }

                progress += interval;

                if (ct.IsCancellationRequested)
                {
                    Console.WriteLine("Task cancelled");
                    return _carvedFiles;
                }
            }

            Console.WriteLine("Complete!");

            return _carvedFiles;
        }

        public void Dump(FileSignature signature, string path)
        {
            const int bufsize = 0x100000;
            var remains = signature.FileSize;
            _volume.SeekFileArea(signature.Offset);

            path = path + "/" + signature.FileName;
            using (FileStream file = new FileStream(path, FileMode.Create))
            {
                while (remains > 0)
                {
                    var read = Math.Min(remains, bufsize);
                    remains -= read;
                    byte[] buf = new byte[read];
                    _volume.Reader.Read(buf, (int)read);
                    file.Write(buf, 0, (int)read);
                }
            }
        }
    }
}
