using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

/*
 * Dictionaryを使うときのメモ
 * 
 * Dictionaryとかでキー比較はEqualメソッドでやってる
 * Equalメソッドがオーバライドされていない場合 object.Equalが使用され
 * インスタンスが同じかどうかで調べるのみだから
 * 基本的にはEqualはオーバライドして独自のEqualを実装する
 * DictionaryにIEqualityComparerを入れてもいいけど
 * これは通常とは違うEqualを実装したいときにやるためで基本的にはEqualのオーバライドで行う
 */

/*
 * 1.   「内容が同じ」を表したいときは、まず Equals メソッドをオーバーライドする。
 * 2. そのクラスの型の値を引数にとる Equals も一緒に作る。パフォーマンス向上につながる*1。
 * 3. さらに、 immutable で値型のように扱えるクラスなら、 == 演算子をオーバーライドする。
 * 
 * 値型っぽく使う時は==もオーバライドする
 * ==は基本的には参照のイコール
 * 
 * == は主にスタックメモリの直接比較を行いますね
 * string型の場合は例外的に、ヒープメモリの値を直接比較しますね。
 * 参照型のデータを==で比較すると、大抵は指し示すアドレスが同じか？を比較していますね。

 * Equalsは、ヒープメモリの値を比較しますね。
 * 参照型のデータをEqualsで比較すると、中身が同じか？という比較をしますね。
 */

namespace P2P.TcpConnection
{
    public class TcpMessageReceiver {
		private IPAddress serverAddress      = null;
		private int       port               = 0;
		private int       nowConnectionCount = 0;
		private ITcpClientManager manager    = null;

		private delegate void ConnectedClientCallback(TcpClient client);
		public  delegate void ReceiveMessageHandler(object sender, Network.ReceiveMessageEventArgs rmea);
		public  event ReceiveMessageHandler MessageReceive;
		private void OnReceiveMessage(byte[] data, Network.ClientStatus status, Network.ConnectionResult result) {
			var rmea = new Network.ReceiveMessageEventArgs(data, status, result);
			OnReceiveMessage(rmea);
		}

		private void OnReceiveMessage(Network.ReceiveMessageEventArgs rmea) {
			if (MessageReceive != null) {
				MessageReceive(this, rmea);
			}
		}

		//コールバック用の大麻
		private System.Timers.Timer serverTimer = null;

		//サーバー用のリスナー
		private TcpListener listener = null;

		public TcpMessageReceiver(IPAddress address, int port,ITcpClientManager manager) {
			this.serverAddress = address;
			this.port = port;
			this.manager = manager;
		}

		private void AcceptClientCallback(IAsyncResult ar) {
			TcpListener listener = (TcpListener)ar.AsyncState;
			TcpClient   client   = listener.EndAcceptTcpClient(ar);
		
			manager.Put(client, null);
			ReceiveClient(client);
		}

		public void AddReceiveClient(TcpClient client) {
			ReceiveClient(client);
		}


		//スレッドごとに個別に呼ばれている
		private void ReceiveClient(TcpClient client) {
			try {
				using (NetworkStream ns = client.GetStream()) {
					var ipep  = (IPEndPoint)client.Client.RemoteEndPoint;
					int count = 0;
					while (true) {
						if (ns.DataAvailable) {
							//count = 0;//ここをコメントアウトすると10秒で毎回切断する
							var read = NetworkIO.ReadStream(ns);
							OnReceiveMessage(read, new Network.ClientStatus(ipep), Network.ConnectionResult.OK);
							break;
						}

						count++;
						Thread.Sleep(500);
						
						//20回以上応答がない場合
						if (count * 500 > 10000) {
							//break;
						}
					}
				}

			}
			catch (ObjectDisposedException oed) {
				//throw oed;
			}
			catch (InvalidOperationException ioe) {
				//throw ioe;
			}
			catch (Exception e) {
				//throw e;
			}
		}

		public void Start() {
			listener = new TcpListener(serverAddress, port);
			listener.Start();
			serverTimer           = new System.Timers.Timer();
			serverTimer.Interval  = 50;//適当
			serverTimer.Disposed += new EventHandler(serverTimer_Disposed);
			serverTimer.Elapsed  += new System.Timers.ElapsedEventHandler(serverTimer_Elapsed);
			serverTimer.Start();
		}

		void serverTimer_Disposed(object sender, EventArgs e) {
			listener.Stop();
		}

		public void Stop() {
			serverTimer.Stop();
			serverTimer.Dispose();
		}

		void serverTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
			lock (listener) {
				if (listener.Pending()) {
					while (listener.Pending()) {
						try {
							listener.BeginAcceptSocket(new AsyncCallback(AcceptClientCallback), listener);
						}
						catch (SocketException se) {
							//throw se;
						}
						catch (ObjectDisposedException ode) {
							//throw ode;
						}
						catch (Exception ex) {
							//throw ex;
						}
					}
				}
			}
		}
	}
}
