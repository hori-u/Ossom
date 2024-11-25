using System.Collections.Generic;
using System.IO;
using P2P.BBase;
using System;
using P2P.BFileSystem;

namespace P2P {
	/// <summary>
	/// 入力を適切な関数にハンドルするためのクラスのインタフェース
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IDataHandler {
		void Handle(P2P.Network.ReceiveMessageEventArgs rmea);
	}


	//メモ
	//送られてくるデータにデータの形式を入れておいて
	//DataConverterクラスなるものを定義してそのクラスから本当のその形式に対応するメソッドを呼び出すのがいいよね

	public class BroadCastHandler : IDataHandler {
		private BroadCastMethod bmethod = null;//ちょっと苦肉の策的

		#region IDataHandler メンバ

		public void Handle(P2P.Network.ReceiveMessageEventArgs rmea) {
			throw new NotImplementedException();
		}

		#endregion
	}

	public class PeerService {
		private PeerSystem system = null;
		private PeerFileManager manager = null;
		private PeerConfig config = null;
		private PeerSender sender = null;

		public PeerService(PeerSystem ps, PeerFileManager pfm,PeerConfig pc,PeerSender pss) {
			this.system = ps;
			this.manager = pfm;
			this.config = pc;
			this.sender = pss;
		}

		public void HookHandle(PeerHandler ph) {
			ph.AddHandle((int)Base.MessageTag.GetAllContent, this.GetAllContent);
			ph.AddHandle((int)Base.MessageTag.GetAllData, this.GetAllData);
			ph.AddHandle((int)Base.MessageTag.GetContentNodeDictionary, this.GetContentNodeDictionary);
			ph.AddHandle((int)Base.MessageTag.GetContentNodeList, this.GetContentNodeList);
			ph.AddHandle((int)Base.MessageTag.GetContentNodePair, this.GetContentNodePair);
			ph.AddHandle((int)Base.MessageTag.GetDataSegment, this.GetDataSegment);
			ph.AddHandle((int)Base.MessageTag.GetFoundContent, this.GetFoundContent);
			ph.AddHandle((int)Base.MessageTag.GetNode, this.GetNode);
			ph.AddHandle((int)Base.MessageTag.GetNodeContentList, this.GetNodeContentList);
			ph.AddHandle((int)Base.MessageTag.GetNodeList, this.GetNodeList);

			ph.AddHandle((int)Base.MessageTag.PutAllContent, this.PutAllContent);
			ph.AddHandle((int)Base.MessageTag.PutContentMetaData, this.PutContentMetaData);
			ph.AddHandle((int)Base.MessageTag.PutContentNodeDictionary, this.PutContentNodeDictionary);
			ph.AddHandle((int)Base.MessageTag.PutContentNodeList, this.PutContentNodeList);
			ph.AddHandle((int)Base.MessageTag.PutContentNodePair, this.PutContentNodePair);
			ph.AddHandle((int)Base.MessageTag.PutDataSegment, this.PutDataSegment);
			ph.AddHandle((int)Base.MessageTag.PutFoundContent, this.PutFoundContent);
			ph.AddHandle((int)Base.MessageTag.PutNode, this.PutNode);
			ph.AddHandle((int)Base.MessageTag.PutNodeContentList, this.PutNodeContentList);
			ph.AddHandle((int)Base.MessageTag.PutNodeList, this.PutNodeList);
			ph.AddHandle((int)Base.MessageTag.PutThumbnail, this.PutThumbnail);
		}

		//Get系メソッドは相手にデータを送る必要がある．
		//そのときはメッセージタイプをPut系に変換する

		//Put系メソッドは相手からデータが送られてきたことを示す
		//データを保存する方がよい

		#region Get/Put AllContent
		public void GetAllContent(P2P.Network.ReceiveMessageEventArgs rmea) {
			var dic = system.GetContentDictionary();
			Base.PeerMessage td = new Base.PeerMessage(
				Base.MessageTag.PutAllContent,
				ByteArraySerializeHelper.Serialize(dic));

			sender.Send(rmea.SenderStatus.IPEP, td);
		}

