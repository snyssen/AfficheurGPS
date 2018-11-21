using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WinSCP;

namespace AfficheurGPS
{
	class WinSCP_Utilitaries
	{
		// WinSCP session
		private Session sshSession;
		private readonly SessionOptions sessionOptions;

		public WinSCP_Utilitaries(string server, string username, string password, string hostFingerPrint)
		{
			sessionOptions = new SessionOptions
			{
				Protocol = Protocol.Scp,
				HostName = server,
				UserName = username,
				Password = password,
				SshHostKeyFingerprint = hostFingerPrint
			};
			Console.WriteLine("Configuration of connection to server : {0}@{1} w/ pwd={2} and fingerprint={3}", sessionOptions.UserName, sessionOptions.HostName, sessionOptions.Password, sessionOptions.SshHostKeyFingerprint);
			sshSession = new Session();
			if (TestConnection())
				Console.WriteLine("SSH Connection established with server");
		}

		public bool TestConnection()
		{
			try
			{
				Console.WriteLine("Connecting to server using SSH...");
				sshSession.Open(sessionOptions);
				sshSession.Close();
				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine("Impossible to open SSH session. Error : " + ex.ToString());
				return false;
			}
		}

		public string DowloadPic(string remotePath, string localPath)
		{
			try
			{
				Console.WriteLine("Trying to download file from server with SCP...");
				sshSession.Open(sessionOptions);
				TransferOptions transferOptions = new TransferOptions { TransferMode = TransferMode.Binary };
				TransferOperationResult transferResult = sshSession.GetFiles(remotePath, localPath, false, transferOptions);
				// Will throw any error during transfer
				transferResult.Check();
				TransferEventArgsCollection transfer = transferResult.Transfers;
				Console.WriteLine("Downloaded successfully file : " + transfer[0].Destination);
				return transfer[0].Destination;
			}
			catch (Exception ex)
			{
				Console.WriteLine("An error occured while trying to download the file from the server. Error : " + ex.ToString());
				return null;
			}
		}
	}
}
