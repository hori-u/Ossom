using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;

namespace P2P
{
    /*
	public static class Enum<T> where T : struct {
		private readonly static T[] values;
		private readonly static string[] strings;

		static Enum() {
			Debug.Assert(typeof(T).IsEnum);
			Debug.Assert(!Attribute.IsDefined(typeof(T),
			  typeof(FlagsAttribute))); // 1/28追加
			values = Enum.GetValues(typeof(T)).Cast<T>().ToArray();
			strings = Enum.GetNames(typeof(T));
			Array.Sort<string, T>(strings, values); // 1/28追加
		}

		public static T Parse(string value) {
			int n = Array.IndexOf<string>(strings, value);
			if (n < 0)
				throw new ArgumentException();
			return values[n];
		}

		public static IEnumerable<T> GetValues() {
			foreach (var item in values) {
				yield return item;
			}
		}
	}

	*/

    public class ByteArraySerializeHelper {
		//初めての拡張メソッド
		/*static BinaryFormatter bf = new BinaryFormatter();

		static public byte[] Serialize(object gruph) {
			MemoryStream ms = new MemoryStream();
			bf.Serialize(ms, gruph);
			return ms.ToArray();
		}

		static public T Deserialize<T>(byte[] buffer) {
			MemoryStream ms = new MemoryStream(buffer);
			object o = bf.Deserialize(ms);
			return (T)o;//o as T　にしたかったけどクラス制約をつけないといけないからキャストで
		}

		static public object Deserialize(byte[] buffer) {
			MemoryStream ms = new MemoryStream(buffer);
			object o = bf.Deserialize(ms);
			return o;
		}*/

		//2024-11-19
		static public byte[] Serialize<T>(T obj) {
        	// JSONシリアライズしてバイト配列として返す
        	return JsonSerializer.SerializeToUtf8Bytes(obj);
    	}

    	static public T Deserialize<T>(byte[] buffer) {
        	// バイト配列をデシリアライズしてオブジェクトとして返す
        	return JsonSerializer.Deserialize<T>(buffer);
    	}

    	static public object Deserialize(byte[] buffer, Type type) {
        	// 型情報を指定してデシリアライズ
        	return JsonSerializer.Deserialize(buffer, type);
    	}
	}


	static public class StreamExtension {
		static public long? DataSearch(Stream s, byte[] data) {
			long initPos = s.Position;
			byte[] tmp   = new byte[data.Length];
			long findPos = 0;

			//sがファイルとかの場合はとんでもないループになるよ
			for (long i = 0; i < s.Length - data.Length; i++) {
				long nowPos = s.Position;
				for (int j = 0; j < data.Length; j++) {
					tmp[j] = (byte)s.ReadByte();
					if (tmp[j] != data[j]) {
						break;
					}
					if (j == data.Length - 1) {
						findPos = nowPos;
						goto FIND;
					}
				}
			}
			return null;

		FIND:
			return findPos;
		}

	}

	public class BTool {
		public static int IndexOf<T>(IEnumerable<T> source, T target) {
			int index = 0;
			foreach (T item in source) {
				if (EqualityComparer<T>.Default.Equals(item, target))
					return index;
				index++;
			}
			return -1;
		}

		static public string MakeHTTPURL(IPAddress url, int port) {
			return "http://" + url.ToString() + ":" + port.ToString() + "/";
		}

		static public string MakeHTTPURL(string url, int port) {
			return "http://" + url + ":" + port.ToString() + "/";
		}

		static public string MakeHTTPURL(IPEndPoint ipep) {
			return MakeHTTPURL(ipep.Address, ipep.Port);
		}

		/// <summary>
		/// Array.Equalsが役に立たないのでこっちを使う
		/// 配列の中身を比較してすべて等しければ真
		/// </summary>
		/// <param name="ary1"></param>
		/// <param name="ary2"></param>
		/// <returns></returns>
		public static bool ArrayCompare(Array ary1, Array ary2) {
			if ((ary1 == null) || (ary2 == null))
				return ary1 == ary2;
			int j = ary1.Length;
			if (j != ary2.Length)
				return false;
			for (int i = 0; i < j; i++)
				if (!ary1.GetValue(i).Equals(ary2.GetValue(i)))
					return false;
			return true;
		}

		static public string AddFolderPrefix(string path) {
			//パスに\が無い場合つける
			if (path != "" && !path.EndsWith("\\")) {
				path += "\\";
			}
			return path;
		}

		/// <summary>
		/// 文字列からIPアドレスのバイト列に変換する
		/// </summary>
		/// <param name="ipaddr"></param>
		/// <returns></returns>
		static public byte[] IPAddressFromString(string ipaddr) {
			string[] ipstrs = ipaddr.Split('.');
			if (ipstrs.Length != 4) {
				throw new ArgumentException("IPアドレスが不正です");
			}
			byte[] ipbytes = new byte[4];
			for (int i = 0; i < ipbytes.Length; i++) {
				ipbytes[i] = Convert.ToByte(ipstrs[i]);
			}
			return ipbytes;
		}

		/// <summary>
		/// 32ビット整数をバイト列に変換する
		/// バイト列は領域を確保しておく必要あり
		/// </summary>
		/// <param name="num"></param>
		/// <param name="ret"></param>
		static public void BytesFromInt(int num, byte[] ret) {
			BytesFromInt(num, ret, 0);
		}

