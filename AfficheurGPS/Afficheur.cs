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
        string curDir = Directory.GetCurrentDirectory();
        private string PathToPics; // Chemin d'accès où l'afficheur pourra stocker les images qui téléchargera du serveur
        bool ReadyToShow;
        int TickCount; // Index de la page affichée en ce moment && Nombre de Ticks actuel du timer (permet de compter une heure pour refresh les infos)

        List<string>[] Messages;

        // Configuration de connexion
        private string server = "192.168.1.12";
        //private string server = "snyssen.be";
        private string database = "bd_projet_reseau";
		private string dbUser = "iset";
		private string dbPassword = "isetmdp2";
		private string sshUser = "iset";
		private string sshPassword = "@dminRoot12";
		private string sshHostFingerPrint = "ssh-ed25519 256 32:26:b0:fa:5d:bf:8f:60:66:0b:36:7a:c4:7a:b7:e3";

		public Afficheur()
		{
            ReadyToShow = false;
			InitializeComponent();
            PathToPics = curDir;

			SIM808 = new SerialPort
			{
				ReadTimeout = 1000, // Permet l'envoi d'une erreur timeout si la méthode ReadLine() ne reçoit aucune donnée après 1 secondes
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
				this.browser.Url = new Uri(String.Format("file:///{0}/NoGPS.html", curDir));
			}
			else
			{
                // On récupère la position de l'afficheur
                StartGetPosition();
                // TEST
                //CurrentLong = 5.586936;
                //CurrentLat = 50.623461;
                //CallbackGetPosition();
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
			string response;
			Console.WriteLine(CreateHeadline("GPS"));
			ConnectedToSIM808 = false;
			Console.WriteLine("Searching for COM port used by GPS module");
			string[] ports = SerialPort.GetPortNames(); // Récupère les ports utilisés
			foreach(string port in ports)
			{
				SIM808.Close();
				SIM808.PortName = port;
				SIM808.Open();
				if(SIM808.IsOpen)
				{
					// On vide les buffers par sécurité
					SIM808.DiscardInBuffer();
					SIM808.DiscardOutBuffer();
					try
					{
						Console.WriteLine("Port " + port + " open, sending AT command");
						SIM808.WriteLine("AT");
						response = SIM808.ReadLine();
						Console.WriteLine(response);
						response = SIM808.ReadLine();
						Console.WriteLine(response);
						if (response.Trim() == "OK")
						{
							Console.WriteLine("SIM808 module recognized !");
							Console.WriteLine("Configuring...");
							Console.WriteLine("Baudrate = " + SIM808.BaudRate);
							SIM808.WriteLine("AT+IPR=" + SIM808.BaudRate);
							response = SIM808.ReadLine();
							Console.WriteLine(response);
							response = SIM808.ReadLine();
							Console.WriteLine(response);
							if (response.Trim() == "OK")
							{
								Console.WriteLine("Saving configuration...");
								SIM808.WriteLine("AT&W");
								response = SIM808.ReadLine();
								Console.WriteLine(response);
								response = SIM808.ReadLine();
								Console.WriteLine(response);
								if (response.Trim() == "OK")
								{
									Console.WriteLine("Initializing GPS module...");
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
										Console.WriteLine("ERROR : Couldn't start GPS.");
									}
								}
								else
								{
									Console.WriteLine("ERROR : Couldn't save configuration.");
								}
							}
							else
							{
								Console.WriteLine("ERROR : Couldn't set baudrate.");
							}
						}
						else
						{
							Console.WriteLine("Module isn't responding to AT command, skipping...");
						}
					} // END try

					catch (TimeoutException) { Console.WriteLine("ERROR : module took too much time to respond (timeout = 1 seconde)"); }
					catch (Exception ex) { Console.WriteLine("ERROR on the serial port !\n" + ex.ToString()); }
					
				}
				// END if IsOpen
				if (ConnectedToSIM808) // Si on est bien connecté
				{
					Console.WriteLine("Module working as expected !");
					break;
				}
			}
			// END foreach
			if (!ConnectedToSIM808)
			{
				Console.WriteLine("ERROR : COULDN'T CONNECT TO SIM808 MODULE\nPlease check it is connected.");
			}
		}

        private void StartGetPosition()
        {
            // reset affichage des pages
            ReadyToShow = false;
            MyTimer.Stop();
            TickCount = 0;

            this.browser.Url = new Uri(String.Format("file:///{0}/WaitingGPS.html", curDir));
            ThreadStart ThStGetPos = new ThreadStart(GetPosition);
            ThStGetPos += CallbackGetPosition;
            ThGetPos = new Thread(ThStGetPos) { IsBackground = true };
            ThGetPos.Start();
        }

		private void GetPosition()
		{
			if (ConnectedToSIM808)
			{
				Console.WriteLine(CreateHeadline("Searching for GPS signal"));
				string response;
                bool GotPos = false;
				try
				{
					do
					{
						Console.WriteLine("Sending AT+CGPSSTATUS?");
						SIM808.WriteLine("AT+CGPSSTATUS?");
                        Thread.Sleep(100);
                        while (SIM808.BytesToRead > 0 )
                        {
                            response = SIM808.ReadLine();
                            Console.WriteLine(response);
                            if (response.Trim() == "+CGPSSTATUS: Location 3D Fix")
                                GotPos = true;
                        }
						// Attente pour ne pas submerger le module
						Thread.Sleep(3000);
					}
					while (!GotPos);
					Console.WriteLine("Gotcha !");
					Console.WriteLine(CreateHeadline(""));
					SIM808.DiscardInBuffer(); // On vide le buffer par sécurité
					Console.WriteLine("Getting coordinates :");
					Console.WriteLine("AT+CGPSINF=0");
					SIM808.WriteLine("AT+CGPSINF=0");
					Thread.Sleep(200); // Attente pour être sûr que la commande à le temps de s'effectuer au niveau du module SIM868
					response = SIM808.ReadExisting();
					Console.WriteLine(response);
					Console.WriteLine("Parsing answer...");
					string[] TmpTab = response.Split(':'); // On se débarasse de l'entête
					response = TmpTab[1];
					Console.WriteLine(response);
					TmpTab = response.Split(','); // On sépare tous les arguments. On ne s'intéresse qu'aux arguments 2 et 3 (d'ID 1 et 2 dans le tableau), qui sont respectivement la latitude et la longitude, sous le format DDMM.MMMMMM

					// On sépare au point décimal
					string[] strCurrentLat = TmpTab[1].Split('.');
					string[] strCurrentLong = TmpTab[2].Split('.');
					strCurrentLat[1] = "0." + strCurrentLat[1];
					strCurrentLong[1] = "0." + strCurrentLong[1];

					// Et on parse ici individuellement degrés, minutes et secondes (il faut multiplier ces dernières par après)
					if (int.TryParse(strCurrentLat[0].Substring(strCurrentLat[0].Length - 2), out int LatMinut)
						&& int.TryParse(strCurrentLat[0].Remove(strCurrentLat[0].Length - 2), out int LatDegr)
						&& double.TryParse(strCurrentLat[1], out double LatSecond)
						
						&& int.TryParse(strCurrentLong[0].Substring(strCurrentLong[0].Length - 2), out int LongMinut)
						&& int.TryParse(strCurrentLong[0].Remove(strCurrentLong[0].Length - 2), out int LongDegr)
						&& double.TryParse(strCurrentLong[1], out double LongSecond))
					{
						// Pour avoir les "vraies" secondes, il faut multiplier le résultat précédent (fraction de minute) par 60
						LatSecond = LatSecond * 60;
						LongSecond = LongSecond * 60;
						Console.WriteLine("Parsing OK, coordinates are :");
						Console.WriteLine(LatDegr + "° " + LatMinut + "\' " + LatSecond + "\"");
						Console.WriteLine(LongDegr + "° " + LongMinut + "\' " + LongSecond + "\"");
						Console.WriteLine("\nConverting to decimal degrees :");
						// Et enfin on convertit ces données en degrés décimaux
						CurrentLat = ConvertDegMinSecToDecDeg(LatDegr, LatMinut, LatSecond);
						CurrentLong = ConvertDegMinSecToDecDeg(LongDegr, LongMinut, LongSecond);
						Console.WriteLine(CurrentLat);
						Console.WriteLine(CurrentLong);
					}
					else
						Console.WriteLine("ERROR : parsing didn't work !");
				} // END try

				catch (TimeoutException) { Console.WriteLine("ERREUR : timeout dépassé lors de la lecture sur le port série ! (timeout = 1 seconde)"); ConnectedToSIM808 = false; }
				catch (Exception ex) { Console.WriteLine("ERREUR sur le port série !\n" + ex.ToString()); ConnectedToSIM808 = false; }
				
			}
			else
				Console.WriteLine("ERROR : No connection to GPS module, impossible to get position !");
		}

        private void CallbackGetPosition()
        {
            if (ConnectedToSIM808)
            {
                this.browser.Url = new Uri(String.Format("file:///{0}/WaitingServer.html", curDir));

                Console.WriteLine(CreateHeadline("Database"));
                DBConnect db = new DBConnect(server, database, dbUser, dbPassword);

                // Chaque message contient 4 champs :
                // 0) ID
                // 1) priorite
                // 2) message
                // 3) lien de l'image 
                // On extrait donc un tableau de 4 dimensions, chaque dimension contenant une liste
                // Du champs en question.
                List<string>[] messages = db.GetMessages(CurrentLong, CurrentLat);
                if (messages != null && messages[0].Count > 0)
                {
                    Console.WriteLine(CreateHeadline("SSH & SCP"));
                    bool error = false;
                    WinSCP_Utilitaries scp = new WinSCP_Utilitaries(server, sshUser, sshPassword, sshHostFingerPrint);

                    // On vide le dossier d'images avant de télécharger les nouvelles
                    //DirectoryInfo di = new DirectoryInfo(curDir + "\\Images");
                    //foreach (FileInfo file in di.GetFiles()) { file.Delete(); }

                    for (int i = 0; i < messages[3].Count; i++)
                    {
                        Console.WriteLine("Remote path to file : " + messages[3][i]);
                        // Si le message contient un chemin d'accès (supposé valide)...
                        if (messages[3][i] != null && messages[3][i].Trim() != "")
                        {
                            // On télécharge le fichier (supposé une photo) et on sauvegarde son chemin d'accès local
                            messages[3][i] = scp.DowloadPic(messages[3][i], PathToPics);
                            if (messages[3][i] != null)
                            {
                                Console.WriteLine("File saved in " + messages[3][i]);
                            }
                            else
                                error = true;
                        }
                    }
                    if (!error)
                    {
                        this.browser.Url = new Uri(String.Format("file:///{0}/WaitingGen.html", curDir));

                        // On vide le dossier de pages générées (sauf css)
                        //di = new DirectoryInfo(curDir + "\\GeneratedPages");
                        //foreach (FileInfo file in di.GetFiles())
                        //{
                        //    if (file.Extension != ".css")
                        //        file.Delete();
                        //}

                        // Assignation des messages en global
                        Messages = messages;

                        // restart affichage des pages
                        ReadyToShow = true;
                        MyTimer.Start(); // le timer change la page affichée à chaque tick (avec un tick toutes les 30 sec)

                    }
                }
                else
                {
                    this.browser.Url = new Uri(String.Format("file:///{0}/NoMessage.html", curDir));
                }
            }
            else
            {
                MessageBox.Show("La connexion avec le port série a été perdue ! Veuillez rebrancher le module et relancer l'application");
                this.Invoke((MethodInvoker)(() => { this.Close(); }));
            }
        }

        static public void GeneratePageGlob(List<string>[] Liste)
        {
            /*
             * Liste[0] = new List<string>() : liste des id
             * Liste[1] = new List<string>() : liste des priorités
             * Liste[2] = new List<string>() : liste des messages
             * Liste[3] = new List<string>() : liste des chemins des images
             */

            //Liste des index des différents types de messages
            List<int> IndexUrg = new List<int>();
            List<int> IndexGlo = new List<int>();

            string Message = "<!DOCTYPE html><html><head><meta charset=\"utf - 8\"/><title>Affichage</title><link rel=\"stylesheet\" href=\"Style.css\"><h2>Informations du jour <img id=\"Logo\" src=\"logo.png\" alt=\"Logo de l'école\">";
            StreamWriter sw = new StreamWriter("Index.html", false);
            sw.Write(Message);

            //Va rechercher la priorité pour voir le type de message
            foreach (var priority in Liste[1])
            {
                int i = 0;

                if (priority == "2") //Message global
                {
                    IndexGlo.Add(i);
                }
                else if (priority == "3")//Message du jour
                {
                    IndexUrg.Add(i);
                }
                i++;
            }

            //Global
            foreach (int index in IndexGlo)
            {
                Message = "";
                if (Liste[3].ElementAt(index) != null)
                {
                    Message = "<div class=\"Global\"><p>" + Liste[2].ElementAt(index) + "<p><br><img src=\"" + Liste[3].ElementAt(index) + "\"></div>";
                }
                else
                {
                    Message = "<div class=\"Global\"><p>" + Liste[2].ElementAt(index) + "<p><br></div>";
                }
                sw.Write(Message);
            }

            //Urgent (bandeau)
            string MessageU = "<textarea id=\"basetext\" style=\"display: none\">";
            foreach (int index in IndexUrg)
            {
                MessageU = MessageU + "      " + Liste[2].ElementAt(index);
            }
            MessageU = MessageU + "</textarea>";
            string MessageAf = "<script lang=\"javascript\"> var position = 0; var msg = document.getElementById(\"basetext\").value;" +
                "var msg = \"     \" + msg; var longue = msg.length; var fois = (100 / msg.length) + 1; function textdefil() { " +
                "document.form1.deftext.value = msg.substring(position, position + 2000); position++; if (position == longue) position = 0; " +
                "setTimeout(textdefil(), 115); } window.onload = textdefil; </script><form name=\"form1\"><div align=\"center\">" +
                "<input type=\"text\" name=\"deftext\" size=226  id=\"MsgUrg\"/></div></form>";
            sw.Write(MessageU);
            sw.Write(MessageAf);
            sw.Close();
        }

        static public void GeneratePageJour(List<string>[] Liste)
        {
            /*
             * Liste[0] = new List<string>() : liste des id
             * Liste[1] = new List<string>() : liste des priorités
             * Liste[2] = new List<string>() : liste des messages
             * Liste[3] = new List<string>() : liste des chemins des images
             */

            //Liste des index des différents types de messages
            List<int> IndexUrg = new List<int>();
            List<int> IndexDay = new List<int>();

            string Message = "<!DOCTYPE html><html><head><meta charset=\"utf - 8\"/><title>Affichage</title><link rel=\"stylesheet\" href=\"Style.css\">";
            StreamWriter sw = new StreamWriter("Index.html", false);
            sw.Write(Message);

            //Va rechercher la priorité pour voir le type de message
            foreach (var priority in Liste[1])
            {
                int i = 0;

                if (priority == "3") //Message urgent
                {
                    IndexUrg.Add(i);
                }
                else if (priority == "1") //Message du jour
                {
                    IndexDay.Add(i);
                }
                i++;
            }

            //Du jour
            foreach (int index in IndexDay)
            {
                Message = "";
                if (Liste[3].ElementAt(index) != null) //Si il y a un chemin d'image
                {
                    Message = "<div class=\"Jour\"><p>" + Liste[2].ElementAt(index) + "<p><br><img src=\"" + Liste[3].ElementAt(index) + "\"></div>";
                }
                else //Si pas d'image
                {
                    Message = "<div class=\"Jour\"><p>" + Liste[2].ElementAt(index) + "<p><br></div>";
                }
                sw.Write(Message);
            }

            //Urgent (bandeau)
            string MessageU = "<textarea id=\"basetext\" style=\"display: none\">";
            foreach (int index in IndexUrg)
            {
                MessageU = MessageU + "      " + Liste[2].ElementAt(index);
            }
            MessageU = MessageU + "</textarea>";
            string MessageAf = "<script lang=\"javascript\"> var position = 0; var msg = document.getElementById(\"basetext\").value;" +
                "var msg = \"     \" + msg; var longue = msg.length; var fois = (100 / msg.length) + 1; function textdefil() { " +
                "document.form1.deftext.value = msg.substring(position, position + 2000); position++; if (position == longue) position = 0; " +
                "setTimeout(textdefil(), 115); } window.onload = textdefil; </script><form name=\"form1\"><div align=\"center\">" +
                "<input type=\"text\" name=\"deftext\" size=226  id=\"MsgUrg\"/></div></form>";
            sw.Write(MessageU);
            sw.Write(MessageAf);
            sw.Close();
        }

        private void Afficheur_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (ThGetPos != null)
				if (ThGetPos.IsAlive) // Fermeture du thread de recherche de la position si il est actif
					ThGetPos.Abort();
			if (SIM808.IsOpen)
			{
				// J'ai retiré l'extinction du module vu la lenteur du redémarrage de celui-ci....
				/*
				SIM808.WriteLine("AT+CGPSPWR=0"); // Extinction du GPS
				Thread.Sleep(50);
				SIM808.WriteLine("AT+CPOWD=1"); // Extinction du module
				Thread.Sleep(50);*/
				SIM808.Close(); // Fermeture de la connexion série
			}
		}

		private double ConvertDegMinSecToDecDeg(int Deg, int Min, double Sec)
		{
			return (double)((double)Deg + ((double)Min / 60) + (Sec / 3600));
		}

        // Le timer tick toutes les 30 secondes
        private void MyTimer_Tick(object sender, EventArgs e)
        {
            TickCount++;
            if (TickCount >= 120) // Après une heure (120x 30 sec) on refresh les données
            {
                StartGetPosition();
                return;
            }
            if (ReadyToShow) // prêt à afficher des pages
            {
                if (TickCount % 2 == 0)
                    GeneratePageGlob(Messages);
                else
                    GeneratePageJour(Messages);
                this.browser.Url = new Uri(String.Format("file:///{0}/Index.html", curDir));
            }
        }

        private string CreateHeadline(string headline)
		{
			headline = headline.PadLeft(50, '=');
			headline = headline.PadRight(100, '=');
			return headline;
		}
	}
}
