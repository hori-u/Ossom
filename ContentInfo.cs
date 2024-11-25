using System;
using System.Collections.Generic;
using System.IO;

namespace P2P.DataType
{
    /*
	 *いろいろ考えること
	 *ハッシュが同じでファイル名が違う場合．
	 *シノニムとする．サイズも調べて同じならば，名前違いの同じファイルとしてもいいのではないかと思う．
	 * 
	 *ファイル名が同じでハッシュが違う場合．
	 *違うファイルとして扱う．
	 *
	 */

    public class PeerStaticData {
		public const int FileDivideSize = 512 * 1024;
	}

	/// <summary>
	/// ネットワーク上で共通のデータ
	/// </summary>
	[Serializable]
	public class ContentCommonData : DHTData<string>, IEquatable<ContentCommonData> {
		public const long FileDivideSize = PeerStaticData.FileDivideSize;

        protected DateTime contentDate = DateTime.Now;//とりあえず日付　何に使うかは未定
        protected string originalName = string.Empty;
		protected string trip         = string.Empty;
        protected long fileSize = 0;
        protected int version   = 0;

		[NonSerialized]
		protected string fileName      = string.Empty;

		[NonSerialized]
		protected string cacheFileName = string.Empty;

		public string OriginalName {
			get { return originalName; }
		}

		public long FileSize {
			get { return this.FileSize; }
		}

		public Hash FileHash {
			get { return this.DataHash; }
		}

		public int FileBlockCount {
			get { return (int)(this.FileSize / FileDivideSize) + 1; }
		}

		public string FileName {
			get { return fileName; }
		}

		public string CacheFileName {
			get { return cacheFileName; }
		}

		public string Id {
			get { return trip; }
		}

		public int Version {
			get { return version; }
		}

		public DateTime ContentTime {
			get { return contentDate; }
		}

		//メソッド
		public void UpdateTimeUpdate() {
			contentDate = DateTime.Now;
		}

		public ContentCommonData(string originalName, long fileSize, Hash fileHash) {
			this.originalName  = originalName;
			this.fileSize      = fileSize;
			this.hash          = fileHash;
			this.trip          = "TEST";
			this.fileName      = originalName;
			this.cacheFileName = hash.Str;
		}

		#region IEquatable<ContentCommonData> メンバ

		public bool Equals(ContentCommonData other) {
			return this.hash.Equals(other.hash);
		}

		#endregion

		public override int GetHashCode() {
			return base.hash.Str.GetHashCode();
		}

		/*
		public Database.ContentCommonData ToDatabaseData() {
			Database.ContentCommonData ccd = new P2P.Database.ContentCommonData();
			ccd.Hash = this.hash.Str;
			ccd.Id = this.Id;
			ccd.Name = this.originalName;
			ccd.Size = this.fileSize;

			return ccd;
		}
		*/

		static public void LoadFile(string path, NodeInfo myNode, out ContentCommonData ccd, out PossesionInfo pi) {
			FileStream fs           = new FileStream(path, FileMode.Open);
			string     originalName = fs.Name;
			long       fileSize     = fs.Length;
			Hash       hash         = new Hash(HashWrapper.ComputeHash(fs,HashAlgorithm.SHA1));

			ccd = new ContentCommonData(originalName, fileSize, hash);
			pi  = new PossesionInfo(ccd.FileHash, myNode.NodeHash, true);
		}
	}

	/// <summary>
	/// ノードごとのコンテンツ所有状況のデータ
	/// </summary>
	[Serializable]
	public class PossesionInfo : IEquatable<PossesionInfo> {
		public enum BlockState : byte {
			Complete,
			Incomplete,
			Unknown,
		}
        protected System.Collections.BitArray bitBlockArray = null;
        protected Hash       contentHash = null;
		protected Hash       nodeHash    = null;
		protected DateTime?  contentTime = null;
        protected BlockState state       = BlockState.Unknown;
        protected long       fileSize    = 0;//現状でのファイルサイズが合った方がいいだろう．


		public Hash ContentHash {
			get { return this.contentHash; }
		}

		public Hash NodeHash {
			get { return this.nodeHash; }
		}

		//マルチスレッド同期用
		private object SyncObject = new object();

		private void InitBlockState(int blockCount, bool initState) {
			bitBlockArray = new System.Collections.BitArray(bitBlockArray);
			bitBlockArray.SetAll(initState);
		}

		public PossesionInfo(Hash contentHash, Hash nodeHash, bool isComplete) {
			this.contentHash = contentHash;
			this.nodeHash = nodeHash;

			InitBlockState(this.BlockCount, isComplete);
		}

		public PossesionInfo(Hash contentHash, Hash nodeHash, byte[] blockArray,int blockCount) {
			this.contentHash          = contentHash;
			this.nodeHash             = nodeHash;
			this.bitBlockArray        = new System.Collections.BitArray(blockArray);
			this.bitBlockArray.Length = blockCount;

		}

		//コンテンツ状態関連
		#region コンテンツ状態関連

		public BlockState State {
			get {
				return CheckState();
			}
		}

		public System.Collections.BitArray BitBlockArray {
			get { return this.bitBlockArray;}
		}


		public void CheckActualFileExist(string filePath) {
			bool b = File.Exists(filePath);
			if (!b) {
				ClearBlock();
			}
		}

		public int BlockCount {
			get { return bitBlockArray.Length; }
		}


