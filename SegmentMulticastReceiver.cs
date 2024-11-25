using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace P2P
{
    public class SegmentMulticastReceiver {
		MulticastClient mc          = null;	
		PeerFileManager fileManager = null;
		PeerDownloader  downloader  = null;
        PeerSystem system = null;
        PeerAction action = null;

        bool isHybridMode = false;
        private readonly PeerDownloader pd;
        private int p2pSeg;

        public bool IsHybridMode {
			get { return isHybridMode; }
			set { isHybridMode = value; }
		}

		public SegmentMulticastReceiver(PeerFileManager pfm, PeerSystem ps,PeerAction pa,PeerDownloader pd) {

			mc = new MulticastClient(IPAddress.Parse("224.168.100.2"), 11000);
            fileManager = pfm;
            downloader  = pd;
            action      = pa;
            system      = ps;
		}

		public void StartReceive(P2P.BBase.ContentInfoBase cib) {

			fileManager.AddFileData(cib);
			var fdi = fileManager.GetFileData(cib.BaseHash);
			List<MulticastPacket> packetList = new List<MulticastPacket>(fdi.BlockCount);

			for(int count = 0;count < fdi.BlockCount;count++){
				var mp = new MulticastPacket(count) ;
				packetList.Add(mp);		
			}

			List<string> segNumList = new List<string>();
			List<int>    intList    = new List<int>();
            bool hybridMode = false;
            bool init       = false;
            downloader = pd;

            action.ReplaceContentDictionary();
			if (system.ContentDictionary[fdi].Count == 0) {
				hybridMode = false;
			}
			else {
				hybridMode = true;
			}

			hybridMode = isHybridMode;

			while (true) {

				var bytes = mc.ReceiveMulticastMessages();

				MulticastDividedPacket mdp = new MulticastDividedPacket();
				mdp.Create(bytes);

				packetList[mdp.SegmentNumber].PutDividedPacket(mdp);

				if (packetList[mdp.SegmentNumber].IsSegmentComplete()) {

					if (hybridMode) {
						//P2Pを開始
						if (!init) {
							//mdp.SegmentNumberは中途半端なはずだからここまでP2Pでとってくる
							downloader.AddDownload(fdi, mdp.SegmentNumber);
							p2pSeg = mdp.SegmentNumber;
							init = true;
						}
					}

					BBase.DataSegment ds = new P2P.BBase.DataSegment();
					ds.ByteDeserialize(packetList[mdp.SegmentNumber].GetRawData());
					if (!intList.Contains(ds.SegmentNumber)) {

						
						fileManager.WriteFile(fdi.BaseHash, ds) ;

					
						segNumList.Add(DateTime.Now.ToLongTimeString() + " " + ds.SegmentNumber.ToString());
						intList.Add(ds.SegmentNumber);
					}
				}


				if (hybridMode) {
					bool complete = true;
					for (int i = p2pSeg; i < packetList.Count; i++) {
						complete &= packetList[i].IsSegmentComplete();
					}
					//最後のパケットが完了したら抜ける．
					if (complete) {
						mc.CloseSocket();

						string s = string.Empty;

						s += "P2P " + p2pSeg.ToString() + "\n";
						segNumList.ForEach((string num) => {
							s += num + "\n";
						});


						System.IO.File.WriteAllText("segmentNumber.txt", s, Encoding.UTF8);


						System.Windows.Forms.MessageBox.Show("マルチキャスト完了");
						break;
					}

				}
				else {
					//すべてかんりょうしたら抜ける
					bool allComplete = true;
					packetList.ForEach((MulticastPacket mp) => {
						allComplete &= mp.IsSegmentComplete();
					});

					if (allComplete) {
						mc.CloseSocket();

						string s = string.Empty;


						segNumList.ForEach((string num) => {
							s += num + "\n";
						});

						System.IO.File.WriteAllText("segmentNumber.txt", s, Encoding.UTF8);

						system.AddContent(new KeyValuePair<P2P.BBase.ContentInfoBase, P2P.BBase.Node>(fdi, system.MyNodeInfo));
						action.RegMyContent();

						System.Windows.Forms.MessageBox.Show("受信完了");
						break;
					}
				}

			}

		}

		public void EndReceive() {


		}
	}
}
