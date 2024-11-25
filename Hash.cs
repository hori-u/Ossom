using System;
using System.Security.Cryptography;

namespace P2P
{

    static public class HashWrapper {
		static MD5CryptoServiceProvider md5   = new MD5CryptoServiceProvider();
		static SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider();

		static public byte[] ComputeHash( byte[] buffer, HashAlgorithm algo) {
			if      (algo == HashAlgorithm.MD5)  return md5.ComputeHash (buffer);
			else if (algo == HashAlgorithm.SHA1) return sha1.ComputeHash(buffer);

			return null;
		}

		static public byte[] ComputeHash( System.IO.Stream fs, HashAlgorithm algo) {
			if      (algo == HashAlgorithm.MD5)  return md5.ComputeHash (fs);
			else if (algo == HashAlgorithm.SHA1) return sha1.ComputeHash(fs);

			return null;
		}

		static public byte[] ComputeHash( byte[] buffer, HashAlgorithm algo, int offset, int count) {
			if      (algo == HashAlgorithm.MD5)  return md5.ComputeHash (buffer, offset, count);
			else if (algo == HashAlgorithm.SHA1) return sha1.ComputeHash(buffer, offset, count);

			return null;
		}

	}


	public enum HashAlgorithm { SHA1, MD5, };

	[Serializable]
	public class Hash :IEquatable<Hash> {
		protected byte[] hashData = null;

		public Hash(byte[] hash) {
			this.hashData = hash;
		}

		public Hash(string hash) {
			this.hashData = BTool.ByteParse(hash);
		}

		public byte[] ByteData {
			get {
                return hashData;
            }
			set {
				SetHash(value);
			}
		}

		/// <summary>
		/// ビット数
		/// </summary>
		public int Length {
			get {
                return hashData.Length * 8;
            }
		}


		/// <summary>
		/// 未チェック
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public bool this[int index] {
			get {
				int len = this.Length;
				if (index > len) throw new IndexOutOfRangeException();

				int i = index / 8;
				int j = index % 8;

				return (hashData[i] & (1 << j)) != 0;
			}

			set {
				int len = this.Length;
				if (index > len) throw new IndexOutOfRangeException();

				int i = index / 8;
				int j = index % 8;

				int k = i << j;

				if (value == false) {
					k = ~k;
					hashData[i] = (byte)((int)hashData[i] & k);
				}
				else {
					hashData[i] = (byte)((int)hashData[i] | k);
				}
			}

		}

		public string Str {
			get {
                return BTool.StringParse(hashData);
            }
			set {
				byte[] hash = BTool.ByteParse(value);
				SetHash(hash);
			}
		}

		private void SetHash(byte[] hash) {
			bool b = CheckHashFormat(hash);
			if (b) {
				hashData = hash;
			}
			else {
				throw new ApplicationException("ハッシュの形式が正しくないです．");
			}
		}

		static bool CheckLength(Hash x, Hash y) {
			return x.Length == y.Length;
		}

		public Hash And(Hash hash) {
			if (!CheckLength(this, hash)) new ApplicationException("ハッシュの長さが違います");

			int len = this.hashData.Length;
			byte[] ret = new byte[len];

			for (int i = 0; i < len; i++) {
				ret[i] = (byte)(this.hashData[i] & hash.hashData[i]);
			}
			return new Hash(ret);
		}


		public Hash Not() {

			int len = this.hashData.Length;
			byte[] ret = new byte[len];

			for (int i = 0; i < len; i++) {
				ret[i] = (byte)(~this.hashData[i]);
			}
			return new Hash(ret);
		}

		public Hash Or(Hash hash) {
			if (!CheckLength(this, hash)) new ApplicationException("ハッシュの長さが違います");

			int len = this.hashData.Length;

			byte[] ret = new byte[len];

			for (int i = 0; i < len; i++) {
				ret[i] = (byte)(this.hashData[i] | hash.hashData[i]);
			}
			return new Hash(ret);
		}

		public Hash Xor(Hash hash) {
			if (!CheckLength(this, hash)) new ApplicationException("ハッシュの長さが違います");

			int len = this.hashData.Length;
			byte[] ret = new byte[len];

			for (int i = 0; i < len; i++) {
				ret[i] = (byte)(this.hashData[i] ^ hash.hashData[i]);
			}
			return new Hash(ret);
		}


		virtual public bool CheckHashFormat(byte[] hash) {
			return true;
		}


		#region IEquatable<Hash> メンバ

		public bool Equals(Hash other) {
			return BTool.ArrayCompare(this.ByteData, other.ByteData);
		}

		#endregion

		public override bool Equals(object obj) {
			Hash other = obj as Hash;
			if (null == other) return false;
			return this.Equals(other);
		}

		public override int GetHashCode() {
			//return this.hashData.GetHashCode();//こっちじゃだめかな
			return this.Str.GetHashCode();
		}
	}

	public class Sha1Hash : Hash {
		const HashAlgorithm algo = HashAlgorithm.SHA1;

		public Sha1Hash(byte[] hash)
			: base(hash) {

		}

		public Sha1Hash(string hash)
			: base(hash) {
		}

		public override bool CheckHashFormat(byte[] hash) {
			return (hash.Length == 20);//SHA-1は160ビット
		}
	}

	public class MD5Hash : Hash {
		const HashAlgorithm algo = HashAlgorithm.MD5;

		public MD5Hash(byte[] hash)
			: base(hash) {

		}

		public MD5Hash(string hash)
			: base(hash) {

		}

		public override bool CheckHashFormat(byte[] hash) {
			return (hash.Length == 16);//MD5は128ビット
		}
	}
}
