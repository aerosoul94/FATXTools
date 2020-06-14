using FATX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FATXTools.Database
{
    public class DriveDatabase
    {
        string driveName;

        List<PartitionDatabase> partitionDatabases;

        public DriveDatabase(string driveName)
        {
            this.driveName = driveName;

            partitionDatabases = new List<PartitionDatabase>();
        }

        public PartitionDatabase AddPartition(Volume volume)
        {
            var partitionDatabase = new PartitionDatabase(volume);
            partitionDatabases.Add(partitionDatabase);
            return partitionDatabase;
        }

        public void Save(string path)
        {
            Dictionary<string, object> databaseObject = new Dictionary<string, object>();

            databaseObject["Version"] = 1;
            databaseObject["Drive"] = new Dictionary<string, object>();

            var driveObject = databaseObject["Drive"] as Dictionary<string, object>;
            driveObject["FileName"] = driveName;

            driveObject["Partitions"] = new List<Dictionary<string, object>>();
            var partitionList = driveObject["Partitions"] as List<Dictionary<string, object>>;

            foreach (var partitionDatabase in partitionDatabases)
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
            foreach (var partitionDatabase in partitionDatabases)
            {
                if (partitionDatabase.PartitionName == partitionElement.GetProperty("Name").GetString())
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
                    //var partitionList = (List < Dictionary<string, object> > )driveObject["Partitions"];

                    foreach (var partitionElement in partitionsElement.EnumerateArray())
                    {
                        // Check if partitionview exists
                        if (!LoadIfNotExists(partitionElement))
                        {
                            // Otherwise, add it in
                            //Volume newVolume = new Volume(this.drive,
                            //    partitionElement.GetProperty("Name").GetString(),
                            //    partitionElement.GetProperty("Offset").GetInt64(),
                            //    partitionElement.GetProperty("Length").GetInt64());

                            //partitionViews.Add(PartitionView.FromJson(partitionElement, this.taskRunner, newVolume));
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
