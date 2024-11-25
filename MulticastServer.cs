using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace P2P
{
    static public class IPAddressHelper {
		static public bool InAddressGroup(this IPAddress address, IPAddress groupAddress) {
			var a0 = address.GetAddressBytes();
			Array.Reverse(a0, 0, a0.Length);

			var ga0 = groupAddress.GetAddressBytes();
			Array.Reverse(ga0, 0, ga0.Length);
			System.Collections.BitArray a  = new System.Collections.BitArray(a0);
			System.Collections.BitArray ga = new System.Collections.BitArray(ga0);

			int zeroCount = 0;
			Console.WriteLine();

			for (int i = ga.Length - 1; i > 0; i--) {
				int z = a[i] == true ? 1 : 0;
				Console.Write(z);
			}
			Console.WriteLine();

			for (int i = ga.Length - 1; i > 0; i--) {
				int z = ga[i] == true ? 1 : 0;
				Console.Write(z);
			}

			Console.WriteLine();
			//gaの下からどんだけ0かを探す
			for (int i = 0; i < ga.Length; i++) {
				if (ga[i] == true) {
					break;
				}
				zeroCount++;
			}

			//ゼロの分を除外してそれ以上でマッチしているかを
			for (int i = ga.Length - 1; i > zeroCount; i--) {
				if (ga[i] != a[i]) {
					return false;
				}
			}
			return true;
		}

	}

	class MulticastServer {

		IPAddress mcastAddress;
		int mcastPort;
		Socket mcastSocket;

		public MulticastServer(IPAddress mcastAddress, int port) {
			// Initialize the multicast address group and multicast port.
			// Both address and port are selected from the allowed sets as
			// defined in the related RFC documents. These are the same 
			// as the values used by the sender.
			this.mcastAddress = IPAddress.Parse("224.168.100.2");
			this.mcastPort = 11000;

			CreateMulticastSocket();
		}

		public void CreateMulticastSocket() {
			try {
				// Create a multicast socket.
				mcastSocket = new Socket(AddressFamily.InterNetwork,
										 SocketType.Dgram,
										 ProtocolType.Udp);

				// Get the local IP address used by the listener and the sender to
				// exchange multicast messages. 
				Console.Write("\nEnter local IPAddress for sending multicast packets: ");
				var host  = Dns.GetHostName();
				var entry = Dns.GetHostAddresses(host);

				IPAddress myGroup     = IPAddress.Parse("192.168.0.0");
				IPAddress localIPAddr = entry[0];//IPAddress.Parse(Console.ReadLine());
				foreach (var e in entry) {
					if (e.InAddressGroup(myGroup)) {
						localIPAddr = e;
						break;
					}
				}
				
				// Create an IPEndPoint object. 
				IPEndPoint IPlocal = new IPEndPoint(localIPAddr, 0);

				// Bind this endpoint to the multicast socket.
				mcastSocket.Bind(IPlocal);

				// Define a MulticastOption object specifying the multicast group 
				// address and the local IP address.
				// The multicast group address is the same as the address used by the listener.
				MulticastOption mcastOption;
				mcastOption = new MulticastOption(mcastAddress, localIPAddr);

				mcastSocket.SetSocketOption(SocketOptionLevel.IP,
											SocketOptionName.AddMembership,
											mcastOption);

				mcastSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 10);

			}
			catch (Exception e) {
				Console.WriteLine("\n" + e.ToString());
			}
		}

		public void CloseSocket() {
			mcastSocket.Close();
		}

		public int PacketDivideSize {
			get {
				return mcastSocket.SendBufferSize;
			}
		}

		public void MulticastMessage(MulticastPacket mPacket) {
			IPEndPoint endPoint;
			endPoint = new IPEndPoint(mcastAddress, mcastPort);

			var divs = mPacket.MakeDividedPacket(mcastSocket.SendBufferSize);

			try {
				for (int i = 0; i < divs.Length; i++) {
					//Send multicast packets to the listener.
					mcastSocket.SendTo(divs[i].ToByteArray(), endPoint);
					Thread.Sleep(3);//ない方がいいかも

					//二重で送る
					mcastSocket.SendTo(divs[divs.Length - i - 1].ToByteArray(), endPoint);
					Thread.Sleep(3);//ない方がいいかも
				}
			}
			catch (Exception e) {

			}
		}
	}
}
