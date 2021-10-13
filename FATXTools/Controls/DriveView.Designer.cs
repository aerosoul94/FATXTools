namespace FATXTools
{
    partial class DriveView
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
            this.partitionTabControl = new System.Windows.Forms.TabControl();
            this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // partitionTabControl
            // 
            this.partitionTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.partitionTabControl.Location = new System.Drawing.Point(0, 0);
            this.partitionTabControl.Name = "partitionTabControl";
            this.partitionTabControl.SelectedIndex = 0;
            this.partitionTabControl.Size = new System.Drawing.Size(1913, 1006);
            this.partitionTabControl.TabIndex = 0;
            this.partitionTabControl.SelectedIndexChanged += new System.EventHandler(this.partitionTabControl_SelectedIndexChanged);
            this.partitionTabControl.MouseClick += new System.Windows.Forms.MouseEventHandler(this.partitionTabControl_MouseClick);
            //
            // contextMenuStrip
            //
            this.contextMenuStrip.ImageScalingSize = new System.Drawing.Size(32, 32);
            this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1});
            this.contextMenuStrip.Name = "contextMenuStrip1";
            this.contextMenuStrip.Size = new System.Drawing.Size(270, 42);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(269, 38);
            this.toolStripMenuItem1.Text = "Remove Partition";
            this.toolStripMenuItem1.Click += ToolStripMenuItem1_Click;
            // 
            // DriveView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.partitionTabControl);
            this.Name = "DriveView";
            this.Size = new System.Drawing.Size(1913, 1006);
            this.contextMenuStrip.ResumeLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl partitionTabControl;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
    }
}
