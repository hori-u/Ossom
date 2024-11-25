using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using P2P.BBase;
using P2P.BFileSystem;

namespace P2P {
	/// <summary>
	/// Peerに必要な情報を保持しておくクラス
	/// Singletonにしてもいいかも
	/// メモリに蓄えられる動的なデータ．．．かも
	/// 名前が微妙でした
	/// </summary>
	public class PeerSystem {
		[Serializable]
		public class ContentInfoBaseComparer : IEqualityComparer<ContentInfoBase> {

			#region IEqualityComparer<ContentInfoBase> メンバ

			public bool Equals(ContentInfoBase x, ContentInfoBase y) {
				return x.BaseHash.Equals(y.BaseHash);
			}

			public int GetHashCode(ContentInfoBase obj) {
				return obj.BaseHash.Str.GetHashCode();
			}

			#endregion
		}

		private XorShift random = null;

		//メモリをたくさん食うかもしれんがとりあえずこれで
		//そんなの関係ね
		private List<Node> nodeList = null;
		public List<Node> NodeList {
			get { return nodeList; }
			set { nodeList = value; }
		}

		private Dictionary<Hash, Node> nodeDictionary = null;
		public Dictionary<Hash, Node> NodeDictionary {
			get { return nodeDictionary; }
			set { nodeDictionary = value; }
		}


		private Dictionary<Hash, DataType.PossesionInfo> myPossesionDictionary = null;
		public Dictionary<Hash, DataType.PossesionInfo> MyPossessionDictionary {
			get { return myPossesionDictionary; }
			set { myPossesionDictionary = value; }
		}

		private MultiDictionary<Hash, DataType.PossesionInfo> possesionDictionary = null;
		public MultiDictionary<Hash,DataType.PossesionInfo> PossetionDictionary {	
			get { return possesionDictionary;}
			set { possesionDictionary = value; }
		}
	

		private Dictionary<ContentInfoBase, List<Node>> contentDictionary = null;
		public Dictionary<ContentInfoBase, List<Node>> ContentDictionary {
			get { return contentDictionary; }
			set { contentDictionary = value; }
		}

		private Dictionary<Hash, P2P.DataType.ContentMetaData> tagDictionary = null;
		public Dictionary<Hash, P2P.DataType.ContentMetaData> TagDictionary {
			get { return tagDictionary; }
			set { tagDictionary = value; }
		}

		private Dictionary<Hash, P2P.DataType.ThumbnailData> thumbnailDictionary = null;
		public Dictionary<Hash, P2P.DataType.ThumbnailData> ThumbnailDictionary {
			get { return thumbnailDictionary; }
			set { thumbnailDictionary = value; }
		}

		public DataType.ContentMetaData GetTagInfo(ContentInfoBase c) {
			DataType.ContentMetaData tag;
			tagDictionary.TryGetValue(c.BaseHash, out tag);
			return tag;
		}

		public ContentInfoBase GetContentInfo(Hash hash) {

			foreach (var cib in contentDictionary.Keys) {
				if (cib.BaseHash.Equals(hash)) {
					return cib;
				}
			}
			return null;

		}



		private Node myNode = null;
		public Node MyNodeInfo {
			get { return myNode; }
			set { myNode = value; }
		}

		private List<ContentInfoBase> myContentList = null;
		public List<ContentInfoBase> MyContentList {
			get { return myContentList; }
			set { myContentList = value; }
		}

		public void SetContentList(List<FileDataInfo> fdiList) {
			foreach (var fdi in fdiList) {
				ContentInfoBase cib = new ContentInfoBase(fdi);
				myContentList.Add(cib);
				try {
					contentDictionary.Add(cib, new List<Node>() { myNode });
				}
				catch (Exception) {

				}
			}
		}

		private PeerConfig config = null;
		public PeerConfig Config {
			get { return config; }
			set { config = value; }
		}



		public PeerSystem(PeerConfig config) {
			this.config = config;

			nodeList = new List<Node>();
			contentDictionary = new Dictionary<ContentInfoBase, List<Node>>(new ContentInfoBaseComparer());
			tagDictionary = new Dictionary<Hash, DataType.ContentMetaData>();
			thumbnailDictionary = new Dictionary<Hash, P2P.DataType.ThumbnailData>();

			var ips = Dns.GetHostAddresses(Dns.GetHostName());

			foreach (var ip in ips) {
				//IPv4
				if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) {
					myNode = new MyNode(ip, config.Port);
					myNode.BandWidth = config.Speed;
				}
			}

