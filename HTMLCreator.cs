using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace P2P
{
    namespace HtmlCreator
    {
        interface IHtmlWriter<T> {
			void BeginWrite(XmlTextWriter writer, IEnumerable<T> values);
			void EndWrite(XmlTextWriter writer);
		}

		interface IBeginBlock {
			IDisposable GetBlock(XmlTextWriter writer);
		}

		/*
		 * HtmlElementとHtmlElementGroupの共通項目をだして
		 * 同じインタフェースで実装する．
		 */

		public class HtmlElement : IBeginBlock {
			private string name  = string.Empty;
			private string value = string.Empty;
			private List<HtmlAttribute> attList = new List<HtmlAttribute>();

			public string Name {
				get { return name; }
				set { name = value; }
			}

			public string Value {
				get { return value; }
				set { this.value = value; }
			}

			public string this[string s] {
				get {
					HtmlAttribute att = attList.Find(x => x.Value == s);
					if (att != null) {
						return att.Value;
					}
					else {
						return string.Empty;
					}
				}

				set {
					HtmlAttribute att = attList.Find(x => x.Value == s);
					if (att != null) {
						att.Value = value;
					}
				}
			}

			public List<HtmlAttribute> AttributeList {
				get { return attList; }
				set { attList = value; }
			}

			public HtmlElement() { }

			public HtmlElement(string name) {
				this.name = name;
			}

			public HtmlElement(string name,List<HtmlAttribute> attList){
				this.name    = name;
				this.attList = attList;
			}

			public HtmlElement(string name, string value) {
				this.name  = name;
				this.value = value;
			}

			public HtmlElement(string name,string value,List<HtmlAttribute> attList){
				this.name    = name;
				this.value   = value;
				this.attList = attList;
			}

			static public void ElementBeginWrite(XmlTextWriter writer,string name, List<HtmlAttribute> attList) {
				writer.WriteStartElement(name);
				foreach (var a in attList) {
					a.WriteAttribute(writer);
				}
			}

			static public void ElementBeginWrite(XmlTextWriter writer, string name,string value, List<HtmlAttribute> attList) {
				writer.WriteStartElement(name);
				foreach (var a in attList) {
					a.WriteAttribute(writer);
				}
				if (!string.IsNullOrEmpty(value)) {
					writer.WriteString(value);
				}
			}

			static public void ElementEndWrite(XmlTextWriter writer) {
				writer.WriteEndElement();
			}

			static public void ElementWrite(XmlTextWriter writer, string name, string value,List<HtmlAttribute> attList) {
				ElementBeginWrite(writer, name, attList);
				if (!string.IsNullOrEmpty(value)) {
					writer.WriteString(value);
				}
				ElementEndWrite(writer);
			}

			public void Write(XmlTextWriter writer) {
				ElementWrite(writer, name, value, attList);
			}

			public void ElementWrite(XmlTextWriter writer,string value) {
				ElementBeginWrite(writer, name, attList);
				writer.WriteString(value);
				ElementEndWrite(writer);
			}

			public void BeginWrite(XmlTextWriter writer) {
				ElementBeginWrite(writer, name,value, attList);
			}
			
			public void EndWrite(XmlTextWriter writer) {
				HtmlElement.ElementEndWrite(writer);
			}
			
			public ElementWriter BeginBlock(XmlTextWriter writer) {
				return new ElementWriter(writer, this);
			}

			public class ElementWriter : IDisposable {
				private HtmlElement element = null;
				private XmlTextWriter writer = null;

				public ElementWriter(XmlTextWriter writer,HtmlElement element) {
					this.element = element;
					this.writer = writer;
					HtmlElement.ElementBeginWrite(writer, element.name, element.attList);
				}
				#region IDisposable メンバ

				public void Dispose() {
					HtmlElement.ElementEndWrite(this.writer);
				}

				#endregion
			}
			#region IBeginBlock メンバ

			public IDisposable GetBlock(XmlTextWriter writer) {
				return new ElementWriter(writer, this);
			}

			#endregion
		}

		public class HtmlElementList {
			private List<HtmlElement> elementList = new List<HtmlElement>();

			/*
			 <hoge></hoge>
			 <foo></foo>
			 <bar></bar>
			 という風に出力するやつ
			 */
		}

		public interface IHtmlGroup {
			void BeginWrite(XmlTextWriter writer);
			void EndWrite(XmlTextWriter writer);
		}

		public class HtmlElementSequencialGroup : IBeginBlock,IHtmlGroup {
			private List<HtmlElement> elementList = new List<HtmlElement>();

			public void Add(HtmlElement element) {
				elementList.Add(element);
			}

			static public void GroupBeginWrite(XmlTextWriter writer, List<HtmlElement> elementList) {
				foreach (var e in elementList) {
					e.BeginWrite(writer);
					e.EndWrite(writer);
				}
			}

			static public void GroupEndWrite(XmlTextWriter writer, List<HtmlElement> elementList) {
				return;
			}

			public void BeginWrite(XmlTextWriter writer) {
				GroupBeginWrite(writer, this.elementList);
			}

			public void EndWrite(XmlTextWriter writer) {
				GroupEndWrite(writer, this.elementList);
			}

			static public void GroupWrite(XmlTextWriter writer, List<HtmlElement> elementList) {
				GroupBeginWrite(writer, elementList);
				GroupEndWrite(writer, elementList);
			}

			public void Write(XmlTextWriter writer) {
				BeginWrite(writer);
				EndWrite(writer);
			}

			public GroupWriter BeginBlock(XmlTextWriter writer) {
				return new GroupWriter(writer, this);
			}


			public class GroupWriter : IDisposable {
				private XmlTextWriter writer = null;
				private IHtmlGroup group = null;

				public GroupWriter(XmlTextWriter writer, IHtmlGroup elementList) {
					this.writer = writer;
					this.group = elementList;
					group.BeginWrite(writer);
				}

				#region IDisposable メンバ

				public void Dispose() {
					group.EndWrite(writer);
				}

				#endregion
			}

			#region IBeginBlock メンバ

			public IDisposable GetBlock(XmlTextWriter writer) {
				return new GroupWriter(writer, this);
			}

			#endregion
		}


		public class HtmlElementNestGroup : IBeginBlock, IHtmlGroup {
			private List<HtmlElement> elementList = new List<HtmlElement>();

			public void Add(HtmlElement element) {
				elementList.Add(element);
			}

			static public void GroupBeginWrite(XmlTextWriter writer,List<HtmlElement> elementList){
				foreach(var e in elementList){
					e.BeginWrite(writer);
				}
			}

			static public void GroupEndWrite(XmlTextWriter writer,List<HtmlElement> elementList){
				var revList = new List<HtmlElement>(elementList);
				revList.Reverse();
				foreach (var e in revList) {
					e.EndWrite(writer);
				}
			}

			public void BeginWrite(XmlTextWriter writer) {
				GroupBeginWrite(writer, this.elementList);
			}

			public void EndWrite(XmlTextWriter writer) {
				GroupEndWrite(writer, this.elementList);
			}

			static public void GroupWrite(XmlTextWriter writer, List<HtmlElement> elementList) {
				GroupBeginWrite(writer, elementList);
				GroupEndWrite(writer, elementList);
			}

			public void Write(XmlTextWriter writer) {
				BeginWrite(writer);
				EndWrite(writer);
			}

			public GroupWriter BeginBlock(XmlTextWriter writer) {
				return new GroupWriter(writer, this);
			}


			public class GroupWriter : IDisposable {
				private XmlTextWriter writer = null;
				private IHtmlGroup group = null;

				public GroupWriter(XmlTextWriter writer, IHtmlGroup elementList) {
					this.writer = writer;
					this.group = elementList;
					group.BeginWrite(writer);
				}

				#region IDisposable メンバ

				public void Dispose() {
					group.EndWrite(writer);
				}

				#endregion
			}

			#region IBeginBlock メンバ

			public IDisposable GetBlock(XmlTextWriter writer) {
				return new GroupWriter(writer, this);
			}

			#endregion
		}


		public class HtmlAttribute {
			private string value = string.Empty;
			private string name = string.Empty;

			public HtmlAttribute(string attributeName, string value) {
				Set(attributeName, value);
			}

			public void Set(string name, string value) {
				this.value = value;
				this.name = name;
			}

			public string Value {
				get { return value; }
				set { this.value = value; }
			}

			public string Name {
				get { return name; }
				set { this.name = value; }
			}


			public void WriteAttribute(XmlTextWriter writer) {
				writer.WriteAttributeString(this.name, value.ToString());
			}
		}


		//上で作ったHTML関連のクラスで書き直せるけどとりあえずこのままで
		public class BasicTable {
			private List<string> elementList = null;
			public class TableSetting {
				public List<HtmlAttribute> TableAttributes  = new List<HtmlAttribute>();
				public List<HtmlAttribute> RowAttributes    = new List<HtmlAttribute>();
				public List<HtmlAttribute> ColumnAttributes = new List<HtmlAttribute>();

				public int ColumnCellNum = 3;
			}

			public TableSetting Setting = new TableSetting();

			public BasicTable() {
				this.Setting.ColumnCellNum = 3;
				elementList = new List<string>();
			}

			public BasicTable(int cn) {
				this.Setting.ColumnCellNum = cn;
				elementList = new List<string>();
			}

			public BasicTable(TableSetting setting) {
				this.Setting = setting;
				this.elementList = new List<string>();
			}

			public void AddText(string s) {
				elementList.Add(s);
			}

			public void AddElement(IEnumerable<string> strEnu) {
				foreach (var s in strEnu) {
					AddText(s);
				}
			}

			public string Emit() {
				if (this.elementList.Count == 0) {
					return string.Empty;
				}

				int elementIndex = 0;
				MemoryStream ms = new MemoryStream();
				StreamWriter sw = new StreamWriter(ms);
				XmlTextWriter writer = new XmlTextWriter(sw);
				writer.WriteStartElement("table");
				foreach (var tableAttribute in Setting.TableAttributes) {
					tableAttribute.WriteAttribute(writer);
				}

				while (true) {

					writer.WriteStartElement("tr");
					foreach (var trAttribute in Setting.RowAttributes) {
						trAttribute.WriteAttribute(writer);
					}

					for (int i = 0; i < this.Setting.ColumnCellNum; i++) {
						writer.WriteStartElement("td");
						foreach (var tdAttribute in Setting.ColumnAttributes) {
							tdAttribute.WriteAttribute(writer);
						}
						writer.WriteRaw(elementList[elementIndex++]);

						if (elementList.Count == elementIndex) {
							break;
						}
						writer.WriteEndElement();//td
					}
					writer.WriteEndElement();//tr

					if (elementList.Count == elementIndex) {
						break;
					}
				}
				writer.WriteEndElement();//table
				writer.Flush();

				string ret = sw.Encoding.GetString(ms.ToArray());

				return ret;
			}
		}
	}
}
