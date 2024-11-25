using System;
using System.Collections.Generic;
using System.Threading;
using P2P.BBase;

namespace P2P
{
    public class BroadCastSystem {
		private LinkedList<Node> peerList = null;

		public BroadCastSystem() {
			peerList = new LinkedList<Node>();
		}

		public LinkedList<Node> GetPeerList() {
			return peerList;
		}

		public void AddNode(Node node) {
			if (!peerList.Contains(node)) {
				peerList.AddLast(node);
			}
		}

		public void RemoveNode(Node node) {
			peerList.Remove(node);
		}
	}

	public class BroadCastData {
		private FileDataInfo fileData = null;

		private int sendTimeSpan = 0;

		private int count = 0;

		public BroadCastData(FileDataInfo fdi, int timeSpan) {
			this.fileData = fdi;
			sendTimeSpan = timeSpan;
		}

		public FileDataInfo FileData {
			get { return fileData; }
		}

		public int MaxCount {
			get { return fileData.BlockCount; }
		}

		public int TimeSpan {
			get { return sendTimeSpan; }
		}

		public void Reset() {
			count = 0;
		}

		public int GetCount() {
			return count;
		}

		public void Next() {
			count++;
		}

		public int GetAndNextCount() {
			return count++;//カウントを返してから1プラスのはず
		}
	}

	public class BroadCastMethod {
		private PeerFileManager fileManager = null;
		private BroadCastSystem system = null;
		private PeerAction action = null;
		private BroadCastData data = null;

		public BroadCastMethod(BroadCastSystem bcs, BroadCastData bcd, PeerAction pa, PeerFileManager pfm) {
			this.fileManager = pfm;
			this.action = pa;
			this.system = bcs;
			this.data = bcd;
		}

		public void BroadCastProcess() {
			for (int i = 0; i < data.MaxCount; i++) {
				BroadCast(data.GetAndNextCount());
				Thread.Sleep(data.TimeSpan);
			}
		}

		public void BroadCast(int count) {
			FileDataInfo fdi = data.FileData;
			DataSegment ds = fileManager.ReadFile(fdi.BaseHash, count);

			var peerList = system.GetPeerList();
			foreach (Node n in peerList) {
				SendMethod sm = new SendMethod(n, ds, this.action);

				Thread t = new Thread(sm.Send);
				t.IsBackground = true;
				t.Start();
			}
		}

		private class SendMethod {
			public Node n = null;
			public DataSegment ds = null;
			public PeerAction action = null;

			public SendMethod(Node n, DataSegment ds, PeerAction pa) {
				this.n = n;
				this.ds = ds;
				this.action = pa;
			}

			public void Send(){
				try {
					action.PutDataSegment(n, ds);
				}
				catch (Exception e) {
					Console.WriteLine(e.Message);
				}
			}
    	}

		public void RegNode(Node n) {
			system.AddNode(n);
		}

		public void RemoveNode(Node n) {
			system.RemoveNode(n);
		}
	}
}
