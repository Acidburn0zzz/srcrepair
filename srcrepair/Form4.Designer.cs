﻿namespace srcrepair
{
    partial class frmRepBuilder
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmRepBuilder));
            this.WelcomeLabel = new System.Windows.Forms.Label();
            this.Compress = new System.Windows.Forms.CheckBox();
            this.GenerateNow = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // WelcomeLabel
            // 
            resources.ApplyResources(this.WelcomeLabel, "WelcomeLabel");
            this.WelcomeLabel.Name = "WelcomeLabel";
            // 
            // Compress
            // 
            resources.ApplyResources(this.Compress, "Compress");
            this.Compress.Checked = true;
            this.Compress.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Compress.Name = "Compress";
            this.Compress.UseVisualStyleBackColor = true;
            // 
            // GenerateNow
            // 
            resources.ApplyResources(this.GenerateNow, "GenerateNow");
            this.GenerateNow.Name = "GenerateNow";
            this.GenerateNow.UseVisualStyleBackColor = true;
            this.GenerateNow.Click += new System.EventHandler(this.GenerateNow_Click);
            // 
            // frmRepBuilder
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.GenerateNow);
            this.Controls.Add(this.Compress);
            this.Controls.Add(this.WelcomeLabel);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmRepBuilder";
            this.ShowInTaskbar = false;
            this.Load += new System.EventHandler(this.frmRepBuilder_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label WelcomeLabel;
        private System.Windows.Forms.CheckBox Compress;
        private System.Windows.Forms.Button GenerateNow;
    }
}