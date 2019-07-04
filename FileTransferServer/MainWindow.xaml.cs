using System.IO;
using System.Windows;
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
        }

        private IFileServer _server;

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

            _server = FileServerFactory.Create(port, tbFolder.Text);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            _server = null;
        }
    }
}
