using System.Collections.Generic;
using System.IO;
using P2P.BBase;

namespace P2P
{
    /*
	 * キャッシュのための動作
	 * データベースからデータを読み込む．
	 * アップロードフォルダからファイルを読み込む．
	 * 
	 * データベースのデータとアップロードフォルダのデータを比較する．
	 * データベースに無いやつは新規にアップロードされたやつなので
	 * キャッシュ化する．
	 * 
	 * データベースのデータがキャッシュフォルダにあるかを確かめることも必要
	 * 
	 */

    public class DataStorage {
		static public void Save(string filename, object o) {
			var ba = ByteArraySerializeHelper.Serialize(o);
			var fs = new FileStream(filename, FileMode.Create);
			fs.Write(ba, 0, ba.Length);
			fs.Close();
		}

		static public object Load(string filename) {
			var fs = new FileStream(filename, FileMode.Open);
			var ba = new byte[fs.Length];

			fs.Read(ba, 0, ba.Length);
			fs.Close();

			//return ByteArraySerializeHelper.Deserialize(ba);
			return ByteArraySerializeHelper.Deserialize<object>(ba); //2024-11-21
		}
	}

	public class CacheCreator {
		/// <summary>
		/// リストになくてファイルとしてあるのは別にいい，というかどうしようもない
		/// リストにあってファイルとしてないデータは消す
		/// </summary>
		/// <param name="cacheFolder"></param>
		/// <param name="fileList"></param>
		static public void CheckCache(string cacheFolder, ref List<FileDataInfo> fileList) {
			var files   = Directory.GetFiles(cacheFolder);
			var newList = new List<FileDataInfo>();

			foreach (var fdi in fileList) {
				bool b = File.Exists(Path.Combine(cacheFolder, fdi.BaseHash.Str));
				if (b) {
					newList.Add(fdi);
				}
			}
			fileList = newList;
		}

		//キャッシュのファイル名はハッシュを16進数にした文字列
		/// <summary>
		/// フォルダは存在するものとしてやるのでもしなかったら例外
		/// </summary>
		/// <param name="upLoadFolder"></param>
		/// <param name="cacheFolder"></param>
		/// <param name="fileList"></param>
		static public void Createcache(string upLoadFolder, string cacheFolder, ref List<FileDataInfo> fileList) {

			CheckCache(cacheFolder, ref fileList);
			List<FileDataInfo> newList = new List<FileDataInfo>();

			var files = Directory.GetFiles(upLoadFolder);
			var list  = new List<string>();

			foreach (var s in files) {
				if (Path.GetExtension(s) == ".flv" || Path.GetExtension(s) == ".mp4") {
					list.Add(s);
				}
			}

			// 拡張子 ".flv" を持つファイル名を表示
			files = list.ToArray();

			foreach (var filepath in files) {
				var fi = new FileInfo(filepath);
				var b  = fileList.Exists(delegate(FileDataInfo fdi) {
					//ファイル名とサイズがリストの要素と一致したらある
					var isName = fi.Name   == fdi.Name;
					var isSize = fi.Length == fdi.FileSize;

					return isName && isSize;
				});

				//ファイルがリストになくてアップフォルダにある場合
				if (!b) {
					//新しくファイルデータを作る
					var f = FileDataInfo.LoadFile(filepath);

					File.Copy(filepath, Path.Combine(cacheFolder, f.BaseHash.Str), true);

					newList.Add(f);
				}
			}
			fileList.AddRange(newList);
		}
	}

	public class PeerCacheHelper {
		/*
		private List<string> uploadFileList = null;
		public PeerCacheHelper() {

		}

		static public bool CheckCacheFolder(string cachePath, Hash fileHash) {
			var files = Directory.GetFiles(cachePath);
			return files.Contains(fileHash.Str);
		}

		static public void MakeCache(string filePath, string cachePath, NodeInfo myNode, out ContentCommonData ccd, out PossesionInfo pi) {
			ContentCommonData.LoadFile(filePath, myNode, out ccd, out pi);
			File.Copy(filePath, cachePath);
		}

		static public string[] GetNotCacheFilePaths(string uploadPath, PeerData pd) {
			var files = Directory.GetFiles(uploadPath);

			if (!string.IsNullOrEmpty(filter)) {
				// 拡張子からファイルリストへのルックアップテーブルを作成
				var extToFiles = files.ToLookup(f => Path.GetExtension(f));
				files = extToFiles[filter].ToArray();//フィルターした値を入れなおす
			}

			var q = files.Where(t => t == pd.ContentCommonDataDictionary.Values.Select(c => c.FileName)).ToArray();


			PeerLogger.Instance.OnMassage(
				PeerLogger.MessageLevel.Info,
				"FileFound", uploadFileList.Count + "のファイルをアップロードフォルダに発見しました");
		}

		public void LoadUploadFolder(string uploadPath, string filter) {
			var files = Directory.GetFiles(uploadPath);

			if (string.IsNullOrEmpty(filter)) {
				uploadFileList = new List<string>(files);
			}
			else {
				// 拡張子からファイルリストへのルックアップテーブルを作成
				var extToFiles = files.ToLookup(f => Path.GetExtension(f));
				uploadFileList = new List<string>(extToFiles[filter]);

			}

			PeerLogger.Instance.OnMassage(
				PeerLogger.MessageLevel.Info,
				"FileFound", uploadFileList.Count + "のファイルをアップロードフォルダに発見しました");
		}
		*/
		/*
		public List<DataType.ContentCommonData> CheckCacheFolder(string cachePath, string uploadPath) {
			var cachefiles = Directory.GetFiles(cachePath);

			var cacheNames = cachefiles.Select(n => Path.GetFileName(n));


			foreach (var path in filteredFiles) {
				ContentCommonData ci = ContentCommonData.LoadFile(path);
				ContentMetaData md = null;
				ThumbnailData td = null;

				string pathExceptExt = Path.GetFileNameWithoutExtension(path);

				string filePath = pathExceptExt + ".xml";
				if (File.Exists(filePath)) {
					md = ContentMetaData.LoadFile(ci.ContentHash, filePath);
				}

				filePath = pathExceptExt + ".jpg";
				if (File.Exists(filePath)) {
					td = ThumbnailData.LoadFile(ci.ContentHash, filePath);
				}

				
			}

		}
		 * */
	}
}
