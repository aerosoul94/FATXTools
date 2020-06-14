using FATX;
using System.Collections.Generic;

namespace FATXTools.Database
{
    public class FileDatabase
    {
        // Map of recovered files according to its offset
        Dictionary<long, RecoveredFile> files;

        public FileDatabase(Volume volume)
        {
            this.files = new Dictionary<long, RecoveredFile>();

            MergeActiveFileSystem(volume);
        }

        public void MergeActiveFileSystem(Volume volume)
        {
            RegisterDirectoryEntries(volume.GetRoot());
        }

        private void RegisterFile(long offset, DirectoryEntry dirent, bool deleted)
        {
            if (!files.ContainsKey(offset))
            {
                files[offset] = new RecoveredFile(dirent, deleted);
            }
        }

        private void RegisterDirectoryEntries(List<DirectoryEntry> dirents)
        {
            foreach (var dirent in dirents)
            {
                if (dirent.IsDeleted())
                {
                    RegisterFile(dirent.Offset, dirent, true);
                }
                else
                {
                    RegisterFile(dirent.Offset, dirent, false);

                    if (dirent.IsDirectory())
                    {
                        RegisterDirectoryEntries(dirent.Children);
                    }
                }
            }
        }

        private void RegisterDeletedDirectoryEntries(List<DirectoryEntry> dirents)
        {
            foreach (var dirent in dirents)
            {
                RegisterFile(dirent.Offset, dirent, true);

                if (dirent.IsDirectory())
                {
                    RegisterDeletedDirectoryEntries(dirent.Children);
                }
            }
        }

        public void MergeMetadataAnalysis(MetadataAnalyzer analyzer)
        {
            RegisterDeletedDirectoryEntries(analyzer.GetRootDirectory());
        }

        public void MergeFileCarver(FileCarver carver)
        {
            // TODO
        }

        public RecoveredFile GetFile(long offset)
        {
            if (files.ContainsKey(offset))
            {
                return files[offset];
            }

            return null;
        }

        public RecoveredFile GetFile(DirectoryEntry dirent)
        {
            if (files.ContainsKey(dirent.Offset))
            {
                return files[dirent.Offset];
            }

            return null;
        }

        public Dictionary<long, RecoveredFile> GetFiles()
        {
            return files;
        }
    }
}