		/// <summary>
		/// ファイルがコンプリート状態かどうか調べる
		/// </summary>
		/// <returns></returns>
		public BlockState CheckState() {
			lock (SyncObject) {
				for (int i = 0; i < bitBlockArray.Length; i++) {
					if (false == bitBlockArray[i]) {
						this.state = BlockState.Incomplete;
						return state;
					}
				}
			}

			state = BlockState.Complete;
			return state;
		}

		public void ClearBlock() {
			lock (SyncObject) {
				bitBlockArray.SetAll(false);
				state = BlockState.Incomplete;
			}
		}

		public void FillBlock() {
			lock (SyncObject) {
				bitBlockArray.SetAll(true);
				state = BlockState.Complete;
			}
		}


		#endregion

		public bool isComplete {
			get {
				CheckState();
				return state == BlockState.Complete;
			}
			set {
				if (value == true) {
					FillBlock();
				}
			}
		}

		//ブロックの状態を取得する関連
		#region ブロックの状態を取得する関連

		/// <summary>
		/// returnがnullならBlankBlockがない
		/// </summary>
		/// <returns></returns>
		public IEnumerable<int> GetBlankBlock() {
			List<int> blankIndeces = new List<int>();
			for (int i = 0; i < bitBlockArray.Length; i++) {
				if (bitBlockArray[i] == false) {
					blankIndeces.Add(i);
				}
			}

			return blankIndeces;
		}

		public System.Collections.BitArray GetBlankBit() {
			return bitBlockArray;
		}

		public int[] GetBlankBlockArray() {
			return GetBlankBlockList().ToArray();
		}

		public List<int> GetBlankBlockList() {
			var rs = GetBlankBlock();
			return new List<int>(rs);
		}



		public IEnumerable<int> GetEnableBlock() {
			List<int> blankIndeces = new List<int>();
			for (int i = 0; i < bitBlockArray.Length; i++) {
				if (bitBlockArray[i] == true) {
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
					bitBlockArray[number] = true;
				}
				catch (IndexOutOfRangeException) {
					//握りつぶす//これは別にいいよね
				}
			}
		}

		public void UnputBlock(int number) {
			lock (SyncObject) {
				try {
					bitBlockArray[number] = false;
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
					b = bitBlockArray[number];
				}
				catch (ArgumentOutOfRangeException) {
					//つぶす//bはfalseのままだからいいよね
				}
			}
			return b;
		}




		#endregion

		#region IEquatable<ContentDataPerNode> メンバ

		public bool Equals(PossesionInfo other) {
			return this.contentHash.Equals(other.contentHash) && this.nodeHash.Equals(other.nodeHash);
		}

		#endregion

		public override int GetHashCode() {
			return (contentHash.Str + nodeHash.Str).GetHashCode();
		}

		/*
		public Database.PosessionInfo ToDatabaseData() {
			Database.PosessionInfo pi = new P2P.Database.PosessionInfo();

			pi.ContentHash = this.contentHash.Str;
			pi.ContentTime = this.contentTime;
			pi.NodeHash = this.nodeHash.Str;

			byte[] fileBlock = new byte[this.bitBlockArray.Length / 8 + 1];
			this.bitBlockArray.CopyTo(fileBlock, 0);
			pi.FileBlock = fileBlock;
			pi.FileBlockCount = this.bitBlockArray.Length;	

			return pi;
		}
		 * */

	}

	public class PossesionList : IList<PossesionInfo> {
		private List<PossesionInfo> list = new List<PossesionInfo>();

		public bool IsComplete {
			get {
				System.Collections.BitArray ba = GetAllBitBlock();

				for(int i = 0;i < ba.Length;i++){
					if (!ba[i]) {
						return false;
					}
				}
				return true;
			}
		}

		public System.Collections.BitArray GetAllBitBlock() {
			int len = list[0].BlockCount;//どれも同じ長さ
			System.Collections.BitArray ba = new System.Collections.BitArray(len);

			foreach (var v in list) {
				ba = v.BitBlockArray.Or(ba);
			}
			return ba;
		}

		#region IList<PossesionInfo> メンバ

		public int IndexOf(PossesionInfo item) {
			return list.IndexOf(item);
		}

		public void Insert(int index, PossesionInfo item) {
			list.Insert(index,item);
		}

		public void RemoveAt(int index) {
			list.RemoveAt(index);
		}

		public PossesionInfo this[int index] {
			get {
				return list[index];
			}
			set {
				list[index] = value;
			}
		}

		#endregion

		#region ICollection<PossesionInfo> メンバ

		public void Add(PossesionInfo item) {
			list.Add(item);
		}

		public void Clear() {
			list.Clear();
		}

		public bool Contains(PossesionInfo item) {
			return list.Contains(item);
		}

		public void CopyTo(PossesionInfo[] array, int arrayIndex) {
			list.CopyTo(array, arrayIndex);
		}

		public int Count {
			get { return list.Count; }
		}

		public bool IsReadOnly {
			get { return true; }
		}

		public bool Remove(PossesionInfo item) {
			return list.Remove(item);
		}

		#endregion

		#region IEnumerable<PossesionInfo> メンバ

		public IEnumerator<PossesionInfo> GetEnumerator() {
			return list.GetEnumerator();
		}

		#endregion

		#region IEnumerable メンバ

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return list.GetEnumerator();
		}

		#endregion
	}

	public enum ContentState : byte {
		Complete,//ローカルでコンプリートしている
		Downloading,//現在ダウン中
		Network,//ネットワークからコンテンツ情報を取得した
		Local,//ファイルは無いが，ローカルにコンテンツ情報がある
		Unknown,
	}
}
