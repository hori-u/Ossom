using System;
using System.Collections.Generic;

namespace P2P
{

    public class DualKey<TMainKey, TSubKey> {
		private TMainKey mainKey;
		private TSubKey subKey;

		public TMainKey MainKey {
			get {
                return mainKey;
            }
			set {
                mainKey = value;
            }
		}

		public TSubKey Subkey {
			get {
                return subKey;
            }
			set {
                subKey = value;
            }
		}

		public DualKey(TMainKey mainKey, TSubKey subKey) {
			this.mainKey = mainKey;
			this.subKey = subKey;
		}
	}

	public class DualKeyDictionary<TMainKey,TSubKey,TValue> : IDictionary<TMainKey,TValue>  {

		private Dictionary<TMainKey, TValue> mainKeyDictionary;
		private Dictionary<TSubKey, TMainKey> subKeyDictionary;

		public DualKeyDictionary() {
			Initialize(null, 0, null, 0);
		}

		public DualKeyDictionary(int capacity) {
			Initialize(null, capacity, null, capacity);
		}

		public DualKeyDictionary(IEqualityComparer<TMainKey> mainComparer,IEqualityComparer<TSubKey> subComparer) {
			Initialize(mainComparer,0, subComparer,0);
		}

		public DualKeyDictionary(int capacity,IEqualityComparer<TMainKey> mainComparer, IEqualityComparer<TSubKey> subComparer) {
			Initialize(mainComparer, capacity, subComparer, capacity);
		}

		public void Initialize(IEqualityComparer<TMainKey> mainComparer,int mainCapacity, IEqualityComparer<TSubKey> subComparer,int subCapacity) {
			if (mainComparer != null) {
				mainKeyDictionary = new Dictionary<TMainKey, TValue>(mainCapacity,mainComparer);
			}
			else {
				mainKeyDictionary = new Dictionary<TMainKey, TValue>(mainCapacity);
			}

			if (subComparer != null) {
				subKeyDictionary = new Dictionary<TSubKey, TMainKey>(subCapacity,subComparer);
			}
			else {
				subKeyDictionary = new Dictionary<TSubKey, TMainKey>(subCapacity);
			}
		}

		public DualKeyDictionary(Dictionary<TMainKey, TValue> dictinary) {
			mainKeyDictionary = new Dictionary<TMainKey, TValue>(dictinary);
			subKeyDictionary  = new Dictionary<TSubKey, TMainKey>();
		}


		#region IDictionary<MainKey,TValue> メンバ

		public void Add(DualKey<TMainKey, TSubKey> keys, TValue value) {
			mainKeyDictionary.Add(keys.MainKey, value);
			subKeyDictionary.Add(keys.Subkey, keys.MainKey);
		}

		public void Add(TMainKey main, TSubKey sub, TValue value) {
			mainKeyDictionary.Add(main, value);
			subKeyDictionary.Add(sub, main);
		}

		public void Add(TMainKey key, TValue value) {
			mainKeyDictionary.Add(key, value);
		}

		public bool ContainsKey(TMainKey key) {
			return mainKeyDictionary.ContainsKey(key);
		}

		public bool ContainsSubKey(TSubKey key) {
			return subKeyDictionary.ContainsKey(key);
		}

		public ICollection<TMainKey> Keys {
			get { return mainKeyDictionary.Keys; }
		}

		public ICollection<TSubKey> SubKeys {
			get { return subKeyDictionary.Keys; }
		}

		/// <summary>
		/// 遅いからサブキーで削除できるときはサブキーで削除
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public bool Remove(TMainKey key) {
			bool b = mainKeyDictionary.Remove(key);

			foreach (var k in subKeyDictionary.Keys) {
				var v = subKeyDictionary[k];
				if (v.Equals(key)) {
					subKeyDictionary.Remove(k);
					break;
				}
			}

			return b;
		}

		public bool RemoveFromSubKey(TSubKey key){
			TMainKey main;
			if (subKeyDictionary.TryGetValue(key, out main)) {
				subKeyDictionary.Remove(key);
				return mainKeyDictionary.Remove(main);
			}
			return false;
		}

		public bool TryGetValue(TMainKey key, out TValue value) {
			return mainKeyDictionary.TryGetValue(key, out value);
		}

		public bool TryGetValueFromSubKey(TSubKey key, out TValue value) {
			TMainKey key1;
			if (subKeyDictionary.TryGetValue(key, out key1)) {
				return mainKeyDictionary.TryGetValue(key1, out value);
			}
			value = default(TValue);
			return false;
		}

		public ICollection<TValue> Values {
			get { return mainKeyDictionary.Values; }
		}

		public TValue this[TMainKey key] {
			get {
				return mainKeyDictionary[key];
			}
			set {
				mainKeyDictionary[key] = value;
			}
		}

		public TValue GetValueFromSubKey(TSubKey key) {
			return mainKeyDictionary[subKeyDictionary[key]];
		}

		public void SetValueFromSubKey(TSubKey key, TValue value) {
			mainKeyDictionary[subKeyDictionary[key]] = value;
		}

		#endregion

		#region ICollection<KeyValuePair<MainKey,TValue>> メンバ

		public void Add(KeyValuePair<TMainKey, TValue> item) {
			this.Add(item.Key, item.Value);
		}

		public void Add(KeyValuePair<DualKey<TMainKey,TSubKey>, TValue> item) {
			this.Add(item.Key.MainKey, item.Key.Subkey, item.Value);
		}

		public void Clear() {
			this.mainKeyDictionary.Clear();
			this.subKeyDictionary.Clear();
		}

		public bool Contains(KeyValuePair<TMainKey, TValue> item) {
			try {
				return mainKeyDictionary[item.Key].Equals(item.Value);
			}
			catch (Exception) {
				return false;
			}
		}

		public void CopyTo(KeyValuePair<TMainKey, TValue>[] array, int arrayIndex) {
			throw new NotImplementedException();
		}

		public int Count {
			get { return mainKeyDictionary.Count; }
		}

		public bool IsReadOnly {
			get { return false; }
		}

		public bool Remove(KeyValuePair<TMainKey, TValue> item) {
			if(Contains(item)){
				return Remove(item.Key);
			}
			return false;
		}

		#endregion

		#region IEnumerable<KeyValuePair<MainKey,TValue>> メンバ

		public IEnumerator<KeyValuePair<TMainKey, TValue>> GetEnumerator() {
			return mainKeyDictionary.GetEnumerator();
		}

		#endregion

		#region IEnumerable メンバ

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return mainKeyDictionary.GetEnumerator();
		}

		#endregion
	}
}
