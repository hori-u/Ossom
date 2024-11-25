using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace P2P.DataType
{
    [Serializable]
	public class ThumbnailData : DHTData<string> {
		public string FilePath {
			get { return data; }
			set {data = value;}
		}

		public ThumbnailData(string filePath, Hash hash) {
			this.FilePath = filePath;
			this.hash = hash;
		}

	}
	[Serializable]
	public class ContentMetaData : DHTData<string> {
		const int tagNum = 10;
		private string title = string.Empty;
		private string discription = string.Empty;
		private string[] tagList = new string[tagNum];


		public Hash FileHash {
			get { return hash; }
			set { hash = value; }
		}

		public string Title {
			get { return this.title; }
			set { this.title = value; }
		}

		public string Discription {
			get { return discription; }
			set { discription = value; }
		}

		public string[] TagList {
			get { return tagList; }
		}

		public string GetTag(int i) {
			if (0 < i || i > tagList.Length) {
				return tagList[i];
			}
			return string.Empty;
		}

		public void SetTag(int i,string tag) {
			if (0 < i || i < tagList.Length) {
				tagList[i] = tag;
			}
		}

		public ContentMetaData(Hash fileHash) { }

		public ContentMetaData(Hash fileHash, string title, string discription, string[] tagList) {
			this.hash = fileHash;
			this.title = title;
			this.discription = discription;

			for (int i = 0; i < tagList.Length; i++) {
				if (tagList.Length < i) {
					break;
				}
				this.SetTag(i, tagList[i]);
			}

		}

		/*
		public Database.ContentMetaData ToDatabaseData() {
			Database.ContentMetaData cmd = new P2P.Database.ContentMetaData();
			cmd.Hash = this.hash.Str;
			cmd.Discription = this.discription;
			cmd.Tag0 = this.GetTag(0);
			cmd.Tag1 = this.GetTag(1);
			cmd.Tag2 = this.GetTag(2);
			cmd.Tag3 = this.GetTag(3);
			cmd.Tag4 = this.GetTag(4);
			cmd.Tag5 = this.GetTag(5);
			cmd.Tag6 = this.GetTag(6);
			cmd.Tag7 = this.GetTag(7);
			cmd.Tag8 = this.GetTag(8);
			cmd.Tag9 = this.GetTag(9);

			return cmd;
		}
		 * */

		static public ContentMetaData LoadFile(Hash fileHash, string path) {
			System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
			System.IO.StreamReader sr = new System.IO.StreamReader(path, Encoding.GetEncoding("shift_jis"));

			doc.Load(sr);

			foreach (XmlNode topnode in doc.ChildNodes) {
				if (topnode.Name == "contenttag") {
					string title = string.Empty;
					string discription = string.Empty;

					List<string> tagList = new List<string>();
					foreach (XmlNode cnode in topnode.ChildNodes) {
						if (cnode.Name == "title") {
							title = cnode.InnerText;
						}
						else if (cnode.Name == "discription") {
							discription = cnode.InnerText;
						}

						else if (cnode.Name == "tags") {
							foreach (XmlNode tagNode in cnode.ChildNodes) {
								tagList.Add(tagNode.InnerXml);
							}
						}
					}
					ContentMetaData md = new ContentMetaData(fileHash, title, discription, tagList.ToArray());
					return md;
				}
			}
			return null;
		}
	}
}
