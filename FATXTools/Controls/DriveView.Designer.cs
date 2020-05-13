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
            // 
            // DriveView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.partitionTabControl);
            this.Name = "DriveView";
            this.Size = new System.Drawing.Size(1913, 1006);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl partitionTabControl;
    }
}
