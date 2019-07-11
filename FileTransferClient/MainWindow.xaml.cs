using Microsoft.Win32;
using System;
using System.Windows;
using ZeroLevel;
using ZeroLevel.Network;
using ZeroLevel.Network.FileTransfer;

namespace FileTransferClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FileSender _client;
        private IExchange _exchange;

        public MainWindow()
        {
            InitializeComponent();
            _exchange = Bootstrap.CreateExchange();
            _client = new FileSender();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (false == _client.Connected(_exchange.GetConnection(tbEndpoint.Text), TimeSpan.FromMilliseconds(300)))
            {
                MessageBox.Show("No connection");
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == true)
            {
                _client.Send(_exchange.GetConnection(tbEndpoint.Text), ofd.FileName);
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {

        }
    }
}
