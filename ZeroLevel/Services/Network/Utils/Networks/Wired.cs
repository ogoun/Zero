using System;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace ZeroLevel.Services.Network.Utils.Networks
{
    /// <summary>
    /// Wired Network.
    /// </summary>
    public static class Wired
	{
		/// <summary>
		/// Gets the interface name.
		/// </summary>
		/// <value>The interface name.</value>
		public static string Name
		{
			get
			{
				try
				{
					foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
					{
						if (item.NetworkInterfaceType == NetworkInterfaceType.Ethernet && item.OperationalStatus == OperationalStatus.Up)
						{
							return item.Name;
						}
					}

					return "";
				}
				catch (Exception e)
				{
					Console.WriteLine("Error: " + e.Message);
					return "";
				}
			}
		}

		/// <summary>
		/// Gets a value indicating if wired network is up.
		/// </summary>
		/// <value><c>true</c> if network is up; otherwise, <c>false</c>.</value>
		public static bool IsUp
		{
			get
			{
				try
				{
					foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
					{
						if (item.NetworkInterfaceType == NetworkInterfaceType.Ethernet && item.OperationalStatus == OperationalStatus.Up)
						{
							return true;
						}
					}

					return false;
				}
				catch (Exception e)
				{
					Console.WriteLine("Error: " + e.Message);
					return false;
				}
			}
		}

		/// <summary>
		/// Gets the IP address.
		/// </summary>
		/// <value>The IP address.</value>
		public static string IPAddress
		{
			get
			{
				try
				{
					string output = "";

					foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
					{
						if (item.NetworkInterfaceType == NetworkInterfaceType.Ethernet && item.OperationalStatus == OperationalStatus.Up)
						{
							foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
							{
								if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
								{
									output = ip.Address.ToString();
								}
							}
						}
					}

					return output;
				}
				catch (Exception e)
				{
					Console.WriteLine("Error: {0}", e.Message);
					return "";
				}
			}
		}
	}
}