		/// <summary>
		/// 32ビット整数をバイト列に変換する
		/// バイト列は領域を確保しておく必要あり
		/// </summary>
		/// <param name="num"></param>
		/// <param name="ret"></param>
		/// <param name="offset"></param>
		static public void BytesFromInt(int num, byte[] ret, int offset) {
			int tmpnum = num;
			for (int i = offset; i < ret.Length; i++) {
				ret[i] = (byte)(tmpnum % 256);
				tmpnum /= 256;
			}
		}

		/// <summary>
		/// バイト列から32ビット整数に変換する
		/// </summary>
		/// <param name="ret"></param>
		/// <returns></returns>
		static public int IntFromByte(byte[] ret) {
			return IntFromByte(ret, 0, ret.Length);
		}

		/// <summary>
		/// バイト列から32ビット整数に変換する
		/// </summary>
		/// <param name="ret"></param>
		/// <param name="index"></param>
		/// <param name="length"></param>
		/// <returns></returns>
		static public int IntFromByte(byte[] ret, int index, int length) {
			int num = 0;
			for (int i = 0; i < index + length; i++) {
				num += ((int)ret[i]) * (int)System.Math.Pow(256, i - index);
			}
			return num;
		}

		/// <summary>
		/// バイト列から文字列に変換する
		/// </summary>
		/// <param name="lbyte"></param>
		/// <returns></returns>
		static public string StringFromByte(byte[] lbyte) {
			return StringFromByte(lbyte, 0, lbyte.Length);
		}

		/// <summary>
		/// バイト列から文字列に変換する
		/// </summary>
		/// <param name="lbytes"></param>
		/// <param name="index"></param>
		/// <param name="length"></param>
		/// <returns></returns>
		static public string StringFromByte(byte[] lbytes, int index, int length) {
			char[] chars = CharsFromBytes(lbytes, index, length);
			string str   = new string(chars);
			return str;
		}

		/// <summary>
		/// バイト列を文字列に変換する
		/// </summary>
		/// <param name="lbytes"></param>
		/// <returns></returns>
		static public char[] CharsFromBytes(byte[] lbytes) {
			return CharsFromBytes(lbytes, 0, lbytes.Length);
		}

		/// <summary>
		/// バイト列を文字列に変換する
		/// </summary>
		/// <param name="lbytes"></param>
		/// <param name="index"></param>
		/// <param name="length"></param>
		/// <returns></returns>
		static public char[] CharsFromBytes(byte[] lbytes, int index, int length) {
			//Shift-JISをユニコードに変換する
			byte[] bytes = Encoding.Convert(Encoding.GetEncoding(932), Encoding.Unicode, lbytes, index, length);
			Encoding enc = Encoding.Unicode;
			char[] chars = enc.GetChars(bytes);
			return chars;
		}

		/// <summary>
		/// 文字列からバイト列に変換する
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		static public byte[] ByteFromString(string str) {
			char[]   chars    = str.ToCharArray();
			Encoding enc      = Encoding.Unicode;
			byte[]   bytes    = enc.GetBytes(chars);
			byte[]   retBytes = Encoding.Convert(Encoding.Unicode, Encoding.GetEncoding(932), bytes);
			return   retBytes;
		}

		/// <summary>
		/// 文字列をバイト列として解釈する
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		static public byte[] ByteParse(string str) {
			byte[] bytes = new byte[str.Length / 2];
			for (int i = 0; i < str.Length / 2; i++) {
				string strbyte = str.Substring(i * 2, 2);
				bytes[i] = byte.Parse(strbyte, NumberStyles.AllowHexSpecifier);
			}
			return bytes;
		}

		/// <summary>
		/// バイト列を文字列として解釈する
		/// </summary>
		/// <param name="bytes"></param>
		/// <returns></returns>
		static public string StringParse(byte[] bytes) {
			string ret = "";
			for (int i = 0; i < bytes.Length; i++) {
				ret += bytes[i].ToString("X2");
			}
			return ret;
		}

		/// <summary>
		/// バイト列をトリミングする
		/// </summary>
		/// <param name="bytes"></param>
		/// <param name="startindex"></param>
		/// <param name="length"></param>
		/// <returns></returns>
		static public byte[] SubByte(byte[] bytes, int startindex, int length) {
			byte[] ret = new byte[length];
			Array.Copy(bytes, startindex, ret, 0, length);
			return ret;
		}

		/// <summary>
		/// バイト列を先頭からトリミングする
		/// </summary>
		/// <param name="bytes"></param>
		/// <param name="startindex"></param>
		/// <returns></returns>
		static public byte[] SubByte(byte[] bytes, int startindex) {
			return SubByte(bytes, startindex, bytes.Length - startindex);
		}

		/// <summary>
		/// IPEndPointをバイト列　IPアドレス4バイト＋ポート番号4バイト＝8バイトに変換する
		/// </summary>
		/// <param name="ipep"></param>
		/// <returns></returns>
		static public byte[] ByteParseIPEndPoint(IPEndPoint ipep) {
			byte[] ip   = ipep.Address.GetAddressBytes();
			byte[] port = new byte[4];
			BTool.BytesFromInt(ipep.Port, port);
			byte[] ret  = new byte[8];
			Array.Copy(ip, ret, ip.Length);
			Array.Copy(port, 0, ret, ip.Length, port.Length);
			return ret;
		}
	}
}
