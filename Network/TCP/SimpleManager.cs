using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace P2P.TcpConnection
{
    public class SenderFixManager : ITcpClientManager {

		IPEndPoint senderEP = null;

		public SenderFixManager(IPEndPoint senderEP) {
			this.senderEP = senderEP;
			clientPoolDictionary = new Dictionary<IPEndPoint, TcpClient>();
		}

		Dictionary<IPEndPoint, TcpClient> clientPoolDictionary = null;

		public delegate void RefDictionary<T, S>(Dictionary<T, S> dic);
		private RefDictionary<IPEndPoint, TcpClient> removePolicy = null;

		private void RandomRemove(Dictionary<IPEndPoint, TcpClient> dic) {
			XorShift random = new XorShift((uint)DateTime.Now.Millisecond);
			int      i      = random.NextInt(dic.Count);

			RemoveIndex(dic, i);
		}

		private void RemoveIndex(Dictionary<IPEndPoint, TcpClient> dic, int i) {
			foreach (var key in dic.Keys) {
				i--;
				if (i == 0) {
					dic.Remove(key);
				}
			}
		}
		#region ITcpClientManager メンバ

		public TcpClient Get(IPEndPoint ipep, object o) {
			lock (this) {
				TcpClient client;
				IPEndPoint localEP = senderEP;
				bool b = clientPoolDictionary.TryGetValue(ipep, out client);
				if (b) {
					if (!client.Connected) {
						client.Connect(ipep);
					}
					return client;
				}
				else {
					localEP.Port = 0;
					client = new TcpClient(localEP);
					client.Connect(ipep);
					//clientPoolDictionary.Add(ipep, client);
					return client;
				}
			}
		}

		public void Put(TcpClient client, object o) {

			try {
				this.clientPoolDictionary.Add((IPEndPoint)client.Client.RemoteEndPoint, client);
			}
			catch (ArgumentException) {
				//すでにキーがある場合
				//この場合プールが1つ少なくなってしまう場合があるがまぁいいか
			}
		}


		public void Remove(TcpClient client, object o) {
			client.Close();
			this.clientPoolDictionary.Remove((IPEndPoint)client.Client.RemoteEndPoint);
		}

		#endregion
	}


	public class SimpleManager : ITcpClientManager {

		public SimpleManager() {
			clientPoolDictionary = new Dictionary<IPEndPoint, TcpClient>();
		}

		Dictionary<IPEndPoint, TcpClient> clientPoolDictionary = null;

		public delegate void RefDictionary<T, S>(Dictionary<T, S> dic);
		private RefDictionary<IPEndPoint, TcpClient> removePolicy = null;

		private void RandomRemove(Dictionary<IPEndPoint, TcpClient> dic) {
			XorShift random = new XorShift((uint)DateTime.Now.Millisecond);
			int      i      = random.NextInt(dic.Count);
			RemoveIndex(dic, i);
		}

		private void RemoveIndex(Dictionary<IPEndPoint, TcpClient> dic, int i) {
			foreach (var key in dic.Keys) {
				i--;
				if (i == 0) {
					dic.Remove(key);
				}
			}
		}
		#region ITcpClientManager メンバ

		public TcpClient Get(IPEndPoint ipep, object o) {
			TcpClient client;
			bool b = clientPoolDictionary.TryGetValue(ipep, out client);
			if (b) {
				return client;
			}
			else {
				client = new TcpClient();
				client.Connect(ipep);
				return client;
			}
		}

		public TcpClient Get(IPEndPoint ipep, IPEndPoint localEP,object o) {
			TcpClient client;
			bool b = clientPoolDictionary.TryGetValue(ipep, out client);
			if (b) {
				return client;
			}
			else {
				client = new TcpClient(localEP);
				client.Connect(ipep);
				return client;
			}
		}

		public void Put(TcpClient client, object o) {

			try {
				this.clientPoolDictionary.Add((IPEndPoint)client.Client.RemoteEndPoint, client);
			}
			catch (ArgumentException) {
				//すでにキーがある場合
				//この場合プールが1つ少なくなってしまう場合があるがまぁいいか
			}
		}


		public void Remove(TcpClient client, object o) {
			client.Close();
			this.clientPoolDictionary.Remove((IPEndPoint)client.Client.RemoteEndPoint);
		}

		#endregion
	}
}
