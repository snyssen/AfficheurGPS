﻿/*
 * 
 * TO-DO : 
 * -> Si connection série OK, récupérer coordonnées GPS
 * -> Envoyer ces données au serveur (Requête SQL ?) => voir avec Hugues
 *		=> Intégrer modèle en couches à ce programme
 * -> Attendre réponse serveur et générer la page web à partir des données récup => voir avec Ju et Robin
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

namespace AfficheurGPS
{
	public partial class Afficheur : Form
	{
		SerialPort SIM808;
		bool ConnectedToSIM808;
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
				browser.Navigate("https://snyssen.be");
		}

		private void Afficheur_Load(object sender, EventArgs e)
		{
			//this.TopMost = true;
			this.SetTopLevel(true);
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

		private void SelectGPSPort()
		{
			string headline = "Selection du GPS";
			headline = headline.PadLeft(50, '=');
			headline = headline.PadRight(100, '=');
			Console.WriteLine(headline);
			SIM808.Close();
			// On se désabonne des événements le temps du test, pour pouvoir lire la réponse en pulling => bloquant
			SIM808.DataReceived -= SP_DataReceived;
			SIM808.ErrorReceived -= SP_ErrorReceived;
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
					if (response == "OK")
					{
						Console.WriteLine("Module SIM808 reconnu !");
						Console.WriteLine("Configuration...");
						Console.WriteLine("Baudrate = " + SIM808.BaudRate);
						SIM808.WriteLine("AT+IPR=" + SIM808.BaudRate);
						response = SIM808.ReadLine();
						Console.WriteLine(response);
						if (response == "OK")
						{
							Console.WriteLine("Sauvegarde de la config");
							SIM808.WriteLine("AT&W");
							Console.WriteLine(response);
							if (response == "OK")
							{
								Console.WriteLine("Activation du module GPS");
								SIM808.WriteLine("AT+CGPSPWR=1");
								response = SIM808.ReadLine();
								Console.WriteLine(response);
								if (response == "OK")
								{
									// Ajouter vérif que le GPS a un signal (cmd = "AT+CGPSSTATUS?", renvoie = "+CGPSSTATUS: Location Not Fix \n OK" si pas de position OU = "+CGPSSTATUS: Location 3D Fix \n OK" si pos fixée) ? Dans une méthode à part peut être ?

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
					// On se réabonne aux événements
					SIM808.DataReceived += SP_DataReceived;
					SIM808.ErrorReceived += SP_ErrorReceived;
					Console.WriteLine("Tout fonctionne correctement !");
					break;
				}
			}
			if (!ConnectedToSIM808)
			{
				Console.WriteLine("ERREUR : IMPOSSIBLE DE SE CONNECTER AU MODULE SIM808\nVerfiez que le module est bien connecté");
			}
			headline = "";
			headline = headline.PadRight(100, '=');
			Console.WriteLine(headline);
		}

		private void SP_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
		{
			Console.WriteLine("Une erreur est survenue sur le port série du GPS !\n" + e.ToString());
			Console.WriteLine("Reconnexion...");
			SelectGPSPort();
		}

		private void SP_DataReceived(object sender, SerialDataReceivedEventArgs e)
		{
			SerialPort sp = (SerialPort)sender;
			try
			{
				string Donnees = sp.ReadLine();
				// A tester ? Devrait permettre de récupérer TOUTES les données du buffer, contrairement à ReadLine qui ne lira que la première ligne
				// Logiquement pas utile dans notre cas vu qu'on lit les données en une seule ligne, qui sont les réponses à nos commandes
				//string Donnees = sp.ReadExisting();
				Console.WriteLine("Données GPS reçues : " + Donnees);
				// DEBUG
				// Je ne suis pas sûr de ce que contient l'EventArgs ici, pour le test je vais donc le print :-)
				Console.WriteLine(e.ToString());
			}
			catch (Exception ex)
			{
				Console.WriteLine("Erreur lors de la lecture des données GPS ! " + ex.ToString());
			}
		}
	}
}
