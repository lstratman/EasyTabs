using System.Drawing;

namespace Stratman.Windows.Forms.TitleBarTabs
{
	partial class TornTabForm
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
			this._tabThumbnail = new System.Windows.Forms.PictureBox();
			((System.ComponentModel.ISupportInitialize)(this._tabThumbnail)).BeginInit();
			this.SuspendLayout();
			// 
			// _tabThumbnail
			// 
			this._tabThumbnail.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._tabThumbnail.BackColor = System.Drawing.Color.Transparent;
			this._tabThumbnail.Location = new System.Drawing.Point(-1, -1);
			this._tabThumbnail.Margin = new System.Windows.Forms.Padding(0);
			this._tabThumbnail.Name = "_tabThumbnail";
			this._tabThumbnail.Size = new System.Drawing.Size(286, 264);
			this._tabThumbnail.TabIndex = 0;
			this._tabThumbnail.TabStop = false;
			// 
			// TornTabForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 262);
			this.Controls.Add(this._tabThumbnail);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.Name = "TornTabForm";
			this.Opacity = 0.5D;
			this.Text = "TornTabForm";
			this.TransparencyKey = System.Drawing.Color.Transparent;
			((System.ComponentModel.ISupportInitialize)(this._tabThumbnail)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.PictureBox _tabThumbnail;
	}
}