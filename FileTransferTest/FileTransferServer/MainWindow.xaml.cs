using System.IO;
using System.Windows;
using ZeroLevel;
using ZeroLevel.Network;
using ZeroLevel.Network.FileTransfer;

namespace FileTransferServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            _exchange = Bootstrap.CreateExchange();
        }

        private FileReceiver _server;
        private IExchange _exchange;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            int port = -1;
            if (!int.TryParse(tbPort.Text, out port)) port = -1;
            if (!Directory.Exists(tbFolder.Text))
            {
                try
                {
                    Directory.CreateDirectory(tbFolder.Text);
                }
                catch { }
            }
            if (port == -1 || !Directory.Exists(tbFolder.Text))
            {
                MessageBox.Show("Incorrect parameters");
                return;
            }
            var router = _exchange.UseHost(port);
            _server = new FileReceiver(router, tbFolder.Text, c => $"{c.Endpoint.Address}{c.Endpoint.Port}");
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
        }
    }
}
