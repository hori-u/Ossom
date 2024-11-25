using P2P;
using P2P.BBase;
using P2P.HtmlCreator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Xml;

namespace WebServer
{
    public class BrossomWebServer {

	}

    public class WebServer {
		private HttpListener listener;
		private string       folderPath;
		private int          state      = 0;
		private Thread       mainThread = null;

		private void Initialize(string uri) {
			ThreadPool.SetMaxThreads(50, 1000);
			ThreadPool.SetMinThreads(50,   50);

			listener = new HttpListener();
			listener.Prefixes.Add(uri);
		}

		public WebServer(string uri) {
			Initialize(uri);
		}

		public WebServer(string uri, string folderPath) {
			BaseFolderPath = folderPath;
			Initialize(uri);
		}

		public String BaseFolderPath {
			get {
				return folderPath;
			} set {
				folderPath = value;
			}
		}

		public void Process() {
			listener.Start();

			while (true) {
				try {
					HttpListenerContext context = listener.GetContext();
					ThreadPool.QueueUserWorkItem(ProcessContext, context);
				} catch (HttpListenerException) {
                    break;
                } catch (InvalidOperationException) {
                    break;
                }
			}
		}

		public void Start() {
			mainThread              = new Thread(Process);
			mainThread.IsBackground = true;
			mainThread.Start();
		}

		public void Stop() {
			mainThread.Abort();
			listener.Stop();
		}

		protected virtual void ProcessContext(object listenerContext) {
			try {
				HttpListenerContext context = (HttpListenerContext)listenerContext;
				string filename             = Path.GetFileName(context.Request.RawUrl);
				string path                 = Path.Combine(BaseFolderPath, filename);
                byte[] msg;

				if (!File.Exists(path)) {
					context.Response.StatusCode = (int)HttpStatusCode.NotFound;
					msg = Encoding.UTF8.GetBytes("Sorry, that page does not exist");
				} else {
					context.Response.StatusCode = (int)HttpStatusCode.OK;
					msg = File.ReadAllBytes(path);
				}
				context.Response.ContentLength64 = msg.Length;
				using (Stream s = context.Response.OutputStream)
				s.Write(msg, 0, msg.Length);
			}
			catch (Exception ex) {
                Console.WriteLine("Request error: " + ex);
            }
		}
	}

	public class WebServerConfig {
		private IPAddress address   = null;
		private int port            = 0;
		private string baseFolder   = string.Empty;

		public string HTTPAddress {
			get {
                return "http://" + address.ToString() + ":" + port.ToString() + "/";
            }
		}
	}

	public class HandleWebServer : WebServer {
		IWebHandler handler = null;

		public HandleWebServer(string uri, IWebHandler handler)
			: base(uri) {
			this.handler = handler;
		}

		public HandleWebServer(string uri, string folderPath, IWebHandler handler)
			: base(uri, folderPath) {
			this.handler = handler;
		}

		protected override void ProcessContext(object listenerContext) {
			var context = (HttpListenerContext)listenerContext;
			handler.Handle(context);
		}
	}



	public interface IWebHandler {
		void Handle(HttpListenerContext context);
	}

	public class WebHandler : IWebHandler {
		private WebMethod method = null;

		public WebHandler(WebMethod wm) {
			this.method = wm;
		}

		#region IWebHandler メンバ

		public void Handle(HttpListenerContext context) {
			string filename = Path.GetFileName(context.Request.Url.AbsolutePath);

			string ext = Path.GetExtension(filename);

			switch (ext) {
				case ".flv":
					method.FLVProcess(context);
					break;

				case ".mpg":
					method.FLVProcess(context);
					break;

				case ".html":
					method.HtmlProcess(context);
					break;

				case ".txt":
					method.HtmlProcess(context);
					break;

				default:
					method.DefaultProcess(context);
					break;
			}
		}

		#endregion
	}

