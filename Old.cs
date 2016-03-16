using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SpeedwayClientWpf
{
    class Old
    {
        static void Func(string[] args)
        {   // currently I get setting from an xml config file. I want to get the setting from a form. I want the settings in the form saved in a config file so that the application remembers that last settings
            string IPReader = ConfigurationManager.AppSettings.Get("IPReader");  // I will need this setting in the form. As noted below, I would like to be able to get the stream of up to 4 readers
            string IPComputer = ConfigurationManager.AppSettings.Get("IPComputer"); // I will need this setting in the form. This is the IP address of the computer running the application. The application broadcasts on this IP Address
            string SavePath = ConfigurationManager.AppSettings.Get("SavePath");  // I will need this setting in the form, with a browse button
            string TagFilter = ConfigurationManager.AppSettings.Get("TagFilter"); // I will need this setting in the form
            Dictionary<int, int> tags = new Dictionary<int, int>();  // this dictionary holds tag reads. It is used to save tag reads, so that the tag will not report if read again before x seconds have elapsed. The form will need a field to set this seconds value.
            // server to broadcast tags
            const int Port = 23;
            TcpListener listener = null;
            //  IPAddress ipAddress = IPAddress.Parse("169.254.244.241");
            IPAddress ipAddress = IPAddress.Parse(IPComputer);
            try
            {            // Start listening for connections on our IP address + Our Port number 
                listener = new TcpListener(ipAddress, Port);
                listener.Start();
                Console.WriteLine("Waiting for a connection...{0}", ipAddress);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.ToString());
            }
            // This code needs changed. Presently the code does blocking. It stops on this command until a connection is received. I need this changed in several ways.
            // One, I want the application to start without waiting for a listener. 
            // Two, I want to be able to accomodate up to 4 listeners.
            // I think that I need this to run in a background thread

            TcpClient ourTCP_Client = listener.AcceptTcpClient();
            //  Read the data stream from the client.
            NetworkStream ourStream = ourTCP_Client.GetStream();
            StreamWriter streamWriter = new StreamWriter(ourStream);

            //  The IP address or hostname of your reader
            //  const string READER_HOSTNAME = "169.254.1.1";
            string READER_HOSTNAME = IPReader;
            //  The TCP port specified in Speedway Connect
            const int READER_PORT = 14150;
            char[] delimiterChars = { ',' };
            try
            {

                // This code also needs changed. Presently I can get the stream of one RFID reader. I would like to be able to get the stream of up to 4 readers, thus 4 reader IP addresses on the form. This may also need to be a different and/or background thread
                // Create a new TCPClient
                TcpClient client = new TcpClient();
                // Connect to the reader
                client.Connect(READER_HOSTNAME, READER_PORT);
                // Get a reference to the NetworkStream
                NetworkStream stream = client.GetStream();
                // Create a new StreamReader from the NetworkStream
                StreamReader streamReader = new StreamReader(stream);
                // Receive data in an infinite loop
                while (true)
                {
                    // Read one line at a time
                    string line = streamReader.ReadLine();
                    //                    using (StreamWriter writer = new StreamWriter(SavePath + IPReader + "_raw.txt", true))
                    //                    {
                    //                        writer.WriteLine(line);
                    //                    }
                    try
                    {
                        // This is what I do with the data received. Essentially I am receiving several fields from an RFID reader. I need to alter the data received and rebroadcast the data
                        string[] parts = line.Split(delimiterChars);
                        string bibhex = parts[1]; // sometimes the EPC hex is too long. Get the last 7 of hex
                        if (7 < bibhex.Length)
                        {
                            bibhex = bibhex.Substring(bibhex.Length - 7);
                        }
                        int epochTimeToArray = Convert.ToInt32(parts[2].Substring(0, 10));  //extract epoch time
                        int epochTimeUltra = epochTimeToArray - 312768000; // convert the time
                        int milliEpochTimeToArray = Convert.ToInt32(parts[2].Substring(10, 3));  // peel of the milliseconds
                        int bib = Convert.ToInt32(bibhex, 16);
                        if (bib.ToString().IndexOf(TagFilter) >= 0)  // This filter EPC tags based on the filter given in the form.
                        {
                            string ettime = parts[2].Substring(0, 13);
                            long etime = Convert.ToInt64(ettime);
                            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(etime).ToString("HH:mm:ss.fff");
                            int priorTime = 0;
                            if (tags.TryGetValue(bib, out priorTime))  // check dictionary to see of the tag has been seen before
                            {
                                if (priorTime + 20 > epochTimeToArray)  // check to see if tag was seen in the last 20 seconds. The 20 seconds need to be an input in the form.
                                {
                                    // do nothing, I realize that this code is poorly written. 
                                }
                                else
                                {   // this is where there is a prior time but 20 seconds have passed
                                    tags[bib] = epochTimeToArray;  //update the dictionary with more current time.
                                    Console.WriteLine("from reader {0},{1},0,\"{2}\"", parts[0], bib, epoch);  // this currently writes to the console. I would like it to write to a box on the form.
                                    string output = parts[0] + "," + bib + ",0,\"" + epoch + "\"";
                                    string outputr = "0," + bib + "," + epochTimeUltra + "," + milliEpochTimeToArray + ",1,23,0,0,0,0000000000000000,0,0";
                                    using (StreamWriter writer = new StreamWriter(SavePath + IPReader + ".txt", true))
                                    {   //write to file
                                        writer.WriteLine(output);  // This writes to a text file. It saves the data in the format I need.
                                    }


                                    if (ourStream.CanWrite)
                                    {
                                        streamWriter.WriteLine(outputr);  // This streams the data for other applications to grap.
                                        streamWriter.Flush();
                                    }
                                    else
                                    {
                                        Console.WriteLine("Sorry.  You cannot write to this NetworkStream.");
                                    }
                                }
                            }
                            else  // this is an inefficient else. It is a repeat of the above. The logic needs changed so as not to do this. This else runs when the dictionary doesn't have the tag.
                            {
                                tags.Add(bib, epochTimeToArray); // doesn't write anything of first read.
                                Console.WriteLine("from reader {0},{1},0,\"{2}\"", parts[0], bib, epoch);
                                string output = parts[0] + "," + bib + ",0,\"" + epoch + "\"";
                                string outputr = "0," + bib + "," + epochTimeUltra + "," + milliEpochTimeToArray + ",1,23,0,0,0,0000000000000000,0,0";
                                using (StreamWriter writer = new StreamWriter(SavePath + IPReader + ".txt", true))
                                {
                                    writer.WriteLine(output);
                                }
                                if (ourStream.CanWrite)
                                {
                                    streamWriter.WriteLine(outputr);
                                    streamWriter.Flush();
                                }
                                else
                                {
                                    Console.WriteLine("Sorry.  You cannot write to this NetworkStream.");
                                }
                            }
                        }   // end of tag filter
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        Console.WriteLine("Unknown line");
                    }
                } // end while
            }
            catch (Exception e)
            {
                // An error has occurred
                Console.WriteLine(e.Message);
            }





        }

    }
}
