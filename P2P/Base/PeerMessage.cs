using System;

namespace P2P.Base
{
    /// <summary>
    /// shortでいいでしょう
    /// 16bitもあれば十分
    /// </summary>
    public enum MessageTag : short {
		Unknown,
		PutNode,
		PutNodeList,
		PutContentNodePair,
		PutContentNodeList,
		PutContentNodeDictionary,
		PutDataSegment,
		PutFoundContent,
		PutNodeContentList,
		PutAllContent,

		GetNode,
		GetNodeList,
		GetContentNodePair,
		GetContentNodeList,
		GetDataSegment,
		GetNodeContentList,
		GetContentNodeDictionary,
		GetFoundContent,
		GetAllData,
		GetAllContent,

		RegBroadCast,
		RemoveBroadCast,

		PutThumbnail,
		PutContentMetaData,

		GetMovieHeader,
	}

	interface IByteArrayConvertible {
		byte[] ToByteArray();
		void MakeFromByteArray(byte[] data);
	}

	public class PeerMessage : IByteArrayConvertible {
		//はじめの2バイトだけがshort型の整数値
		byte[] rawData = null;

		public Base.MessageTag Tag {
			get { return (Base.MessageTag)BitConverter.ToInt16(this.rawData, 0); }
			set {
				short s = (short)value;
				byte[] data = BitConverter.GetBytes(s);
				Array.Copy(data, this.rawData, data.Length);
			}
		}

		public byte[] Data {
			get {
				if (rawData != null) {
					if (rawData.Length > 3) {
						byte[] retData = new byte[rawData.Length - 2];//shortが2バイトなので
						Array.Copy(this.rawData, 2, retData, 0, rawData.Length - 2);
						return retData;
					}
				}
				else {
					return null;
				}
				return null;
			}
			set {
				if (value != null) {
					byte[] newData = new byte[value.Length + 2];
					Array.Copy(this.rawData, newData, 2);//short分
					Array.Copy(value, 0, newData, 2, value.Length);
					this.rawData = newData;
				}
			}
		}

		public PeerMessage() {
			rawData = new byte[3];
			this.Tag = Base.MessageTag.Unknown;
		}

		public PeerMessage(Base.MessageTag tag, byte[] data) {
			SetData(tag, data);
		}

		public void SetData(Base.MessageTag tag, byte[] data) {
			rawData = new byte[3];
			this.Data = data;
			this.Tag = tag;
		}

		#region IByteArrayConvertible<Base.PeerMessage> メンバ

		public byte[] ToByteArray() {
			return this.rawData;
		}

		public void MakeFromByteArray(byte[] data) {
			this.rawData = data;
		}

		#endregion
	}
}
