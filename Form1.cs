using P2P;
using System;
using System.IO;
using System.Windows.Forms;
//

namespace Ossom
{
    public partial class BrossomForm : Form {
		public BrossomForm() {//初期化作業を行うだけ．
			InitializeComponent();
			InitializeListView();
		}
		private Peer[] peers = null;
		private WebServer.HandleWebServer web = null;
		        PeerConfig config;

		private bool isShowBallonTips = false;
		private NotifyIcon notifyIcon = null;
		private ContextMenu contextMenu;
		private MenuItem endMenuItem;
		private MenuItem openMenuItem;

		void Instance_MessagingEvent2(object sender, PeerLogger.MessageEventArgs mea) {
			try {
				FileStream fs = new FileStream("test.log", FileMode.Append, FileAccess.Write);
				StreamWriter sw = new StreamWriter(fs);
				sw.WriteLine(mea.Message);
				sw.Close();
				fs.Close();
			}
			catch (IOException) {
				System.Threading.Thread.Sleep(10);
				Instance_MessagingEvent2(sender, mea);
			}
		}

		void Instance_MessagingEvent(object sender, PeerLogger.MessageEventArgs mea) {

            notifyIcon.ShowBalloonTip(5000, Enum.GetName(mea.Level.GetType(), mea.Level),
				mea.LogTime + "\n" +
				mea.ID + "\n" +
				mea.Message + "\n",
                ToolTipIcon.Info);
		}

		private void Window_StateChanged(object sender, EventArgs e) {
			if (WindowState == FormWindowState.Minimized) {
				MinimamProcess();
			}
		}

		private void MinimamProcess() {
            Visible = false;
			notifyIcon.Visible = true;
		}

		private void RestoreProcess() {
            Visible = true;
            WindowState = FormWindowState.Normal;
            Activate();
            notifyIcon.Visible = false;
		}

		#region NotifyIcon関係のイベント

		void notifyIcon_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e) {
			//RestoreProcess();
			System.Diagnostics.Process.Start(System.IO.Path.Combine(BTool.MakeHTTPURL(config.WebServerEndPoint.IPEP), "index.html"));
		}


		void endMenuItem_Click(object sender, EventArgs e) {
			this.notifyIcon.Visible = false;
			this.Close();
		}

		void openMenuItem_Click(object sender, EventArgs e) {
			RestoreProcess();
		}

