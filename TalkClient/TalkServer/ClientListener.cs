using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace TalkServer
{
    /// <summary>
    /// Listens for TCP connections on specific port
    /// </summary>
    class ClientListener
    {
        /// <summary>
        /// Server socket
        /// </summary>
        TcpListener TcpL;

        /// <summary>
        /// Port to listen on
        /// </summary>
        int ListenPort;


        /// <summary>
        /// Thread used for listening for clients
        /// </summary>
        Thread ListenThread;

        public ClientListener(int port)
        {
            ListenPort = port;
        }

        public bool Start()
        {
            try
            {
                if(TcpL != null){
                    TcpL = new TcpListener(IPAddress.Any, ListenPort);
                    TcpL.Start();
                }
                else
                {
                    throw new InvalidOperationException("Already listening");
                }
                

            }catch(Exception e)
            {

            }
        }

    }
}
