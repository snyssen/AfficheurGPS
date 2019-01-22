namespace AfficheurGPS
{
	partial class Afficheur
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
            this.components = new System.ComponentModel.Container();
            this.browser = new System.Windows.Forms.WebBrowser();
            this.MyTimer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // browser
            // 
            this.browser.AllowWebBrowserDrop = false;
            this.browser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.browser.IsWebBrowserContextMenuEnabled = false;
            this.browser.Location = new System.Drawing.Point(0, 0);
            this.browser.MinimumSize = new System.Drawing.Size(20, 20);
            this.browser.Name = "browser";
            this.browser.ScriptErrorsSuppressed = true;
            this.browser.Size = new System.Drawing.Size(784, 561);
            this.browser.TabIndex = 0;
            this.browser.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.browser_PreviewKeyDown);
            // 
            // MyTimer
            // 
            this.MyTimer.Interval = 30000;
            this.MyTimer.Tick += new System.EventHandler(this.MyTimer_Tick);
            // 
            // Afficheur
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 561);
            this.ControlBox = false;
            this.Controls.Add(this.browser);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "Afficheur";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Afficheur";
            this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Afficheur_FormClosing);
            this.Load += new System.EventHandler(this.Afficheur_Load);
            this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.WebBrowser browser;
        private System.Windows.Forms.Timer MyTimer;
    }
}