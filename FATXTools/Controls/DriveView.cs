using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text.Json;
using FATX;
using FATXTools.Controls;
using FATXTools.Utilities;
using System.IO;

namespace FATXTools
{
    public partial class DriveView : UserControl
    {
        /// <summary>
        /// List of loaded drives.
        /// </summary>
        //private List<DriveReader> driveList = new List<DriveReader>();

        /// <summary>
        /// Currently loaded drive.
        /// </summary>
        private DriveReader drive;

        private string driveName;

        /// <summary>
        /// List of partitions in this drive.
        /// </summary>
        private List<Volume> partitionList = new List<Volume>();

        private List<PartitionView> partitionViews = new List<PartitionView>();

        private TaskRunner taskRunner;

        public event EventHandler TabSelectionChanged;

        public DriveView()
        {
            InitializeComponent();
        }

        public void AddDrive(string name, DriveReader drive)
        {
            this.driveName = name;
            this.drive = drive;

            // Single task runner for this drive
            // Currently only one task will be allowed to operate on a drive to avoid race conditions.
            this.taskRunner = new TaskRunner(this.ParentForm);

            foreach (var volume in drive.Partitions)
            {
                AddPartition(volume);
            }

            // Fire SelectedIndexChanged event.
            SelectedIndexChanged();
        }

        public void AddPartition(Volume volume)
        {
            try
            {
                volume.Mount();

                Console.WriteLine($"Successfuly mounted {volume.Name}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to mount {volume.Name}: {e.Message}");
            }

            partitionList.Add(volume);

            var page = new TabPage(volume.Name);
            var partitionView = new PartitionView(taskRunner, volume);
            partitionView.Dock = DockStyle.Fill;
            page.Controls.Add(partitionView);
            partitionTabControl.TabPages.Add(page);
            partitionViews.Add(partitionView);
        }

        public DriveReader GetDrive()
        {
            return drive;
        }

        public void Save(string path)
        {
            Dictionary<string, object> databaseObject = new Dictionary<string, object>();

            databaseObject["Version"] = 1;
            databaseObject["Drive"] = new Dictionary<string, object>();

            var driveObject = databaseObject["Drive"] as Dictionary<string, object>;
            driveObject["FileName"] = this.driveName;

            driveObject["Partitions"] = new List<Dictionary<string, object>>();
            var partitionList = driveObject["Partitions"] as List<Dictionary<string, object>>;

            foreach (var partitionView in partitionViews)
            {
                var partitionObject = new Dictionary<string, object>();
                partitionList.Add(partitionObject);
                partitionView.Save(partitionObject);
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
            foreach (var partitionView in partitionViews)
            {
                if (partitionView.PartitionName == partitionElement.GetProperty("Name").GetString())
                {
                    partitionView.LoadFromJson(partitionElement);

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
        
        private void SelectedIndexChanged()
        {
            TabSelectionChanged?.Invoke(this, new PartitionSelectedEventArgs()
            {
                volume = partitionList[partitionTabControl.SelectedIndex]
            });
        }

        private void partitionTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            SelectedIndexChanged();
        }
    }
}
