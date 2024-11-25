using System;
using System.Collections.Generic;

namespace P2P
{
    /// <summary>
    /// 重複を許すDictionary
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="V"></typeparam>
    public class MultiDictionary<T, V> : IDictionary<T, List<V>> {
		private Dictionary<T, List<V>> dic;

		public MultiDictionary(){
			dic = new Dictionary<T,List<V>>();
		}

		public MultiDictionary(int capacity){
			dic = new Dictionary<T,List<V>>(capacity);
		}

		public MultiDictionary(IEqualityComparer<T> comparer){
			dic = new Dictionary<T,List<V>>(comparer);
		}

		public MultiDictionary(IDictionary<T,V> dictionary){
			dic = new Dictionary<T,List<V>>(dic.Count);
			foreach(var k in dictionary.Keys){
                Add(k,dictionary[k]);
			}
		}

		public MultiDictionary(IDictionary<T,List<V>> dictionary){
			dic = new Dictionary<T,List<V>>(dictionary);
		}

		public MultiDictionary(int capacity,IEqualityComparer<T> comparer){
			dic = new Dictionary<T,List<V>>(capacity,comparer);
		}

		public MultiDictionary(IDictionary<T,List<V>> dictionary,IEqualityComparer<T> comparer){
			dic = new Dictionary<T,List<V>>(dictionary,comparer);
		}

		public MultiDictionary(IDictionary<T,V> dictionary,IEqualityComparer<T> comparer){
			dic = new Dictionary<T,List<V>>(comparer);

			foreach(var k in dictionary.Keys){
				this.Add(k,dictionary[k]);
			}
		}

		/// <summary>
		/// 追加
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void Add(T key, V value) {
			var l = dic[key];
			l.Add(value);
		}

		#region IDictionary<T,List<V>> メンバ

		public void Add(T key, List<V> value) {
			var l = dic[key];
			l.AddRange(value);
		}

		public bool ContainsKey(T key) {
			return dic.ContainsKey(key);
		}

		public ICollection<T> Keys {
			get { return dic.Keys; }
		}

		public bool Remove(T key) {
			return dic.Remove(key);
		}

		public bool TryGetValue(T key, out List<V> value) {
			return dic.TryGetValue(key, out value);
		}

		public ICollection<List<V>> Values {
			get { return dic.Values; }
		}

		public List<V> this[T key] {
			get {
				return dic[key];
			}
			set {
				dic[key] = value;
			}
		}

		#endregion

		#region ICollection<KeyValuePair<T,List<V>>> メンバ
		/// <summary>
		/// 追加
		/// </summary>
		/// <param name="item"></param>
		public void Add(KeyValuePair<T, V> item) {
			this.Add(item.Key, item.Value);
		}

		public void Add(KeyValuePair<T, List<V>> item) {
			this.Add(item.Key, item.Value);
		}

		public void Clear() {
			dic.Clear();
		}

		/// <summary>
		/// 追加
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public bool Contains(KeyValuePair<T, V> item) {
			var l = dic[item.Key];
			return l.Contains(item.Value);		
		}
		
		/*
		/// <summary>
		/// 追加
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public bool Contains(KeyValuePair<T, V> item,IEqualityComparer<V> comparer) {
			var l = dic[item.Key];
			return l.Contains(item.Value,comparer);		
		}
		 */

		/// <summary>
		/// 指定したリストの中のどれかを持っているか
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public bool Contains(KeyValuePair<T, List<V>> item) {
			var l = dic[item.Key];

			foreach (var v in item.Value) {
				bool b = l.Contains(v);
				if (b) {
					return true;
				}
			}

			return false;
		}

		/*
		public bool Contains(KeyValuePair<T, List<V>> item, IEqualityComparer<V> comparer) {
			var l = dic[item.Key];

			foreach (var v in item.Value) {
				bool b = l.Contains(v, comparer);
				if (b) {
					return true;
				}
			}

			return false;
		}
		*/

		/// <summary>
		/// 追加
		/// </summary>
		/// <param name="array"></param>
		/// <param name="arrayIndex"></param>
		public void CopyTo(KeyValuePair<T, V>[] array, int arrayIndex) {
			for (int i = arrayIndex; i < array.Length; i++) {
				this.Add(array[i]);
			}
		}

		public void CopyTo(KeyValuePair<T, List<V>>[] array, int arrayIndex) {
			for (int i = arrayIndex; i < array.Length; i++) {
				this.Add(array[i]);
			}
		}

		public int Count {
			get { return dic.Count; }
		}

		/// <summary>
		/// 追加
		/// </summary>
		public int AllCount {
			get {
				int count = 0;
				foreach (var v in dic.Values) {
					count += v.Count;
				}
				return count;
			}
		}

		public bool IsReadOnly {
			get { return false; }
		}

		public bool Remove(KeyValuePair<T, List<V>> item) {
			try {
				var l = dic[item.Key];
				bool r = false;
				foreach (var v in item.Value) {
					r = r | l.Remove(v);
				}

				if (l.Count == 0) {
					dic.Remove(item.Key);
				}

				return r;
			}
			catch (Exception) {
				return false;
			}
		}

		#endregion

		#region IEnumerable<KeyValuePair<T,List<V>>> メンバ

		public IEnumerator<KeyValuePair<T, List<V>>> GetEnumerator() {
			return dic.GetEnumerator();
		}

		#endregion

		#region IEnumerable メンバ

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return dic.GetEnumerator();
		}

		#endregion
	}
}
