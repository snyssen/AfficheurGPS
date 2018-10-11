using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AfficheurGPS
{
	public partial class Afficheur : Form
	{
		public Afficheur()
		{
			InitializeComponent();
			if (!WBEmulator.IsBrowserEmulationSet())
			{
				WBEmulator.SetBrowserEmulationVersion();
			}
			browser.Navigate("http://localhost/drupal");
		}

		private void Afficheur_Load(object sender, EventArgs e)
		{
			this.TopMost = true;
			this.FormBorderStyle = FormBorderStyle.None;
			this.WindowState = FormWindowState.Maximized;
		}

		private void Afficheur_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
			{
				if (MessageBox.Show("Voulez-vous vraiment fermer l'application ?", "Confirmation", MessageBoxButtons.YesNo) == DialogResult.Yes)
					this.Close();
			}
		}
	}
}
