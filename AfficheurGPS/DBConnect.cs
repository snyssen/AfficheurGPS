using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
//Add MySql Library
using MySql.Data.MySqlClient;

namespace AfficheurGPS
{
    class DBConnect
    {
        private MySqlConnection connection;

        //Constructor
        public DBConnect(string server, string database, string uid, string password)
        {
            Initialize(server, database, uid, password);
        }

        //Initialize values
        private void Initialize(string server, string database, string uid, string password)
        {
            string connectionString = "SERVER=" + server + ";" + "DATABASE=" + database + ";" + "UID=" + uid + ";" + "PASSWORD=" + password + ";";

            connection = new MySqlConnection(connectionString);
				Console.WriteLine("Configuration of connection to database : " + connectionString);
        }


        //open connection to database
        private bool OpenConnection()
        {
            try
            {
					Console.WriteLine("Connecting to database...");
					connection.Open();
                return true;
            }
            catch (MySqlException ex)
            {
                //When handling errors, you can your application's response based on the error number.
                //The two most common error numbers when connecting are as follows:
                //0: Cannot connect to server.
                //1045: Invalid user name and/or password.
                switch (ex.Number)
                {
                    case 0:
								Console.WriteLine("Cannot connect to server.  Contact administrator.");
                        break;

                    case 1045:
								Console.WriteLine("Invalid username/password, please try again.");
                        break;
						default:
							Console.WriteLine("Unknown error... Error code is {0}. Error message is: {1}", ex.Number, ex.Message);
							break;
                }
                return false;
            }
        }

        //Close connection
        private bool CloseConnection()
        {
            try
            {
                connection.Close();
					Console.WriteLine("Closing connection with database...");
                return true;
            }
            catch (MySqlException ex)
            {
					Console.WriteLine(ex.Message);
                return false;
            }
        }
        
        public List<string>[] GetMessages(double longitude, double latitude)
        {
            string query = "CALL demande_messages('" + longitude + "', '" + latitude + "')";
				Console.WriteLine("Sending query -> " + query);

				//Create a list to store the result
				List<string>[] list = new List<string>[4];
            list[0] = new List<string>();
            list[1] = new List<string>();
            list[2] = new List<string>();
				list[3] = new List<string>();

			//Open connection
			if (this.OpenConnection() == true)
            {
                //Create Command
                MySqlCommand cmd = new MySqlCommand(query, connection);
                //Create a data reader and Execute the command
                MySqlDataReader dataReader = cmd.ExecuteReader();
                
                //Read the data and store them in the list
                while (dataReader.Read())
                {
                    list[0].Add(dataReader["id_information"] + "");
                    list[1].Add(dataReader["i_message"] + "");
                    list[2].Add(dataReader["i_lienimage"] + "");
				    list[3].Add(dataReader["i_priorite"] + "");
                }

                //close Data Reader
                dataReader.Close();

                //close Connection
                this.CloseConnection();

					//return list to be displayed
					Console.WriteLine("Returning query response");
                return list;
            }
            else
            {
					Console.WriteLine("Returning empty response...");
                return list;
            }
        }
    }
}
