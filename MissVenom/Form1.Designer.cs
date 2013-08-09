namespace MissVenom
{
    partial class Form1
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.textBoxOutput = new System.Windows.Forms.TextBox();
            this.checkBoxDns = new System.Windows.Forms.CheckBox();
            this.checkBoxTCP = new System.Windows.Forms.CheckBox();
            this.checkBoxARP = new System.Windows.Forms.CheckBox();
            this.checkBoxReg = new System.Windows.Forms.CheckBox();
            this.checkBoxMedia = new System.Windows.Forms.CheckBox();
            this.checkBoxSync = new System.Windows.Forms.CheckBox();
            this.buttonStart = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxPasswd = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // textBoxOutput
            // 
            this.textBoxOutput.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxOutput.CausesValidation = false;
            this.textBoxOutput.Location = new System.Drawing.Point(14, 111);
            this.textBoxOutput.Multiline = true;
            this.textBoxOutput.Name = "textBoxOutput";
            this.textBoxOutput.ReadOnly = true;
            this.textBoxOutput.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxOutput.Size = new System.Drawing.Size(258, 138);
            this.textBoxOutput.TabIndex = 2;
            this.textBoxOutput.TabStop = false;
            // 
            // checkBoxDns
            // 
            this.checkBoxDns.AutoSize = true;
            this.checkBoxDns.Checked = true;
            this.checkBoxDns.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxDns.Location = new System.Drawing.Point(14, 14);
            this.checkBoxDns.Name = "checkBoxDns";
            this.checkBoxDns.Size = new System.Drawing.Size(78, 17);
            this.checkBoxDns.TabIndex = 3;
            this.checkBoxDns.Text = "DNS Proxy";
            this.checkBoxDns.UseVisualStyleBackColor = true;
            // 
            // checkBoxTCP
            // 
            this.checkBoxTCP.AutoSize = true;
            this.checkBoxTCP.Location = new System.Drawing.Point(13, 37);
            this.checkBoxTCP.Name = "checkBoxTCP";
            this.checkBoxTCP.Size = new System.Drawing.Size(76, 17);
            this.checkBoxTCP.TabIndex = 4;
            this.checkBoxTCP.Text = "TCP Proxy";
            this.checkBoxTCP.UseVisualStyleBackColor = true;
            // 
            // checkBoxARP
            // 
            this.checkBoxARP.AutoSize = true;
            this.checkBoxARP.Location = new System.Drawing.Point(13, 61);
            this.checkBoxARP.Name = "checkBoxARP";
            this.checkBoxARP.Size = new System.Drawing.Size(79, 17);
            this.checkBoxARP.TabIndex = 5;
            this.checkBoxARP.Text = "ARP Spoof";
            this.checkBoxARP.UseVisualStyleBackColor = true;
            // 
            // checkBoxReg
            // 
            this.checkBoxReg.AutoSize = true;
            this.checkBoxReg.Checked = true;
            this.checkBoxReg.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxReg.Location = new System.Drawing.Point(99, 14);
            this.checkBoxReg.Name = "checkBoxReg";
            this.checkBoxReg.Size = new System.Drawing.Size(117, 17);
            this.checkBoxReg.TabIndex = 6;
            this.checkBoxReg.Text = "Capture registration";
            this.checkBoxReg.UseVisualStyleBackColor = true;
            // 
            // checkBoxMedia
            // 
            this.checkBoxMedia.AutoSize = true;
            this.checkBoxMedia.Location = new System.Drawing.Point(99, 37);
            this.checkBoxMedia.Name = "checkBoxMedia";
            this.checkBoxMedia.Size = new System.Drawing.Size(94, 17);
            this.checkBoxMedia.TabIndex = 7;
            this.checkBoxMedia.Text = "Capture media";
            this.checkBoxMedia.UseVisualStyleBackColor = true;
            // 
            // checkBoxSync
            // 
            this.checkBoxSync.AutoSize = true;
            this.checkBoxSync.Location = new System.Drawing.Point(99, 61);
            this.checkBoxSync.Name = "checkBoxSync";
            this.checkBoxSync.Size = new System.Drawing.Size(127, 17);
            this.checkBoxSync.TabIndex = 8;
            this.checkBoxSync.Text = "Capture ContactSync";
            this.checkBoxSync.UseVisualStyleBackColor = true;
            // 
            // buttonStart
            // 
            this.buttonStart.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonStart.Location = new System.Drawing.Point(218, 14);
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.Size = new System.Drawing.Size(54, 30);
            this.buttonStart.TabIndex = 9;
            this.buttonStart.Text = "Start";
            this.buttonStart.UseVisualStyleBackColor = true;
            this.buttonStart.Click += new System.EventHandler(this.buttonStart_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 87);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(99, 13);
            this.label1.TabIndex = 10;
            this.label1.Text = "Password (optional)";
            // 
            // textBoxPasswd
            // 
            this.textBoxPasswd.Location = new System.Drawing.Point(117, 84);
            this.textBoxPasswd.Name = "textBoxPasswd";
            this.textBoxPasswd.Size = new System.Drawing.Size(155, 20);
            this.textBoxPasswd.TabIndex = 11;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Controls.Add(this.textBoxPasswd);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.buttonStart);
            this.Controls.Add(this.checkBoxSync);
            this.Controls.Add(this.checkBoxMedia);
            this.Controls.Add(this.checkBoxReg);
            this.Controls.Add(this.checkBoxARP);
            this.Controls.Add(this.checkBoxTCP);
            this.Controls.Add(this.checkBoxDns);
            this.Controls.Add(this.textBoxOutput);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(300, 300);
            this.Name = "Form1";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.Text = "MissVenom";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxOutput;
        private System.Windows.Forms.CheckBox checkBoxDns;
        private System.Windows.Forms.CheckBox checkBoxTCP;
        private System.Windows.Forms.CheckBox checkBoxARP;
        private System.Windows.Forms.CheckBox checkBoxReg;
        private System.Windows.Forms.CheckBox checkBoxMedia;
        private System.Windows.Forms.CheckBox checkBoxSync;
        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxPasswd;


    }
}

