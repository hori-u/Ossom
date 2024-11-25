using System.Net;
using System.Net.Sockets;

namespace P2P.Network
{
    public interface IMessageSender {
		/// <summary>
		/// 接続されたTcpClientに対して送信
		/// </summary>
		/// <param name="client"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		Network.SendResult Send(TcpClient client, byte[] data);

		/// <summary>
		/// IPEPに接続を試してみてから送信
		/// </summary>
		/// <param name="ipep"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		Network.SendResult              Send(IPEndPoint ipep, byte[] data);
		Network.ReceiveMessageEventArgs SendAndReceive(TcpClient client, byte[] data);
		Network.ReceiveMessageEventArgs SendAndReceive(IPEndPoint ipep, byte[] data);
	}
}
