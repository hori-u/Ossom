using System;
using System.Net;

namespace P2P.DataType
{
    //ノードに対する評価値のクラスもそのうち考える．

    [Serializable]
	public class NodeInfo : DHTData<int>, IEquatable<NodeInfo> {
		public enum NodeTypes : byte {
			Search,
			Download,
			Sleep,
			Unknown,
		}

		public enum NodeInitState {
			Create,
			Initialized,
			Connected,
		}

		public enum ConnectionTypes {
			RAW,
			NAT,
			Unknown,
		}

        protected IPEndPoint nodeEndPoint  = null;
        protected IPAddress  deviceAddress = null;
		protected DateTime   startTime     = DateTime.Now;
		protected int speed = 0;

		[NonSerialized]
		protected NodeTypes nodeType = NodeTypes.Unknown;

		[NonSerialized]
		NodeInitState initState = NodeInitState.Create;

		public Hash NodeHash {
			get {
                return hash;
            }
		}

		public NodeTypes NodeType {
			get {
                return nodeType;
            }
			set {
                nodeType = value;
            }
		}

		public ConnectionTypes NodeConnectionType {
			get {
				if (this.deviceAddress == null || this.nodeEndPoint == null) {
					return ConnectionTypes.Unknown;
				}
				else if (this.nodeEndPoint.Address == this.deviceAddress) {
					return ConnectionTypes.RAW;
				}
				else {
					return ConnectionTypes.NAT;
				}
			}
		}

		public IPAddress DeviceAddress {
			get {
                return deviceAddress;
            }
		}

		public IPAddress NetAddress {
			get {
                return nodeEndPoint.Address;
            }
		}

		public int Port {
			get {
                return nodeEndPoint.Port;
            }
		}

		public IPEndPoint NodeEndPoint {
			get {
                return nodeEndPoint;
            }
		}

		public int Speed {
			get {
                return speed;
            }
		}

		public DateTime StartTime {
			get {
                return startTime;
            }
		}

		public TimeSpan StartTimeElapse {
			get {
                return DateTime.Now - startTime;
            }
		}

		public NodeInfo(Hash hash,IPAddress networkAddress, int port, IPAddress deviceAddress, int speed,DateTime time) {

			this.nodeEndPoint = new IPEndPoint(networkAddress, port);
			this.deviceAddress = deviceAddress;
			this.startTime = time;
            this.speed = speed;
            this.hash = hash;
        }

		public NodeInfo(IPAddress networkAddress, int port, IPAddress deviceAddress, int speed, DateTime time) {
			this.nodeEndPoint = new IPEndPoint(networkAddress, port);
			this.deviceAddress = deviceAddress;
            this.startTime = time;
            this.speed = speed;

			this.hash = new Hash(HashWrapper.ComputeHash(BTool.ByteParse(nodeEndPoint.ToString()),HashAlgorithm.SHA1));
		}

		/*
		public Database.NodeInfo ToDatabaseData() {
			Database.NodeInfo ni = new P2P.Database.NodeInfo();
			ni.Hash = this.hash.Str;
			ni.IPv4DeviceAddress = this.deviceAddress.ToString();
			ni.IPv4NetworkAddress = this.NetAddress.ToString();
			ni.Port = this.Port;
			ni.Speed = this.speed;

			return ni;
		}
		*/

		public NodeInfo CreateMyNodeInfo(PeerConfig pc) {

			var hostName = Dns.GetHostName();

			IPHostEntry ipHostEntry = Dns.GetHostEntry(hostName);

			IPAddress deviceAddress = ipHostEntry.AddressList[0];

			NodeInfo ni = new NodeInfo(null, pc.Port, deviceAddress, pc.Speed, DateTime.Now);

			return ni;
		}
		#region IEquatable<NodeInfo> メンバ

		public bool Equals(NodeInfo other) {
			return this.hash.Equals(other.hash);
		}

		#endregion
	}
}