	public class WebMethod {
		public PeerSystem       system      = null;
		public PeerFileManager  manager     = null;
		public PeerDownloader   downloader  = null;
		public PeerAction       action      = null;
		public PeerConfig       config      = null;
        private bool    isfirst     = false;//緊急
        private object  sycObject   = new object();
        private HTMLManager videoPage;

        SegmentMulticastReceiver multicastReceiver = null;
		public delegate byte[] PageCreateMethod(HttpListenerContext hlc);
		private Dictionary<string, PageCreateMethod> pageDataCreatorDictionary = new Dictionary<string, PageCreateMethod>();
		public WebMethod(PeerSystem ps, PeerFileManager pfm, PeerDownloader pd, 
                         PeerAction pa, PeerConfig      pc,  SegmentMulticastReceiver smr) {
			system      = ps;
			manager     = pfm;
			downloader  = pd;
			action      = pa;
			config      = pc;
			multicastReceiver = smr;
            videoPage = new HTMLManager(Path.Combine(config.DataPath, "video.html"));
		}

		public void DefaultProcess(HttpListenerContext context) {
			try {
				string  filename    = Path.GetFileName(context.Request.Url.AbsolutePath);
				        filename    = System.Web.HttpUtility.UrlDecode(filename);
				string  path        = Path.Combine(system.Config.DataPath, filename);

				byte[] msg;
				if (!File.Exists(path)) {
					path = Path.Combine(system.Config.UploadPath, filename);
					if (File.Exists(path)) {
						context.Response.StatusCode = (int)HttpStatusCode.OK;
						msg = File.ReadAllBytes(path);
					} else {
						context.Response.StatusCode = (int)HttpStatusCode.NotFound;
						msg = Encoding.UTF8.GetBytes("Sorry, that page does not exist");
					}
				} else {
					context.Response.StatusCode = (int)HttpStatusCode.OK;
					msg = File.ReadAllBytes(path);
				}
				context.Response.ContentLength64 = msg.Length;
                using (Stream s = context.Response.OutputStream)
                    s.Write(msg, 0, msg.Length);
			} catch (Exception ex) {
                Console.WriteLine("Request error: " + ex);
            }
		}
		private HttpListenerContext flvContext = null;
		public void FLVThread() {
			HttpListenerContext context = flvContext;
			try {
				System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
				sw.Start();

				string  filename = Path.GetFileName(context.Request.Url.AbsolutePath);
				        filename = System.Web.HttpUtility.UrlDecode(filename);

				//拡張子を削る
				string ext = Path.GetExtension(filename);
				if (!string.IsNullOrEmpty(ext)) {
					filename = filename.Substring(0, filename.Length - 4);
				}

				string path = Path.Combine(system.Config.CachePath, filename);

				FileDataInfo fdi = null ;
				ContentInfoBase cdi = null;

				byte[] hashByte = null;
				if (!filename.Equals("multicast")) {
					hashByte = BTool.ByteParse(filename);
					do {
						fdi = manager.GetFileData(new Hash(hashByte));
						cdi = system.GetContentInfo(new Hash(hashByte));
						Thread.Sleep(50);
					} while (fdi == null);
				}
				//マルチキャストの場合
				else {
                    foreach (var ci in system.ContentDictionary.Keys) {
						if (ci.Type == ContentType.Multicast) {
							manager.AddFileData(ci);
							fdi = manager.GetFileData(ci.BaseHash);
						}
					}
				}

				context.Response.ContentLength64 = fdi.FileSize;
				PeerLogger.Instance.OnMassage("初期化に要する時間" + sw.Elapsed.TotalMilliseconds.ToString());
				int count = 0;
				double first = 0d;
				//100kByte/sで計算
				System.Diagnostics.Stopwatch sw2 = new System.Diagnostics.Stopwatch();
				sw2.Start();
				using (Stream s = context.Response.OutputStream) {
					for (var i = 0; i < fdi.BlockCount; i++) {
						DataSegment ds = null;
						while (true) {
							ds = manager.ReadFile(fdi.BaseHash, i);
							if (null != ds) {
								if (ds.SegmentNumber == 0) {
									sw.Reset();
									sw.Start();
								}
								else if (ds.SegmentNumber == 5) {
									var span = sw.Elapsed;
									first = span.TotalSeconds;
								}
								break;
							}
							Thread.Sleep(200);//512kbyteは100kbyte/sでは約5秒なので
							count++;
						}
						s.Write(ds.Data, 0, ds.Data.Length);
						//PeerLogger.Instance.OnMassage("再生までの時間 " + sw.Elapsed.ToString());
					}
					PeerLogger.Instance.OnMassage("はじめのバッファがたまるまでの時間 " + first.ToString());
					PeerLogger.Instance.OnMassage("全部のセグメントが配信されるまでの時間 " + sw2.Elapsed.ToString());
					PeerLogger.Instance.OnMassage("途切れ数 " + count.ToString());
				}
			}
			catch (Exception ex) {
				Console.WriteLine("Request error: " + ex);
			}
		}

