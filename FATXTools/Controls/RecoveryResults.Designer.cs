namespace FATXTools
{
    partial class RecoveryResults
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
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.treeContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.recoverSelectedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.recoverAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.listView1 = new System.Windows.Forms.ListView();
            this.columnHeader8 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader7 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.listContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.dumpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.recoverCurrentDirectoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.recoverCurrentClusterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dumpAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.viewInformationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.treeContextMenu.SuspendLayout();
            this.listContextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.treeView1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.listView1);
            this.splitContainer1.Size = new System.Drawing.Size(1546, 954);
            this.splitContainer1.SplitterDistance = 412;
            this.splitContainer1.TabIndex = 0;
            // 
            // treeView1
            // 
            this.treeView1.ContextMenuStrip = this.treeContextMenu;
            this.treeView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeView1.Location = new System.Drawing.Point(0, 0);
            this.treeView1.Name = "treeView1";
            this.treeView1.Size = new System.Drawing.Size(412, 954);
            this.treeView1.TabIndex = 0;
            this.treeView1.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeView1_AfterSelect);
            // 
            // treeContextMenu
            // 
            this.treeContextMenu.ImageScalingSize = new System.Drawing.Size(32, 32);
            this.treeContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.recoverSelectedToolStripMenuItem,
            this.recoverAllToolStripMenuItem});
            this.treeContextMenu.Name = "treeContextMenu";
            this.treeContextMenu.Size = new System.Drawing.Size(354, 80);
            // 
            // recoverSelectedToolStripMenuItem
            // 
            this.recoverSelectedToolStripMenuItem.Name = "recoverSelectedToolStripMenuItem";
            this.recoverSelectedToolStripMenuItem.Size = new System.Drawing.Size(353, 38);
            this.recoverSelectedToolStripMenuItem.Text = "Recover Selected Cluster";
            this.recoverSelectedToolStripMenuItem.Click += new System.EventHandler(this.treeRecoverSelectedToolStripMenuItem_Click);
            // 
            // recoverAllToolStripMenuItem
            // 
            this.recoverAllToolStripMenuItem.Name = "recoverAllToolStripMenuItem";
            this.recoverAllToolStripMenuItem.Size = new System.Drawing.Size(353, 38);
            this.recoverAllToolStripMenuItem.Text = "Recover All Clusters";
            this.recoverAllToolStripMenuItem.Click += new System.EventHandler(this.treeRecoverAllToolStripMenuItem_Click);
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
            this.listView1.Size = new System.Drawing.Size(1130, 954);
            this.listView1.TabIndex = 1;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            this.listView1.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.listView1_ColumnClick);
            this.listView1.DoubleClick += new System.EventHandler(this.listView1_DoubleClick);
            // 
            // columnHeader8
            // 
            this.columnHeader8.Text = "";
            this.columnHeader8.Width = 45;
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
            // listContextMenu
            // 
            this.listContextMenu.ImageScalingSize = new System.Drawing.Size(28, 28);
            this.listContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.dumpToolStripMenuItem,
            this.recoverCurrentDirectoryToolStripMenuItem,
            this.recoverCurrentClusterToolStripMenuItem,
            this.dumpAllToolStripMenuItem,
            this.toolStripSeparator1,
            this.viewInformationToolStripMenuItem});
            this.listContextMenu.Name = "contextMenuStrip1";
            this.listContextMenu.Size = new System.Drawing.Size(366, 200);
            // 
            // dumpToolStripMenuItem
            // 
            this.dumpToolStripMenuItem.Name = "dumpToolStripMenuItem";
            this.dumpToolStripMenuItem.Size = new System.Drawing.Size(365, 38);
            this.dumpToolStripMenuItem.Text = "Recover Selected";
            this.dumpToolStripMenuItem.Click += new System.EventHandler(this.listRecoverSelectedToolStripMenuItem_Click);
            // 
            // recoverCurrentDirectoryToolStripMenuItem
            // 
            this.recoverCurrentDirectoryToolStripMenuItem.Name = "recoverCurrentDirectoryToolStripMenuItem";
            this.recoverCurrentDirectoryToolStripMenuItem.Size = new System.Drawing.Size(365, 38);
            this.recoverCurrentDirectoryToolStripMenuItem.Text = "Recover Current Directory";
            this.recoverCurrentDirectoryToolStripMenuItem.Click += new System.EventHandler(this.listRecoverCurrentDirectoryToolStripMenuItem_Click);
            // 
            // recoverCurrentClusterToolStripMenuItem
            // 
            this.recoverCurrentClusterToolStripMenuItem.Name = "recoverCurrentClusterToolStripMenuItem";
            this.recoverCurrentClusterToolStripMenuItem.Size = new System.Drawing.Size(365, 38);
            this.recoverCurrentClusterToolStripMenuItem.Text = "Recover Current Cluster";
            this.recoverCurrentClusterToolStripMenuItem.Click += new System.EventHandler(this.listRecoverCurrentClusterToolStripMenuItem_Click);
            // 
            // dumpAllToolStripMenuItem
            // 
            this.dumpAllToolStripMenuItem.Name = "dumpAllToolStripMenuItem";
            this.dumpAllToolStripMenuItem.Size = new System.Drawing.Size(365, 38);
            this.dumpAllToolStripMenuItem.Text = "Recover All Clusters";
            this.dumpAllToolStripMenuItem.Click += new System.EventHandler(this.listRecoverAllToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(362, 6);
            // 
            // viewInformationToolStripMenuItem
            // 
            this.viewInformationToolStripMenuItem.Name = "viewInformationToolStripMenuItem";
            this.viewInformationToolStripMenuItem.Size = new System.Drawing.Size(365, 38);
            this.viewInformationToolStripMenuItem.Text = "View Information";
            // 
            // RecoveryResults
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.Controls.Add(this.splitContainer1);
            this.Name = "RecoveryResults";
            this.Size = new System.Drawing.Size(1546, 954);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.treeContextMenu.ResumeLayout(false);
            this.listContextMenu.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
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
        private System.Windows.Forms.ContextMenuStrip listContextMenu;
        private System.Windows.Forms.ToolStripMenuItem dumpToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem viewInformationToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem dumpAllToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem recoverCurrentDirectoryToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip treeContextMenu;
        private System.Windows.Forms.ToolStripMenuItem recoverSelectedToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem recoverAllToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem recoverCurrentClusterToolStripMenuItem;
    }
}
