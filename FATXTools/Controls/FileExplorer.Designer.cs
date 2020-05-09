namespace FATXTools.Controls
{
    partial class FileExplorer
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.treeContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.runMetadataAnalyzerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.runFileCarverToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.listView1 = new System.Windows.Forms.ListView();
            this.columnHeader8 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader7 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.backgroundWorker2 = new System.ComponentModel.BackgroundWorker();
            this.listContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.saveSelectedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAllToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.runMetadataAnalyzerToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.runFileCarverToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.saveSelectedToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAllToolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.viewInformationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.treeContextMenu.SuspendLayout();
            this.listContextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.treeView1);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.listView1);
            this.splitContainer2.Size = new System.Drawing.Size(2133, 1113);
            this.splitContainer2.SplitterDistance = 356;
            this.splitContainer2.TabIndex = 2;
            // 
            // treeView1
            // 
            this.treeView1.ContextMenuStrip = this.treeContextMenu;
            this.treeView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeView1.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.treeView1.Location = new System.Drawing.Point(0, 0);
            this.treeView1.Name = "treeView1";
            this.treeView1.Size = new System.Drawing.Size(356, 1113);
            this.treeView1.TabIndex = 0;
            this.treeView1.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeView1_AfterSelect);
            // 
            // treeContextMenu
            // 
            this.treeContextMenu.ImageScalingSize = new System.Drawing.Size(32, 32);
            this.treeContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.runMetadataAnalyzerToolStripMenuItem,
            this.runFileCarverToolStripMenuItem,
            this.toolStripSeparator2,
            this.saveSelectedToolStripMenuItem1,
            this.saveAllToolStripMenuItem2});
            this.treeContextMenu.Name = "contextMenuStrip1";
            this.treeContextMenu.Size = new System.Drawing.Size(339, 162);
            // 
            // runMetadataAnalyzerToolStripMenuItem
            // 
            this.runMetadataAnalyzerToolStripMenuItem.Name = "runMetadataAnalyzerToolStripMenuItem";
            this.runMetadataAnalyzerToolStripMenuItem.Size = new System.Drawing.Size(338, 38);
            this.runMetadataAnalyzerToolStripMenuItem.Text = "Run Metadata Analyzer";
            this.runMetadataAnalyzerToolStripMenuItem.Click += new System.EventHandler(this.runMetadataAnalyzerToolStripMenuItem_Click);
            // 
            // runFileCarverToolStripMenuItem
            // 
            this.runFileCarverToolStripMenuItem.Name = "runFileCarverToolStripMenuItem";
            this.runFileCarverToolStripMenuItem.Size = new System.Drawing.Size(338, 38);
            this.runFileCarverToolStripMenuItem.Text = "Run File Carver";
            this.runFileCarverToolStripMenuItem.Click += new System.EventHandler(this.runFileCarverToolStripMenuItem_Click);
            // 
            // listView1
            // 
            this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader8,
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader4,
            this.columnHeader5,
            this.columnHeader6,
            this.columnHeader7});
            this.listView1.ContextMenuStrip = this.listContextMenu;
            this.listView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listView1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.857143F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.listView1.FullRowSelect = true;
            this.listView1.HideSelection = false;
            this.listView1.Location = new System.Drawing.Point(0, 0);
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(1773, 1113);
            this.listView1.TabIndex = 0;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            this.listView1.DoubleClick += new System.EventHandler(this.listView1_DoubleClick);
            // 
            // columnHeader8
            // 
            this.columnHeader8.Text = "";
            this.columnHeader8.Width = 44;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Name";
            this.columnHeader1.Width = 200;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Size";
            this.columnHeader2.Width = 150;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Date Created";
            this.columnHeader3.Width = 150;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "Date Modified";
            this.columnHeader4.Width = 150;
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "Date Accessed";
            this.columnHeader5.Width = 150;
            // 
            // columnHeader6
            // 
            this.columnHeader6.Text = "Offset";
            this.columnHeader6.Width = 150;
            // 
            // columnHeader7
            // 
            this.columnHeader7.Text = "Cluster";
            this.columnHeader7.Width = 150;
            // 
            // backgroundWorker1
            // 
            this.backgroundWorker1.WorkerReportsProgress = true;
            this.backgroundWorker1.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker1_DoWork);
            this.backgroundWorker1.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.backgroundWorker1_ProgressChanged);
            this.backgroundWorker1.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker1_RunWorkerCompleted);
            // 
            // backgroundWorker2
            // 
            this.backgroundWorker2.WorkerReportsProgress = true;
            this.backgroundWorker2.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker2_DoWork);
            this.backgroundWorker2.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.backgroundWorker2_ProgressChanged);
            this.backgroundWorker2.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker2_RunWorkerCompleted);
            // 
            // listContextMenu
            // 
            this.listContextMenu.ImageScalingSize = new System.Drawing.Size(32, 32);
            this.listContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.runMetadataAnalyzerToolStripMenuItem1,
            this.runFileCarverToolStripMenuItem1,
            this.toolStripSeparator1,
            this.saveSelectedToolStripMenuItem,
            this.saveAllToolStripMenuItem,
            this.saveAllToolStripMenuItem1,
            this.toolStripSeparator3,
            this.viewInformationToolStripMenuItem});
            this.listContextMenu.Name = "listContextMenu";
            this.listContextMenu.Size = new System.Drawing.Size(339, 288);
            // 
            // saveSelectedToolStripMenuItem
            // 
            this.saveSelectedToolStripMenuItem.Name = "saveSelectedToolStripMenuItem";
            this.saveSelectedToolStripMenuItem.Size = new System.Drawing.Size(338, 38);
            this.saveSelectedToolStripMenuItem.Text = "Save Selected";
            this.saveSelectedToolStripMenuItem.Click += new System.EventHandler(this.listSaveSelectedToolStripMenuItem_Click);
            // 
            // saveAllToolStripMenuItem
            // 
            this.saveAllToolStripMenuItem.Name = "saveAllToolStripMenuItem";
            this.saveAllToolStripMenuItem.Size = new System.Drawing.Size(338, 38);
            this.saveAllToolStripMenuItem.Text = "Save Current Directory";
            this.saveAllToolStripMenuItem.Click += new System.EventHandler(this.saveAllToolStripMenuItem_Click);
            // 
            // saveAllToolStripMenuItem1
            // 
            this.saveAllToolStripMenuItem1.Name = "saveAllToolStripMenuItem1";
            this.saveAllToolStripMenuItem1.Size = new System.Drawing.Size(338, 38);
            this.saveAllToolStripMenuItem1.Text = "Save All";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(335, 6);
            // 
            // runMetadataAnalyzerToolStripMenuItem1
            // 
            this.runMetadataAnalyzerToolStripMenuItem1.Name = "runMetadataAnalyzerToolStripMenuItem1";
            this.runMetadataAnalyzerToolStripMenuItem1.Size = new System.Drawing.Size(338, 38);
            this.runMetadataAnalyzerToolStripMenuItem1.Text = "Run Metadata Analyzer";
            this.runMetadataAnalyzerToolStripMenuItem1.Click += new System.EventHandler(this.runMetadataAnalyzerToolStripMenuItem_Click);
            // 
            // runFileCarverToolStripMenuItem1
            // 
            this.runFileCarverToolStripMenuItem1.Name = "runFileCarverToolStripMenuItem1";
            this.runFileCarverToolStripMenuItem1.Size = new System.Drawing.Size(338, 38);
            this.runFileCarverToolStripMenuItem1.Text = "Run File Carver";
            this.runFileCarverToolStripMenuItem1.Click += new System.EventHandler(this.runFileCarverToolStripMenuItem_Click);
            // 
            // saveSelectedToolStripMenuItem1
            // 
            this.saveSelectedToolStripMenuItem1.Name = "saveSelectedToolStripMenuItem1";
            this.saveSelectedToolStripMenuItem1.Size = new System.Drawing.Size(338, 38);
            this.saveSelectedToolStripMenuItem1.Text = "Save Selected";
            this.saveSelectedToolStripMenuItem1.Click += new System.EventHandler(this.treeSaveSelectedToolStripMenuItem1_Click);
            // 
            // saveAllToolStripMenuItem2
            // 
            this.saveAllToolStripMenuItem2.Name = "saveAllToolStripMenuItem2";
            this.saveAllToolStripMenuItem2.Size = new System.Drawing.Size(338, 38);
            this.saveAllToolStripMenuItem2.Text = "Save All";
            this.saveAllToolStripMenuItem2.Click += new System.EventHandler(this.saveAllToolStripMenuItem2_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(335, 6);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(335, 6);
            // 
            // viewInformationToolStripMenuItem
            // 
            this.viewInformationToolStripMenuItem.Name = "viewInformationToolStripMenuItem";
            this.viewInformationToolStripMenuItem.Size = new System.Drawing.Size(338, 38);
            this.viewInformationToolStripMenuItem.Text = "View Information";
            this.viewInformationToolStripMenuItem.Click += new System.EventHandler(this.viewInformationToolStripMenuItem_Click);
            // 
            // FileExplorer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer2);
            this.Name = "FileExplorer";
            this.Size = new System.Drawing.Size(2133, 1113);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.treeContextMenu.ResumeLayout(false);
            this.listContextMenu.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.ColumnHeader columnHeader8;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.ColumnHeader columnHeader6;
        private System.Windows.Forms.ColumnHeader columnHeader7;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private System.ComponentModel.BackgroundWorker backgroundWorker2;
        private System.Windows.Forms.ContextMenuStrip treeContextMenu;
        private System.Windows.Forms.ToolStripMenuItem runMetadataAnalyzerToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem runFileCarverToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip listContextMenu;
        private System.Windows.Forms.ToolStripMenuItem runMetadataAnalyzerToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem runFileCarverToolStripMenuItem1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem saveSelectedToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAllToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAllToolStripMenuItem1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem saveSelectedToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem saveAllToolStripMenuItem2;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripMenuItem viewInformationToolStripMenuItem;
    }
}
