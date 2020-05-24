using System;
using System.IO;
using System.Net;

namespace ZeroLevel.Services.Network.Utils
{
	/// <summary>
	/// Methods related to Network.
	/// </summary>
	public static class Network
	{
		/// <summary>
		/// Gets the external IP Address.
		/// </summary>
		/// <value>The external IP Address.</value>
		public static string ExternalIP
		{
			get
			{
				try
				{
					WebRequest request = WebRequest.Create("http://ipv4.icanhazip.com");
					using (var response = request.GetResponse())
					using (var sr = new StreamReader(response.GetResponseStream()))
					{
						return sr.ReadLine();
					}
				}
				catch (Exception e)
				{
					Console.WriteLine("Error: " + e.Message);
					return "";
				}
			}
		}
	}
}
