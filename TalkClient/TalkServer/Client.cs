using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TalkLib;

namespace TalkServer
{
    class Client
    {        /// <summary>
             /// Called when client loses connection
             /// </summary>
        public event EventHandler<bool> ClientDisconnected = delegate { };

        public event EventHandler<string[]> SentMessage = delegate { };


        /// <summary>
        /// Client socket
        /// </summary>
        TcpClient TcpC;

        /// <summary>
        /// Is TCP client connected
        /// </summary>
        bool IsActive = false;

        /// <summary>
        /// Data listen thread
        /// </summary>
        Thread ListenThread;

        /// <summary>
        /// Disconnects client when true
        /// </summary>
        bool RemoveClient = false;

        /// <summary>
        /// Binary Stream reader
        /// </summary>
        BinaryReader BinRead;

        /// <summary>
        /// Stream writer
        /// </summary>
        BinaryWriter BinWrite;

        /// <summary>
        /// Client address
        /// </summary>
        IPAddress Address;


        /// <summary>
        /// Client IP address
        /// </summary>
        public IPAddress ClientAddress { get => Address; }
        public string Username { get => ClientName; }
        public Guid Id { get => ClientID;}
        internal Channel ClientChannel { get => CurrentChannel; set => CurrentChannel = value; }

        /// <summary>
        /// Client unique ID
        /// </summary>
        Guid ClientID;

        /// <summary>
        /// Client name
        /// </summary>
        string ClientName;

        /// <summary>
        /// Clients current channel
        /// </summary>
        Channel CurrentChannel;

        /// <summary>
        /// Creates a new unknown client and sets up listen thread
        /// </summary>
        /// <param name="Client"></param>
        public Client(TcpClient Client, string name, Guid gid)
        {
            ClientName = name;
            ClientID = gid;


            TcpC = Client;
            BinRead = new BinaryReader(TcpC.GetStream());
            BinWrite = new BinaryWriter(TcpC.GetStream());
            Address = (TcpC.Client.RemoteEndPoint as IPEndPoint).Address;
            ListenThread = new Thread(Listen);
            ListenThread.Start();
            IsActive = true;
        }

        /// <summary>
        /// Waits for data
        /// </summary>
        void Listen()
        {
            try
            {
                while (!RemoveClient)
                {
                    int _datasize = BinRead.ReadInt32();
                    byte[] _data = BinRead.ReadBytes(_datasize);
                    ParseData(_data);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Server: Client socket error " + e.Message);
                ClientDisconnected(this, false);
            }
        }

        /// <summary>
        /// Parses incoming byte array
        /// </summary>
        /// <param name="data"></param>
        /// <param name="size"></param>
        void ParseData(byte[] data)
        {
            int _cmd = BitConverter.ToInt32(data, 0);
            Debug.WriteLine(ClientName + " COMMAND {" + _cmd + "}");

            if(_cmd == (int)ClientCommand.Disconnect)
            {
                ClientDisconnected(this, true);
                Shutdown();
            }else if(_cmd == (int)ClientCommand.SendMessage)
            {
                byte[] _serialized = new byte[data.Length - 4];
                Array.Copy(data, 4, _serialized, 0, _serialized.Length);
                Packet_Message _msg = (Packet_Message)Serializer.DeserializeObject(_serialized);
                string[] s = new string[2] { _msg.Message, _msg.Dest };
                SentMessage(this, s);
            }
        }


        void SendCommand(ServerCommand command, params object[] args)
        {
            Debug.WriteLine("sending " + ClientName + " command " + (int)command);
            if (command == ServerCommand.NotifyConnected)
            {
                BinWrite.Write(4);
                BinWrite.Write((int)ServerCommand.NotifyConnected);

            }
            else if (command == ServerCommand.ClientAlreadyConnected)
            {
                BinWrite.Write(4);
                BinWrite.Write((int)ServerCommand.ClientAlreadyConnected);
            }
            else if (command == ServerCommand.ServerShutdown)
            {
                BinWrite.Write(4);
                BinWrite.Write((int)ServerCommand.ServerShutdown);
            }
            else if (command == ServerCommand.ChannelList)
            {
                byte[] data = Serializer.SerializeObject(args[0]);
                BinWrite.Write(data.Length + 4);
                BinWrite.Write((int)ServerCommand.ChannelList);
                BinWrite.Write(data);
                Debug.WriteLine("Sent clientlist {" + data.Length + 4 + "}");
            }
            else if (command == ServerCommand.MessageFromClient)
            {
                Packet_MessageFromClient a = new Packet_MessageFromClient(args[0] as string, (string)args[1]);
                byte[] data = Serializer.SerializeObject(a);
                BinWrite.Write(4 + data.Length);
                BinWrite.Write((int)command);
                BinWrite.Write(data);
            }
        }

        public void NotifyServerShutdown()
        {
            SendCommand(ServerCommand.ServerShutdown);
        }

        public void MessageFromClient(string msg, string sender)
        {
            SendCommand(ServerCommand.MessageFromClient, msg, sender);
        }

        /// <summary>
        /// Disconnects and cleans up
        /// </summary>
        public void Shutdown()
        {
            RemoveClient = true;
            TcpC.Client.Disconnect(false);
            TcpC.Dispose();

            IsActive = false;
            ListenThread.Abort();
        }

        public void NotifyLogin()
        {
            SendCommand(ServerCommand.NotifyConnected);
        }

        public void SendChannelList(List<Channel> channels)
        {
            List<Data_Channel> list = new List<Data_Channel>();  

            foreach(Channel x in channels)
            {
                List<Data_Client> _clients =new  List<Data_Client>();

                foreach(Client z in x.ClientList)
                {
                    Data_Client c = new Data_Client(z.ClientName, z.ClientID.ToString());
                    _clients.Add(c);
                }

                Data_Channel _cur = new Data_Channel(x.ChannelName, x.ChannelId, _clients.ToArray());
                list.Add(_cur);
            }

            Packet_ChannelList i = new Packet_ChannelList(list.ToArray());
            SendCommand(ServerCommand.ChannelList, i);

        }
    }
}
