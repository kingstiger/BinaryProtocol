using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace TSLAB7SERVER_UDP
{
	class Host
	{
		public IPEndPoint Endpoint;
		private string SessionId;

		public Host(IPEndPoint endpoint)
		{
			this.Endpoint = endpoint;
			RandomSessionId();
		}

		public void RandomSessionId()
		{
		Random random = new Random();
		
		const string chars = "ABCDEFGHIJKLMNOPRSTUVWXYZ";
		this.SessionId = new string(Enumerable.Repeat(chars, 3)
		.Select(s => s[random.Next(s.Length)]).ToArray());
		}

		public string GetSessionId()
		{
			return SessionId;
		}

		// IPEndPoint endpoint1 = new IPEndPoint(IPAddress.Any, 8080);  //Serwer działa na porcie 8080
		//IPEndPoint endpoint2 = new IPEndPoint(IPAddress.Any, 8080);
	}
}
