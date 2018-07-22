using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TalkServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ClientListener Clients;
        ChannelManager Channels;
        ClientManager ClientList;

        Thread UiThread;
        public MainWindow()
        {
            ClientList = new ClientManager();
            this.Closing += MainWindow_Closed;
            InitializeComponent();
            Channels = new ChannelManager();
            Channels.CreateChannel("Test");
            Channels.DefaultChannel = Channels.GetChannelFromName("Test");
            Channels.Updated += Channels_Updated;

            Clients = new ClientListener();
            Clients.ClientAccepted += Clients_ClientAccepted;
            Clients.Stopped += Clients_Stopped;
            Clients.Started += Clients_Started;
            Clients.Start(4567);

            ConsoleTextbox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            ConsoleTextbox.IsReadOnly = true;
            ConsoleTextbox.Background = Brushes.Black;
            ConsoleTextbox.Foreground = Brushes.Green;
            ConsoleTextbox.Document.Blocks.Clear();
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
               // Clients.Stop();


            foreach (Client i in ClientList.AllClients.ToArray())
            {
                try
                {
                    Clients.Stop();
                }catch(Exception ex)
                {

                }

                try
                {
                    i.NotifyServerShutdown();
                    ClientList.RemoveClient(i);
                    i.Shutdown();   
                }catch(Exception ex)
                {

                }


            }
        }

        private void Channels_Updated(object sender, EventArgs e)
        {
            Update();

            

        }

        void UpdateUi()
        {
            try
            {
                ClientListBox.Dispatcher.Invoke((Action)delegate
                {
                    Update();
                });
            }catch(Exception e)
            {

            }

        }
        
        void Update()
        {
            List<MenuItem> _items = new List<MenuItem>();
            foreach (Channel _channel in Channels.Channels)
            {
                MenuItem t = new MenuItem();
                t.Header = "---" + _channel.ChannelName + "---";
                t.Tag = "channel:" + _channel.ChannelName;
                _items.Add(t);
                foreach (Client i in _channel.ClientList)
                {
                    MenuItem x = new MenuItem();
                    x.Header = i.Username;
                    x.Tag = "client:" + i.Username;
                    _items.Add(x);
                }
            }
            ClientListBox.Items.Clear();
            foreach (MenuItem i in _items)
            {
                ClientListBox.Items.Add(i);
            }

            foreach(Client i in ClientList.AllClients)
            {
                i.SendChannelList(Channels.Channels);
            }
        }

        private void Clients_Started(object sender, int e)
        {
            Print("Started listening in port " + e);
        }

        private void Clients_Stopped(object sender, int e)
        {
            Print("Stopped listening in port " + e);
        }

        void Clients_ClientAccepted(object sender, System.Net.Sockets.TcpClient e)
        {
            Print("Accepted client from " + e.Client.RemoteEndPoint.ToString());
            UnknownClient _client = new UnknownClient(e);
            _client.ClientRegistered += _client_ClientRegistered;
        }

        private void _client_ClientRegistered(object sender, string[] e)
        {
            UnknownClient _client = (sender as UnknownClient);
            Print(e[0] + " logged in (" + _client.ClientAddress + ") (" + e[1] + ")");

            if (ClientList.GetClientByGuid(new Guid(e[1])) == null){
                Client i = new Client(_client.Socket, e[0], new Guid(e[1]));
                Channels.MoveClient(i, Channels.DefaultChannel);
                i.ClientDisconnected += I_ClientDisconnected;
                i.SentMessage += I_SentMessage;
                i.NotifyLogin();
                ClientList.AddClient(i);
                UpdateUi();
            }
            else
            {
                _client.SendCommand(TalkLib.ServerCommand.ClientAlreadyConnected);
            }
            
        }

        private void I_SentMessage(object sender, string[] e)
        {
            Client i = sender as Client;
            Print(i.Id + " '" + e[0] + "' -> " + e[1]);

            Client dest = ClientList.GetClientByGuid(new Guid(e[1]));
            dest.MessageFromClient(e[0], e[1]);
        }

        private void I_ClientDisconnected(object sender, bool e)
        {
            Client i = sender as Client;

            bool _notify = false;

            foreach(Client c in ClientList.AllClients)
            {
                if(c == i)
                {
                    _notify = true;
                }
            }
            if (_notify)
            {
                Print(i.Username + " disconnected");
                ClientList.RemoveClient(i);
                Channels.RenoveClient(i);
                UpdateUi();
            }


        }

        void Print(string msg)
        {
            try
            {
                this.Dispatcher.Invoke(new Action(() =>
                {
                    ConsoleTextbox.AppendText(msg);
                    ConsoleTextbox.AppendText(Environment.NewLine);
                }));
            }catch(Exception E)
            {

            }

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            foreach (Client i in ClientList.AllClients)
            {
                i.SendChannelList(Channels.Channels);
            }
        }
    }
}
