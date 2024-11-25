using System;
using System.Net;
using System.Net.Sockets;

namespace P2P
{
    public class MulticastClient {

		private IPAddress mcastAddress;
		private int mcastPort;
		private Socket mcastSocket;
		private MulticastOption mcastOption;

		public MulticastClient(IPAddress mcastAddress,int port) {
			// Initialize the multicast address group and multicast port.
			// Both address and port are selected from the allowed sets as
			// defined in the related RFC documents. These are the same 
			// as the values used by the sender.
			this.mcastAddress = IPAddress.Parse("224.168.100.2");
			this.mcastPort = 11000;

			CreateMulticastSocket();
		}

		public void MulticastOptionProperties() {
			Console.WriteLine("Current multicast group is: " + mcastOption.Group);
			Console.WriteLine("Current multicast local address is: " + mcastOption.LocalAddress);
		}


		public void CreateMulticastSocket() {

			try {
				mcastSocket = new Socket(AddressFamily.InterNetwork,
										 SocketType.Dgram,
										 ProtocolType.Udp);

				Console.Write("Enter the local IP address: ");
				var host = Dns.GetHostName();
				var entry = Dns.GetHostAddresses(host);

				IPAddress myGroup = IPAddress.Parse("192.168.0.0");
				
				IPAddress localIPAddr = entry[0];//IPAddress.Parse(Console.ReadLine());
				foreach (var e in entry) {
					if (e.InAddressGroup(myGroup)) {
						localIPAddr = e;
						break;
					}
				}
				//IPAddress localIPAddr = IPAddress.Parse(Console.ReadLine());

				//IPAddress localIP = IPAddress.Any;
				//EndPoint localEP = (EndPoint)new IPEndPoint(localIPAddr, mcastPort);
				EndPoint localEP = (EndPoint)new IPEndPoint(IPAddress.Any, mcastPort);

				mcastSocket.Bind(localEP);


				// Define a MulticastOption object specifying the multicast group 
				// address and the local IPAddress.
				// The multicast group address is the same as the address used by the server.
				mcastOption = new MulticastOption(mcastAddress, localIPAddr);

				mcastSocket.SetSocketOption(SocketOptionLevel.IP,
											SocketOptionName.AddMembership,
											mcastOption);
			}

			catch (Exception e) {
				Console.WriteLine(e.ToString());
			}
		}

		public void CloseSocket() {
			mcastSocket.Close();
		}

		public int PacketDevideSize {
			get {
				return mcastSocket.SendBufferSize;
			}
		}

		public byte[] ReceiveMulticastMessages() {
			int size = 0;
			byte[] bytes = new Byte[mcastSocket.ReceiveBufferSize];
			IPEndPoint groupEP = new IPEndPoint(mcastAddress, mcastPort);
			EndPoint remoteEP = (EndPoint)new IPEndPoint(IPAddress.Any, 0);

			try {
				size = mcastSocket.ReceiveFrom(bytes, ref remoteEP);
				if(size != bytes.Length){
					byte[] temp = new byte[size];
					Array.Copy(bytes,temp,temp.Length);
					bytes = temp;
				}
				return bytes;
			}
			catch (Exception e) {
				Console.WriteLine(e.ToString());
			}
			return null;
		}
		public void Start() {

			// Start a multicast group.
			CreateMulticastSocket();

			// Display MulticastOption properties.
			MulticastOptionProperties();

			// Receive broadcast messages.
			ReceiveMulticastMessages();
		}
	}
}
