using Microsoft.Win32;
using System.Windows;
using ZeroLevel.Network.FileTransfer;

namespace FileTransferClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IFileClient client;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            client = FileClientFactory.Create(tbEndpoint.Text, System.IO.Path.Combine(ZeroLevel.Configuration.BaseDirectory, "INCOMING"));
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == true)
            {
                client.Send(ofd.FileName);
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {

        }
    }
}
