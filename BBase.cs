using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace P2P
{
    namespace BBase
    {
        [Serializable]
		public class BaseData : IEquatable<BaseData> {
			protected string name = string.Empty;
			public string Name {
				get { return name; }
				set { name = value; }
			}

			protected Hash hash = null;
             
			public Hash BaseHash {
				get { return hash; }
				set { hash = value; }
			}

			public BaseData() {
				//なにもしない．値はNULLとかがはいってます．
			}

			public BaseData(string name, Hash hash) {
				this.name = name;
				this.hash = hash;
			}

			#region IEquatable<BaseData> メンバ

			/// <summary>
			/// ハッシュ値だけで同一性を検証する
			/// </summary>
			/// <param name="other"></param>
            /// <returns></returns>

			public bool Equals(BaseData other) {
				if (other == null) {
					return false;
				}

				bool b = this.hash.Equals(other.hash);
				return b;
			}

			public override int GetHashCode() {
				return hash.Str.GetHashCode();
			}

			#endregion
		}

		public enum ContentType {
			P2P,
			Multicast,
		}

		/// <summary>
		///  ネットワーク上を流れるファイルの情報
		/// 必要最低限のコンテンツの情報
		/// </summary>
		[Serializable]
		public class ContentInfoBase : BaseData, IEquatable<ContentInfoBase> {
			protected int fileSize = 0;
			protected ContentType type = ContentType.P2P;

			public ContentType Type {
				get { return type; }
				set { type = value; }
			}

			public int FileSize {
				get { return this.fileSize; }
			}

			public ContentInfoBase() {

			}

			public ContentInfoBase(string name, int size, Hash hash)
				: base(name, hash) {
				this.fileSize = size;
			}

			public ContentInfoBase(ContentInfoBase other)
				: base(other.name, other.hash) {
				this.fileSize = other.fileSize;
			}

			public byte[] ToByteArray() {
				//とりあえずこれで勘弁して
				//意外とSerializeのオーバーヘッドが大きいので自前でParseした方がいいよ
				return ByteArraySerializeHelper.Serialize(this);
			}

			#region IEquatable<ContentInfoBase> メンバ

			public bool Equals(ContentInfoBase other) {
				return this.BaseHash == other.hash && this.fileSize == other.fileSize;
			}

			#endregion
		}

		public class ContentData {
			protected string name = string.Empty;
			protected long fileSize = 0;
			protected Hash fileHash = null;
		}

		public class NodeData{

		}

		public class PossesionData{
			ContentData content = null;
			NodeData node = null;
		}

		/// <summary>
		/// ファイル分割のぽりしー
		/// </summary>
		public class FileDividePolicy {
			public const int FileDivideSize = 128 * 1024;//512KByte
		}

		public enum FileDataState : byte {Incomplete, Complete, Unknown, }

		/// <summary>
		/// コンテンツの情報が自PCでファイルとしての実体を持つときのクラス
		/// </summary>
		[Serializable]
		public class FileDataInfo : ContentInfoBase {
			#region フィールド
			private System.Collections.BitArray blockState = null;

			private string filename = string.Empty;

			public string OriginalName {
				get { return this.name; }
				set { this.name = value; }
			}

			public System.Collections.BitArray BlockState {
				get { return blockState; }
				set { blockState = value; }
			}


			public string FileName {
				get { return filename; }
				set { filename = value; }
			}

			new public int FileSize {
				get { return fileSize; }
				set {
					fileSize = value;
					SetBlockState(fileSize);
				}
			}
			#endregion
			//マルチスレッド同期用
			private object SyncObject = new object();

			/// <summary>
			/// Getのみ　全ブロック数　
			/// </summary>
			public int BlockCount {
				get { return blockState.Length; }
			}

			private void SetBlockState(int fileSize) {
				blockState = new System.Collections.BitArray(fileSize / FileDividePolicy.FileDivideSize + 1);
			}

			#region コンストラクタ，インスタンスを作成するものたち
			public FileDataInfo()
				: base() {
				//なにもしない
			}

			public FileDataInfo(ContentInfoBase cib)
				: base(cib.Name, cib.FileSize, cib.BaseHash) {
				this.SetBlockState(this.fileSize);
				this.blockState.SetAll(false);

				this.filename = this.hash.Str;
			}
            
			public FileDataInfo(string name, Hash hash, int fileSize)
				: base(name, fileSize, hash) {
				this.fileSize = fileSize;
				SetBlockState(fileSize);

				this.filename = this.hash.Str;
			}
                        
			public FileDataInfo(FileDataInfo other)
				: base(other) {

				this.blockState = other.blockState;

				this.filename = this.hash.Str;

			}

			/// <summary>
			/// ハッシュ値の計算に時間がかかる場合があるので分離した方がいいかも
			/// </summary>
			/// <param name="path"></param>
			/// <returns></returns>

			static public FileDataInfo LoadFile(string path) {
				if (!File.Exists(path)) {
					throw new ApplicationException("ファイルがありません");
				}
				FileStream fs = new FileStream(path, FileMode.Open);

				FileDataInfo fdi = new FileDataInfo();
				fdi.Name = Path.GetFileName(fs.Name);
				fdi.fileSize = (int)fs.Length;
				fdi.hash = new Hash(HashWrapper.ComputeHash(fs, HashAlgorithm.SHA1)); ;//時間がかかるよ
				fdi.SetBlockState((int)fs.Length);
				fdi.isComplete = true;//ファイルからロードしたと言うことはファイルはコンプリートしている
				fdi.filename = fdi.BaseHash.Str;

				fs.Close();

				return fdi;
			}
            
			public ContentInfoBase CreateContent() {
				ContentInfoBase cib = new ContentInfoBase(this.name, this.fileSize, this.hash);
				return cib;
			}

			#endregion

			public bool isComplete {
				get {
					var s = GetState();
					return (s == FileDataState.Complete);
				}

				set {
					if (value == true) {
						blockState.SetAll(true);//すべてそろっている様にする
					}
				}
			}

			//Linqのおかげでちょうすっきり
            /// <summary>
			/// returnがnullならBlankBlockがない
			/// </summary>
			/// <returns></returns>
			public IEnumerable<int> GetBlankBlock() {
				List<int> blankIndeces = new List<int>();

				for (int i = 0; i < blockState.Length; i++) {
					if (blockState[i] == false) {
						blankIndeces.Add(i);
					}
				}

				return blankIndeces;
			}

			public System.Collections.BitArray GetBlankBit() {
				return blockState;
			}

			public int[] GetBlankBlockArray() {
				var rs = GetBlankBlockList();
				return rs.ToArray();
			}

			public List<int> GetBlankBlockList() {
				var rs = GetBlankBlock();
				return new List<int>(rs);
			}
            
			public IEnumerable<int> GetEnableBlock() {
				List<int> blankIndeces = new List<int>();
				for (int i = 0; i < blockState.Length; i++) {
					if (blockState[i] == true) {
						blankIndeces.Add(i);
					}
				}
				return blankIndeces;
			}

			public int[] GetEnableBlockArray() {
				var rs = GetEnableBlockList();
				return rs.ToArray();
			}

			public List<int> GetEnableBlockList() {
				var rs = GetEnableBlock();
				return new List<int>(rs);
			}

			public void PutBlock(int number) {
				lock (SyncObject) {
					try {
						blockState[number] = true;
					}
					catch (IndexOutOfRangeException) {
						//握りつぶす//これは別にいいよね
					}
				}
			}

			public void UnputBlock(int number) {
				lock (SyncObject) {
					try {
						blockState[number] = false;
					}
					catch (IndexOutOfRangeException) {
						//握りつぶす//これは別にいいよね
					}
				}
			}
			public bool CheckBlock(int number) {
				bool b = false;
				lock (SyncObject) {
					try {
						b = blockState[number];
					}
					catch (ArgumentOutOfRangeException) {
						//つぶす//bはfalseのままだからいいよね
					}
				}
				return b;
			}

			public void ClearState() {
				lock (SyncObject) {
					blockState.SetAll(false);
				}
			}

			/// <summary>
			/// ファイルがコンプリート状態かどうか調べる
			/// </summary>
			/// <returns></returns>
			public FileDataState GetState() {
				for (int i = 0; i < blockState.Length; i++) {
					if (false == blockState[i]) {
						return FileDataState.Incomplete;
					}
				}

				return FileDataState.Complete;
			}

		}

		[Serializable]
		public class DataSegment {
			private byte[] tag = new byte[1];
			private int segmentNum = 0;
			private byte[] data = new byte[1];

			public string TagStr {
				get { return BTool.StringParse(this.tag); }
				set { tag = BTool.ByteParse(value); }
			}

			public byte[] TagArray {
				get { return tag; }
				set { tag = value; }
			}

			public int SegmentNumber {
				get { return segmentNum; }
				set { segmentNum = value; }
			}

			public byte[] Data {
				get { return data; }
				set { data = value; }
			}
			public DataSegment() {

			}

			public DataSegment(byte[] tag, int segnum, byte[] data) {
				this.tag = tag;
				this.segmentNum = segnum;
				this.data = data;
			}
            
			public byte[] ByteSerialize() {
				if (data == null) {
					data = new byte[1];
				}
				byte[] ret = new byte[4 + tag.Length + 4 + 4 + data.Length];

				byte[] buffer = BitConverter.GetBytes(tag.Length);
				int index = 0;
				Array.Copy(buffer, 0, ret, index, buffer.Length);
				index += buffer.Length;
				Array.Copy(tag, 0, ret, index, tag.Length);
				index += tag.Length;

				buffer = BitConverter.GetBytes(segmentNum);
				Array.Copy(buffer, 0, ret, index, buffer.Length);
				index += buffer.Length;

				buffer = BitConverter.GetBytes(data.Length);
				Array.Copy(buffer, 0, ret, index, buffer.Length);
				index += buffer.Length;
				Array.Copy(data, 0, ret, index, data.Length);
				index += tag.Length;
                
				return ret;
			}

			public void ByteDeserialize(byte[] a) {
				int index = 0;
				int size = BitConverter.ToInt32(a, index);
				index += 4;
				tag = new byte[size];
				Array.Copy(a, index, tag, 0, size);
				index += size;

				this.segmentNum = BitConverter.ToInt32(a, index);
				index += 4;

				size = BitConverter.ToInt32(a, index);
				index += 4;
				data = new byte[size];
				Array.Copy(a, index, data, 0, size);
			}
		}
        
		//固定データのみの基本クラス
		[Serializable]
		public class NodeBase : BaseData, IEquatable<NodeBase> {
			private IPAddress address = null;
			private int port = 0;
            
			public IPAddress Address {
				get { return address; }
				set { address = value; }
			}

			public int Port {
				get { return port; }
				set { port = value; }
			}
            
			public IPEndPoint IPEP {
				get {
					return new IPEndPoint(address, port);
				}
			}

			public NodeBase(IPAddress address, int port, Hash hash)
				: base(string.Empty, hash) {
				this.address = address;
				this.port = port;
			}
            
			public NodeBase(IPAddress Address, int Port) {
                address = Address;
                port = Port;
				byte[] ipepbyte = BTool.ByteParseIPEndPoint(IPEP);
                hash = new Hash(HashWrapper.ComputeHash(ipepbyte, HashAlgorithm.SHA1));
			}

			#region IEquatable<NodeBase> メンバ

			public bool Equals(NodeBase other) {
				if (port == other.port) {
					if (address.Equals(other.address)) {
						return true;
					}
				}
				return false;
			}

			#endregion

			//Equalを実装するならこれも必要らしい
			//http://d.hatena.ne.jp/fyts/20071026/asp
			public override int GetHashCode() {
				return this.name.GetHashCode() ^ this.BaseHash.ByteData.GetHashCode()
					^ this.port.GetHashCode() ^ this.address.GetHashCode();
			}
		}

		//自分とほかのノードの関係を表すパラメタの追加
		//基本的にこれを使う
		[Serializable]
		public class Node : NodeBase {
			private int version = 0;

			public int Version {
				get { return version; }
				set { version = value; }
			}
			private int bandWidth = 0;

			public int BandWidth {
				get { return bandWidth; }
				set { bandWidth = value; }
			}

			public Node(IPAddress Address, int Port, Hash hash) : base(Address, Port, hash) { }

			public Node(IPAddress Address, int Port) : base(Address, Port) { }

		}

		//さらに自ノードの情報
		[Serializable]
		public class MyNode : Node {
			[NonSerialized]
			private string uploadFolderPath;

			public string UploadFolderPath {
				get { return uploadFolderPath; }
				set { uploadFolderPath = value; }
			}

			[NonSerialized]
			private string downloadFolderPath;

			public string DownloadFolderPath {
				get { return downloadFolderPath; }
				set { downloadFolderPath = value; }
			}
			public MyNode(IPAddress Address, int Port, Hash hash) : base(Address, Port, hash) { }
			public MyNode(IPAddress Address, int Port) : base(Address, Port) { }
		}
	}
}
