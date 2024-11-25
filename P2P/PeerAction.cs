using System;
using System.Collections.Generic;
using System.Net;
using P2P.BBase;

namespace P2P {



	//IMessageSenderは基本的なメッセージの送信のみだからこのクラスでPeer用に特化させる
	public class PeerSender {
		Network.IMessageSender sender = null;
		PeerHandler handler = null;

		int dataNum = 0;
		System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
		int speed = 0;
		long time;
		int sleepTime = 0;

		public PeerSender(PeerHandler ph, Network.IMessageSender sender, int speed) {
			handler = ph;
			this.sender = sender;
			//sender = new XMLRPC.XMLRPCSender();
			this.speed = speed;

			sleepTime = 1 * 1000 / 1;

			sw.Start();
			time = sw.ElapsedMilliseconds;
		}
		public void Send(IPEndPoint ipep, Base.PeerMessage t) {
			try {
				var startTime = sw.ElapsedMilliseconds;

				byte[] ba = t.ToByteArray();
				int dataLength = ba.Length;

				int waitTime = dataLength / speed ;

				//System.Threading.Thread.Sleep(waitTime);

				var v = sender.Send(ipep, ba);
				dataNum += ba.Length;

				var endTime = sw.ElapsedMilliseconds;

				var elapse = endTime - startTime;

			}
			catch (Exception e) {
				Console.WriteLine(e.ToString());
				NetworkResultList.Instance.Enqueue(new NetworkResult(null, NetworkResult.ResultType.NotFound));
			}
		}

		public void SendAndReceive(IPEndPoint ipep, Base.PeerMessage t) {
			try {
				var ba = t.ToByteArray();
				var v = sender.SendAndReceive(ipep, ba);
				dataNum += ba.Length;
				handler.Handle(v);
				/*
				Base.PeerMessage retData = new Base.PeerMessage();
				retData.MakeFromByteArray(v.ReceiveData);

				return retData;
				 * */
			}
			catch (Exception e) {
				Console.WriteLine(e.ToString());
				NetworkResultList.Instance.Enqueue(new NetworkResult(null, NetworkResult.ResultType.NotFound));

			}
		}



		//片道切符の送信メソッド

		/*
		static public void Send(string httpAddress, Base.PeerMessage t) {
			CommunicationProxy proxy = XmlRpcProxyGen.Create<CommunicationProxy>();
			proxy.Url = httpAddress;
			proxy.KeepAlive = true;


			byte[] buffer = proxy.DataCommunication(t.ToByteArray());
			Base.PeerMessage retData = new Base.PeerMessage();
			retData.MakeFromByteArray(buffer);

			return retData;

		}

		static public void Send(Node n, Base.PeerMessage t) {
			string httpAddress = XMLRPCHelper.ConnectionAddressCreate(n.IPEP);
			Send(httpAddress, t);
		}
		 * */



	}





	/// <summary>
	/// 送信用のメソッド
	/// </summary>
	public class PeerAction {
		private PeerSystem system = null;
		private PeerConfig config = null;
		private PeerSender sender = null;

		public PeerAction(PeerSystem ps, PeerConfig pc, PeerSender sender) {
			this.system = ps;
			this.config = pc;
			this.sender = sender;
		}

		public void RegMyNode() {

			Base.PeerMessage tData = new Base.PeerMessage(Base.MessageTag.PutNode, ByteArraySerializeHelper.Serialize(((Node)system.MyNodeInfo)));

			sender.Send(system.Config.BrossomServerEndPoint.IPEP, tData);

		}

		public void GetContetnt(Hash hash) {

		}

		/// <summary>
		/// 自分のコンテンツだけ登録
		/// </summary>
		public void RegMyContent() {
			var cs = system.GetNodeContentList();
			Base.PeerMessage tData = new Base.PeerMessage(Base.MessageTag.PutContentNodePair, ByteArraySerializeHelper.Serialize(cs));
			
			sender.Send(system.Config.BrossomServerEndPoint.IPEP, tData);
		}