		public void PutAllContent(P2P.Network.ReceiveMessageEventArgs rmea) {
			byte[] data = rmea.ReceiveData;
			//var dic = (Dictionary<ContentInfoBase, List<Node>>)ByteArraySerializeHelper.Deserialize(data);
			var dic = ByteArraySerializeHelper.Deserialize<Dictionary<ContentInfoBase, List<Node>>>(data); //2024-11-21
			system.ReplaceContentDictionary(dic);
		}
		#endregion

		#region Get/Put NodeContentList
		public void GetNodeContentList(P2P.Network.ReceiveMessageEventArgs rmea) {
			Base.PeerMessage msg = new Base.PeerMessage(Base.MessageTag.PutNodeContentList, ByteArraySerializeHelper.Serialize(system.GetNodeContentList()));
			sender.Send(rmea.SenderStatus.IPEP, msg);

		}

		/// <summary>
		/// リストにキーペア追加させる
		/// </summary>
		/// <param name="rmea"></param>
		public void PutNodeContentList(P2P.Network.ReceiveMessageEventArgs rmea) {



			byte[] data = rmea.ReceiveData;
			//var list = (Dictionary<ContentInfoBase, List<Node>>)ByteArraySerializeHelper.Deserialize(data);
			var list = ByteArraySerializeHelper.Deserialize<Dictionary<ContentInfoBase, List<Node>>>(data); //2024-11-21

			foreach (var c in list.Keys) {
				foreach (var n in list[c]) {
					system.ContentDictionary[c].Add(n);
				}
			}
			return;
		}
		#endregion



		#region Get/Put ContentNodeList
		public void GetContentNodeList(P2P.Network.ReceiveMessageEventArgs rmea) {
			return;

		}

		/// <summary>
		/// リストを無理矢理交換させる
		/// </summary>
		/// <param name="rmea"></param>
		public void PutContentNodeList(P2P.Network.ReceiveMessageEventArgs rmea) {
			byte[] data = rmea.ReceiveData;
			//var list = (Dictionary<ContentInfoBase, List<Node>>)ByteArraySerializeHelper.Deserialize(data);
			var list = ByteArraySerializeHelper.Deserialize<Dictionary<ContentInfoBase, List<Node>>>(data); //2024-11-21
			//var list =  (List<Node>)ByteArraySerializeHelper.Deserialize(data);
			system.ContentDictionary = list;
			return;
		}

		#endregion

		#region Get/Put ContentNodePair
		public void GetContentNodePair(P2P.Network.ReceiveMessageEventArgs rmea) {
			return;

		}

		public void PutContentNodePair(P2P.Network.ReceiveMessageEventArgs rmea) {


			byte[] data = rmea.ReceiveData;
			//var list = (KeyValuePair<Node, List<ContentInfoBase>>)ByteArraySerializeHelper.Deserialize(data);
			var list = ByteArraySerializeHelper.Deserialize<Dictionary<ContentInfoBase, List<Node>>>(data); //2024-11-21
			system.AddContent(list);

			return;
		}
		#endregion

		#region Get/Put FoundContent
		public void GetFoundContent(P2P.Network.ReceiveMessageEventArgs rmea) {
			
			//Base.PeerMessage msg = new Base.PeerMessage(Base.MessageTag.PutFoundContent, ByteArraySerializeHelper.Serialize(system.ContentDictionary));

			//sender.Send(rmea.SenderStatus.IPEP, msg);

		}

		public void PutFoundContent(P2P.Network.ReceiveMessageEventArgs rmea) {

			//sender.Send(rmea.SenderStatus.IPEP, td);

		}

		#endregion

		#region Get/Put Node
		public void GetNode(P2P.Network.ReceiveMessageEventArgs rmea) {
			//sender.Send(rmea.SenderStatus.IPEP, td);
		}

