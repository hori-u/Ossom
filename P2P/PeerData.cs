using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using System.Net;
using System.Net.Sockets;
using System.IO;
using P2P.DataType;

namespace P2P {

	/// <summary>
	/// データベース，ディスクに蓄えられる性的なデータ．．．かも
	/// </summary>
	public class PeerData {
		const string DBName = "PeerDB.sdf";

		protected Dictionary<Hash, ContentCommonData> contentCommonDataDictionary = new Dictionary<Hash, ContentCommonData>();
		protected Dictionary<Hash, NodeInfo> nodeDictionary = new Dictionary<Hash, NodeInfo>();
		protected NodeInfo myNode = null;
		protected Dictionary<Hash, PossesionInfo> myPossesionDictionay = new Dictionary<Hash, PossesionInfo>();
		protected MultiDictionary<Hash, PossesionInfo> possetionDictionary = new MultiDictionary<Hash, PossesionInfo>();

		public Dictionary<Hash, ContentCommonData> ContentCommonDataDictionary {
			get { return contentCommonDataDictionary; }
			set { contentCommonDataDictionary = value; }
		}

		public Dictionary<Hash, NodeInfo> NodeDictionary {
			get { return nodeDictionary; }
			set { nodeDictionary = value; }
		}

		public NodeInfo MyNode {
			get { return myNode; }
			set { myNode = value; }
		}

		public Dictionary<Hash, PossesionInfo> MyPossesionDictinary {
			get { return myPossesionDictionay; }
			set { myPossesionDictionay = value; }
		}

		public MultiDictionary<Hash, PossesionInfo> PossetionDictinary {
			get { return possetionDictionary; }
			set { possetionDictionary = value; }
		}

		/*
		public void ReadDB() {
			using (var db = new Database.PeerDB(DBName)) {
				var contentList = db.ContentCommonDataList;
				foreach (var c in contentList) {
					var ccd = c.ToNormalData();
					contentCommonDataDictionary.Add(ccd.FileHash, ccd);
				}

				var nodeList = db.NodeInfoList;
				foreach (var n in nodeList) {
					var ni = n.ToNormalData();
					nodeDictionary.Add(ni.NodeHash, ni);
				}

				var myPossesList = db.MyPosessionInfoList;
				foreach (var m in myPossesList) {
					var mp = m.ToNormalData();
					myPossesionDictionay.Add(mp.ContentHash, mp);
				}

				var possesList = db.PosessionInfoList;
				foreach (var p in possesList) {
					var pi = p.ToNormalData();
					possetionDictionary.Add(pi.ContentHash, pi);
				}
			}

		}
		
		
		public void WriteDB() {
			using (var db = new Database.PeerDB(DBName)) {
				db.DeleteDatabase();
				db.CreateDatabase();

				db.ContentCommonDataList.InsertAllOnSubmit(contentCommonDataDictionary.Values.Select(t => t.ToDatabaseData()));
				db.NodeInfoList.InsertAllOnSubmit(nodeDictionary.Values.Select(t => t.ToDatabaseData()));
				db.MyPosessionInfoList.InsertAllOnSubmit(myPossesionDictionay.Values.Select(t => t.ToDatabaseData()));
				foreach (var list in possetionDictionary.Values) {
					if (list.Count != 0) {
						db.PosessionInfoList.InsertAllOnSubmit(list.Select(t => t.ToDatabaseData()));
					}
				}
				db.SubmitChanges();
			}
		}
		
		*/
		protected LinkedList<DataType.ContentCommonData> downloadList = new LinkedList<DataType.ContentCommonData>();

	}





	public class PeerStaticData {
		public const long FileDivideSize = 512 * 1024;
		public const int Version = 1;
	}



	public class CacheList<T> : LinkedList<WeakReference> {
		private LinkedList<WeakReference> list = null;


		public CacheList() {
			list = new LinkedList<WeakReference>();
		}

		public void AddLast(T t) {
			list.AddLast(new WeakReference(t));
		}

		public void AddFirst(T t) {
			list.AddFirst(new WeakReference(t));
		}

		new public void RemoveFirst() {
			list.RemoveFirst();
		}

		new public void RemoveLast() {
			list.RemoveLast();
		}

		public void Remove(T t) {
			list.Remove(new WeakReference(t));
		}


	}

	public class CacheDictionary<Key, Value> {
		private Dictionary<Key, WeakReference> dic = null;

		public CacheDictionary() {
			dic = new Dictionary<Key, WeakReference>();
		}

		public CacheDictionary(int capacity) {
			dic = new Dictionary<Key, WeakReference>(capacity);
		}

		public CacheDictionary(IEqualityComparer<Key> comparer) {
			dic = new Dictionary<Key, WeakReference>(comparer);
		}

		public CacheDictionary(int capacity, IEqualityComparer<Key> comparer) {
			dic = new Dictionary<Key, WeakReference>(capacity, comparer);
		}

		public void Add(Key key, Value value) {
			dic.Add(key, new WeakReference(value));
		}

		public bool TryGetValue(Key key, out Value value) {
			WeakReference wref;
			bool b = dic.TryGetValue(key, out wref);
			if (wref != null) {
				value = (Value)wref.Target;
				return true;
			}
			else {
				value = default(Value);
				return false;
			}
		}

		public Value this[Key key] {
			get {
				WeakReference wref = dic[key];
				if (!wref.IsAlive) {
					return default(Value);
				}
				else {
					return (Value)wref.Target;
				}
			}
			set {
				dic[key] = new WeakReference(value);
			}
		}

		public bool ContainsValue(Value value) {
			WeakReference wref = new WeakReference(value);
			return dic.ContainsValue(wref);

		}

		public bool ContainsKey(Key key) {
			return dic.ContainsKey(key);
		}

		public int Count {
			get { return dic.Count; }
		}

		public IEqualityComparer<Key> Comparer {
			get { return dic.Comparer; }
		}

		public void Clear() {
			dic.Clear();
		}

		public Dictionary<Key, WeakReference>.KeyCollection Keys {
			get { return dic.Keys; }
		}

		public void Remove(Key key) {
			dic.Remove(key);
		}

	}





}