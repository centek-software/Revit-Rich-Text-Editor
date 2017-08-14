namespace CTEK_Rich_Text_Editor
{
    partial class MainFormCEF
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
            this.toolStripContainer1 = new System.Windows.Forms.ToolStripContainer();
            this.panel1 = new System.Windows.Forms.Panel();
            this.toLowercaseButton = new System.Windows.Forms.Button();
            this.toUppercaseButton = new System.Windows.Forms.Button();
            this.status = new System.Windows.Forms.Label();
            this.aboutButton = new System.Windows.Forms.Button();
            this.spaceSnglButton = new System.Windows.Forms.Button();
            this.spaceDblButton = new System.Windows.Forms.Button();
            this.updateButton = new System.Windows.Forms.Button();
            this.toolStripContainer1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStripContainer1
            // 
            this.toolStripContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // toolStripContainer1.ContentPanel
            // 
            this.toolStripContainer1.ContentPanel.Size = new System.Drawing.Size(1063, 488);
            this.toolStripContainer1.Location = new System.Drawing.Point(0, -2);
            this.toolStripContainer1.Name = "toolStripContainer1";
            this.toolStripContainer1.Size = new System.Drawing.Size(1063, 513);
            this.toolStripContainer1.TabIndex = 0;
            this.toolStripContainer1.Text = "toolStripContainer1";
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.Controls.Add(this.toLowercaseButton);
            this.panel1.Controls.Add(this.toUppercaseButton);
            this.panel1.Controls.Add(this.status);
            this.panel1.Controls.Add(this.aboutButton);
            this.panel1.Controls.Add(this.spaceSnglButton);
            this.panel1.Controls.Add(this.spaceDblButton);
            this.panel1.Controls.Add(this.updateButton);
            this.panel1.Location = new System.Drawing.Point(0, 514);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1063, 35);
            this.panel1.TabIndex = 1;
            // 
            // toLowercaseButton
            // 
            this.toLowercaseButton.Location = new System.Drawing.Point(439, 3);
            this.toLowercaseButton.Name = "toLowercaseButton";
            this.toLowercaseButton.Size = new System.Drawing.Size(94, 23);
            this.toLowercaseButton.TabIndex = 6;
            this.toLowercaseButton.Text = "To Lowercase";
            this.toLowercaseButton.UseVisualStyleBackColor = true;
            this.toLowercaseButton.Click += new System.EventHandler(this.ToLowercaseButton_Click);
            // 
            // toUppercaseButton
            // 
            this.toUppercaseButton.Location = new System.Drawing.Point(334, 3);
            this.toUppercaseButton.Name = "toUppercaseButton";
            this.toUppercaseButton.Size = new System.Drawing.Size(99, 23);
            this.toUppercaseButton.TabIndex = 5;
            this.toUppercaseButton.Text = "To Uppercase";
            this.toUppercaseButton.UseVisualStyleBackColor = true;
            this.toUppercaseButton.Click += new System.EventHandler(this.ToUppercaseButton_Click);
            // 
            // status
            // 
            this.status.AutoSize = true;
            this.status.Location = new System.Drawing.Point(688, 8);
            this.status.Name = "status";
            this.status.Size = new System.Drawing.Size(37, 13);
            this.status.TabIndex = 4;
            this.status.Text = "Status";
            // 
            // aboutButton
            // 
            this.aboutButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.aboutButton.Location = new System.Drawing.Point(984, 3);
            this.aboutButton.Name = "aboutButton";
            this.aboutButton.Size = new System.Drawing.Size(75, 23);
            this.aboutButton.TabIndex = 3;
            this.aboutButton.Text = "About";
            this.aboutButton.UseVisualStyleBackColor = true;
            this.aboutButton.Click += new System.EventHandler(this.AboutButton_Click);
            // 
            // spaceSnglButton
            // 
            this.spaceSnglButton.Location = new System.Drawing.Point(206, 3);
            this.spaceSnglButton.Name = "spaceSnglButton";
            this.spaceSnglButton.Size = new System.Drawing.Size(122, 23);
            this.spaceSnglButton.TabIndex = 2;
            this.spaceSnglButton.Text = "Single Space List";
            this.spaceSnglButton.UseVisualStyleBackColor = true;
            this.spaceSnglButton.Click += new System.EventHandler(this.SpaceSnglButton_Click);
            // 
            // spaceDblButton
            // 
            this.spaceDblButton.Location = new System.Drawing.Point(84, 3);
            this.spaceDblButton.Name = "spaceDblButton";
            this.spaceDblButton.Size = new System.Drawing.Size(116, 23);
            this.spaceDblButton.TabIndex = 1;
            this.spaceDblButton.Text = "Double Space List";
            this.spaceDblButton.UseVisualStyleBackColor = true;
            this.spaceDblButton.Click += new System.EventHandler(this.SpaceDblButton_Click);
            // 
            // updateButton
            // 
            this.updateButton.Location = new System.Drawing.Point(3, 3);
            this.updateButton.Name = "updateButton";
            this.updateButton.Size = new System.Drawing.Size(75, 23);
            this.updateButton.TabIndex = 0;
            this.updateButton.Text = "Update";
            this.updateButton.UseVisualStyleBackColor = true;
            this.updateButton.Click += new System.EventHandler(this.UpdateButton_Click);
            // 
            // MainFormCEF
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1062, 550);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.toolStripContainer1);
            this.Name = "MainFormCEF";
            this.Text = "Centek Revit Rich Text Editor";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainFormCEF_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainFormCEF_FormClosed);
            this.toolStripContainer1.ResumeLayout(false);
            this.toolStripContainer1.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ToolStripContainer toolStripContainer1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button updateButton;
        private System.Windows.Forms.Button aboutButton;
        private System.Windows.Forms.Button spaceSnglButton;
        private System.Windows.Forms.Button spaceDblButton;
        private System.Windows.Forms.Label status;
        private System.Windows.Forms.Button toUppercaseButton;
        private System.Windows.Forms.Button toLowercaseButton;

    }
}

