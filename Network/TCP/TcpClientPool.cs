using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace P2P.TcpConnection
{
    public class ConnectionPool : ITcpClientManager {
		Dictionary<IPEndPoint, TcpClient> clientPoolDictionary = null;
		int poolLimitSize = 0;

		public delegate void RefDictionary<T, S>(Dictionary<T, S> dic);
		private RefDictionary<IPEndPoint, TcpClient> removePolicy = null;

		public ConnectionPool(int poolSize) {
			this.poolLimitSize = poolSize;

			clientPoolDictionary = new Dictionary<IPEndPoint, TcpClient>(poolLimitSize);

			removePolicy = this.RandomRemove;

		}

		private void RandomRemove(Dictionary<IPEndPoint, TcpClient> dic) {
			XorShift random = new XorShift((uint)DateTime.Now.Millisecond);
			int i = random.NextInt(dic.Count);

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

		public void Put(TcpClient client, object o) {
			if (clientPoolDictionary.Count > poolLimitSize) {
				removePolicy(clientPoolDictionary);
			}
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
