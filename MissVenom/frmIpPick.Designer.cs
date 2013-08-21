namespace MissVenom
{
    partial class frmIpPick
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmIpPick));
            this.label1 = new System.Windows.Forms.Label();
            this.lbIpAddresses = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.Location = new System.Drawing.Point(12, 109);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(120, 46);
            this.label1.TabIndex = 2;
            this.label1.Text = "Select the IP address to use with MissVenom. Double click to select.\r\n";
            // 
            // lbIpAddresses
            // 
            this.lbIpAddresses.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbIpAddresses.FormattingEnabled = true;
            this.lbIpAddresses.Location = new System.Drawing.Point(15, 13);
            this.lbIpAddresses.Name = "lbIpAddresses";
            this.lbIpAddresses.ScrollAlwaysVisible = true;
            this.lbIpAddresses.Size = new System.Drawing.Size(114, 82);
            this.lbIpAddresses.TabIndex = 3;
            this.lbIpAddresses.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.lbIpAddresses_MouseDoubleClick);
            // 
            // frmIpPick
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(141, 164);
            this.Controls.Add(this.lbIpAddresses);
            this.Controls.Add(this.label1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmIpPick";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "IP addresses";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListBox lbIpAddresses;
    }
}