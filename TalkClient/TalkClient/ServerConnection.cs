using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace TalkClient
{
    class ServerConnection
    {
        public event EventHandler<IPEndPoint> Connected = delegate { };
        public event EventHandler<IPEndPoint> Disconnected = delegate { };
        public event EventHandler<IPEndPoint> ConnectionFailed = delegate { };
        public event EventHandler<IPEndPoint> ConnectionError = delegate { };

        public event EventHandler<ServerDataRecievedArgs> DataReceived = delegate { };

        /// <summary>
        /// Socket used to connnect
        /// </summary>
        TcpClient TcpSocket;

        /// <summary>
        /// Destination server address
        /// </summary>
        IPEndPoint DestinationServer;

        /// <summary>
        /// Is the client connected
        /// </summary>
        bool Isconnected = false;

        /// <summary>
        /// Thread used to listen for data
        /// </summary>
        Thread ListenThread;

        /// <summary>
        /// Enable to stop connection
        /// </summary>
        bool StopConnection = false;

        /// <summary>
        /// Reads data from the TcpClient
        /// </summary>
        BinaryReader BinRead;

        /// <summary>
        /// Writes data to the binarystream
        /// </summary>
        BinaryWriter BinWrite;

        /// <summary>
        /// Connect to specified server
        /// </summary>
        /// <param name="dest"></param>
        /// <returns></returns>
        public bool Connect(IPEndPoint dest, string username)
        {
            if (Isconnected)
            {
                return false;
            }
            else
            {
                try
                {
                    DestinationServer = dest;
                    TcpSocket = new TcpClient();
                    TcpSocket.Connect(dest);

                    BinRead = new BinaryReader(TcpSocket.GetStream());
                    BinWrite = new BinaryWriter(TcpSocket.GetStream());

                    ListenThread = new Thread(ListenForData);
                    ListenThread.Start();
                    SendInitialData(username);
                    Connected(this, (TcpSocket.Client.RemoteEndPoint as IPEndPoint));
                    return true;
                }catch(SocketException e)
                {
                    Debug.WriteLine("Talkclient: " + e.Message);
                    ConnectionFailed(this, DestinationServer);
                    return false;
                }
            }
        }

        void ListenForData()
        {
            while (!StopConnection)
            {
                Debug.WriteLine("TalkClient: Waiting for data");
                int _size = BinRead.ReadInt32();
                byte[] _data = new byte[_size];
                _data = BinRead.ReadBytes(_size);
                ServerDataRecievedArgs _args = new ServerDataRecievedArgs(_data);
                DataReceived(null, _args);
            }
        }

        void SendInitialData(string name)
        {
            byte[] data = new byte[3];
            data[0] = 144;
            data[1] = 211;
            data[2] = 121;
            BinWrite.Write(data);
            BinWrite.Write(name);
            Debug.WriteLine("Client: Sent data");
        }

    }
}
