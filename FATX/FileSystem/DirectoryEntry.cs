using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace FATX
{
    [Flags]
    public enum FileAttribute
    {
        ReadOnly = 0x1,
        Hidden = 0x2,
        System = 0x4,
        Directory = 0x10,
        Archive = 0x20
    }

    public class DirectoryEntry
    {
        private byte[] _rawBytes;
        private byte _fileNameLength;
        private byte _fileAttributes;
        private byte[] _fileNameBytes;
        private uint _firstCluster;
        private uint _fileSize;
        private uint _creationTimeAsInt;
        private uint _lastWriteTimeAsInt;
        private uint _lastAccessTimeAsInt;
        private TimeStamp _creationTime;
        private TimeStamp _lastWriteTime;
        private TimeStamp _lastAccesTime;
        private string _fileName;

        private Volume _volume;
        private DirectoryEntry _parent;
        private List<DirectoryEntry> _children = new List<DirectoryEntry>();
        private uint _cluster;
        private long _offset;

        public DirectoryEntry(Volume volume)
        {
            //Console.WriteLine(volume.Reader.Position.ToString("x"));
            //this._rawBytes = volume.Reader.ReadBytes(0x40);
            //volume.Reader.Seek(-0x40, SeekOrigin.Current);
            this._offset = volume.Reader.Position;
            this._fileNameLength = volume.Reader.ReadByte();
            this._fileAttributes = volume.Reader.ReadByte();
            this._fileNameBytes = volume.Reader.ReadBytes(42);
            this._firstCluster = volume.Reader.ReadUInt32();
            this._fileSize = volume.Reader.ReadUInt32();
            this._creationTimeAsInt = volume.Reader.ReadUInt32();
            this._lastWriteTimeAsInt = volume.Reader.ReadUInt32();
            this._lastAccessTimeAsInt = volume.Reader.ReadUInt32();

            this._volume = volume;
            this._parent = null;

            if (_fileNameLength == Constants.DirentNeverUsed ||
                _fileNameLength == Constants.DirentNeverUsed2)
                return;

            this._creationTime = (TimeStamp)Activator.CreateInstance(volume._timeStampFormat, this._creationTimeAsInt);
            this._lastWriteTime = (TimeStamp)Activator.CreateInstance(volume._timeStampFormat, this._lastWriteTimeAsInt);
            this._lastAccesTime = (TimeStamp)Activator.CreateInstance(volume._timeStampFormat, this._lastAccessTimeAsInt);

            if (_fileNameLength == Constants.DirentDeleted)
            {
                var trueFileNameLength = Array.IndexOf(_fileNameBytes, (byte)0xff);
                if (trueFileNameLength == -1)
                {
                    trueFileNameLength = 42;
                }
                _fileName = Encoding.UTF8.GetString(_fileNameBytes, 0, trueFileNameLength);
            }
            else
            {
                if (_fileNameLength > 42)
                {
                    // Warn user!
                    //Console.WriteLine("Invalid file name length!");
                    _fileNameLength = 42;
                }

                _fileName = Encoding.UTF8.GetString(_fileNameBytes, 0, _fileNameLength);
            }

        }

        public bool IsDirectory()
        {
            return FileAttributes.HasFlag(FileAttribute.Directory);
        }

        public bool IsDeleted()
        {
            return _fileNameLength == Constants.DirentDeleted;
        }

        public uint FileNameLength
        {
            get { return _fileNameLength; }
        }

        public FileAttribute FileAttributes
        {
            get { return (FileAttribute)_fileAttributes; }
        }

        public string FileName
        {
            get { return _fileName; }
        }

        public uint FirstCluster
        {
            get { return _firstCluster; }
        }

        public uint FileSize
        {
            get { return _fileSize; }
        }

        public TimeStamp CreationTime
        {
            get { return _creationTime; }
        }

        public TimeStamp LastWriteTime
        {
            get { return _lastWriteTime; }
        }

        public TimeStamp LastAccessTime
        {
            get { return _lastAccesTime; }
        }

        public long Offset
        {
            get { return _offset; }
            set { _offset = value; }
        }

        public void SetParent(DirectoryEntry parent)
        {
            this._parent = parent;
        }

        public void SetCluster(uint cluster)
        {
            this._cluster = cluster;
        }

        public uint GetCluster()
        {
            return this._cluster;
        }

        public Volume GetVolume()
        {
            return this._volume;
        }

        public DirectoryEntry GetParent()
        {
            return this._parent;
        }

        public bool HasParent()
        {
            return this._parent != null;
        }

        public List<DirectoryEntry> GetChildren()
        {
            if (!this.IsDirectory())
            {
                Console.WriteLine("UH OH! Trying to get children from non directory.");
            }

            return _children;
        }

        public void AddChild(DirectoryEntry child)
        {
            if (!this.IsDirectory())
            {
                Console.WriteLine("UH OH! Trying to add child to non directory.");
            }

            _children.Add(child);
        }

        public string GetFullPath()
        {
            DirectoryEntry parent = _parent;
            List<string> ancestry = new List<string>();

            while (parent != null)
            {
                ancestry.Add(parent.FileName);
                parent = parent.GetParent();
            }

            ancestry.Reverse();
            return String.Join("/", ancestry.ToArray());
        }

        public void AddDirentStreamToThisDirectory(List<DirectoryEntry> stream)
        {
            if (!IsDirectory())
            {
                // TODO: warn user
                return;
            }

            foreach (DirectoryEntry dirent in stream)
            {
                dirent.SetParent(this);
                _children.Add(dirent);
            }
        }
    }
}
