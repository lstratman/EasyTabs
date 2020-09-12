namespace TestApp
{
    partial class TabWindow
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TabWindow));
            this.toolbarBackground = new System.Windows.Forms.Panel();
            this.forwardButton = new System.Windows.Forms.PictureBox();
            this.backButton = new System.Windows.Forms.PictureBox();
            this.urlBoxBackground = new System.Windows.Forms.Panel();
            this.urlBoxRight = new System.Windows.Forms.PictureBox();
            this.urlBoxLeft = new System.Windows.Forms.PictureBox();
            this.urlTextBox = new System.Windows.Forms.TextBox();
            this.toolbarBackground.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.forwardButton)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.backButton)).BeginInit();
            this.urlBoxBackground.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.urlBoxRight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.urlBoxLeft)).BeginInit();
            this.SuspendLayout();
            // 
            // toolbarBackground
            // 
            this.toolbarBackground.BackgroundImage = global::TestApp.Resources.ToolbarBackground;
            this.toolbarBackground.Controls.Add(this.forwardButton);
            this.toolbarBackground.Controls.Add(this.backButton);
            this.toolbarBackground.Dock = System.Windows.Forms.DockStyle.Top;
            this.toolbarBackground.Location = new System.Drawing.Point(0, 0);
            this.toolbarBackground.Name = "toolbarBackground";
            this.toolbarBackground.Size = new System.Drawing.Size(326, 38);
            this.toolbarBackground.TabIndex = 2;
            // 
            // forwardButton
            // 
            this.forwardButton.BackColor = System.Drawing.Color.Transparent;
            this.forwardButton.Image = global::TestApp.Resources.ForwardActive;
            this.forwardButton.Location = new System.Drawing.Point(38, 5);
            this.forwardButton.Margin = new System.Windows.Forms.Padding(4, 4, 3, 3);
            this.forwardButton.Name = "forwardButton";
            this.forwardButton.Size = new System.Drawing.Size(28, 28);
            this.forwardButton.TabIndex = 3;
            this.forwardButton.TabStop = false;
            this.forwardButton.Click += new System.EventHandler(this.forwardButton_Click);
            this.forwardButton.MouseEnter += new System.EventHandler(this.forwardButton_MouseEnter);
            this.forwardButton.MouseLeave += new System.EventHandler(this.forwardButton_MouseLeave);
            // 
            // backButton
            // 
            this.backButton.BackColor = System.Drawing.Color.Transparent;
            this.backButton.Image = global::TestApp.Resources.BackActive;
            this.backButton.Location = new System.Drawing.Point(6, 5);
            this.backButton.Margin = new System.Windows.Forms.Padding(4, 4, 3, 3);
            this.backButton.Name = "backButton";
            this.backButton.Size = new System.Drawing.Size(28, 28);
            this.backButton.TabIndex = 2;
            this.backButton.TabStop = false;
            this.backButton.Click += new System.EventHandler(this.backButton_Click);
            this.backButton.MouseEnter += new System.EventHandler(this.backButton_MouseEnter);
            this.backButton.MouseLeave += new System.EventHandler(this.backButton_MouseLeave);
            // 
            // urlBoxBackground
            // 
            this.urlBoxBackground.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.urlBoxBackground.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(241)))), ((int)(((byte)(243)))), ((int)(((byte)(244)))));
            this.urlBoxBackground.Controls.Add(this.urlBoxRight);
            this.urlBoxBackground.Controls.Add(this.urlBoxLeft);
            this.urlBoxBackground.Controls.Add(this.urlTextBox);
            this.urlBoxBackground.ForeColor = System.Drawing.Color.Silver;
            this.urlBoxBackground.Location = new System.Drawing.Point(71, 5);
            this.urlBoxBackground.Name = "urlBoxBackground";
            this.urlBoxBackground.Size = new System.Drawing.Size(249, 28);
            this.urlBoxBackground.TabIndex = 2;
            // 
            // urlBoxRight
            // 
            this.urlBoxRight.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.urlBoxRight.Image = global::TestApp.Resources.UrlBoxRight;
            this.urlBoxRight.Location = new System.Drawing.Point(237, 0);
            this.urlBoxRight.Name = "urlBoxRight";
            this.urlBoxRight.Size = new System.Drawing.Size(12, 28);
            this.urlBoxRight.TabIndex = 4;
            this.urlBoxRight.TabStop = false;
            // 
            // urlBoxLeft
            // 
            this.urlBoxLeft.Image = global::TestApp.Resources.UrlBoxLeft;
            this.urlBoxLeft.Location = new System.Drawing.Point(0, 0);
            this.urlBoxLeft.Name = "urlBoxLeft";
            this.urlBoxLeft.Size = new System.Drawing.Size(12, 28);
            this.urlBoxLeft.TabIndex = 3;
            this.urlBoxLeft.TabStop = false;
            // 
            // urlTextBox
            // 
            this.urlTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.urlTextBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(241)))), ((int)(((byte)(243)))), ((int)(((byte)(244)))));
            this.urlTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.urlTextBox.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.urlTextBox.Location = new System.Drawing.Point(19, 5);
            this.urlTextBox.Margin = new System.Windows.Forms.Padding(9);
            this.urlTextBox.Name = "urlTextBox";
            this.urlTextBox.Size = new System.Drawing.Size(213, 18);
            this.urlTextBox.TabIndex = 1;
            this.urlTextBox.Text = "about:blank";
            this.urlTextBox.WordWrap = false;
            this.urlTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.urlTextBox_KeyDown);
            // 
            // TabWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(326, 289);
            this.Controls.Add(this.urlBoxBackground);
            this.Controls.Add(this.toolbarBackground);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "TabWindow";
            this.Text = "TabWindow";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.TabWindow_FormClosing);
            this.toolbarBackground.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.forwardButton)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.backButton)).EndInit();
            this.urlBoxBackground.ResumeLayout(false);
            this.urlBoxBackground.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.urlBoxRight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.urlBoxLeft)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel toolbarBackground;
        private System.Windows.Forms.PictureBox backButton;
        private System.Windows.Forms.PictureBox forwardButton;
        private System.Windows.Forms.Panel urlBoxBackground;
        private System.Windows.Forms.TextBox urlTextBox;
        private System.Windows.Forms.PictureBox urlBoxLeft;
        private System.Windows.Forms.PictureBox urlBoxRight;
    }
}