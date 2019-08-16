using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using ZeroLevel;
using ZeroLevel.Network;
using ZeroLevel.Network.SDL;

namespace ZeroNetworkMonitor
{
    /// <summary>
    /// Interaction logic for ServiceControlPanel.xaml
    /// </summary>
    public partial class ServiceControlPanel : UserControl
    {
        const string SDL_INBOX = "__service_description__";
        private ServiceDescription _description;

        public ServiceControlPanel()
        {
            InitializeComponent();
        }

        public void UpdateView(string serviceKey)
        {
            var exchange = Injector.Default.Resolve<IExchange>();
            var client = exchange.GetConnection(serviceKey);

            exchange.RequestBroadcast<ServiceDescription>(serviceKey, SDL_INBOX, records =>
            {
                if (records != null && records.Any())
                {
                    _description = records.First();
                    foreach (var r in records.Skip(1))
                    {
                        _description.Inboxes.AddRange(r.Inboxes);
                    }
                }
                else
                {
                    _description = null;
                }
                UpdateDescriptionView();
            });

            /*client?.Request<ServiceDescription>(SDL_INBOX, desc =>
            {
                _description = desc;
                UpdateDescriptionView();
            });*/
        }

        private void UpdateDescriptionView()
        {
            if (_description != null)
            {
                Dispatcher.Invoke(() =>
                {
                    tbGroup.Text = _description.ServiceInfo.ServiceGroup;
                    tbKey.Text = _description.ServiceInfo.ServiceKey;
                    tbName.Text = _description.ServiceInfo.Name;
                    tbType.Text = _description.ServiceInfo.ServiceType;
                    tbVersion.Text = _description.ServiceInfo.Version;
                    lbInboxes.ItemsSource = _description.Inboxes;
                });
            }
        }

        private void LbInboxes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var inbox = lbInboxes.SelectedItem as InboxServiceDescription;
            if (inbox != null)
            {

                var info = new StringBuilder();
                info.AppendLine($"Inbox '{inbox.Name}' type: {inbox.InboxKind}");
                info.AppendLine($"Target: '{inbox.Target}'");
                if (inbox.InboxKind == InboxKind.Handler || inbox.InboxKind == InboxKind.Reqeustor)
                {
                    info.AppendLine($"Incoming type: {inbox.IncomingType.Name}");
                    if (inbox.IncomingType.Fields != null && inbox.IncomingType.Fields.Any())
                    {
                        foreach (var field in inbox.IncomingType.Fields)
                        {
                            info.AppendLine($"\t{field.Key}: {field.Value}");
                        }
                    }
                }
                if (inbox.InboxKind == InboxKind.Reqeustor)
                {
                    info.AppendLine($"Outcoming type: {inbox.OutcomingType.Name}");
                    if (inbox.OutcomingType.Fields != null && inbox.OutcomingType.Fields.Any())
                    {
                        foreach (var field in inbox.OutcomingType.Fields)
                        {
                            info.AppendLine($"\t{field.Key}: {field.Value}");
                        }
                    }
                }
                tbInboxDescription.Text = info.ToString();
            }
        }
    }
}
