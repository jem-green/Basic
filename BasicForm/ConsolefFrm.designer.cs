namespace BasicForm
{
    partial class ConsoleForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConsoleForm));
            consoleTextBox = new System.Windows.Forms.TextBox();
            consoleMenuStrip = new System.Windows.Forms.MenuStrip();
            fileMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            openFileMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            recentFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            formatToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            fontToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            colorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            consoleMenuStrip.SuspendLayout();
            SuspendLayout();
            // 
            // consoleTextBox
            // 
            consoleTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            consoleTextBox.Enabled = false;
            consoleTextBox.Location = new System.Drawing.Point(0, 42);
            consoleTextBox.Margin = new System.Windows.Forms.Padding(4);
            consoleTextBox.Multiline = true;
            consoleTextBox.Name = "consoleTextBox";
            consoleTextBox.ReadOnly = true;
            consoleTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            consoleTextBox.Size = new System.Drawing.Size(866, 635);
            consoleTextBox.TabIndex = 0;
            consoleTextBox.Visible = false;
            consoleTextBox.KeyPress += ConsoleTextBox_KeyPress;
            // 
            // consoleMenuStrip
            // 
            consoleMenuStrip.ImageScalingSize = new System.Drawing.Size(32, 32);
            consoleMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { fileMenuItem, formatToolStripMenuItem });
            consoleMenuStrip.Location = new System.Drawing.Point(0, 0);
            consoleMenuStrip.Name = "consoleMenuStrip";
            consoleMenuStrip.Padding = new System.Windows.Forms.Padding(7, 3, 0, 3);
            consoleMenuStrip.Size = new System.Drawing.Size(866, 42);
            consoleMenuStrip.TabIndex = 1;
            consoleMenuStrip.Text = "menuStrip1";
            // 
            // fileMenuItem
            // 
            fileMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { openFileMenuItem, toolStripSeparator1, recentFileToolStripMenuItem, toolStripSeparator2, exitToolStripMenuItem });
            fileMenuItem.Name = "fileMenuItem";
            fileMenuItem.Size = new System.Drawing.Size(71, 36);
            fileMenuItem.Text = "File";
            // 
            // openFileMenuItem
            // 
            openFileMenuItem.Name = "openFileMenuItem";
            openFileMenuItem.Size = new System.Drawing.Size(263, 44);
            openFileMenuItem.Text = "&Open";
            openFileMenuItem.Click += FileOpenMenuItem_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new System.Drawing.Size(260, 6);
            // 
            // recentFileToolStripMenuItem
            // 
            recentFileToolStripMenuItem.Name = "recentFileToolStripMenuItem";
            recentFileToolStripMenuItem.Size = new System.Drawing.Size(263, 44);
            recentFileToolStripMenuItem.Text = "Recent File";
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new System.Drawing.Size(260, 6);
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new System.Drawing.Size(263, 44);
            exitToolStripMenuItem.Text = "&Exit";
            exitToolStripMenuItem.Click += FileExitMenuItem_Click;
            // 
            // formatToolStripMenuItem
            // 
            formatToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { fontToolStripMenuItem, colorToolStripMenuItem });
            formatToolStripMenuItem.Name = "formatToolStripMenuItem";
            formatToolStripMenuItem.Size = new System.Drawing.Size(109, 36);
            formatToolStripMenuItem.Text = "Format";
            // 
            // fontToolStripMenuItem
            // 
            fontToolStripMenuItem.Name = "fontToolStripMenuItem";
            fontToolStripMenuItem.Size = new System.Drawing.Size(204, 44);
            fontToolStripMenuItem.Text = "Font";
            fontToolStripMenuItem.Click += FormatFontMenuItem_Click;
            // 
            // colorToolStripMenuItem
            // 
            colorToolStripMenuItem.Name = "colorToolStripMenuItem";
            colorToolStripMenuItem.Size = new System.Drawing.Size(204, 44);
            colorToolStripMenuItem.Text = "Color";
            colorToolStripMenuItem.Click += FormatColorMenuItem_Click;
            // 
            // ConsoleForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(13F, 32F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(866, 677);
            Controls.Add(consoleTextBox);
            Controls.Add(consoleMenuStrip);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = consoleMenuStrip;
            Margin = new System.Windows.Forms.Padding(4);
            Name = "ConsoleForm";
            Text = "Basic";
            FormClosing += ConsoleForm_FormClosing;
            Load += ConsoleForm_Load;
            consoleMenuStrip.ResumeLayout(false);
            consoleMenuStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox consoleTextBox;
        private System.Windows.Forms.MenuStrip consoleMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem fileMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openFileMenuItem;
        private System.Windows.Forms.ToolStripMenuItem formatToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fontToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem colorToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem recentFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
    }
}

