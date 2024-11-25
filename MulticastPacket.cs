using System;
using System.Collections.Generic;


namespace P2P
{
    public class MulticastPacketManager {

	}

	public class MulticastPacket {
		int segmentNumber = 0;

		public int SegmentNumber {
			get { return segmentNumber; }
			set { segmentNumber = value; }
		}

		byte[] rawData = null;

		List<bool> blockEnable = null;

		public MulticastPacket(int segNum, byte[] rawData) {
			this.segmentNumber = segNum;
			this.rawData = rawData;
		}

		public MulticastPacket(int segNum) {
			this.segmentNumber = segNum;
		}

		System.IO.MemoryStream ms = null;

		public bool IsSegmentComplete() {
			if (blockEnable == null) return false;

			bool ret = true;

			blockEnable.ForEach((bool b) => {
				ret &= b;
			});

			return ret;
		}


		public MulticastDividedPacket[] MakeDividedPacket(int divideSize) {

			int index = 0;

			int divideNum = rawData.Length / (divideSize - 12) + 1;

			MulticastDividedPacket[] rets = new MulticastDividedPacket[divideNum];

			for (int i = 0; i < divideNum; i++) {

				MulticastDividedPacket mdp = new MulticastDividedPacket();
				int length = Math.Min((this.rawData.Length- index), (divideSize - 12));
				mdp.RawData = new byte[length];
				Array.Copy(this.rawData, index, mdp.RawData, 0, length);
				index += (divideSize - 12);

				mdp.SegmentNumber = this.segmentNumber;
				mdp.DivideNumber = i;
				mdp.TotalNumber = divideNum;
				rets[i] = mdp;
			}
			return rets;
		}

		int packetDivideSize = 0;

		public void PutDividedPacket(MulticastDividedPacket mdp){
			if (ms == null) {
				ms = new System.IO.MemoryStream();
			}

			//こんなところで初期はまずいがしたない
			if (blockEnable == null) {
				blockEnable = new List<bool>(new bool[mdp.TotalNumber]);
			}


			//packetDivideSizeが0の時に最後のパケットを書き込むのはまずい
			if (packetDivideSize == 0) {
				if (mdp.DivideNumber == (mdp.TotalNumber - 1)) {
					return;
				}
			}

			//しかたない
			packetDivideSize = Math.Max(mdp.RawData.Length, packetDivideSize);

			if (ms.Length < packetDivideSize * mdp.DivideNumber + mdp.RawData.Length) {
				ms.SetLength(packetDivideSize * mdp.DivideNumber + mdp.RawData.Length);
			}
			ms.Seek(packetDivideSize * mdp.DivideNumber, System.IO.SeekOrigin.Begin);
			ms.Write(mdp.RawData, 0, mdp.RawData.Length);
			//Array.Copy(mdp.RawData, 0, this.rawData, mdp.RawData.Length * mdp.DivideNumber, mdp.RawData.Length);
			blockEnable[mdp.DivideNumber] = true;
		}

		public byte[] GetRawData() {
			if (ms != null) {
                rawData = ms.ToArray();
			}
			return rawData;
		}
	}

	public class MulticastDividedPacket {
		int segmentNumber = 0;

		public int SegmentNumber {
			get { return segmentNumber; }
			set { segmentNumber = value; }
		}

		int divideNumber = 0;
		public int DivideNumber {
			get { return divideNumber; }
			set { divideNumber = value; }
		}

		int totalNumber = 0;
		public int TotalNumber {
			get { return totalNumber; }
			set { totalNumber = value; }
		}


		public int PacketSize {
			get {
				return 4 + 4 + 4 + RawData.Length;
			}
		}


		byte[] rawData = null;

		public byte[] RawData {
			get { return rawData; }
			set { rawData = value; }
		}

		public byte[] ToByteArray() {

			byte[] buffer = new byte[4 + 4 + 4 + rawData.Length];

			int index = 0;
			var segNumBuffer = BitConverter.GetBytes(segmentNumber);
			var divNumBuffer = BitConverter.GetBytes(divideNumber);
			var totalDivNumBuffer = BitConverter.GetBytes(totalNumber);

			//segmentIndex
			Array.Copy(segNumBuffer, 0, buffer, index, segNumBuffer.Length);
			index += segNumBuffer.Length;

			//divideIndex
			Array.Copy(divNumBuffer, 0, buffer, index, divNumBuffer.Length);
			index += divNumBuffer.Length;

			//totalDivNum
			Array.Copy(totalDivNumBuffer, 0, buffer, index, totalDivNumBuffer.Length);
			index += totalDivNumBuffer.Length;

			Array.Copy(rawData, 0, buffer, index, rawData.Length);

			return buffer;

		}

		public void Create(byte[] buffer) {
			int index = 0;

            segmentNumber = BitConverter.ToInt32(buffer, index);
			index += 4;

            divideNumber  = BitConverter.ToInt32(buffer, index);
			index += 4;

            totalNumber   = BitConverter.ToInt32(buffer, index);
			index += 4;

			rawData = new byte[buffer.Length - index];

			Array.Copy(buffer, index, rawData, 0, rawData.Length);
		}
	}
}