		/// <summary>
		/// リストを無理矢理交換させる
		/// </summary>
		public void PutContentNodeList() {
			Base.PeerMessage msg = new Base.PeerMessage(Base.MessageTag.PutContentNodeList, ByteArraySerializeHelper.Serialize(system.ContentDictionary));
			sender.Send(system.Config.BrossomServerEndPoint.IPEP, msg);
		}

		/// <summary>
		/// リストに追加させる
		/// </summary>
		public void PutNodeContentList() {
			Base.PeerMessage msg = new Base.PeerMessage(Base.MessageTag.PutNodeContentList, ByteArraySerializeHelper.Serialize(system.ContentDictionary));
			sender.Send(system.Config.BrossomServerEndPoint.IPEP, msg);

		}


		public void ReplaceNodeList() {
			Base.PeerMessage tdata = new Base.PeerMessage();
			tdata.Tag = Base.MessageTag.GetNodeList;//ノード取得要求

			sender.SendAndReceive(system.Config.BrossomServerEndPoint.IPEP, tdata);

		}

		public void ReplaceContentDictionary() {
			Base.MessageTag tag = Base.MessageTag.GetAllContent;//コンテンツ取得要求
			Base.PeerMessage tdata = new Base.PeerMessage(tag, null);

			sender.SendAndReceive(system.Config.BrossomServerEndPoint.IPEP, tdata);
		}

		public void GetDataSegment(Node node, DataSegment ds) {
			var tag = Base.MessageTag.GetDataSegment;
			Base.PeerMessage tdata = new Base.PeerMessage(tag, null);

			tdata.Data = ds.ByteSerialize();

			sender.SendAndReceive(node.IPEP, tdata);
		}

		public void GetAllData(Node node, ContentInfoBase content) {
			Base.PeerMessage tdata = new Base.PeerMessage();
			tdata.Tag = Base.MessageTag.GetAllData;
			tdata.Data = ByteArraySerializeHelper.Serialize(content);

			sender.SendAndReceive(node.IPEP, tdata);
		}

		public void PutDataSegment(Node node, DataSegment ds) {
			Base.PeerMessage tdata = new Base.PeerMessage();
			tdata.Tag = Base.MessageTag.PutDataSegment;
			tdata.Data = ByteArraySerializeHelper.Serialize(ds);

			sender.Send(node.IPEP, tdata);
		}

		public void RegBoradCast(Node myNode) {

			Base.PeerMessage tdata = new Base.PeerMessage();
			tdata.Tag = Base.MessageTag.RegBroadCast;
			tdata.Data = ByteArraySerializeHelper.Serialize(myNode);

			sender.Send(system.Config.BrossomServerEndPoint.IPEP, tdata);
		}

		public void RemoveBoradCast(Node myNode) {

			Base.PeerMessage tdata = new Base.PeerMessage();
			tdata.Tag = Base.MessageTag.RemoveBroadCast;
			tdata.Data = ByteArraySerializeHelper.Serialize(myNode);

			sender.Send(system.Config.BrossomServerEndPoint.IPEP, tdata);
		}

		public void PutThumbnail(DataSegment ds) {
			Base.PeerMessage tdata = new Base.PeerMessage(Base.MessageTag.PutThumbnail, ByteArraySerializeHelper.Serialize(ds));
			sender.Send(system.Config.BrossomServerEndPoint.IPEP, tdata);

		}


		public void PutContentMetaData(DataType.ContentMetaData metadata) {
			Base.PeerMessage tdata = new Base.PeerMessage(Base.MessageTag.PutContentMetaData, ByteArraySerializeHelper.Serialize(metadata));
			sender.Send(system.Config.BrossomServerEndPoint.IPEP, tdata);
		}

		public void GetMovieHeader(ContentInfoBase content) {
			Base.PeerMessage tdata = new Base.PeerMessage(Base.MessageTag.GetMovieHeader, ByteArraySerializeHelper.Serialize(content));
			sender.SendAndReceive(system.Config.BrossomServerEndPoint.IPEP, tdata);

		}

		public void GetFoundContent(string s) {
			Base.PeerMessage tdata = new Base.PeerMessage(Base.MessageTag.GetFoundContent, System.Text.Encoding.UTF8.GetBytes(s));
			sender.SendAndReceive(system.Config.BrossomServerEndPoint.IPEP, tdata);

		}
	}


}
