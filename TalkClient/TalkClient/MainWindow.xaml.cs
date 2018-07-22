using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
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
using TalkLib;

namespace TalkClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ServerConnection _server;
        Guid ThisId;

        Packet_ChannelList ChannelList;

        public MainWindow()
        {
            this.Closing += MainWindow_Closing;
            ThisId = Guid.NewGuid();

            InitializeComponent();
            DisconnectButton.Visibility = Visibility.Hidden;
            ConsoleText.IsReadOnly = true;
            ConsoleText.Background = Brushes.Black;
            ConsoleText.Foreground = Brushes.Green;
            ConsoleText.VerticalAlignment = VerticalAlignment.Center;
            ConsoleText.Document.Blocks.Clear();
            ConsoleText.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        }



        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(_server != null)
            {
                _server.Disconnect();
            }
              

        }

        private void _server_Disconnected(object sender, IPEndPoint e)
        {
            ConnectButton.Visibility = Visibility.Visible;
            DisconnectButton.Visibility = Visibility.Hidden;

            ChannelList = new Packet_ChannelList(null);
            UpdateUi();
        }

        private void _server_DataReceived(object sender, ServerDataRecievedArgs e)
        {
            int _cmd = BitConverter.ToInt32(e.DataRecieved, 0);
            if (_cmd == (int)ServerCommand.NotifyConnected)
            {
                _server.SetLoggedIn();
                Output("Logged in...");
            }
            else if (_cmd == (int)ServerCommand.ClientAlreadyConnected)
            {
                Output("Cannot connect; already connected");
                _server.Disconnect();
            }
            else if (_cmd == (int)ServerCommand.ServerShutdown)
            {
                Output("Disconnected; Server shutting down...");
                _server.OnServerShutdown();
            }
            else if (_cmd == (int)ServerCommand.ChannelList)
            {
                byte[] _serializedlist = new byte[e.DataRecieved.Length - 4];
                Array.Copy(e.DataRecieved, 4, _serializedlist, 0, _serializedlist.Length);
                object s = Serializer.DeserializeObject(_serializedlist);
                ChannelList = s as Packet_ChannelList;
                UpdateUi();
            }else if(_cmd == (int)ServerCommand.MessageFromClient)
            {
                byte[] _serialized = new byte[e.DataRecieved.Length - 4];
                Array.Copy(e.DataRecieved, 4, _serialized, 0, _serialized.Length);
                Packet_MessageFromClient _msg = (Packet_MessageFromClient)Serializer.DeserializeObject(_serialized);
                Output(_msg.Dest + ": " + _msg.Message);
            }
        }

        private void Event_Connected(object sender, IPEndPoint e)
        {
            Output("Connected to " + e.ToString());
            ConnectButton.Visibility = Visibility.Hidden;
            DisconnectButton.Visibility = Visibility.Visible;
        }

        void Event_ConnectionFailed(object Sender, IPEndPoint Server)
        {
            _server = null;
            ConnectButton.Visibility = Visibility.Visible;
            DisconnectButton.Visibility = Visibility.Hidden;
            Output("Failed to connect to " + Server.ToString());
            ChannelList = new Packet_ChannelList(null);
            UpdateUi();
        }

        void Event_DataReceived(object sender, byte[] data)
        {
            Output("Received " + data.Length + " bytes");
        }

        void Output(string msg)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                ConsoleText.AppendText(msg);
                ConsoleText.AppendText(Environment.NewLine);
            }));
        }

        void UpdateUi()
        {
            ClientListBox.Dispatcher.Invoke(new Action(() =>
            {
                ClientListBox.Items.Clear();
                if (ChannelList.AllChannels != null)
                {
                    foreach (Data_Channel i in ChannelList.AllChannels.ToArray())
                    {


                        MenuItem t = new MenuItem();
                        t.Header = "---" + i.ChannelName + "---";
                        t.Tag = "channel:" + i.ChannelName;
                        t.Background = Brushes.Gray;
                        t.ContextMenu = ChannelContextMenu();
                        ClientListBox.Items.Add(t);
                        foreach (Data_Client z in i.ClientList)
                        {
                            Output(z.Name);
                            MenuItem e = new MenuItem();
                            e.Header = z.Name;
                            e.ContextMenu = ClientContextMenu();
                            e.Tag = "client:" + z.ID + ":" + z.Name;
                            ClientListBox.Items.Add(e);
                        }
                    }
                }
            }));
        }

        ContextMenu ClientContextMenu()
        {
            ContextMenu _menu = new ContextMenu();
            MenuItem _ItemSc = new MenuItem();
            _ItemSc.Click += ClientContextMenu_Screenshot_Click;
            _ItemSc.Header = "Send Screenshot";
            _menu.Items.Add(_ItemSc);

            return _menu;
        }

        ContextMenu ChannelContextMenu()
        {
            ContextMenu _menu = new ContextMenu();
            MenuItem _switch = new MenuItem();
            _switch.Click += ChannelContextMenu_Switch_Click;
            _switch.Header = "Switch to channel";
            _menu.Items.Add(_switch);

            return _menu;
        }

        private void ChannelContextMenu_Switch_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void ClientContextMenu_Screenshot_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem t = sender as MenuItem;
            Output("Selected: " + t.Tag);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
    {
        _server = new ServerConnection();
        _server.Connected += Event_Connected;
        _server.ConnectionFailed += Event_ConnectionFailed;
        _server.DataReceived += _server_DataReceived;
        _server.Disconnected += _server_Disconnected;
        _server.ServerShutdown += _server_ServerShutdown;

        _server.Connect(new System.Net.IPEndPoint(IPAddress.Parse("127.0.0.1"), 4567), ThisId, Environment.MachineName);
    }

    private void _server_ServerShutdown(object sender, IPEndPoint e)
    {
        this.Dispatcher.Invoke(new Action(() =>
        {
            ConnectButton.Visibility = Visibility.Visible;
            DisconnectButton.Visibility = Visibility.Hidden;
        }));

        ChannelList = new Packet_ChannelList(null);
        UpdateUi();
    }

    private void Button_Click_1(object sender, RoutedEventArgs e)
    {
        _server.Disconnect();
        Output("Disconnected...");
    }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if(ClientListBox.SelectedItem is null)
            {

            }else if((ClientListBox.SelectedItem as MenuItem).Tag.ToString().Split(':')[0] == "client")
            {
                MenuItem u = (ClientListBox.SelectedItem as MenuItem);
                _server.SendMessage("This is a text", u.Tag.ToString().Split(':')[1]);
                
            }
           
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            _server = new ServerConnection();
            _server.Connected += Event_Connected;
            _server.ConnectionFailed += Event_ConnectionFailed;
            _server.DataReceived += _server_DataReceived;
            _server.Disconnected += _server_Disconnected;
            _server.ServerShutdown += _server_ServerShutdown;

            _server.Connect(new System.Net.IPEndPoint(IPAddress.Parse("192.168.0.7"), 4567), ThisId, Environment.MachineName);
        }
    }
}
