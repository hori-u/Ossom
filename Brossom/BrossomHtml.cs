using P2P.BBase;
using P2P.HtmlCreator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace P2P
{
    public class BrossomHtml {

		/*
			using (HtmlCreator.HtmlDocument doc = new HtmlCreator.HtmlDocument(sb, "Test")) {
		 			HtmlCreator.BasicTable.TableSetting setting = new HtmlCreator.BasicTable.TableSetting();
			setting.TableAttributes.Add(new HtmlCreator.HtmlAttribute("border", "1", "0"));
			setting.TableAttributes.Add(new HtmlCreator.HtmlAttribute("cellspacing", "1", "0"));
			setting.TableAttributes.Add(new HtmlCreator.HtmlAttribute("cellspacing", "1", "0"));
		 	setting.TableAttributes.Add(new HtmlCreator.HtmlAttribute("align", "center"));
			setting.RowAttributes.Add(new HtmlCreator.HtmlAttribute("height", "100", "0"));
			setting.ColumnAttributes.Add(new HtmlCreator.HtmlAttribute("width", "100", "0"));
			setting.ColumnCellNum = 3;

			HtmlCreator.BasicTable table = new HtmlCreator.BasicTable(setting);

			table.AddText("test1");
			table.AddText("test1");
			table.AddText("test1");
			table.AddText("test1");
			table.AddText("test1");
			table.AddText("test1");
			table.AddText("test1");
			table.AddText("test1");
			table.AddText("test1");
			table.AddText("test1");
			table.AddText("test1");
			table.AddText("test1");
			table.AddText("test1");
			table.AddText("test1");
			table.AddText("test1");
			table.AddText("test1");
			table.AddText("test1");

			FileStream fs = new FileStream(@"C:\Users\Vista\Desktop\test1.html", FileMode.Create);
			StreamWriter sw = new StreamWriter(fs);
			StringBuilder sb = new StringBuilder();

			using (HtmlCreator.HtmlDocument doc = new HtmlCreator.HtmlDocument(sb, "Test")) {
				string s = table.Emit();
				sb.Append(s);
			}

			sw.WriteLine(sb.ToString());

			sw.Close();
			fs.Close();
	
		って使う
		 *  */
		public class BrossomHtmlDoc {
			HtmlElement htmlElement  = new HtmlElement("html");
			HtmlElement headElement  = new HtmlElement("head");
			HtmlElement titleElement = new HtmlElement("title");
			HtmlElement bodyElement  = new HtmlElement("body");

			HtmlElementNestGroup group = new HtmlElementNestGroup();

			public BrossomHtmlDoc(string title) {
				group.Add(htmlElement);
				group.Add(headElement);
				titleElement.Value = title;
				group.Add(titleElement);
				group.Add(bodyElement);
			}
		}

		static public HtmlElementNestGroup HtmlBlock() {
			HtmlElement htmlElement = new HtmlElement("html");
			HtmlElement bodyElement = new HtmlElement("body");
			HtmlElementNestGroup group = new HtmlElementNestGroup();

			group.Add(htmlElement);
			group.Add(bodyElement);

			return group;
		}

		static public HtmlElementNestGroup HeaderBlock(string title) {

			HtmlElement headElement = new HtmlElement("head");
			HtmlElement metaElement = new HtmlElement("mata", new List<HtmlAttribute>(){
				new HtmlAttribute("http-equiv","Content-Type"),
				new HtmlAttribute("content","text/html"),
				new HtmlAttribute("charset","unicode-2-0-utf-8")
			});
			HtmlElement titleElement   = new HtmlElement("title", title);
			HtmlElementNestGroup group = new HtmlElementNestGroup();
			group.Add(headElement);
			group.Add(metaElement);
			group.Add(titleElement);

			return group;
		}

		public class HtmlDocument : IDisposable {
			public StringBuilder sb = new StringBuilder();
			public HtmlDocument(StringBuilder sb, string title) {
				this.sb = sb;

				sb.Append(EmitHTMLHeader(title));
			}

			public static string EmitHTMLHeader(string title) {
				StringBuilder sb = new StringBuilder();
				sb.Append("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\"><html xmlns=\"http://www.w3.org/1999/xhtml\" xml:lang=\"ja\" lang=\"ja\"><head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=unicode-2-0-utf-8\" /><title>");
				sb.Append(title);
				sb.Append("</title></head><body bgcolor=\"#ffffff\">");
				return sb.ToString();
			}

			public static string EmitHTMLFooter() {
				return "</body></html>";
			}

			#region IDisposable メンバ

			public void Dispose() {
				sb.Append(EmitHTMLFooter());
			}

			#endregion
		}

		static public string BrossomCreateMovieLink(ContentInfoBase content) {
			return "video.html?FILE=" +
				content.BaseHash.Str + "&?TYPE=" +
                Enum.GetName(typeof(ContentType), content.Type);
		}

		static public string BrossomContentPreview(PeerConfig config, ContentInfoBase content, DataType.ContentMetaData metaData) {
			string movieLink = BrossomCreateMovieLink(content);
			movieLink = "http://localhost:20000/" + movieLink;
			HtmlElement linkElement =
				new HtmlElement("a", new List<HtmlAttribute>() { 
					new HtmlAttribute("href", movieLink),
					new HtmlAttribute("target","_blank")
				});
			HtmlElement imgElement = new HtmlElement("img",
				new List<HtmlAttribute>(){
					new HtmlAttribute("src",Path.Combine(BTool.MakeHTTPURL(config.LocalEndPoint.IPEP),Path.GetFileNameWithoutExtension(content.Name) + ".jpg")),
					//new HtmlAttribute("align","left"),
					new HtmlAttribute("border","0"),
				});
			HtmlElement font1Element = new HtmlElement("font", new List<HtmlAttribute>() { new HtmlAttribute("size", "-2") });
			HtmlElement font2Element = new HtmlElement("font", new List<HtmlAttribute>() { new HtmlAttribute("size", "-1") });
			HtmlElement font3Elemtnt = new HtmlElement("font", new List<HtmlAttribute>() { new HtmlAttribute("size", "-3") });

			MemoryStream ms = new MemoryStream();
			XmlTextWriter writer = new XmlTextWriter(ms, null);//UTF-8

			using (linkElement.BeginBlock(writer)) {
				imgElement.Write(writer);
				writer.WriteRaw("<br>");
				font2Element.ElementWrite(writer, metaData.Title);
				writer.WriteRaw("<br>");
			}

			font1Element.ElementWrite(writer, metaData.Discription);
			writer.WriteRaw("<br>");
			using (font3Elemtnt.BeginBlock(writer)) {
				foreach (var t in metaData.TagList) {
					HtmlElement tagLink = new HtmlElement("a", new List<HtmlAttribute>() { new HtmlAttribute("href", "./result.html?search=" + t) });
					tagLink.ElementWrite(writer, t);
					writer.WriteRaw("-");
				}
			}
			writer.WriteRaw("<br>");
			writer.Close();

			return Encoding.UTF8.GetString(ms.ToArray());
		}

		static public string BrossomContentPreview(PeerConfig config, ContentInfoBase content) {
			string movieLink = BrossomCreateMovieLink(content);
			movieLink = "http://localhost:20000/" + movieLink;
			HtmlElement linkElement =
				new HtmlElement("a", new List<HtmlAttribute>() { 
					new HtmlAttribute("href"  , movieLink),
					new HtmlAttribute("target","_blank")
				});
			HtmlElement imgElement = new HtmlElement("img",
				new List<HtmlAttribute>(){
					new HtmlAttribute("src",Path.Combine(config.WebServerEndPoint.ToString(),Path.GetFileNameWithoutExtension(content.Name) + ".jpg")),
					//new HtmlAttribute("align","left"),
					new HtmlAttribute("border","0"),
				});
			HtmlElement font1Element = new HtmlElement("font", new List<HtmlAttribute>() { new HtmlAttribute("size", "-2") });
			HtmlElement font2Element = new HtmlElement("font", new List<HtmlAttribute>() { new HtmlAttribute("size", "-1") });
			HtmlElement font3Elemtnt = new HtmlElement("font", new List<HtmlAttribute>() { new HtmlAttribute("size", "-3") });
			HtmlElement tagLink      = new HtmlElement("a",    new List<HtmlAttribute>() { new HtmlAttribute("href", "about:blank") });

			MemoryStream ms = new MemoryStream();
			XmlTextWriter writer = new XmlTextWriter(ms, null);//UTF-8

			using (linkElement.BeginBlock(writer)) {
				imgElement.Write(writer);
				writer.WriteRaw("<br>");
				font2Element.ElementWrite(writer, content.Name);
				writer.WriteRaw("<br>");
			}

			font1Element.ElementWrite(writer, content.FileSize.ToString());
			writer.WriteRaw("<br>");
			//font1Element.ElementWrite(writer, content.HashString);
			writer.WriteRaw("<br>");
			writer.Close();

			return Encoding.UTF8.GetString(ms.ToArray());
		}
	}
}
