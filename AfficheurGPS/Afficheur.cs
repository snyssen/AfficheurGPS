﻿/*
 * 
 * TO-DO : 
 * -> Si connection série OK, récupérer coordonnées GPS
 * -> Envoyer ces données au serveur (Requête SQL ?) => voir avec Hugues
 *		=> Intégrer modèle en couches à ce programme
 * -> Attendre réponse serveur et générer la page web à partir des données récup => voir avec Ju et Robin
 * 
 * -> Ajouter sécurités autour des lectures en série (timeout notamment)
 * 
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;
using System.Threading;

namespace AfficheurGPS
{
	public partial class Afficheur : Form
	{
		SerialPort SIM808;
		bool ConnectedToSIM808;
		double CurrentLat, CurrentLong;
		Thread ThGetPos;
		public Afficheur()
		{
			InitializeComponent();
			SIM808 = new SerialPort
			{
				ReadTimeout = 1000, // Permet l'envoie d'une erreur timeout si la méthode ReadLine() ne reçoit aucune donnée après 1 secondes
				BaudRate = 9600,
				Parity = Parity.None,
				DataBits = 8,
				StopBits = StopBits.One
			};
			SelectGPSPort();
			if (!WBEmulator.IsBrowserEmulationSet())
				WBEmulator.SetBrowserEmulationVersion();
			if (!ConnectedToSIM808)
			{
				string curDir = Directory.GetCurrentDirectory();
				this.browser.Url = new Uri(String.Format("file:///{0}/NoGPS.html", curDir));
				//browser.Navigate()
			}
			else
			{
				string curDir = Directory.GetCurrentDirectory();
				this.browser.Url = new Uri(String.Format("file:///{0}/WaitingGPS.html", curDir));

				// On récupère la position de l'afficheur
				ThreadStart ThStGetPos = new ThreadStart(GetPosition);
				ThStGetPos += () => { // Ajout d'une fonctione de callback
											 /*
											  * 
											  * TODO :
											  * -> Ajouter l'envoi des coordonnées au serveur ici
											  * 
											  */
					Console.WriteLine("Test callback");
				};
				ThGetPos = new Thread(ThStGetPos) { IsBackground = true };
				ThGetPos.Start();
			}
		}

		private void Afficheur_Load(object sender, EventArgs e)
		{
			//this.TopMost = true;
			this.SetTopLevel(true);
			this.FormBorderStyle = FormBorderStyle.None;
			this.WindowState = FormWindowState.Maximized;
		}

		private void browser_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
			{
				if (MessageBox.Show("Voulez-vous vraiment fermer l'application ?", "Confirmation", MessageBoxButtons.YesNo) == DialogResult.Yes)
					this.Close();
			}
		}

		private void SelectGPSPort()
		{
			string headline = "Selection du GPS";
			headline = headline.PadLeft(50, '=');
			headline = headline.PadRight(100, '=');
			Console.WriteLine(headline);
			SIM808.Close();
			ConnectedToSIM808 = false;
			Console.WriteLine("Recherche du port utilisé par le GPS");
			string[] ports = SerialPort.GetPortNames(); // Récupère les ports utilisés
			foreach(string port in ports)
			{
				SIM808.PortName = port;
				SIM808.Open();
				if(SIM808.IsOpen)
				{
					Console.WriteLine("Port " + port + " ouvert, envoi de la commande AT");
					SIM808.WriteLine("AT");
					Console.WriteLine("Commande envoyé, attente de la réponse");
					string response = SIM808.ReadLine();
					Console.WriteLine(response);
					response = SIM808.ReadLine();
					Console.WriteLine(response);
					if (response.Trim() == "OK")
					{
						Console.WriteLine("Module SIM808 reconnu !");
						Console.WriteLine("Configuration...");
						Console.WriteLine("Baudrate = " + SIM808.BaudRate);
						SIM808.WriteLine("AT+IPR=" + SIM808.BaudRate);
						response = SIM808.ReadLine();
						Console.WriteLine(response);
						response = SIM808.ReadLine();
						Console.WriteLine(response);
						if (response.Trim() == "OK")
						{
							Console.WriteLine("Sauvegarde de la config");
							SIM808.WriteLine("AT&W");
							response = SIM808.ReadLine();
							Console.WriteLine(response);
							response = SIM808.ReadLine();
							Console.WriteLine(response);
							if (response.Trim() == "OK")
							{
								Console.WriteLine("Activation du module GPS");
								SIM808.WriteLine("AT+CGPSPWR=1");
								response = SIM808.ReadLine();
								Console.WriteLine(response);
								response = SIM808.ReadLine();
								Console.WriteLine(response);
								if (response.Trim() == "OK")
								{
									// ON EST CONNECTE, TOUT EST OK
									ConnectedToSIM808 = true;
								}
								else
								{
									Console.WriteLine("ERREUR : le GPS n'a pas su s'intialiser");
								}
							}
							else
							{
								Console.WriteLine("ERREUR : Impossible de sauvegarder la configuration");
							}
						}
						else
						{
							Console.WriteLine("ERREUR : Impossible de set le baudrate");
						}
					}
					else
					{
						Console.WriteLine("Ce module ne répond pas au AT, ce n'est pas un SIM808");
					}
				}
				// END if IsOpen
				if (ConnectedToSIM808) // Si on est bien connecté
				{
					Console.WriteLine("Tout fonctionne correctement !");
					break;
				}
			}
			if (!ConnectedToSIM808)
			{
				Console.WriteLine("ERREUR : IMPOSSIBLE DE SE CONNECTER AU MODULE SIM808\nVérifiez que le module est bien connecté");
			}
			headline = "";
			headline = headline.PadRight(100, '=');
			Console.WriteLine(headline);
		}

		private void GetPosition()
		{
			if (ConnectedToSIM808)
			{
				string headline = "Tentative de récupération de la position GPS";
				headline = headline.PadLeft(50, '=');
				headline = headline.PadRight(100, '=');
				Console.WriteLine(headline);
				string response;
				do
				{
					Console.WriteLine("Envoi de AT+CGPSSTATUS?");
					SIM808.WriteLine("AT+CGPSSTATUS?");
					response = SIM808.ReadLine();
					Console.WriteLine(response);
					response = SIM808.ReadLine();
					Console.WriteLine(response);
					// Attente pour ne pas submerger le module
					Thread.Sleep(3000);
				}
				while (response.Trim() != "+CGPSSTATUS: Location 3D Fix");
				Console.WriteLine("Position fixée !");
				headline = "";
				headline = headline.PadRight(100, '=');
				Console.WriteLine(headline);
				SIM808.DiscardInBuffer(); // On vide le buffer par sécurité
				Console.WriteLine("Position GPS :");
				Console.WriteLine("AT+CGPSINF=0");
				SIM808.WriteLine("AT+CGPSINF=0");
				Thread.Sleep(200); // Attente pour être sûr que la commande à le temps de s'effectuer au niveau du module SIM868
				response = SIM808.ReadExisting();
				Console.WriteLine(response);
				Console.WriteLine("Tentative de parse de la réponse...");
				string[] TmpTab = response.Split(':');
				response = TmpTab[1];
				Console.WriteLine(response);
				TmpTab = response.Split(',');
				if (double.TryParse(TmpTab[1], out CurrentLat) && double.TryParse(TmpTab[2], out CurrentLong))
				{
					Console.WriteLine("Parsing OK, les coordonnées sont :");
					Console.WriteLine(CurrentLat);
					Console.WriteLine(CurrentLong);
				}
				else
					Console.WriteLine("ERREUR : le parsing n'a pas fonctionné !");
			}
			else
				Console.WriteLine("ERREUR : pas de connexion au module GPS, impossible de récupérer la position !");
		}

		private void Afficheur_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (ThGetPos != null)
				if (ThGetPos.IsAlive) // Fermeture du thread de recherche de la position si il est actif
					ThGetPos.Abort();
			if (SIM808.IsOpen)
			{
				SIM808.WriteLine("AT+CGPSPWR=0"); // Extinction du GPS
				Thread.Sleep(50);
				SIM808.WriteLine("AT+CPOWD=1"); // Extinction du module
				Thread.Sleep(50);
				SIM808.Close(); // Fermeture de la connexion série
			}
		}
	}
}
