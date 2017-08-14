namespace CTEK_Rich_Text_Editor
{
  partial class MainFormIE
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
            this.panelButtons = new System.Windows.Forms.Panel();
            this.spaceButtonSingle = new System.Windows.Forms.Button();
            this.spaceButton = new System.Windows.Forms.Button();
            this.buttonSave = new System.Windows.Forms.Button();
            this.buttonLoad = new System.Windows.Forms.Button();
            this.panelEditor = new System.Windows.Forms.Panel();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.tinyMceEditor = new CTEK_Rich_Text_Editor.TinyMCE();
            this.aboutBtn = new System.Windows.Forms.Button();
            this.panelButtons.SuspendLayout();
            this.panelEditor.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelButtons
            // 
            this.panelButtons.Controls.Add(this.aboutBtn);
            this.panelButtons.Controls.Add(this.spaceButtonSingle);
            this.panelButtons.Controls.Add(this.spaceButton);
            this.panelButtons.Controls.Add(this.buttonSave);
            this.panelButtons.Controls.Add(this.buttonLoad);
            this.panelButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelButtons.Location = new System.Drawing.Point(0, 530);
            this.panelButtons.Name = "panelButtons";
            this.panelButtons.Size = new System.Drawing.Size(885, 35);
            this.panelButtons.TabIndex = 0;
            // 
            // spaceButtonSingle
            // 
            this.spaceButtonSingle.Location = new System.Drawing.Point(280, 6);
            this.spaceButtonSingle.Name = "spaceButtonSingle";
            this.spaceButtonSingle.Size = new System.Drawing.Size(107, 23);
            this.spaceButtonSingle.TabIndex = 3;
            this.spaceButtonSingle.Text = "Single Space List";
            this.spaceButtonSingle.UseVisualStyleBackColor = true;
            this.spaceButtonSingle.Click += new System.EventHandler(this.spaceButtonSingle_Click);
            // 
            // spaceButton
            // 
            this.spaceButton.Location = new System.Drawing.Point(169, 6);
            this.spaceButton.Name = "spaceButton";
            this.spaceButton.Size = new System.Drawing.Size(105, 23);
            this.spaceButton.TabIndex = 2;
            this.spaceButton.Text = "Double Space List";
            this.spaceButton.UseVisualStyleBackColor = true;
            this.spaceButton.Click += new System.EventHandler(this.spaceButton_Click);
            // 
            // buttonSave
            // 
            this.buttonSave.Location = new System.Drawing.Point(93, 6);
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Size = new System.Drawing.Size(70, 23);
            this.buttonSave.TabIndex = 1;
            this.buttonSave.Text = "Don\'t Click";
            this.buttonSave.UseVisualStyleBackColor = true;
            this.buttonSave.Click += new System.EventHandler(this.buttonSave_Click);
            // 
            // buttonLoad
            // 
            this.buttonLoad.Location = new System.Drawing.Point(12, 6);
            this.buttonLoad.Name = "buttonLoad";
            this.buttonLoad.Size = new System.Drawing.Size(75, 23);
            this.buttonLoad.TabIndex = 0;
            this.buttonLoad.Text = "Update";
            this.buttonLoad.UseVisualStyleBackColor = true;
            this.buttonLoad.Click += new System.EventHandler(this.buttonLoad_Click);
            // 
            // panelEditor
            // 
            this.panelEditor.Controls.Add(this.tinyMceEditor);
            this.panelEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelEditor.Location = new System.Drawing.Point(0, 0);
            this.panelEditor.Name = "panelEditor";
            this.panelEditor.Size = new System.Drawing.Size(885, 530);
            this.panelEditor.TabIndex = 1;
            // 
            // openFileDialog
            // 
            this.openFileDialog.Filter = "HTM files|*.htm|HTML files|*.html|All files|*.*";
            // 
            // saveFileDialog
            // 
            this.saveFileDialog.DefaultExt = "htm";
            this.saveFileDialog.Filter = "HTM files|*.htm|HTML files|*.html|All files|*.*";
            // 
            // tinyMceEditor
            // 
            this.tinyMceEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tinyMceEditor.HtmlContent = "";
            this.tinyMceEditor.Location = new System.Drawing.Point(0, 0);
            this.tinyMceEditor.Name = "tinyMceEditor";
            this.tinyMceEditor.Size = new System.Drawing.Size(885, 530);
            this.tinyMceEditor.TabIndex = 0;
            // 
            // aboutBtn
            // 
            this.aboutBtn.Location = new System.Drawing.Point(798, 5);
            this.aboutBtn.Name = "aboutBtn";
            this.aboutBtn.Size = new System.Drawing.Size(75, 23);
            this.aboutBtn.TabIndex = 4;
            this.aboutBtn.Text = "About";
            this.aboutBtn.UseVisualStyleBackColor = true;
            this.aboutBtn.Click += new System.EventHandler(this.aboutBtn_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(885, 565);
            this.Controls.Add(this.panelEditor);
            this.Controls.Add(this.panelButtons);
            this.MinimumSize = new System.Drawing.Size(500, 200);
            this.Name = "MainForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "Centek Revit Rich Text Editor";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.panelButtons.ResumeLayout(false);
            this.panelEditor.ResumeLayout(false);
            this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.Panel panelButtons;
    private System.Windows.Forms.Button buttonSave;
    private System.Windows.Forms.Button buttonLoad;
    private System.Windows.Forms.Panel panelEditor;
    private System.Windows.Forms.OpenFileDialog openFileDialog;
    private System.Windows.Forms.SaveFileDialog saveFileDialog;
    private TinyMCE tinyMceEditor;
    private System.Windows.Forms.Button spaceButton;
    private System.Windows.Forms.Button spaceButtonSingle;
    private System.Windows.Forms.Button aboutBtn;
  }
}

