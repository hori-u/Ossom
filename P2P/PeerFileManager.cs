using System;
using System.Collections.Generic;
using System.IO;
using P2P.BBase;
using P2P.BFileSystem;

namespace P2P {

	public class PeerFileManager : IDisposable {
		//ハッシュ値とファイルINFSO
		PeerSystem system = null;
		PeerConfig config = null;
		Dictionary<Hash, FileDataInfo> fileDictionary = null;
		IFileOpen fo = null;

		[Serializable]
		public class ByteArrayComparer : IEqualityComparer<byte[]> {

			#region IEqualityComparer<byte[]> メンバ

			public bool Equals(byte[] x, byte[] y) {
				return BTool.ArrayCompare(x, y);
			}

			public int GetHashCode(byte[] obj) {
				return BTool.StringParse(obj).GetHashCode();
			}

			#endregion
		}

		public PeerFileManager(PeerSystem ps,PeerConfig pc) {
			this.system = ps;
			this.config = pc;
			fileDictionary = new Dictionary<Hash, FileDataInfo>() ;
			fo = new FileStreamPool(s => new FileStream(s, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite));

		}

		public List<FileDataInfo> GetFileDataList() {
			return new List<FileDataInfo>(fileDictionary.Values);
		}

		public void AddFileDataList(List<FileDataInfo> fileList) {
			foreach (var v in fileList) {
				AddFileData(v);
			}
		}

		public void AddFileData(FileDataInfo fdi) {
			try {
				fileDictionary.Add(fdi.BaseHash, fdi);
			}
			catch (Exception) {

			}
			PreOpenFile(fdi);
		}

		public void AddFileData(ContentInfoBase cib) {
			if (cib == null) return;
			var fdi = new FileDataInfo(cib);
			try {
				fileDictionary.Add(cib.BaseHash, fdi);
			}
			catch (Exception) {

			}
			PreOpenFile(fdi);
		}

		private void PreOpenFile(FileDataInfo fdi) {
			if (fdi.GetBlankBlock() != null) {
				fo.Get(Path.Combine(system.Config.CachePath, fdi.BaseHash.Str));
			}

		}

		public FileDataInfo GetFileData(Hash hash) {
			FileDataInfo fdi;
			fileDictionary.TryGetValue(hash, out fdi);

			return fdi;
		}


		public FileDataState GetState(Hash hash) {
			FileDataInfo fdi = GetFileData(hash);

			if (null == fdi) {
				return FileDataState.Unknown;
			}

			return fdi.GetState();
		}

		public void WriteFile(Hash hash, DataSegment ds) {
			var fdi = GetFileData(hash);

			if (null == fdi) {
				return;
			}

			SegmentIO.WriteFileSegment(system.Config.CachePath, ds, fdi, fo);
		}


		public DataSegment ReadFile(Hash hash, int segnum) {
			var fdi = GetFileData(hash);

			if (null == fdi) {
				return null;
			}
			if (fdi.BlockState[segnum]) {
				DataSegment ds = SegmentIO.ReadFileSegment(system.Config.CachePath, segnum, fdi, fo);
				return ds;
			}
			return null;

		}

		private void CreateFileIfNotExist(FileDataInfo fdi) {
			bool b = CheckFileExist(fdi);

			//ファイルを作る
			fo.Get(Path.Combine(config.CachePath, fdi.FileName));

			ContentInfoBase cib = system.GetContentInfo(fdi.BaseHash);

			FileDataInfo f = new FileDataInfo(cib);
			this.fileDictionary.Add(f.BaseHash, f);

		}

		private bool CheckFileExist(FileDataInfo fdi){
			string path = Path.Combine(config.CachePath, fdi.FileName);

			return File.Exists(path);

		}

		


		#region IDisposable メンバ

		public void Dispose() {
			var dis = this.fo as IDisposable;
			if (null != dis) {
				dis.Dispose();
			}
		}

		#endregion
	}

}
