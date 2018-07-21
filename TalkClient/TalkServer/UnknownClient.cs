using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TalkServer
{
    class UnknownClient
    {
        public event EventHandler<string> ClientDisconnected = delegate { };
        public event EventHandler ClientRegistered = delegate { };


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

        public UnknownClient(TcpClient Client)
        {
            TcpC = Client;
            BinRead = new BinaryReader(TcpC.GetStream());
            ListenThread = new Thread(Listen);
            ListenThread.Start();
        }

        void Listen()
        {
            try
            {
                while (!RemoveClient)
                {
                    Debug.WriteLine("WAITING FOR DATA");
                    int _size = BinRead.ReadInt32();
                    byte[] _data = BinRead.ReadBytes(_size);
                    Debug.WriteLine("received data");
                    ParseData(_data);
                }
            }catch(SocketException e)
            {
                Debug.WriteLine("Server: Client socket error " + e.Message);
                ClientDisconnected(this, e.Message);
            }

            Debug.WriteLine("EXITED LOOP");
        }

        void ParseData(byte[] data)
        {
            Debug.WriteLine("PARSING DATA " + data.Length + " Bytes");
            if(data[0] == 144 && data[1] == 211 && data[2] == 121)
            {
                Debug.WriteLine("Client connected");
            }
        }
    }
}
