using System;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.IO;
using System.Diagnostics;
using TalkLib;

namespace TalkClient
{
    class ServerConnection
    {
        public event EventHandler<IPEndPoint> Connected = delegate { };
        public event EventHandler<IPEndPoint> Disconnected = delegate { };
        public event EventHandler<IPEndPoint> ConnectionFailed = delegate { };
        public event EventHandler<IPEndPoint> ConnectionError = delegate { };

        public event EventHandler<IPEndPoint> ServerShutdown = delegate { };

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
        /// Is client logged in
        /// </summary>
        bool IsLoggedIn = false;

        IPEndPoint ServerAddress;

        public bool Active { get => Isconnected;}

        /// <summary>
        /// Connect to specified server
        /// </summary>
        /// <param name="dest"></param>
        /// <returns></returns>
        public bool Connect(IPEndPoint dest, Guid Gid, string username)
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
                    ServerAddress = dest;
                    BinRead = new BinaryReader(TcpSocket.GetStream());
                    BinWrite = new BinaryWriter(TcpSocket.GetStream());

                    ListenThread = new Thread(ListenForData);
                    ListenThread.Start();
                    SendInitialData(username, Gid);
                    Isconnected = true;
                    Connected(this, (TcpSocket.Client.RemoteEndPoint as IPEndPoint));
                    return true;
                }catch(SocketException e)
                {
                    Isconnected = false;
                    Debug.WriteLine("Talkclient: " + e.Message);
                    ConnectionFailed(this, DestinationServer);
                    return false;
                }
            }
        }

        public void SendMessage(string msg, string destid)
        {
            SendCommand(ClientCommand.SendMessage, msg, destid);
        }

        void ListenForData()
        {
            while (!StopConnection)
            {
                try
                {
                    Debug.WriteLine("TalkClient: Waiting for data");
                    int _size = BinRead.ReadInt32();
                    byte[] _data = new byte[_size];
                    _data = BinRead.ReadBytes(_size);
                    Debug.WriteLine("CLIENT: " + _data.Length + " bytes");
                    ServerDataRecievedArgs _args = new ServerDataRecievedArgs(_data);
                    DataReceived(null, _args);
                }catch(Exception e)
                {
                    Debug.WriteLine(e.Message);
                    StopConnection = true;
                }

            }
        }

        public void Shutdown(int reason)
        {
            if(reason == 0)
            {
                Disconnected(this, ServerAddress);
            }else if(reason == 1)
            {
                ServerShutdown(this, ServerAddress);
            }
            
        }
        void SendCommand(ClientCommand command, params object[] args)
        {
            try
            {
                if (command == ClientCommand.Disconnect)
                {
                    BinWrite.Write(4);
                    BinWrite.Write((int)command);
                }else if(command == ClientCommand.SendMessage)
                {
                    Packet_Message a = new Packet_Message(args[0] as string, (string)args[1]);
                    byte[] data = Serializer.SerializeObject(a);
                    BinWrite.Write(4 + data.Length);
                    BinWrite.Write((int)command);
                    BinWrite.Write(data);
                }
            }catch(Exception e){
                Debug.WriteLine("Error sending command " + e.Message);
            }

        }

        void SendInitialData(string name, Guid Gid)
        {
            BinWrite.Write(name.Length);
            BinWrite.Write(Gid.ToString().Length);

            byte[] _data;
            using (MemoryStream ms = new MemoryStream())
            {
                //Write login header
                byte[] header = new byte[3];
                header[0] = 144;
                header[1] = 211;
                header[2] = 121;
                ms.Write(header, 0, 3);

                //Write name array
                byte[] _name = Encoding.UTF8.GetBytes(name);

                //Write UID
                byte[] _id = Encoding.UTF8.GetBytes(Gid.ToString());

                ms.Write(_name, 0, _name.Length);
                ms.Write(_id, 0, _id.Length);
                _data = ms.ToArray();
            }

            BinWrite.Write(_data);

            Debug.WriteLine("Client: Sent data");
        }
        
        public void Disconnect()
        {
            if (TcpSocket.Connected)
            {
                SendCommand(ClientCommand.Disconnect);
                TcpSocket.Client.Disconnect(false);
            }
            Disconnected(this, ServerAddress);
            Isconnected = false;
        }

        public void OnServerShutdown()
        {
            ServerShutdown(this, ServerAddress);
        }

        public void SetLoggedIn()
        {
            IsLoggedIn = true;
        }
    }
}
