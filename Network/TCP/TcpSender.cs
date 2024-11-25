using System;
using System.Net;
using System.Net.Sockets;

namespace P2P.TcpConnection
{
    public class TcpMessageSender : Network.IMessageSender {

		private P2P.TcpConnection.ITcpClientManager manager = null;
		public TcpMessageSender(P2P.TcpConnection.ITcpClientManager manager) {
			this.manager = manager;
		}

		#region IMessageSender メンバ

		public Network.SendResult Send(IPEndPoint ipep, byte[] data) {
			try {
				var client = manager.Get(ipep, null);			
				var ns     = client.GetStream();
				NetworkIO.SendStream(ns, data);

				ns.Close();
				client.Close();

				return new Network.SendResult(new Network.ClientStatus(ipep), Network.ConnectionResult.OK);
			}
			catch (SocketException se) {
				throw se;
				//return new Network.SendResult(new Network.ClientStatus(ipep), Network.ConnectionResult.TimeOut);	
			}
			catch (ObjectDisposedException ode) {
				throw ode;
				//return new Network.SendResult(new Network.ClientStatus(ipep), Network.ConnectionResult.TimeOut);	
			}
			catch (Exception e) {
				throw e;
				//return new Network.SendResult(new Network.ClientStatus(ipep), Network.ConnectionResult.TimeOut);
			}
		}

		public Network.SendResult Send(TcpClient client, byte[] data) {
			try {
				var ipep = (IPEndPoint)client.Client.RemoteEndPoint;
				var ns   = client.GetStream();
				NetworkIO.SendStream(ns, data);
				ns.Close();
				client.Close();

				return new Network.SendResult(new Network.ClientStatus(ipep), Network.ConnectionResult.OK);
			}
			catch (SocketException se) {
				throw se;
				//return new Network.SendResult(new Network.ClientStatus(ipep), Network.ConnectionResult.TimeOut);
			}
			catch (ObjectDisposedException ode) {
				throw ode;
				//return new Network.SendResult(new Network.ClientStatus(ipep), Network.ConnectionResult.TimeOut);
			}
			catch (Exception e) {
				throw e;
				//return new Network.SendResult(new Network.ClientStatus(ipep), Network.ConnectionResult.TimeOut);
			}
			return null;
		}

		public Network.ReceiveMessageEventArgs SendAndReceive(TcpClient client, byte[] data) {
			try {
				var ipep = (IPEndPoint)client.Client.RemoteEndPoint;
				var ns   = client.GetStream();
				System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

				NetworkIO.SendStream(ns, data);

				//System.Threading.Thread.Sleep(300);
				//データを送ってから同じクライアントで受信待機
				var receive = NetworkIO.ReadStream(ns);
				//System.Threading.Thread.Sleep(300);
				ns.Close();
				client.Close();

				return new Network.ReceiveMessageEventArgs(receive, new Network.ClientStatus(ipep), Network.ConnectionResult.OK);
			}
			catch (SocketException se) {
				throw se;
				//return new Network.SendResult(new Network.ClientStatus(ipep), Network.ConnectionResult.TimeOut);
			}
			catch (ObjectDisposedException ode) {
				throw ode;
				//return new Network.SendResult(new Network.ClientStatus(ipep), Network.ConnectionResult.TimeOut);
			}
			catch (Exception e) {
				throw e;
				//return new Network.SendResult(new Network.ClientStatus(ipep), Network.ConnectionResult.TimeOut);
			}
			return null;
		}

		public Network.ReceiveMessageEventArgs SendAndReceive(IPEndPoint ipep, byte[] data) {
			try {
				var client = manager.Get(ipep,null);
                var ns     = client.GetStream();
				NetworkIO.SendStream(ns, data);
				//データを送ってから同じクライアントで受信待機
				var receive = NetworkIO.ReadStream(ns);

				ns.Close();
				client.Close();

				return new Network.ReceiveMessageEventArgs(receive, new Network.ClientStatus(ipep), Network.ConnectionResult.OK);
			}
			catch (SocketException se) {
				throw se;
				//return new Network.SendResult(new Network.ClientStatus(ipep), Network.ConnectionResult.TimeOut);
			}
			catch (ObjectDisposedException ode) {
				throw ode;
				//return new Network.SendResult(new Network.ClientStatus(ipep), Network.ConnectionResult.TimeOut);
			}
			catch (Exception e) {
				throw e;
				//return new Network.SendResult(new Network.ClientStatus(ipep), Network.ConnectionResult.TimeOut);
			}
			return null;
		}

		#endregion

		#region IMessageSender メンバ




		#endregion
	}
}