		public void PutNode(P2P.Network.ReceiveMessageEventArgs rmea) {
			byte[] data = rmea.ReceiveData;
			//var node = (Node)ByteArraySerializeHelper.Deserialize(data);
			var node = ByteArraySerializeHelper.Deserialize<Node>(data);//2024-11-21
			system.AddNode(node);
		}
		#endregion

		#region Get/Put DataSegment
		public void GetDataSegment(P2P.Network.ReceiveMessageEventArgs rmea) {
			
			byte[] data = rmea.ReceiveData;
			DataSegment reqDs = new DataSegment();
			reqDs.ByteDeserialize(data);

			var ds = manager.ReadFile(new Hash(reqDs.TagArray), reqDs.SegmentNumber);

			Base.PeerMessage td = new Base.PeerMessage(
				Base.MessageTag.PutDataSegment,
				ds.ByteSerialize());

			sender.Send(rmea.SenderStatus.IPEP, td);
			 
		}

		public void PutDataSegment(P2P.Network.ReceiveMessageEventArgs rmea) {
			byte[] data = rmea.ReceiveData;
			DataSegment ds = new DataSegment();
			ds.ByteDeserialize(data);
			manager.WriteFile(new Hash(ds.TagArray), ds);
		}

		#endregion

		#region Get/Put ContentNodeDictionary
		public void GetContentNodeDictionary(P2P.Network.ReceiveMessageEventArgs rmea) {
			var dic = system.GetContentDictionary();
			Base.PeerMessage td = new Base.PeerMessage(
				Base.MessageTag.PutContentNodeDictionary,
				ByteArraySerializeHelper.Serialize(dic));

			sender.Send(rmea.SenderStatus.IPEP, td);
		}

		public void PutContentNodeDictionary(P2P.Network.ReceiveMessageEventArgs rmea) {
			byte[] data = rmea.ReceiveData;
			//var dic = (Dictionary<ContentInfoBase, List<Node>>)ByteArraySerializeHelper.Deserialize(data);
			var dic = ByteArraySerializeHelper.Deserialize<Dictionary<ContentInfoBase, List<Node>>>(data); //2024-11-21
			system.AddContent(dic);
		}
		#endregion

		public void PutContentMetaData(P2P.Network.ReceiveMessageEventArgs rmea) {
			byte[] data = rmea.ReceiveData;
			//var md = (DataType.ContentMetaData)ByteArraySerializeHelper.Deserialize(data);
			var md = ByteArraySerializeHelper.Deserialize<DataType.ContentMetaData>(data); //2024-11-21
			system.TagDictionary.Add(md.DataHash, md);

		}

		public void PutThumbnail(P2P.Network.ReceiveMessageEventArgs rmea) {
			byte[] data = rmea.ReceiveData;
			DataSegment ds = new DataSegment();
			ds.ByteDeserialize(data);
			var h = new Hash(ds.TagArray);
			var c = system.GetContentInfo(h);
			var path = Path.Combine(config.DataPath,Path.GetFileNameWithoutExtension(c.Name)) + ".jpg";
			File.WriteAllBytes(path,ds.Data);
			DataType.ThumbnailData td = new P2P.DataType.ThumbnailData(path,h);
			system.ThumbnailDictionary.Add(td.DataHash, td);
		}


		#region Get/Put NodeList
		public void GetNodeList(P2P.Network.ReceiveMessageEventArgs rmea) {
			var list = system.GetNodeList();
			Base.PeerMessage td = new Base.PeerMessage(
				Base.MessageTag.GetNodeList,
				ByteArraySerializeHelper.Serialize(list));
			sender.Send(rmea.SenderStatus.IPEP, td);
		}

		public void PutNodeList(P2P.Network.ReceiveMessageEventArgs rmea) {
			byte[] data = rmea.ReceiveData;
			//var list = (List<Node>)(ByteArraySerializeHelper.Deserialize(data));
			var list = ByteArraySerializeHelper.Deserialize<List<Node>>(data); //2024-11-21

			system.AddNode(list);
		}
		#endregion









