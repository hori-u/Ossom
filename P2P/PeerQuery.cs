using System;
using System.Collections.Generic;
using System.Text;

namespace P2P {
	public enum PeerQueryInfo : byte {
		PutNodeList,
		PutContentList,
		PutSegment,

		PutContentNode,

		GetNodeList,
		GetContentList,
		GetSegment,

		Unknown,
	}

	public enum PeerDataType : byte {
		ContentList,
		NodeList,
		NodeAndContentList,

		Segment,

		Unknown,
	}

	public class PeerQuery {
		private PeerQueryInfo queryInfo = PeerQueryInfo.Unknown;
		private PeerDataType dataType = PeerDataType.Unknown;

		private byte[] data = null;

		public byte[] ToByteData() {
			int retSize = data == null ? 2 : data.Length + 2;
			byte[] retData = new byte[retSize];


			retData[0] = (byte)queryInfo;
			retData[1] = (byte)dataType;

			if (data != null) {
				Array.Copy(data, 0, retData, 2, data.Length);
			}
			return retData;

		}

	}

	public class PeerDataConverter {
	

	}
}