		public void FLVProcess(HttpListenerContext context) {
			flvContext          = context;
			Thread flv          = new Thread(new ThreadStart(FLVThread));
			flv.IsBackground    = true;
			flv.Start();

			/*
			try {
				string filename = Path.GetFileName(context.Request.Url.AbsolutePath);
				string path = Path.Combine(system.Config.CachePath, filename);

				byte[] msg;
				if (!File.Exists(path)) {
					context.Response.StatusCode = (int)HttpStatusCode.NotFound;
					msg = Encoding.UTF8.GetBytes("Sorry, that page does not exist");
				}
				else {
					context.Response.StatusCode = (int)HttpStatusCode.OK;
					msg = File.ReadAllBytes(path);
				}
				context.Response.ContentLength64 = msg.Length;
				using (Stream s = context.Response.OutputStream)
					s.MutableDataWrite(msg, 0, msg.Length);

			}
			catch (Exception ex) { Console.WriteLine("Request error: " + ex); }
			

			try {
				System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
				sw.Start();

				string filename = Path.GetFileName(context.Request.Url.AbsolutePath);
				filename = System.Web.HttpUtility.UrlDecode(filename);

				//拡張子を削る
				string ext = Path.GetExtension(filename);
				if (!string.IsNullOrEmpty(ext)) {
					filename = filename.Substring(0, filename.Length - 4);
				}

				string path = Path.Combine(system.Config.CachePath, filename);

				byte[] hashByte = BTool.ByteParse(filename);
				var fdi = manager.GetFileData(new Hash(hashByte));
				var cdi = system.GetContentInfo(new Hash(hashByte));


				context.Response.ContentLength64 = fdi.FileSize;

				Thread.Sleep(100);



				//
				PeerLogger.Instance.OnMassage("初期化に要する時間" + sw.Elapsed.TotalMilliseconds.ToString());

				if (cdi.Type == ContentType.BroadCast) {
					
					using (Stream s = context.Response.OutputStream) {
						bool init = false;
						int enableIndex = 0;

						byte[] headerData = action.GetMovieHeader(cdi);
						s.Write(headerData, 0, headerData.Length);
						isfirst = true;
						


						while (true) {
							DataSegment ds = null;
							while (true) {


								ds = manager.ReadFile(fdi.BaseHash, enableIndex);

								if (null != ds) {
									enableIndex++;
									init = true;
									break;
								}
								else if (ds == null && !init) {

									enableIndex++;
								}


								Thread.Sleep(300);//適当
							}

							s.Write(ds.Data, 0, ds.Data.Length);


							if (fdi.BlockCount == enableIndex) {
								break;
							}
						}
						
					}
					
				}
				else {

					int count = 0;
					double first = 0d ;
					//100kByte/sで計算

					using (Stream s = context.Response.OutputStream) {
						for (var i = 0; i < fdi.BlockCount; i++) {
							DataSegment ds = null;
							while (true) {
								

								ds = manager.ReadFile(fdi.BaseHash, i);
								if (null != ds) {
									if (ds.SegmentNumber == 0) {
										sw.Reset();
										sw.Start();
									}
									else if (ds.SegmentNumber == 5) {
										var span = sw.Elapsed;
										first = span.TotalSeconds;
									}

									break;
								}
								Thread.Sleep(200);//512kbyteは100kbyte/sでは約5秒なので
								

								count++;
							}

							s.Write(ds.Data, 0, ds.Data.Length);
							PeerLogger.Instance.OnMassage("再生までの時間 " + sw.Elapsed.TotalMilliseconds.ToString());
						}
						PeerLogger.Instance.OnMassage("はじめのバッファがたまるまでの時間 " + first.ToString());
						PeerLogger.Instance.OnMassage("全部のセグメントが配信されるまでの時間 " + sw.Elapsed.TotalMilliseconds.ToString());
						PeerLogger.Instance.OnMassage("途切れ数 " + count.ToString());
					}
				}



			}
			catch (Exception ex) {
				Console.WriteLine("Request error: " + ex);
			}
			 * 
			 */
		}