		#endregion

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
			foreach (var peer in peers) {
				peer.Stop();
			}
			web.Stop();
		}

		private void Form1_Load(object sender, EventArgs e) {

			////////////////////////////////////
			notifyIcon = new NotifyIcon();

			//アイコン読み込み
			try {
				System.Drawing.Icon ico = new System.Drawing.Icon("brossom.ico");
				notifyIcon.Icon         = ico;
			}
			catch (Exception) {
				notifyIcon.Icon = System.Drawing.SystemIcons.Application;
			}

            contextMenu  = new ContextMenu();
            endMenuItem  = new MenuItem();
            openMenuItem = new MenuItem();

            // Initialize contextMenu1
            contextMenu.MenuItems.AddRange(
						new MenuItem[] { endMenuItem, openMenuItem, });

            openMenuItem.Index  = 0;
            openMenuItem.Text   = "Open(&O)";
            openMenuItem.Click += new EventHandler(openMenuItem_Click);

            // Initialize menuItem1
            endMenuItem.Index  = 1;
            endMenuItem.Text   = "Exit(&E)";
            endMenuItem.Click += new EventHandler(endMenuItem_Click);

			//NotifyIcon設定
			notifyIcon.MouseDoubleClick += new MouseEventHandler(notifyIcon_MouseDoubleClick);
			notifyIcon.Text              = "Brossom";
			notifyIcon.ContextMenu       = this.contextMenu;

			PeerLogger.Instance.MessagingEvent += new PeerLogger.MessageEventHandler(Instance_MessagingEvent);
			PeerLogger.Instance.MessagingEvent += new PeerLogger.MessageEventHandler(Instance_MessagingEvent2);

			/////////////////////////////////////
			int peerNum = 1;

			while (true) {
				var res = MessageBox.Show("現在のピア数は" + peerNum.ToString() + "です", "起動時確認", MessageBoxButtons.OKCancel);
				if (res == DialogResult.OK) {
					peerNum++;
				}
				else {
					break;
				}
			}

			peers = new Peer[peerNum];

			//////////////////////////////////////
			bool isNodeServer = false;
			bool isMulticast  = false;
			var result = MessageBox.Show("NodeServerで起動しますか？", "起動時確認", MessageBoxButtons.OKCancel);
			if (result == DialogResult.OK) {
				isNodeServer = true;
				result = MessageBox.Show("マルチキャストしますか？", "マルチキャスト", MessageBoxButtons.OKCancel);
				if (result == DialogResult.OK) {
					isMulticast = true;
				}
			}

			string configBaseStr = "Config";

			for (int i = 0; i < peers.Length; i++) {
				string filename = configBaseStr + i.ToString() + ".xml";
				try {
					config = PeerConfig.Load(filename);
				}
				catch (FileNotFoundException) {
					config = new PeerConfig();
					config.CreateDefaultSetting(filename);
				}

				if (i == 0) {
					peers[i] = new Peer(config);
					peers[i].isNodeServer = isNodeServer;
				}
				else {
					peers[i] = new Peer(config, null, null, null, peers[0].PFM);
					peers[i].isNodeServer = false;
				}
				
				peers[i].Start(i);
			}

			int tt = 0;
			smr = new SegmentMulticastReceiver(this.peers[tt].FManager, peers[tt].PS, peers[tt].PA, peers[tt].downloader);

			WebServer.WebMethod wm  = new WebServer.WebMethod(peers[tt].PS, peers[tt].PFM, peers[tt].PD, peers[tt].PA, config, smr);
			WebServer.WebHandler wh = new WebServer.WebHandler(wm);

			web = new WebServer.HandleWebServer(BTool.MakeHTTPURL(peers[tt].PS.Config.LocalEndPoint.IPEP), config.DataPath, wh);

			web.Start();

			//マルチキャストする
			if (isMulticast) {
				var hash = new Hash("C377529196B0F1DDBC9223E84EAC6899CA85C3FA");
				sms = new SegmentMulticastSender(peers[tt].PFM, hash);

				foreach (var k in peers[tt].PS.ContentDictionary.Keys) {

					if (k.BaseHash.Equals(hash)) {
						k.Type = P2P.BBase.ContentType.Multicast;
					}
				}
				sms.MulticastStart();
			}
			//this.MinimamProcess();
		}

		SegmentMulticastReceiver smr = null;
		SegmentMulticastSender sms = null;

        public bool IsShowBallonTips {
            get{
                return isShowBallonTips;
            }
            set {
                isShowBallonTips = value;
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e) {

		}

		// ListViewコントロールを初期化します。
		private void InitializeListView() {
			//Node
			// ListViewコントロールのプロパティを設定
			nodeListView.FullRowSelect = true;
			nodeListView.GridLines = true;
			nodeListView.Sorting = SortOrder.Ascending;
			nodeListView.View = View.Details;

			// 列（コラム）ヘッダの作成
			ColumnHeader columnIPAddress  = new ColumnHeader();
			ColumnHeader columnPortNumber = new ColumnHeader();
			ColumnHeader columnNodeHash   = new ColumnHeader();
			ColumnHeader columnSpeed      = new ColumnHeader();

			columnIPAddress.Text   = "IPアドレス";
			columnIPAddress.Width  = 100;
			columnPortNumber.Text  = "ポート";
			columnPortNumber.Width = 60;
			columnNodeHash.Text    = "ハッシュ";
			columnNodeHash.Width   = 150;
			columnSpeed.Text       = "スピード";
			columnSpeed.Width      = 150;

			ColumnHeader[] colHeaderRegValue = { columnIPAddress, columnPortNumber, columnNodeHash, columnSpeed };
			nodeListView.Columns.AddRange(colHeaderRegValue);

			//content
			// ListViewコントロールのプロパティを設定
			contentListView.FullRowSelect = true;
			contentListView.GridLines     = true;
			contentListView.Sorting       = SortOrder.Ascending;
			contentListView.View          = View.Details;

			// 列（コラム）ヘッダの作成
			ColumnHeader columnName        = new ColumnHeader();
			ColumnHeader columnSize        = new ColumnHeader();
			ColumnHeader columnContentHash = new ColumnHeader();
			ColumnHeader columnHolderIP    = new ColumnHeader();

			columnName.Text         = "コンテンツ名";
			columnName.Width        = 100;
			columnSize.Text         = "データサイズ";
			columnSize.Width        = 60;
			columnContentHash.Text  = "ハッシュ";
			columnContentHash.Width = 150;
			columnHolderIP.Text     = "IP";
			columnHolderIP.Width    = 150;

			ColumnHeader[] colHeaderRegValue2 = { columnName, columnSize, columnContentHash, columnHolderIP };
			contentListView.Columns.AddRange(colHeaderRegValue2);
		}

		private void contentRefreshButton_Click(object sender, EventArgs e) {
			foreach (var peer in peers) {

				foreach (var c in peer.PS.ContentDictionary.Keys) {
					string name = c.Name;
					string size = c.FileSize.ToString();
					string hash = c.BaseHash.Str;
					string ip   = string.Empty;

					foreach (var n in peer.PS.ContentDictionary[c]) {
						ip += n.IPEP.ToString() + " ";
					}
					string[] s = { name, size, hash, ip };

					contentListView.Items.Add(new ListViewItem(s));
				}
			}
		}

		private void nodeRefreshButton_Click(object sender, EventArgs e) {

			foreach (var peer in peers) {
				foreach (var n in peer.PS.NodeList) {
					string address = n.Address.ToString();
					string port    = n.Port.ToString();
					string hash    = n.BaseHash.Str;
					string speed   = n.BaseHash.ToString();
					string[] s     = { address, port, hash, speed };

					nodeListView.Items.Add(new ListViewItem(s));
				}
			}
		
		}

		private void checkBox1_CheckedChanged(object sender, EventArgs e) {
			smr.IsHybridMode = checkBox1.Checked;
		}

		private void checkBox2_CheckedChanged(object sender, EventArgs e) {
			foreach (var peer in peers) {
				peer.PD.IsSimpleMode = checkBox2.Checked;
			}
		}

		private void button1_Click(object sender, EventArgs e) {
			int rateKbps = int.Parse(textBox1.Text);
			foreach (var peer in peers) {
				peer.PD.VideoRate = rateKbps;
			}
		}

		private void button8_Click(object sender, EventArgs e) {
			var ss = textBox2.Text.Split(' ');

			int[] node = new int[ss.Length];

			for (int i = 0; i < ss.Length; i++) {
				node[i] = int.Parse(ss[i]);
			}

			foreach (var peer in peers) {
				peer.PD.SelectNodeIndex = node;
			}
		}

		private void button13_Click(object sender, EventArgs e) {
			int offset = int.Parse(textBox3.Text);
			foreach (var peer in peers) {
				peer.PD.WppsOffset = offset;
			}
		}

        private void ipAdressTextBox_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
