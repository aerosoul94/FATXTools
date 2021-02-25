using System;
using System.Text;
using System.Collections.Generic;

namespace FATX.FileSystem
{
    public class DirectoryEntry
    {
        public byte FileNameLength { get; private set; }
        public FileAttribute FileAttributes { get; private set; }
        public byte[] FileNameBytes { get; private set; }
        public uint FirstCluster { get; private set; }
        public uint FileSize { get; private set; }
        public TimeStamp CreationTime { get; private set; }
        public TimeStamp LastWriteTime { get; private set; }
        public TimeStamp LastAccessTime { get; private set; }

        public List<DirectoryEntry> Children { get; } = new List<DirectoryEntry>();
        public DirectoryEntry Parent { get; set; }
        public uint Cluster { get; set; }
        public long Offset { get; set; }

        public DirectoryEntry(Platform platform, byte[] data, int offset)
        {
            this.Parent = null;

            ReadDirectoryEntry(platform, data, offset);
        }

        private void ReadDirectoryEntry(Platform platform, byte[] data, int offset)
        {
            FileNameLength = data[offset + 0];
            FileAttributes = (FileAttribute)data[offset + 1];

            FileNameBytes = new byte[42];
            Buffer.BlockCopy(data, offset + 2, FileNameBytes, 0, 42);

            if (platform == Platform.Xbox)
            {
                FirstCluster = BitConverter.ToUInt32(data, offset + 0x2C);
                FileSize = BitConverter.ToUInt32(data, offset + 0x30);
                
                CreationTime = new XTimeStamp(BitConverter.ToUInt32(data, offset + 0x34));
                LastWriteTime = new XTimeStamp(BitConverter.ToUInt32(data, offset + 0x38));
                LastAccessTime = new XTimeStamp(BitConverter.ToUInt32(data, offset + 0x3C));
            }
            else if (platform == Platform.X360)
            {
                Array.Reverse(data, offset + 0x2C, 4);
                FirstCluster = BitConverter.ToUInt32(data, offset + 0x2C);
                Array.Reverse(data, offset + 0x30, 4);
                FileSize = BitConverter.ToUInt32(data, offset + 0x30);

                Array.Reverse(data, offset + 0x34, 4);
                CreationTime = new X360TimeStamp(BitConverter.ToUInt32(data, offset + 0x34));
                Array.Reverse(data, offset + 0x38, 4);
                LastWriteTime = new X360TimeStamp(BitConverter.ToUInt32(data, offset + 0x38));
                Array.Reverse(data, offset + 0x3C, 4);
                LastAccessTime = new X360TimeStamp(BitConverter.ToUInt32(data, offset + 0x3C));
            }
        }

        public bool HasParent() => Parent != null;
        public bool IsFile() => !FileAttributes.HasFlag(FileAttribute.Directory);
        public bool IsDirectory() => FileAttributes.HasFlag(FileAttribute.Directory);
        public bool IsDeleted() => FileNameLength == Constants.DirentDeleted;

        string _fileName;
        public string FileName 
        { 
            get
            {
                // TODO: May move this back to ReadDirectoryEntry()
                // The reason for this being here is for lazy loading, to prevent the Indexer from
                // running slowly.
                if (string.IsNullOrEmpty(_fileName))
                {
                    if (FileNameLength == Constants.DirentDeleted)
                    {
                        var trueFileNameLength = Array.IndexOf(FileNameBytes, (byte)0xff);

                        if (trueFileNameLength == -1)
                            trueFileNameLength = 42;

                        _fileName = Encoding.ASCII.GetString(FileNameBytes, 0, trueFileNameLength);
                    }
                    else
                    {
                        if (FileNameLength > 42)
                        {
                            // Should throw exception
                            FileNameLength = 42;
                        }

                        _fileName = Encoding.ASCII.GetString(FileNameBytes, 0, FileNameLength);
                    }
                }
                
                return _fileName;
            }
        }

        public void AddChildren(List<DirectoryEntry> children)
        {
            if (!IsDirectory())
                return;

            foreach (var dirent in children)
            {
                dirent.Parent = this;
                Children.Add(dirent);
            }
        }

        public DirectoryEntry GetRootDirectoryEntry()
        {
            DirectoryEntry parent = Parent;

            if (parent == null)
                return this;

            while (parent.Parent != null)
                parent = parent.Parent;

            return parent;
        }

        public string GetPath()
        {
            List<string> ancestry = new List<string>();

            for (DirectoryEntry parent = Parent; parent != null; parent = parent.Parent)
                ancestry.Add(parent.FileName);

            ancestry.Reverse();
            return string.Join("/", ancestry.ToArray());
        }

        public string GetFullPath()
        {
            return GetPath() + "/" + FileName;
        }

        public long CountFiles()
        {
            if (IsDeleted())
                return 0;

            if (IsDirectory())
            {
                long numFiles = 1;

                foreach (var dirent in Children)
                    numFiles += dirent.CountFiles();

                return numFiles;
            }

            return 1;
        }
    }
}