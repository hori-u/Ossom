using System;
using System.IO;
using System.Xml.Serialization;
using System.Net;

namespace P2P
{

    public class XMLAddressData {
		[XmlElement("Address")]
		public String Address = "127.0.0.1";
		[XmlElement("Port")]
		public  int        Port = 0;
		private IPEndPoint ipep = null;

		public  IPEndPoint IPEP {
			get {
				if (ipep == null) {
					ipep = new IPEndPoint(IPAddress.Parse(Address), Port);
				}
				return ipep;
			}
		}
	}
	
	[XmlRoot("PeerStaticData")]
	public class PeerConfig {
		[XmlElement("DownLoadPath")]
		public string DownloadPath = string.Empty;

		[XmlElement("UploadPath")]
		public string UploadPath   = string.Empty;

		[XmlElement("CachePath")]
		public string CachePath    = string.Empty;

		[XmlElement("DataPath")]
		public string DataPath     = string.Empty;

		[XmlElement("Port")]
		public        int Port              = 0;
		private const int DefaultPortNumber = 10000;

		[XmlElement("Speed")]
		public        int Speed        = 0;
		private const int DefaultSpeed = 0;

		[XmlElement("WPPSSendPort")]
		public int WPPSSendPort             = 30000;

		[XmlElement("WPPSReceiveOffsetSeconds")]
		public int WPPSReceiveOffsetSeconds = 10;

		[XmlElement("LocalEndPoint")]
		public XMLAddressData LocalEndPoint         = new XMLAddressData();

		[XmlElement("BrossomServerEndPoint")]
		public XMLAddressData BrossomServerEndPoint = new XMLAddressData();

		[XmlElement("WebServerEndPoint")]
		public XMLAddressData WebServerEndPoint     = new XMLAddressData();

		/*
		[System.Xml.Serialization.XmlElement("LocalEndPoint")]
		public string LocalEndPoint = string.Empty;
		private const string DefaultLocalEndPoint = "http://127.0.0.1:20000/";


		[System.Xml.Serialization.XmlElement("ServerAddress")]
		public string ServerAddress = string.Empty;
		private const string DefaultServerAddress = "http://127.0.0.1:12345/";

		[System.Xml.Serialization.XmlElement("WebServerAddress")]
		public string WebServerAddress = string.Empty;
		private const string DefaultWebServerAddress = "http://127.0.0.1:20001/";
		*/

		public void CreateDefaultSetting() {
			string current = Directory.GetCurrentDirectory();

			DownloadPath = Path.Combine(current, "Download");
			UploadPath   = Path.Combine(current, "Upload");
			CachePath    = Path.Combine(current, "Cache");
			DataPath     = Path.Combine(current, "Data");

			Port = DefaultPortNumber;

			LocalEndPoint.Address = "127.0.0.1";
			LocalEndPoint.Port    = 20000;

			BrossomServerEndPoint.Address = "192.168.11.10";
			BrossomServerEndPoint.Port    = 12345;

			WebServerEndPoint.Address = "192.168.11.10";
			WebServerEndPoint.Port    = 20001;

			Save();
		}
		public void CreateDefaultSetting(string filename) {
			string current = Directory.GetCurrentDirectory();

			DownloadPath = Path.Combine(current, "Download");
			UploadPath   = Path.Combine(current, "Upload");
			CachePath    = Path.Combine(current, "Cache");
			DataPath     = Path.Combine(current, "Data");

			Port = DefaultPortNumber;

			Random rand = new Random();
			LocalEndPoint.Address = "127.0.0.1";
			LocalEndPoint.Port    = rand.Next(short.MaxValue) ;

			BrossomServerEndPoint.Address = "192.168.11.10";
			BrossomServerEndPoint.Port    = 12345;

			WebServerEndPoint.Address = "192.168.11.10";
			WebServerEndPoint.Port    = 20001;

			Save(filename);
		}

		public void CheckResource() {
			try {

				if (!Directory.Exists(this.DownloadPath)) {
					Directory.CreateDirectory(this.DownloadPath);
					PeerLogger.Instance.OnMassage(PeerLogger.MessageLevel.Info, "DirectroyCreate", DownloadPath + "を作成しました");
				}
				if (!Directory.Exists(this.UploadPath)) {
					Directory.CreateDirectory(this.UploadPath);
					PeerLogger.Instance.OnMassage(PeerLogger.MessageLevel.Info, "DirectroyCreate", UploadPath + "を作成しました");
				}
				if (!Directory.Exists(this.CachePath)) {
					Directory.CreateDirectory(this.CachePath);
					PeerLogger.Instance.OnMassage(PeerLogger.MessageLevel.Info, "DirectroyCreate", CachePath + "を作成しました");
				}
				if (!Directory.Exists(this.DataPath)) {
					Directory.CreateDirectory(this.DataPath);
					PeerLogger.Instance.OnMassage(PeerLogger.MessageLevel.Info, "DirectroyCreate", DataPath + "を作成しました");
				}
			}
			catch (ArgumentException ae) {
				PeerLogger.Instance.OnMassage(PeerLogger.MessageLevel.Error, "PathError", "不正なパス");
				//throw ae;
			}
			catch (Exception e) {
				PeerLogger.Instance.OnMassage(PeerLogger.MessageLevel.Error, "ERROR", e.Message);
				//throw e;
			}
		}

		static public PeerConfig Load() {//オーバーロード
			return Load("Config.xml");
		}

		static public PeerConfig Load(string filename) {
			try {
				//XmlSerializerオブジェクトの作成
				XmlSerializer serializer = new XmlSerializer(typeof(PeerConfig));

				//ファイルを開く
				FileStream fs = new FileStream(filename, FileMode.Open);
				PeerConfig pc = (PeerConfig)serializer.Deserialize(fs);

				fs.Close();
				return pc;
			}
			catch (FileNotFoundException) {
				PeerConfig pc = new PeerConfig();
				pc.CreateDefaultSetting();
				pc.CheckResource();

				return pc;
			}
		}

		public void Save() {//オーバーロード
			Save("Config.xml");
		}

		public void Save(string filename) {
			//XmlSerializerオブジェクトを作成
			//書き込むオブジェクトの型を指定する
			XmlSerializer serializer = new XmlSerializer(typeof(PeerConfig));
			//ファイルを開く
			FileStream fs = new FileStream(filename, FileMode.Create);
			//シリアル化し、XMLファイルに保存する
			serializer.Serialize(fs, this);
			//閉じる
			fs.Close();
		}
	}
}
