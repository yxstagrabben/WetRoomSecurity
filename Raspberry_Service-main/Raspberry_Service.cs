using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.IO;
using System.Threading;
using System.Collections;
using System.Net.Mail;
using System.Diagnostics;
using System.Timers;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Text;

namespace Raspberry_DataService
{
    partial class Raspberry_Service : ServiceBase
    {
        TcpListener server = null;
        TcpClient client = null;
        static IPAddress ipAddress;
        static String hostName = Dns.GetHostName();
        static Int32 port = 13000;
        Thread listenerThread;       
        Queue connectionQueue = null;
        byte[] bytes = new byte[256];      

        public Raspberry_Service()
        {
            InitializeComponent();
            this.CanHandlePowerEvent = true;
            this.CanHandleSessionChangeEvent = true;
            this.CanPauseAndContinue = true;
            this.CanShutdown = true;
            this.CanStop = true;
        }

        public void OnDebug()
        {
            OnStart(null);
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                if (!EventLog.SourceExists("LogRaspberry"))
                {
                    EventLog.CreateEventSource("LogRaspberry", "LogRaspberry");
                }

                EventLog.WriteEntry("LogRaspberry", "OnStart function starting.", EventLogEntryType.SuccessAudit);

                IPHostEntry ipEntry = Dns.GetHostEntry(hostName);
             IPAddress[] addr = ipEntry.AddressList;
             ipAddress = addr[3];

             listenerThread = new Thread(new ThreadStart(ListenerMethod));
             listenerThread.Start();
           }
           catch (SocketException e)
           {
                EventLog.WriteEntry("LogRaspberry", "OnStart function error:" + e.ToString(), EventLogEntryType.Error);
           }
        }

        protected override void OnStop()
        {
            server.Stop();
            TcpClient client = (TcpClient)connectionQueue.Dequeue();
        }
        protected void ListenerMethod()
        {
            List<string> addressList = new List<string>();
            List<string> googleAdapted_AddressList = new List<string>();
            List<string> optimizedList = new List<string>();

            Queue unsyncq = new Queue();
            Byte[] bytes = new Byte[256];
            String sensorData = null;
            Thread workingthread;

            connectionQueue = Queue.Synchronized(unsyncq);
            IPHostEntry ipEntry = Dns.GetHostEntry(hostName);
            IPAddress[] addr = ipEntry.AddressList;
            ipAddress = addr[1];

            byte[] msg = System.Text.Encoding.ASCII.GetBytes("ack\0");
            byte[] msgError = System.Text.Encoding.ASCII.GetBytes("nack\0");

            try
            {
                EventLog.WriteEntry("LogRaspberry", "Listener function starting.", EventLogEntryType.SuccessAudit);

                TcpListener server = new TcpListener(ipAddress, port); // Start listening for client requests.
                server.Start();

                while (true) // Enter the listening loop.
                {
                    client = server.AcceptTcpClient();
                    connectionQueue.Enqueue(client);
                    workingthread = new Thread(new ThreadStart(TheConnectionHandler));

                    workingthread.Start();
                    sensorData = null;
                    NetworkStream stream = client.GetStream(); // Get a stream object for reading and writing

                    int i;
                    EventLog.WriteEntry("LogRaspberry", "Listener function while true.", EventLogEntryType.SuccessAudit);

                    while ((i = stream.Read(bytes, 0, bytes.Length)) > 0 && bytes[0] != 13) //Now we have to insert the new socket in the queue
                    {
                        sensorData = System.Text.Encoding.ASCII.GetString(bytes, 0, i);// Translate bytes to a ASCII string.
                        string tSensorData = sensorData.Trim();

                        string logged = LogData(tSensorData);

                        if (logged != null)
                        {
                            stream.Write(msg, 0, msg.Length);  //Send back a response.                            
                        }
                        else
                        {
                            stream.Write(msgError, 0, msg.Length);
                        }
                        addressList = UploadAdressList_FromFile();

                        string destination = "Karlsgatan, 20, 70375, örebro"; //addressList[addressList.Count - 1];                       
                    }
                    client.Close();
                }
            }
            catch (SocketException e)
            {
                EventLog.WriteEntry("LogRaspberry", "ListenerMethod function error:" + e.ToString(), EventLogEntryType.Error);
            }        
        }

        public void TheConnectionHandler()
        {
            TcpClient socket = (TcpClient)connectionQueue.Dequeue();
        }
        private List<string> UploadAdressList_FromFile()
        {
            var today = DateTime.Now;
            string yesterday = DateTime.Now.AddDays(-1).ToString("dd/MM/yyyy");
            List<string> addressList = new List<string>();

            string destPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sensor_LogData" + yesterday + ".txt");

            if (File.Exists(destPath))
            {
                using (StreamReader reader = new StreamReader(destPath, System.Text.Encoding.GetEncoding(1252), true))
                {
                    string address;

                    while ((address = reader.ReadLine()) != null)
                    {
                        addressList.Add(address);
                    }
                    EventLog.WriteEntry("LogRaspberry", "mail function done.", EventLogEntryType.SuccessAudit);
                    reader.Close();
                }
            }
            return addressList;
        }

        private static string LogData(string sensorData)
        {
            string request = null;
            string logTime = DateTime.Now.ToString("dd/MM/yyyy");

            try
            {
                string destPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LogData" + logTime + ".txt");
                using (StreamWriter writer = new StreamWriter(destPath, true, System.Text.Encoding.GetEncoding(1252)))
                {
                    writer.WriteLine(string.Format(sensorData));
                    writer.Close();
                    request = sensorData + " logged";
                }
            }
            catch (Exception e)
            {
                EventLog.WriteEntry("LogRaspberry", "Logg function error:" + e.ToString(), EventLogEntryType.Error);
            }
            return request;
        }

        protected static void releaseObject(Object obj)
        {
            try
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(obj);
                obj = null;
                GC.Collect();
            }
            catch (Exception e)
            {
                EventLog.WriteEntry("LogRaspberry", "release function error:" + e.ToString(), EventLogEntryType.Error);
            }
        }
    }
}
