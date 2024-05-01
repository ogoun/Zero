using System;
using System.Collections.ObjectModel;
using System.Windows;
using ZeroLevel;
using ZeroLevel.Network;

namespace ZeroNetworkMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<string> _service_keys = new ObservableCollection<string>();
        private IExchange _exchange;
        private long _refresh_services_task = -1;

        public ObservableCollection<string> Services
        {
            get { return _service_keys; }
        }

        public MainWindow()
        {
            InitializeComponent();
            _exchange = Bootstrap.CreateExchange();
            Injector.Default.Register<IExchange>(_exchange);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _exchange.UseDiscovery(tbDiscovery.Text);
            if (_refresh_services_task == -1)
            {
                _refresh_services_task = Sheduller.RemindEvery(TimeSpan.FromSeconds(5), () =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        var si = lbServices.SelectedItem;
                        _service_keys.Clear();
                        foreach (var s in _exchange.DiscoveryStorage.GetKeys())
                        {
                            _service_keys.Add(s);
                        };
                        lbServices.SelectedItem = si;
                    });
                });
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            lbServices.ItemsSource = Services;
        }

        private void LbServices_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (lbServices.SelectedItem != null)
            {
                pService.UpdateView((string)lbServices.SelectedItem);
            }
        }
    }
}
