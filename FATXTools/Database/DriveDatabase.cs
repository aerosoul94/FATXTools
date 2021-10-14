using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

using FATX.Drive;
using FATX.FileSystem;

namespace FATXTools.Database
{
    public class DriveDatabase
    {
        XDrive _drive;
        string _driveName;
        List<PartitionDatabase> _partitionDatabases;

        public DriveDatabase(string driveName, XDrive drive)
        {
            _drive = drive;
            _driveName = driveName;

            _partitionDatabases = new List<PartitionDatabase>();
        }

        public event EventHandler<AddPartitionEventArgs> OnPartitionAdded;
        public event EventHandler<RemovePartitionEventArgs> OnPartitionRemoved;

        public PartitionDatabase AddPartition(Volume volume)
        {
            var partitionDatabase = new PartitionDatabase(volume);
            _partitionDatabases.Add(partitionDatabase);
            return partitionDatabase;
        }

        public void RemovePartition(int index)
        {
            _partitionDatabases.RemoveAt(index);

            OnPartitionRemoved?.Invoke(this, new RemovePartitionEventArgs(index));
        }

        public void Save(string path)
        {
            Dictionary<string, object> databaseObject = new Dictionary<string, object>
            {
                ["Version"] = 1,
                ["Drive"] = new Dictionary<string, object>()
            };

            var driveObject = databaseObject["Drive"] as Dictionary<string, object>;
            driveObject["FileName"] = _driveName;

            driveObject["Partitions"] = new List<Dictionary<string, object>>();
            var partitionList = driveObject["Partitions"] as List<Dictionary<string, object>>;

            foreach (var partitionDatabase in _partitionDatabases)
            {
                var partitionObject = new Dictionary<string, object>();
                partitionList.Add(partitionObject);
                partitionDatabase.Save(partitionObject);
            }

            JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions()
            {
                WriteIndented = true,
            };

            string json = JsonSerializer.Serialize(databaseObject, jsonSerializerOptions);

            File.WriteAllText(path, json);
        }

        private bool LoadIfNotExists(JsonElement partitionElement)
        {
            foreach (var partitionDatabase in _partitionDatabases)
            {
                if (partitionDatabase.Volume.Offset == partitionElement.GetProperty("Offset").GetInt64())
                {
                    partitionDatabase.LoadFromJson(partitionElement);

                    return true;
                }
            }

            return false;
        }

        public void LoadFromJson(string path)
        {
            string json = File.ReadAllText(path);

            Dictionary<string, object> databaseObject = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

            if (databaseObject.ContainsKey("Drive"))
            {
                JsonElement driveJsonElement = (JsonElement)databaseObject["Drive"];

                if (driveJsonElement.TryGetProperty("Partitions", out var partitionsElement))
                {
                    foreach (var partitionElement in partitionsElement.EnumerateArray())
                    {
                        // Check if partition exists
                        if (!LoadIfNotExists(partitionElement))
                        {
                            // It does not exist, let's load it in.
                            var offset = partitionElement.GetProperty("Offset").GetInt64();
                            var length = partitionElement.GetProperty("Length").GetInt64();
                            var name = partitionElement.GetProperty("Name").GetString();

                            var partition = _drive.AddPartition(name, offset, length);

                            partition.Volume = new Volume(partition, _drive is XboxDrive ? Platform.Xbox : Platform.X360);

                            OnPartitionAdded?.Invoke(this, new AddPartitionEventArgs(partition));

                            // Might need some clean up here. Should not rely on the event to add the partition to the database.
                            _partitionDatabases[_partitionDatabases.Count - 1].LoadFromJson(partitionElement);
                        }
                    }

                }
                else
                {
                    throw new FileLoadException("Database: Drive has no Partition list!");
                }
            }
            else
            {
                throw new FileLoadException("Database: Missing Drive object!");
            }
        }
    }
}
