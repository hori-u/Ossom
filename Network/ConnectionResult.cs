using System;
using System.Net;

namespace P2P.Network
{
    public enum ConnectionResult {
		OK,
		TimeOut,
		DNSError,
		Disconnect,
		Unknown,
	}

	public class SendResult {
		public ClientStatus ReceiverStatus = null;
		public ConnectionResult Result = ConnectionResult.Unknown;

		public SendResult(ClientStatus cs, ConnectionResult result) {
			this.ReceiverStatus = cs;
			this.Result = result;
		}

	}

	public class ReceiveMessageEventArgs : EventArgs {
		public byte[] ReceiveData = null;
		public ClientStatus SenderStatus = null;
		public ConnectionResult Result = ConnectionResult.Unknown;

		public ReceiveMessageEventArgs(byte[] data, ClientStatus status, ConnectionResult result) {
			this.ReceiveData = data;
			this.SenderStatus = status;
			this.Result = result;
		}
	}

	public class ClientStatus : IEquatable<ClientStatus> {

		private DateTime beginConnectionTime = DateTime.Now;
		private DateTime endConnectionTime = DateTime.Now;

		public DateTime BeginConnectionTime {
			get { return beginConnectionTime; }
		}

		public DateTime EndConnectionTime {
			get { return endConnectionTime; }
		}

		public TimeSpan ConnectionTimeElapse {
			get { return endConnectionTime - beginConnectionTime; }
		}

		private IPEndPoint ipep = null;
		public IPEndPoint IPEP {
			get { return ipep; }
		}

		public IPAddress ConnectedAddress {
			get { return ipep.Address; }
		}

		public int Port {
			get { return ipep.Port; }
		}

		public ClientStatus(IPEndPoint ipep) {
			this.ipep = ipep;
			TimerStart();
		}

		public void TimerStart() {
			beginConnectionTime = DateTime.Now;
		}

		public void TimerEnd() {
			endConnectionTime = DateTime.Now;
		}
		#region IEquatable<ClientStatus> メンバ

		public bool Equals(ClientStatus other) {
			return this.ipep.Equals(other.ipep);
		}

		#endregion
	}
}