			myContentList = new List<ContentInfoBase>();


			random = new XorShift((uint)DateTime.Now.ToBinary());


		}


		public void ReplaceNodeList(List<Node> nodeList) {
			NodeList = nodeList;
		}

		public void ReplaceContentDictionary(Dictionary<ContentInfoBase, List<Node>> contentDictionary) {
			ContentDictionary = contentDictionary;
		}


		public void AddPeer(Node node) {
			NodeList.Add(node);
		}

		public List<Node> GetNodeList() {
			return NodeList;
		}

		public Node GetMyNode() {
			return MyNodeInfo;
		}

		public KeyValuePair<Node, List<ContentInfoBase>> GetNodeContentList() {
			var pair = new KeyValuePair<Node, List<ContentInfoBase>>(MyNodeInfo, MyContentList);
			return pair;
		}

		public Dictionary<ContentInfoBase, List<Node>> GetContentDictionary() {
			return ContentDictionary;
		}

		public void AddContent(KeyValuePair<ContentInfoBase, Node> contentNodePair) {
			ContentInfoBase content = contentNodePair.Key;
			Node node = contentNodePair.Value;
			bool exist = ContentDictionary.ContainsKey(content);

			if (exist) {
				ContentDictionary[content].Add(node);
			}
			else {
				//地味に3.0構文dehanai
				ContentDictionary.Add(content, new List<Node>() { node });
			}
		}

		public void AddContent(KeyValuePair<ContentInfoBase, List<Node>> contentNodeList) {
			ContentInfoBase content = contentNodeList.Key;
			List<Node> nodeList = contentNodeList.Value;
			bool exist = ContentDictionary.ContainsKey(content);

			if (exist) {
				ContentDictionary[content].AddRange(nodeList);
			}
			else {
				//地味に3.0構文dehanai
				ContentDictionary.Add(content, nodeList);
			}
		}

		public void AddContent(Dictionary<ContentInfoBase, List<Node>> contentDictionary) {
			foreach (var cib in contentDictionary.Keys) {
				bool exist = ContentDictionary.ContainsKey(cib);

				if (exist) {
					ContentDictionary[cib].AddRange(NodeList);
				}
				else {
					//地味に3.0構文
					ContentDictionary.Add(cib, NodeList);
				}
			}
		}

		public void AddContent(KeyValuePair<Node, List<ContentInfoBase>> nodeContentList) {
			foreach (var content in nodeContentList.Value) {
				KeyValuePair<ContentInfoBase, Node> keyPair = new KeyValuePair<ContentInfoBase, Node>(content, nodeContentList.Key);
				AddContent(keyPair);
			}
		}

		public void AddNode(Node node) {
			if (!NodeList.Contains(node)) {
				NodeList.Add(node);
			}
		}

		public void AddNode(List<Node> nodeList) {
			foreach (var node in nodeList) {
				if (!nodeList.Contains(node)) {
					nodeList.Add(node);
				}
			}
		}

		public void RemoveNode(Node node) {
			NodeList.Remove(node);
		}

		public void RemoveContent(ContentInfoBase content) {
			ContentDictionary.Remove(content);
		}

		public List<Node> GetContentNodeList(ContentInfoBase content) {
			List<Node> nodeList = null;
			bool exist = ContentDictionary.TryGetValue(content, out nodeList);

			return exist ? nodeList : null;
		}



		public byte[] GetAllData(ContentInfoBase content) {
			string path = Path.Combine(Config.UploadPath, content.Name);
			byte[] allData = File.ReadAllBytes(path);

			return allData;

		}

		public void PutAllData(ContentInfoBase content, byte[] data) {
			string path = Path.Combine(Config.UploadPath, content.Name);
			File.WriteAllBytes(path, data);
		}




		public void ClearContent() {
			foreach (var list in ContentDictionary.Values) {
				list.Clear();
			}

			ContentDictionary.Clear();
		}

		public void ClearNode() {
			NodeList.Clear();
		}

		public void Clear() {
			ClearContent();
			ClearNode();
		}



	}

}
