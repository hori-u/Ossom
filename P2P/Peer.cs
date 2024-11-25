using P2P.BBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;

namespace P2P
{
    public class Peer {
		private PeerConfig  config = null;
		private PeerSystem  ps     = null;
		private PeerAction  pa     = null;
		private PeerHandler ph     = null;
		private PeerService pse    = null;

		private PeerFileManager manager    = null;
		public  PeerDownloader  downloader = null;


		public PeerFileManager FManager {
			get { return manager; }
			set { manager = value; }
		}

		public PeerDownloader Download {
			get { return downloader; }
			set { downloader = value; }
		}

		public PeerSystem PS {
			get { return ps; }
		}

		public PeerFileManager PFM {
			get { return manager; }
		}

		public PeerDownloader PD {
			get { return downloader; }
		}

		public PeerAction PA {
			get { return pa; }
		}

		private bool   nodeServerMode = false;
		private Thread serverThread   = null;
		//private DataReceiveService drs = null;
		//private XMLRPCServer server = null;

		private TcpConnection.TcpMessageReceiver server2 = null;

		Thread broadThread = null;

		public bool isNodeServer {
			get {return nodeServerMode;}
			set {nodeServerMode = value;}
		}

		public Peer(PeerConfig config) {
			this.config = config;
		}

		public Peer(PeerConfig config, PeerService service, PeerAction action, PeerHandler handler, PeerFileManager pfm) {
			this.config  = config;
			this.manager = pfm;
			this.ph      = handler;
			this.pse     = service;
			this.pa      = action;
		}

		public void Start(int num) {
			//Peerの必要クラス
			ps = new PeerSystem(config);//内部データ

			if (manager == null) {
				manager = new PeerFileManager(ps, config);//ファイル管理
			}

			IPAddress localIP = null;
			var ips = Dns.GetHostAddresses(Dns.GetHostName());
			foreach (var ip in ips) {
				if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) {
					string ipStr = ip.ToString();

					if (ipStr.StartsWith("192.168")) {
						localIP = ip;
						if (num == 0) {
							break;
						}
						num--;
					}
				}
			}

			IPEndPoint localEP = new IPEndPoint(localIP, config.WPPSSendPort);
			TcpConnection.ITcpClientManager tcpManager = new TcpConnection.SenderFixManager(localEP);

			if (ph == null) {
				ph = new PeerHandler(ps, manager, null);
			}

			PeerSender sender = new PeerSender(ph, new TcpConnection.TcpMessageSender(tcpManager), ps.MyNodeInfo.BandWidth);

			if (pse == null) {
				pse = new PeerService(ps, manager, config, sender);//受信というか公開するサービス
			}

			if (pa == null) {
				pa = new PeerAction(ps, config, sender);//送信
			}

			pse.HookHandle(ph);//関数をフックしておく

			downloader = new PeerDownloader(ps, pa, manager);//ダウンローダー

			List<FileDataInfo> fileDataList = new List<FileDataInfo>();
			try {
				//fileDataList = (List<FileDataInfo>)DataStorage.Load("FileDataList.dat");
				//ローカルからデータを読み込んだら
				//manager.AddFileDataList(fileDataList);
				//ps.SetContentList(fileDataList);
			}
			catch (FileNotFoundException e) {
				Console.WriteLine(e.ToString());
				PeerLogger.Instance.OnMassage("FileDataList.datがないよ");
			}

			List<FileDataInfo> fileList = fileDataList;
			CacheCreator.Createcache(config.UploadPath, config.CachePath, ref fileList);
			manager.AddFileDataList(fileList);
			ps.SetContentList(fileList);

			foreach (var c in ps.ContentDictionary.Keys) {
				try {
					var tagfileName = Path.Combine(config.UploadPath, Path.GetFileNameWithoutExtension(c.Name) + ".xml");
					var cm = DataType.ContentMetaData.LoadFile(c.BaseHash, tagfileName);
					//pa.PutContentMetaData(cm);

					var thumbnailfileName = Path.Combine(config.UploadPath, Path.GetFileNameWithoutExtension(c.Name) + ".jpg");
					DataSegment ds = new DataSegment(c.BaseHash.ByteData, 0, File.ReadAllBytes(thumbnailfileName));
					//pa.PutThumbnail(ds);
					ps.TagDictionary.Add(c.BaseHash, cm);
				}
				catch (Exception e) {
					Console.WriteLine(e.ToString());
					PeerLogger.Instance.OnMassage(e.Message);
				}
			}
			//////////////


			//XML-RPCサーバーの作成
			//drs = new DataReceiveService(ph);

			server2 = new P2P.TcpConnection.TcpMessageReceiver(localIP, config.Port, tcpManager);
			server2.MessageReceive += new P2P.TcpConnection.TcpMessageReceiver.ReceiveMessageHandler(server2_MessageReceive);

			serverThread = new Thread(server2.Start);
			serverThread.IsBackground = true;
			serverThread.Start();
			//this.Process();
			//server.Start();

			if (!nodeServerMode) {
				//ノードとコンテンツを登録
				pa.RegMyNode();
				pa.RegMyContent();

				//ノードリストとかを取得
				pa.ReplaceNodeList();
				pa.ReplaceContentDictionary();

				//Peer用のプロセス
				Process();
			}

			DataStorage.Save("FileDataList.dat", manager.GetFileDataList());

			downloader.Start();

			//ps.ReadDB();

			PeerLogger.Instance.OnMassage("Peer開始");
		}

		void server2_MessageReceive(object sender, P2P.Network.ReceiveMessageEventArgs rmea) {
			ph.Handle(rmea);
		}

		public void Stop(){
			DataStorage.Save("FileDataList.dat", manager.GetFileDataList());
			if(broadThread != null) broadThread.Abort();
			downloader.Stop();
			manager.Dispose();

			if (serverTimer != null) {
				serverTimer.Stop();
			}
			
			serverThread.Abort();
			server2.Stop();

			//ps.WriteDB();
			PeerLogger.Instance.OnMassage("peer停止");
		}

		System.Timers.Timer serverTimer = null;

		private void Process() {
			serverTimer = new System.Timers.Timer(5000);//5秒単位で発生
			serverTimer.Elapsed += new System.Timers.ElapsedEventHandler(serverTimer_Elapsed);
			serverTimer.Start();
		}

		void serverTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
			pa.ReplaceContentDictionary();
			pa.ReplaceNodeList();
		}
	}
}