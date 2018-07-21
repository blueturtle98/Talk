using System;
using System.Collections.Generic;
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

namespace TalkClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ServerConnection _server;

        public MainWindow()
        {
            
            _server = new ServerConnection();
            _server.Connected += Event_Connected;
            _server.ConnectionFailed += Event_ConnectionFailed;

            InitializeComponent();
            ConsoleText.Background = Brushes.Black;
            ConsoleText.Foreground = Brushes.Green;
            ConsoleText.Document.Blocks.Clear();
        }

        private void Event_Connected(object sender, IPEndPoint e)
        {
            Output("Connected to " + e.ToString());
        }

        void Event_ConnectionFailed(object Sender, IPEndPoint Server)
        {
            Output("Failed to connect to " + Server.ToString());
        }

        void Event_DataReceived(object sender, byte[] data)
        {
            Output("Received " + data.Length + " bytes");
        }

        void Output(string msg)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                ConsoleText.AppendText(msg + " \n");
            }));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _server.Connect(new System.Net.IPEndPoint(IPAddress.Parse("127.0.0.1"), 4567), Environment.MachineName);
        }
    }
}