		public void AddPage(string uri, PageCreateMethod creator) {
			pageDataCreatorDictionary.Add(uri, creator);
		}

		public void HtmlProcess(HttpListenerContext context) {
			Uri uri = context.Request.Url;
			string filename = Path.GetFileName(uri.AbsolutePath);

			/* 今のところ封印
			byte[] msg = null;
			try {
				msg = pageDataCreatorDictionary[filename](context);
			}
			catch(Exception){
				context.Response.StatusCode = (int)HttpStatusCode.NotFound;
				msg = Encoding.UTF8.GetBytes("Sorry, that page does not exist");

				context.Response.ContentLength64 = msg.Length;
			}
			using (Stream s = context.Response.OutputStream) {
				s.Write(msg, 0, msg.Length);
			}
			*/

			switch (filename) {
				case "search.html":
				SEARCH: {
						var html = BrossomHtml.HtmlBlock();
						HtmlElement headElement = new HtmlElement("head");
						HtmlElement metaElement = new HtmlElement("mata", new List<HtmlAttribute>(){
							new HtmlAttribute("http-equiv"  , "Content-Type"        ),
							new HtmlAttribute("content"     , "text/html"       ),
							new HtmlAttribute("charset"     , "unicode-2-0-utf-8")
						});

						HtmlElement titleElement = new HtmlElement("title", "Search");

						var centering = new HtmlElement("div", new List<HtmlAttribute>() { new HtmlAttribute("align", "center") });
						var image = new HtmlElement("img",
							new List<HtmlAttribute>(){
										new HtmlAttribute("src",Path.Combine(config.LocalEndPoint.ToString(),"brossom_logo.jpg")),
										new HtmlAttribute("border","0"),
									});

						string torrent  = "BitTorrent";
						string okayamaU = "岡山大学";
						string ipa      = "IPA";
						string gotoh    = "後藤研究室";

						var torrentLink     = new HtmlElement("a", torrent,  new List<HtmlAttribute>() { new HtmlAttribute("href", "http://www.bittorrent.com/") });
						var okayamaUnivLink = new HtmlElement("a", okayamaU, new List<HtmlAttribute>() { new HtmlAttribute("href", "http://www.okayama-u.ac.jp/") });
						var ipaLink         = new HtmlElement("a", ipa,      new List<HtmlAttribute>() { new HtmlAttribute("href", "http://www.ipa.go.jp/") });
						var gotohLink       = new HtmlElement("a", gotoh,    new List<HtmlAttribute>() { new HtmlAttribute("href", "http://www.mis.cs.okayama-u.ac.jp/") });

                        var multicastLink   = new HtmlElement("a", "マルチキャスト", new List<HtmlAttribute>() { new HtmlAttribute("href", "./multicast.html") });
						var form            = new HtmlElement("form", new List<HtmlAttribute>() { new HtmlAttribute("action", "./result.html")});

                        var searchTextBox = new HtmlElement("input", new List<HtmlAttribute>(){
							new HtmlAttribute("type"  , "text"),
							new HtmlAttribute("name"  , "search"),
							new HtmlAttribute("style" , "font-size:12pt;"),
							new HtmlAttribute("size"  , "80")
						});

						var searchButton = new HtmlElement("input", new List<HtmlAttribute>(){
							new HtmlAttribute("type"  ,"submit"),
							new HtmlAttribute("name"  ,"btn"),
							new HtmlAttribute("value" ,"検索")
						});

						var randomButton = new HtmlElement("input", new List<HtmlAttribute>(){
							new HtmlAttribute("type"  ,"submit"),
							new HtmlAttribute("name"  ,"btn"),
							new HtmlAttribute("value" ,"ランダム検索")
						});

						MemoryStream  ms     = new MemoryStream();
						StreamWriter  sw     = new StreamWriter(ms, Encoding.UTF8);
						XmlTextWriter writer = new XmlTextWriter(sw);

						using (html.BeginBlock(writer)) {
							using (headElement.BeginBlock(writer)) {
								metaElement.Write(writer);
								titleElement.Write(writer);
							}
							using (centering.BeginBlock(writer)) {
								image.Write(writer);
								using (form.BeginBlock(writer)) {
									searchTextBox.Write(writer);
									writer.WriteRaw("<br>");
									searchButton.Write(writer);
									randomButton.Write(writer);
									multicastLink.Write(writer);
								}
								writer.WriteRaw("<br>");
								torrentLink.Write(writer);
								writer.WriteRaw("-");
								okayamaUnivLink.Write(writer);
								writer.WriteRaw("-");
								ipaLink.Write(writer);
								writer.WriteRaw("<br>");
								gotohLink.Write(writer);
							}
						}
						writer.Close();
						sw.Close();
						byte[] msg = ms.ToArray();
						ms.Close();
						context.Response.ContentLength64 = msg.Length;

						using (Stream s = context.Response.OutputStream) {
							s.Write(msg, 0, msg.Length);
						}
					}
				break;

				case "request.txt": {
					string querys = uri.GetComponents(UriComponents.Query, UriFormat.UriEscaped);
					string file = string.Empty;
					if (querys != string.Empty) {
						QueryAnalizer qa = new QueryAnalizer(querys);
						file = qa.Get("FILE");
					}

					byte[] hashArray = BTool.ByteParse(file);
					var c = system.GetContentInfo(new Hash(hashArray));
					manager.AddFileData(c);

					downloader.AddDownload(c);

					DateTime dt = DateTime.Now;
					File.WriteAllText("download.txt", dt.ToString(), Encoding.UTF8);

				}
				break;

				case "video.html": {

					string querys = uri.GetComponents(UriComponents.Query, UriFormat.UriEscaped);
					string file = string.Empty;
					string type = string.Empty;
					if (querys != string.Empty) {
						QueryAnalizer qa = new QueryAnalizer(querys);

						file = qa.Get("FILE");
						type = qa.Get("TYPE");
					}
					/*
					//ピアに要求を出す．//////////////////////////////////////
					if (string.Compare(type, "P2P") == 0) {

						byte[] hashArray = BTool.ByteParse(file);
						var c = system.GetContentInfo(new Hash(hashArray));
						manager.AddFileData(c);

						downloader.AddDownload(c);

					}
					else if (string.Compare(type, "BroadCast") == 0) {
						byte[] hashArray = BTool.ByteParse(file);
						var c = system.GetContentInfo(new Hash(hashArray));
						manager.AddFileData(c);

						action.RegBoradCast(system.MyNodeInfo);
					}

					///////////////////////////////////////////////////////////
					*/

					StringBuilder sb = new StringBuilder();

					string v = File.ReadAllText("video.html");
					v = v.Replace("?--filename--?", file);
					//FlowPlayer
					//sb.Append("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\"><html xmlns=\"http://www.w3.org/1999/xhtml\" xml:lang=\"en\" lang=\"en\"><head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\" /><title>FlowPlayer</title></head><body bgcolor=\"#ffffff\"><object type=\"application/x-shockwave-flash\" data=\"FlowPlayer.swf\" width=\"320\" height=\"263\" id=\"FlowPlayer\"><param name=\"allowScriptAccess\" value=\"sameDomain\" /><param name=\"movie\" value=\"FlowPlayer.swf\" /><param name=\"quality\" value=\"high\" /><param name=\"scale\" value=\"noScale\" /><param name=\"wmode\" value=\"transparent\" /><param name=\"flashvars\" value=\"baseURL=http://localhost:20000&&amp;videoFile=");
					//sb.Append(file);
					//sb.Append("&autoPlay=true&startingBufferLength=5&bufferLength=10\" /></object><br>");
					//sb.Append("<script type=\"text/javascript\"><!--function loadTextFile(){httpObj = new XMLHttpRequest();httpObj.open(\"GET\",\"request.txt" + querys + ",true);httpObj.send(null);}function displayData(){document.ajaxForm.result.value = httpObj.responseText;}// --></script>");
					//sb.Append("<input type=\"button\" value=\"送信\" onClick=\"loadTextFile()/><BR></body></html>");

					//flvplayer
					//sb.Append("<html><head><script type=\"text/javascript\" src=\"swfobject.js\"></script></head><body><h3>single file, with preview image:</h3><p id=\"player1\"><a href=\"http://www.macromedia.com/go/getflashplayer\">Get the Flash Player</a> to see  player.</p><script type=\"text/javascript\">	var s1 = new SWFObject(\"flvplayer.swf\",\"single\",\"512\",\"384\",\"7\");s1.addParam(\"allowfullscreen\",\"true\");s1.addVariable(\"file\",\"");
					//sb.Append(file);
					//sb.Append("\");s1.addVariable(\"image\",\"preview.jpg\");s1.addVariable(\"width\",\"512\");s1.addVariable(\"height\",\"384\");s1.write(\"player1\");</script></body></html>");

					//madiaplayer


					/*
					var html = BrossomHtml.HtmlBlock();
					var header = BrossomHtml.HeaderBlock("Video");
					var embed = new HtmlElement("embed", new List<HtmlAttribute>() { new HtmlAttribute("src", file + ".mpg") });
					byte[] hashArray1 = BTool.ByteParse(file);
					var cc = system.GetContentInfo(hashArray1);
					cc.Type = ContentType.P2P;

					var centering = new HtmlElement("div", new List<HtmlAttribute>() { new HtmlAttribute("align", "center") });

					var link = new HtmlElement("a", "P2PLink",
						new List<HtmlAttribute>() { 
								new HtmlAttribute("href", BrossomHtml.BrossomCreateMovieLink(cc)),
								new HtmlAttribute("target=","_blank"),
							}
					);

					cc.Type = ContentType.BroadCast;

					MemoryStream ms = new MemoryStream();
					StreamWriter sw = new StreamWriter(ms, Encoding.UTF8);
					XmlTextWriter writer = new XmlTextWriter(sw);

					using (html.BeginBlock(writer)) {
						header.Write(writer);
						using (centering.BeginBlock(writer)) {
							embed.Write(writer);
							writer.WriteRaw("<br>");
							link.Write(writer);
						}
					}


					writer.Close();
					sw.Close();
					byte[] msg = ms.ToArray();
					ms.Close();
					*/

					string ss = v;// sb.ToString();
					byte[] msg = Encoding.UTF8.GetBytes(ss);
					context.Response.ContentLength64 = msg.Length;

					/*
					string pah = "http://localhost:20000/" + file + ".flv";
					videoPage.ReplaceList.Add("\"" + pah + "\"");
					string ss = videoPage.ReplaceWord();
					videoPage.ReplaceList.Clear();
					//string ss = sb.ToString();
					byte[] msg = Encoding.UTF8.GetBytes(ss);
					context.Response.ContentLength64 = msg.Length;
					*/


					using (Stream s = context.Response.OutputStream) {
						s.Write(msg, 0, msg.Length);
					}
				}
				break;

				case "result.html": {
					string querys = uri.GetComponents(UriComponents.Query, UriFormat.UriEscaped);
					string word = string.Empty;
					string btn = string.Empty;
					if (querys != string.Empty) {
						QueryAnalizer qa = new QueryAnalizer(querys);

						word = System.Web.HttpUtility.UrlDecode(qa.Get("search"));//wordが検索ワード
						btn = System.Web.HttpUtility.UrlDecode(qa.Get("btn"));
					}
					//action.GetFoundContent(word);

					MemoryStream ms = new MemoryStream();
					StreamWriter sw = new StreamWriter(ms, Encoding.UTF8);
					XmlTextWriter writer = new XmlTextWriter(sw);



					var html    = BrossomHtml.HtmlBlock();
					var header  = BrossomHtml.HeaderBlock("Result");

					var centering    = new HtmlElement("div", new List<HtmlAttribute>() { new HtmlAttribute("align", "center") });
					var tableSetting = new BasicTable.TableSetting();

					tableSetting.TableAttributes.Add(new HtmlAttribute("border", "1"));
					tableSetting.TableAttributes.Add(new HtmlAttribute("cellspacing", "1"));
					tableSetting.TableAttributes.Add(new HtmlAttribute("cellspacing", "1"));

					//tableSetting.TableAttributes.Add(new HtmlAttribute("align", "center"));
					//tableSetting.TableAttributes.Add(new HtmlAttribute("valign", "top"));

					tableSetting.ColumnAttributes.Add(new HtmlAttribute("style", "text-align:center;vertical-align:top;"));

					tableSetting.RowAttributes.Add(new HtmlAttribute("height", "200"));

					tableSetting.ColumnAttributes.Add(new HtmlAttribute("width", "200"));
					tableSetting.ColumnCellNum = 3;
					var table = new BasicTable(tableSetting);

					var goToTopLink = new HtmlElement("a", new List<HtmlAttribute>() { new HtmlAttribute("href", "./index.html") });

					var indexImage = new HtmlElement("img",
							new List<HtmlAttribute>(){
										new HtmlAttribute("src",Path.Combine(config.LocalEndPoint.ToString(),"brossom_logo_small.jpg")),
										new HtmlAttribute("border","0"),
									});

					var indexLink = new HtmlElement("a", new List<HtmlAttribute>() { new HtmlAttribute("href", "./index.html") });


					var form = new HtmlElement("form", new List<HtmlAttribute>() { new HtmlAttribute("action", "./result.html") });
					var searchTextBox = new HtmlElement("input", new List<HtmlAttribute>(){
							new HtmlAttribute("type" , "text"),
							new HtmlAttribute("name" , "search"),
							new HtmlAttribute("style", "font-size:12pt;"),
							new HtmlAttribute("size" , "80"),
							new HtmlAttribute("value", word),
						});

					var searchButton = new HtmlElement("input", new List<HtmlAttribute>(){
							new HtmlAttribute("type" , "submit"),
							new HtmlAttribute("name" , "btn"),
							new HtmlAttribute("value", "検索")
						});

					var randomButton = new HtmlElement("input", new List<HtmlAttribute>(){
							new HtmlAttribute("type" , "submit"),
							new HtmlAttribute("name" , "btn"),
							new HtmlAttribute("value", "ランダム検索")
						});

					if (btn == "ランダム検索") {
						foreach (var c in system.ContentDictionary.Keys) {
							Random rand = new Random();
							int i = rand.Next(3);

							if (i == 0) {
								var t = system.GetTagInfo(c);

								if (t != null) {
									table.AddText(BrossomHtml.BrossomContentPreview(config, c, t));
								}
							}
						}

					}
					else {

						foreach (var c in system.ContentDictionary.Keys) {
							var t = system.GetTagInfo(c);

							if (t != null) {
								if (t.Title.Contains(word)) {
									table.AddText(BrossomHtml.BrossomContentPreview(config, c, t));
								}
								else {
									foreach (var tag in t.TagList) {
										if (tag != null && tag.Contains(word)) {
											table.AddText(BrossomHtml.BrossomContentPreview(config, c, t));
											break;
										}
									}
								}
							}
							else {
								if (c.Name.Contains(word)) {
									table.AddText(BrossomHtml.BrossomContentPreview(config, c));
								}
							}

						}
					}

					string tableText = table.Emit();

					using (html.BeginBlock(writer)) {
						header.Write(writer);

						using (centering.BeginBlock(writer)) {
							using (indexLink.BeginBlock(writer)) {
								indexImage.Write(writer);
								//writer.WriteRaw("インデックスに戻る");
							}
							writer.WriteElementString("h1", word + " の検索結果");
							using (form.BeginBlock(writer)) {
								searchTextBox.Write(writer);
								writer.WriteRaw("<br>");
								searchButton.Write(writer);
								randomButton.Write(writer);
							}

							using (goToTopLink.BeginBlock(writer)) {
								writer.WriteElementString("h5", "トップへもどる");
							}
							if (string.IsNullOrEmpty(tableText)) {
								writer.WriteRaw("NotFound<br>");
							}
							else {
								writer.WriteRaw(tableText);
							}
						}

					}


					writer.Close();
					sw.Close();

					byte[] msg = ms.ToArray();
					context.Response.ContentLength64 = msg.Length;

					using (Stream s = context.Response.OutputStream) {
						s.Write(msg, 0, msg.Length);
					}
				}
				break;

				case "multicast_start.html": {
					//要求が出された時間を記録
					var ipep = this.system.MyNodeInfo.IPEP.Address.ToString();
					DateTime dt = DateTime.Now;
					File.WriteAllText(ipep + "multicastStartTime.txt", dt.ToString(), Encoding.UTF8);

					ContentInfoBase cib = null;
					foreach (var c in system.ContentDictionary.Keys) {

						if (c.Type == ContentType.Multicast) {
							cib = c;
							break;
						}
					}
					if (cib != null) {
						multicastReceiver.StartReceive(cib);
					}
					break;
				}

				case "multicast.html": {
					string querys = uri.GetComponents(UriComponents.Query, UriFormat.UriEscaped);
					string file = string.Empty;
					string type = string.Empty;
					if (querys != string.Empty) {
						QueryAnalizer qa = new QueryAnalizer(querys);
						file = qa.Get("FILE");
						type = qa.Get("TYPE");
					}

					StringBuilder sb = new StringBuilder();
					string v   = File.ReadAllText("multicast.html");
					string ss  = v;// sb.ToString();
					byte[] msg = Encoding.UTF8.GetBytes(ss);
					context.Response.ContentLength64 = msg.Length;

					using (Stream s = context.Response.OutputStream) {
						s.Write(msg, 0, msg.Length);
					}
					break;
				}

				case "index.html":
				goto SEARCH;

				default: {
					goto SEARCH;

					/*
					byte[] msg;
					context.Response.StatusCode = (int)HttpStatusCode.NotFound;
					msg = Encoding.UTF8.GetBytes("Sorry, that page does not exist");

					context.Response.ContentLength64 = msg.Length;
					using (Stream s = context.Response.OutputStream)
						s.Write(msg, 0, msg.Length);
					 */
				}
				//break;
			}
		}

		public class QueryAnalizer {
			private string[] querys = null;
			public QueryAnalizer(string query) {
                querys = query.Split('&');

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
						catch (ArgumentOutOfRangeException e) {
							Console.WriteLine(e.ToString());
							return string.Empty;
						}
						return retStr;
					}
				}
				return string.Empty;
			}
		}
	}
}