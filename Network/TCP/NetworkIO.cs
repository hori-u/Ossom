using System;
using System.IO;
using System.Net.Sockets;

namespace P2P.TcpConnection
{
    /// <summary>
    /// 4バイト(int型)のデータサイズ＋そのサイズの実データを
    /// 送受信するためのメソッド
    /// 例外はキャッチしない
    /// </summary>
    public class NetworkIO {
		static public byte[] ReadStream(NetworkStream ns) {
			byte[] readSizeBuffer = new byte[4];//int型
			ns.Read(readSizeBuffer, 0, readSizeBuffer.Length);

			int readSize    = BitConverter.ToInt32(readSizeBuffer, 0);
			MemoryStream ms = new MemoryStream(readSize);

			while (true) {
				byte[] dataBuffer = new byte[8192];
				int read          = ns.Read(dataBuffer, 0, dataBuffer.Length);
				ms.Write(dataBuffer, 0, read);

				if (ms.Length >= readSize) {
					return ms.ToArray();
				}
			}
		}

		static public void SendStream(NetworkStream ns, byte[] data) {
			byte[] sendSizeBuffer = BitConverter.GetBytes(data.Length);
			ns.Write(sendSizeBuffer, 0, sendSizeBuffer.Length);
			ns.Write(data, 0, data.Length);
		}
	}
}