		public void GetAllData(P2P.Network.ReceiveMessageEventArgs rmea) {
			byte[] data = rmea.ReceiveData;
			//var content = (ContentInfoBase)ByteArraySerializeHelper.Deserialize(data);
			var content = ByteArraySerializeHelper.Deserialize<ContentInfoBase>(data); //2024-11-21

			Base.PeerMessage td = new Base.PeerMessage(
				Base.MessageTag.GetAllData,
				system.GetAllData(content));
			sender.Send(rmea.SenderStatus.IPEP, td);
		}





	}

	public class BroadCastService {
		private BroadCastMethod method = null;
		private PeerConfig config = null;
		private PeerSender sender = null;

		public BroadCastService(PeerConfig ps,BroadCastMethod bcm,PeerSender pss) {
			this.config = ps;
			this.method = bcm;
			this.sender = pss;
		}

		public void RegBroadCast(P2P.Network.ReceiveMessageEventArgs rmea) {
			byte[] data = rmea.ReceiveData;
			//var node = (Node)ByteArraySerializeHelper.Deserialize(data);
			var node = ByteArraySerializeHelper.Deserialize<Node>(data); //2024-11-21
			method.RegNode(node);
		}

		public void RemoveBroadCast(P2P.Network.ReceiveMessageEventArgs rmea) {
			byte[] data = rmea.ReceiveData;
			//var node = (Node)ByteArraySerializeHelper.Deserialize(data);
			var node = ByteArraySerializeHelper.Deserialize<Node>(data); //2024-11-21
			method.RemoveNode(node);
		}

		public void GetMovieHeader(P2P.Network.ReceiveMessageEventArgs rmea) {
			byte[] data = rmea.ReceiveData;
			//var content = (ContentInfoBase)ByteArraySerializeHelper.Deserialize(data);
			var content = ByteArraySerializeHelper.Deserialize<ContentInfoBase>(data); //2024-11-21
			FileStream fs = new FileStream(Path.Combine(config.CachePath, content.BaseHash.Str), FileMode.Open, FileAccess.Read);

			byte[] header = new byte[] { 0x00, 0x00, 0x01, 0xB8 };

			long findPos = (long)StreamExtension.DataSearch(fs,header);

			findPos--;

			byte[] headerData = new byte[findPos];
			fs.Seek(0, SeekOrigin.Begin);
			fs.Read(headerData, 0, headerData.Length);

			var tData = new Base.PeerMessage(Base.MessageTag.GetMovieHeader, headerData);

			sender.Send(rmea.SenderStatus.IPEP, tData);
		}

		public void PutThumbnail(P2P.Network.ReceiveMessageEventArgs rmea) {
			byte[] data = rmea.ReceiveData;
			DataSegment image = new DataSegment();
			image.ByteDeserialize(data);

			FileStream fs = new FileStream(Path.Combine(config.CachePath, image.TagStr + ".jpg") , FileMode.Create);
			fs.Write(image.Data, 0, image.Data.Length);
			fs.Close();
		}


		public void HookHandle(PeerHandler ph) {
			ph.AddHandle((int)Base.MessageTag.RegBroadCast, this.RegBroadCast);
			ph.AddHandle((int)Base.MessageTag.RemoveBroadCast, this.RemoveBroadCast);
			ph.AddHandle((int)Base.MessageTag.GetMovieHeader,this.GetMovieHeader);
			ph.AddHandle((int)Base.MessageTag.PutThumbnail, this.PutThumbnail);
		}

	}


	/// <summary>
	/// Peer用のハンドラの実体
	/// </summary>
	public class PeerHandler : IDataHandler {
		public delegate void HandledMethod(P2P.Network.ReceiveMessageEventArgs rmea);

		public Dictionary<int, HandledMethod> handlerDictionary = null;

		public PeerHandler(PeerSystem system, PeerFileManager manager,BroadCastMethod bmethod) {

			handlerDictionary = new Dictionary<int, HandledMethod>();
		}

