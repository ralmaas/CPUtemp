using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using OpenHardwareMonitor.Hardware;
using System.Timers;

namespace CPUtemp
{
    public partial class Service1 : ServiceBase
    {
        // Logging
        static string logFile = AppDomain.CurrentDomain.BaseDirectory + @"\CPUtemp.log";
        // MQTT
        static MqttClient client;
        static string clientId;
        static string BrokerAddress = "192.168.2.111";
        static int broker_entries = BrokerAddress.Length;
        string MQTT_Topic = "Laptop/Temp/CPU";

        public Service1()
        {
            InitializeComponent();
            client = new MqttClient(BrokerAddress);    // Connect to the default server
        }

        public static void writeLog(string message)
        {
            /* DEBUG USE ONLY
             */
            string now = DateTime.Now.ToString("H:mm:ss");
            string combined = now + " 2: " + message;
            // eventLog1.WriteEntry(combined);
            using (System.IO.StreamWriter w = File.AppendText(logFile))
            {
                w.WriteLine(combined);
                w.Close();
            }
        }

        /*
         * MQTT functions
         */
        static void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string ReceivedMessage = Encoding.UTF8.GetString(e.Message);
            string ReceivedTopic = e.Topic;
        }

        public static void Client_ConnectionClosed(object sender, EventArgs e)
        {
            writeLog("MQTT Connection Closed");
        }

        private static void Client_Reconnect()
        {
            while (!client.IsConnected)
            {
                client = new MqttClient(BrokerAddress);
                client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
                client.ConnectionClosed += Client_ConnectionClosed;

                // use a unique id as client id, each time we start the application
                clientId = Guid.NewGuid().ToString();
                try
                {
                    client.Connect(clientId);
                }
                catch
                {
                    writeLog("Problem with MQTT (re)connect");
                }
            }
        }
        /*
         * OpenHardwareMonitor
         */

        public float getSystemInfo()
        {
            float temp = 0;

            UpdateVisitor updateVisitor = new UpdateVisitor();
            Computer computer = new Computer();
            computer.Open();
            computer.CPUEnabled = true;
            computer.Accept(updateVisitor);
            for (int i = 0; i < computer.Hardware.Length; i++)
            {
                if (computer.Hardware[i].HardwareType == HardwareType.CPU)
                {
                    for (int j = 0; j < computer.Hardware[i].Sensors.Length; j++)
                    {
                        if (computer.Hardware[i].Sensors[j].SensorType == SensorType.Temperature)
                        {
                            temp = (float)computer.Hardware[i].Sensors[j].Value;
                            /*
                            string buffer = String.Format("getSystemInfo: {0} value: {1}", computer.Hardware[i].Sensors[j].Name, temp);
                            writeLog(buffer);
                            */
                        }
                    }
                }
            }
            computer.Close();
            return temp;
        }

        /*
         * Timer
         */
        private void OnTimer(object sender, ElapsedEventArgs e)
        {
            float temperature;

            if (!client.IsConnected)
                Client_Reconnect();

            // System.Diagnostics.Debugger.Launch();
            temperature = getSystemInfo();
            /*
            string buff = String.Format("Temp returned is: {0}\r\n", temperature);
            writeLog(buff);
            */
            client.Publish(MQTT_Topic, Encoding.UTF8.GetBytes(temperature.ToString()), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);
        }

        protected override void OnStart(string[] args)
        {
            // System.Diagnostics.Debugger.Launch();
            writeLog("Service Started");
            // Set up a timer that triggers two minute.
            Timer timer = new Timer();
            timer.Interval = 120000; // 60 seconds
            timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
            timer.Start();
        }

        protected override void OnStop()
        {
            writeLog("Service Stopped");
        }
    }
}
