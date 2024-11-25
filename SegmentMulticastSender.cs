using System;
using System.Net;
using System.Threading;

namespace P2P
{
    public class SegmentMulticastSender {
		MulticastServer ms;
		PeerFileManager fileManager = null;
		Hash fileHash = null;

		public SegmentMulticastSender(PeerFileManager pfm,Hash hash) {
			ms = new MulticastServer(IPAddress.Parse("224.168.100.2"), 11000);
            fileManager = pfm;
            fileHash    = hash;
		}
		public void SetFile() {

		}
        
		public void MulticastProcess() {
            var fileinfo = fileManager.GetFileData(fileHash);
            int segmentNum = fileinfo.FileSize / (128 * 1024) + 1;
            int count      = 0;

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            TimeSpan wait = new TimeSpan(0, 0, 0, 0, (int)(60.0 * 1000 / segmentNum));
			wait -= new TimeSpan(0, 0, 0, 0, 100);//ちょっと早めに送る

			sw.Start();

			//同じ動画を繰り返し放送する
			while (true) {

                int segmentIndex = count % fileinfo.BlockCount;
                var start        = sw.Elapsed;
				var segment      = fileManager.ReadFile(fileHash, segmentIndex);
				var bytes        = segment.ByteSerialize();

				MulticastPacket mp = new MulticastPacket(segmentIndex, bytes);

				ms.MulticastMessage(mp);
				count++;
				var now = sw.Elapsed;
				
				//指定時間まで待つ
				while ((now - start) < wait) {
					ms.MulticastMessage(mp);
					now = sw.Elapsed;
					/* System.Threading.Thread.Sleep(10); */
				}
			}
		}
		public void MulticastStart(){
            Thread t = new Thread(this.MulticastProcess);
			t.IsBackground = true;
			t.Start();
        }
	}
}
