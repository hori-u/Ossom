using System.IO;
using System.Net;
using System.Text;
using System.Xml;

namespace P2P
{
    /*
	public class BrossomPageData {
		private ResultPage resultPage = null;
		private VideoPage videoPage = null;
		private SearchPage searchPage = null;

		public BrossomPageData() {
			resultPage = new ResultPage();
		}
	}
	*/
    public abstract class XmlWebPageCreator {
		private XmlTextWriter writer = null;
		private StreamWriter  sw     = null;
		private MemoryStream  ms     = null;

		public byte[] Create(HttpListenerContext context) {
			InitCreator();
			CreatePage(context);
			return EndCreator();
		}

		public abstract void CreatePage(HttpListenerContext context);

		private void InitCreator() {
			ms     = new MemoryStream();
			sw     = new StreamWriter(ms, Encoding.UTF8);
			writer = new XmlTextWriter(sw);
		}

		private byte[] EndCreator() {
			writer.Close();
			sw.Close();
			byte[] msg = ms.ToArray();
			ms.Close();
			return msg;

		}
	}

	/*
	public class ResultPage : XmlWebPageCreator {
		private IBeginBlock html = null;
		private IBeginBlock header = null;

		private List<string> contentList = new List<string>();

		public void AddContent(string c) {
			contentList.Add(c);
		}

		public void ClearContent() {
			contentList.Clear();
		}

		public ResultPage() {
			html = BrossomHtml.HtmlBlock();
			header = BrossomHtml.HeaderBlock("Result");

		}

		public override void CreatePage(HttpListenerContext context) {
			Uri uri = context.Request.Url;
			string querys = uri.GetComponents(UriComponents.Query, UriFormat.UriEscaped);
			string word = string.Empty;
			if (querys != string.Empty) {
				QueryAnalizer qa = new QueryAnalizer(querys);

				word = qa.Get("search");
			}

			var tableSetting = new BasicTable.TableSetting();
			tableSetting.TableAttributes.Add(new HtmlAttribute("border", "1"));
			tableSetting.TableAttributes.Add(new HtmlAttribute("cellspacing", "1"));
			tableSetting.TableAttributes.Add(new HtmlAttribute("cellspacing", "1"));
			tableSetting.TableAttributes.Add(new HtmlAttribute("align", "center"));

			tableSetting.RowAttributes.Add(new HtmlAttribute("height", "100"));

			tableSetting.ColumnAttributes.Add(new HtmlAttribute("width", "100"));
			tableSetting.ColumnCellNum = 3;
			var table = new BasicTable(tableSetting);

			foreach (var c in system.ContentDictionary.Keys) {
				if (c.Name.EndsWith(".flv") || c.Name.EndsWith(".mpg")) {
					if (f.Contains(word)) {
						table.AddText(BrossomHtml.BrossomContentPreview(config, c));
					}
				}
			}

			string tableText = table.Emit();

			using (html.BeginBlock(writer)) {
				header.Write(writer);
				if (string.IsNullOrEmpty(tableText)) {
					writer.WriteRaw("NotFound<br>");
				}
				else {
					writer.WriteRaw(tableText);
				}

			}
		}
	}
	public class VideoPage : XmlWebPageCreator {
		private IBeginBlock html = null;
		private IBeginBlock header = null;
		private HtmlElement embed = null;


		public VideoPage() {
			html = BrossomHtml.HtmlBlock();
			header = BrossomHtml.HeaderBlock("Video");
			embed = new HtmlElement("embed", new List<HtmlAttribute>() { new HtmlAttribute("src", string.Empty) });

		}

		public override void CreatePage(HttpListenerContext context) {
			Uri uri = context.Request.Url;
			string querys = uri.GetComponents(UriComponents.Query, UriFormat.UriEscaped);
			string file = string.Empty;
			string type = string.Empty;
			if (querys != string.Empty) {
				QueryAnalizer qa = new QueryAnalizer(querys);

				file = qa.Get("FILE");
				type = qa.Get("TYPE");
			}

			//ピアに要求を出す．//////////////////////////////////////
			if (string.Compare(type, "P2P") == 0) {
				byte[] hashArray = BTool.ByteParse(file);
				var c = system.GetContentInfo(hashArray);
				manager.AddFileData(c);

				downloader.AddDownload(c);
			}
			else if (string.Compare(type, "BroadCast") == 0) {
				byte[] hashArray = BTool.ByteParse(file);
				var c = system.GetContentInfo(hashArray);
				manager.AddFileData(c);

				action.RegBoradCast(system.MyNodeInfo);
			}

			//FlowPlayer
			//sb.Append("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\"><html xmlns=\"http://www.w3.org/1999/xhtml\" xml:lang=\"en\" lang=\"en\"><head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\" /><title>FlowPlayer</title></head><body bgcolor=\"#ffffff\"><object type=\"application/x-shockwave-flash\" data=\"FlowPlayer.swf\" width=\"320\" height=\"263\" id=\"FlowPlayer\">	<param name=\"allowScriptAccess\" value=\"sameDomain\" /><param name=\"movie\" value=\"FlowPlayer.swf\" /><param name=\"quality\" value=\"high\" /><param name=\"scale\" value=\"noScale\" /><param name=\"wmode\" value=\"transparent\" /><param name=\"flashvars\" value=\"videoFile=");
			//sb.Append(file);
			//sb.Append("&autoPlay=true\" /></object></body></html>");

			//flvplayer
			//sb.Append("<html><head><script type=\"text/javascript\" src=\"swfobject.js\"></script></head><body><h3>single file, with preview image:</h3><p id=\"player1\"><a href=\"http://www.macromedia.com/go/getflashplayer\">Get the Flash Player</a> to see  player.</p><script type=\"text/javascript\">	var s1 = new SWFObject(\"flvplayer.swf\",\"single\",\"512\",\"384\",\"7\");s1.addParam(\"allowfullscreen\",\"true\");s1.addVariable(\"file\",\"");
			//sb.Append(file);
			//sb.Append("\");s1.addVariable(\"image\",\"preview.jpg\");s1.addVariable(\"width\",\"512\");s1.addVariable(\"height\",\"384\");s1.write(\"player1\");</script></body></html>");

			embed.AttributeList[0].Value = file + ".mpg";//embed["src"] = file + ".mpg";と同じ


			using (html.BeginBlock(writer)) {
				header.Write();
				embed.Write();
			}

		}
	}

	public class SearchPage : XmlWebPageCreator {
		private HtmlElement html = null;
		private HtmlElement header = null;
		private HtmlElement form = null;
		private HtmlElement input0 = null;
		private HtmlElement input1 = null;

		public SearchPage() {
			html = BrossomHtml.HtmlBlock();
			header = BrossomHtml.HeaderBlock("Search");

			form = new HtmlElement("form", new List<HtmlAttribute>() { new HtmlAttribute("action", "./result.html") });
			input0 = new HtmlElement("input", new List<HtmlAttribute>(){
							new HtmlAttribute("type","text"),
							new HtmlAttribute("name","search")
						});

			input1 = new HtmlElement("input", new List<HtmlAttribute>(){
							new HtmlAttribute("type","submit"),
							new HtmlAttribute("value","search")
						});
		}


		public override void CreatePage(HttpListenerContext context) {
			using (html.BeginBlock(writer)) {
				header.Write();
				form.Write(writer);
				input0.Write(writer);
				input1.Write(writer);
			}
		}
	}

	public class QueryAnalizer {
		private string[] querys = null;

		public QueryAnalizer(string query) {
			this.querys = query.Split('&');

			for (int i = 0; i < querys.Length; i++) {
				querys[i] = querys[i].TrimStart('?');
			}

		}

		public string Get(string id) {
			foreach (var s in querys) {
				if (s.StartsWith(id)) {
					int i = s.IndexOf("=");
					string retStr = string.Empty;
					try {
						retStr = s.Substring(i + 1);
					}
					catch (ArgumentOutOfRangeException) {
						return string.Empty;
					}
					return retStr;
				}
			}
			return string.Empty;
		}


	}
	*/
}