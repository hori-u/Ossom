using System.Net;
using System.Net.Sockets;

namespace P2P.TcpConnection
{
    /// <summary>
    /// Objectはオプション
    /// </summary>
    public interface ITcpClientManager {
		TcpClient Get(IPEndPoint ipep ,object o);
		void Put(TcpClient client, object o);
		void Remove(TcpClient client, object o);
	}
}
