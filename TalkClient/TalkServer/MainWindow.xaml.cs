using System;
using System.Collections.Generic;
using System.Linq;
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

namespace TalkServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ClientListener Clients;


        public MainWindow()
        {
            InitializeComponent();
            Clients = new ClientListener();
            Clients.ClientAccepted += Clients_ClientAccepted;
            Clients.Stopped += Clients_Stopped;
            Clients.Started += Clients_Started;
            Clients.Start(4567);

            ConsoleTextbox.Background = Brushes.Black;
            ConsoleTextbox.Foreground = Brushes.Green;
            ConsoleTextbox.Document.Blocks.Clear();
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
        }

        void Print(string msg)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                ConsoleTextbox.AppendText(msg + " \n");
            }));
        }

    }
}
