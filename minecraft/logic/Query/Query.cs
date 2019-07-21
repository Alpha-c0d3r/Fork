using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using nihilus.Logic.Model;

namespace nihilus.Logic.Query
{
    public class Query
    {
        #region Properties

        // Public properties
        public static long responseTime { get; set; } = 0;
        public UdpClient udpClient { get; set; } = new UdpClient();

        // Private properties
        private string serverIp { get; set; }
        private int serverPort { get; set; }
        private int timeoutPing { get; set; } = 3000;
        private bool messageReceived { get; set; } = false;
        private byte[] outputBytes { get; set; }

        #endregion

        #region Variables

        private IPEndPoint receivePoint;
        private IPEndPoint endPoint;
        private static IPAddress[] serverIpAddresses;

        #endregion

        public Query(string serverIp, int serverPort)
        {
            this.serverIp = serverIp;
            this.serverPort = serverPort;

            //Setup connection
            //TODO may throw socketException
            serverIpAddresses = Dns.GetHostAddresses(serverIp);
            receivePoint = new IPEndPoint(IPAddress.Any, serverPort);
            endPoint = new IPEndPoint(serverIpAddresses.First(), serverPort);
            udpClient = new UdpClient();
        }
        protected Query(){}

        public bool ServerAvailable()
        {
            try
            {
                Ping p = new Ping();
                // Sending ping to server
                PingReply reply = p.Send(serverIp, 3000);

                // If not reply, server is offline
                if (reply == null) return false;

                // Get time of response
                responseTime = reply.RoundtripTime;

                // Return if the server is OK or not
                return reply.Status == IPStatus.Success;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public FullStats FullServerStats()
        {
            int handshake = Handshake();
            byte[] fullStatsBytes = FullStats(handshake);
            FullStats fullStats = new FullStats(fullStatsBytes);
            return fullStats;
        }
        
        private byte[] ConnectToServer(byte[] inputBytes)
        {
            // Ping minecraft server
            if (!ServerAvailable()) throw new SocketException();

            // Connecting with minecraft server
            udpClient.Connect(endPoint);

            // Sending data
            udpClient.Send(inputBytes, inputBytes.Length);

            // Cleaning data of output
            outputBytes = new byte[] { };
            messageReceived = false;

            // Set datetime of timeout
            DateTime endTimeout = DateTime.Now.AddMilliseconds(timeoutPing);

            // Receiving data async
            udpClient.BeginReceive(ReceiveCallback, receivePoint);

            while (!messageReceived)
            {
                Thread.Sleep(100);
                if (DateTime.Now > endTimeout)
                {
                    throw new TimeoutException("The server does not respond at specified port");
                }
            }

            // Receiving data sync - Block application if not receive data of port
            // byte[] outputBytes = udpClient.Receive(ref receivePoint);

            // Returning data
            return outputBytes;
        }
        
        private void ReceiveCallback(IAsyncResult ar)
        {
            outputBytes = udpClient.EndReceive(ar, ref receivePoint);
            messageReceived = true;
        }
        
        private int Handshake()
        {
            // Declare vars
            MemoryStream stream = new MemoryStream();

            // Writing stream bytes
            stream.WriteByte(0xFE); // Magic
            stream.WriteByte(0xFD); // Magic
            stream.WriteByte(0x09); // Type
            stream.WriteByte(0x01); // Session
            stream.WriteByte(0x01); // Session
            stream.WriteByte(0x01); // Session
            stream.WriteByte(0x01); // Session

            // Preparing byte array
            byte[] sendme = stream.ToArray();

            // Closing stream for save resources
            stream.Close();

            // Returned data
            byte[] receivedBytes = ConnectToServer(sendme);

            string number = "";

            for (int i = 0; i < receivedBytes.Length; i++)
            {
                if (i > 4 && receivedBytes[i] != 0x00)
                {
                    number += (char)receivedBytes[i];
                }
            }

            // Return token
            return int.Parse(number);
        }

        private byte[] FullStats(int number)
        {
            // Declare vars
            MemoryStream stream = new MemoryStream();
            byte[] numberbytes = BitConverter.GetBytes(number).Reverse().ToArray();

            // Writing stream bytes
            stream.WriteByte(0xFE); // Magic
            stream.WriteByte(0xFD); // Magic
            stream.WriteByte(0x00); // Type
            stream.WriteByte(0x01); // Session
            stream.WriteByte(0x01); // Session
            stream.WriteByte(0x01); // Session
            stream.WriteByte(0x01); // Session
            stream.Write(numberbytes, 0, 4); // Challenge
            stream.WriteByte(0x00); // Padding
            stream.WriteByte(0x00); // Padding
            stream.WriteByte(0x00); // Padding
            stream.WriteByte(0x00); // Padding

            // Preparing byte array
            byte[] sendme = stream.ToArray();

            // Closing stream for save resources
            stream.Close();

            // Return data
            return ConnectToServer(sendme);
        }
    }
}