using System;

namespace P2P.DataType
{
    /// <summary>
    /// DHTのデータ
    /// </summary>
    [Serializable]
	public class DHTData<T> {
		protected Hash hash = null;
		protected T data = default(T);

		public Hash DataHash {
			get { return hash; }
			set { hash = value; }
		}

		public T Data {
			get { return data; }
			set { Data = value; }
		}
		public DHTData() {

		}
		public DHTData(Hash hash, T data) {
			this.hash = hash;
			this.data = data;
		}
	}
}