		public void AddHandle(int id,HandledMethod handle) {
			handlerDictionary.Add(id, handle);
		}

		public void RemoveHandle(int id) {
			handlerDictionary.Remove(id);
		}

		public void Handle(P2P.Network.ReceiveMessageEventArgs rmea) {
			byte[] data = rmea.ReceiveData;

			Base.PeerMessage tData = new Base.PeerMessage();
			tData.MakeFromByteArray(data);

			//分解する
			int methodId = (int)tData.Tag;
			rmea.ReceiveData = tData.Data;

			

			//Base.PeerMessage retData = handlerDictionary[methodId](buffer);

			HandledMethod hm;
			handlerDictionary.TryGetValue(methodId, out hm);


			if (hm != null) {
				hm(rmea);
			}

		}

		//古いコード
		/*
		public byte[] Handle(byte[] data) {
			Base.PeerMessage tData = new Base.PeerMessage();
			tData.MakeFromByteArray(data);

			//分解する
			Base.MessageTag dtag = tData.Tag;
			byte[] buffer = tData.Data;

			tData.Data = new byte[1];//データを消しておく

			//ここら辺はあんまり汎用性がないからもっとかっこよく実装してもいいかも
			switch (dtag) {
				case Base.MessageTag.PutContentNodeDictionary:
					Dictionary<ContentInfoBase, List<Node>> dic =
						buffer.Deserialize<Dictionary<ContentInfoBase, List<Node>>>();
					system.AddContent(dic);
					break;

				case Base.MessageTag.PutContentNodeList:
					KeyValuePair<ContentInfoBase, List<Node>> list =
						buffer.Deserialize<KeyValuePair<ContentInfoBase, List<Node>>>();
					system.AddContent(list);
					break;

				case Base.MessageTag.PutContentNodePair:
					KeyValuePair<ContentInfoBase, Node> pair =
						buffer.Deserialize<KeyValuePair<ContentInfoBase, Node>>();
					system.AddContent(pair);
					break;

				case Base.MessageTag.PutDataSegment:
					DataSegment ds1 = buffer.Deserialize<DataSegment>();
					manager.WriteFile(ds1.TagArray, ds1);
					break;

				case Base.MessageTag.PutNode:
					Node node = buffer.Deserialize<Node>();
					system.AddNode(node);
					break;

				case Base.MessageTag.PutNodeList:
					List<Node> nodeList = buffer.Deserialize<List<Node>>();
					system.AddNode(nodeList);
					break;

				case Base.MessageTag.PutNodeContentList:
					var nodeContentList = buffer.Deserialize<KeyValuePair<Node, List<ContentInfoBase>>>();
					system.AddContent(nodeContentList);
					break;



				////////////////////////////////////////////////////////////////

				case Base.MessageTag.GetContentNodeDictionary:
					Dictionary<ContentInfoBase, List<Node>> contentNodeDictionary = system.GetContentDictionary();
					tData.Tag = Base.MessageTag.PutContentNodeDictionary;
					tData.Data = contentNodeDictionary.Serialize();

					break;

				case Base.MessageTag.GetNodeList:
					List<Node> nodeList2 = system.GetNodeList();
					tData.Tag = Base.MessageTag.PutNodeList;
					tData.Data = nodeList2.Serialize();

					break;


				///////////////////////////////////////////////////////////////

				case Base.MessageTag.GetAllData:
					ContentInfoBase cib = buffer.Deserialize<ContentInfoBase>();
					tData.Data = system.GetAllData(cib);
					break;


				case Base.MessageTag.GetDataSegment:
					DataSegment ds = buffer.Deserialize<DataSegment>();
					var d = manager.ReadFile(ds.TagArray, ds.SegmentNumber);
					tData.Data = d.Serialize();

					break;


				case Base.MessageTag.RegBroadCast:
					Node n = buffer.Deserialize<Node>();
					bmethod.RegNode(n);

					break;


				default:
					throw new ApplicationException("不明なタグ");
			}

			return tData.ToByteArray();
		}
		 * */

	}

}
