using System;

using System.Collections.Generic;
using System.Threading;
using P2P.BBase;

namespace P2P {

	public class DownloadTask {
		public static System.Diagnostics.Stopwatch masterWatch = null;
		private FileDataInfo fdi = null;

		private Node node = null;

		private int segmentNum = 0;

		private PeerFileManager manager = null;
		private PeerAction action = null;
		private ConnectionManager cm = null;

		private bool taskEnd = false;

		public bool TaskEnd {
			get { return taskEnd; }
			set { taskEnd = value; }
		}

		public DownloadTask(FileDataInfo fdi, Node node, int segmentNum, PeerFileManager manager, PeerAction action) {
			this.fdi = fdi;
			this.node = node;
			this.segmentNum = segmentNum;
			this.manager = manager;
			this.action = action;
		}


		public void Download() {
			System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
			sw.Reset();
			sw.Start();
			TimeSpan baseSpan = masterWatch.Elapsed;

			DataSegment ds = new DataSegment(fdi.BaseHash.ByteData, segmentNum, null);
			action.GetDataSegment(node, ds);

			TimeSpan elapsed = sw.Elapsed;


			double dataSizeRatio = (double)(ds.TagArray.Length + ds.Data.Length) / (double)FileDividePolicy.FileDivideSize;
			double timeRatio = elapsed.TotalMilliseconds / ((double)FileDividePolicy.FileDivideSize / (node.BandWidth * 1024.0));

			PeerLogger.Instance.OnMassage(
				"ダウンロード," + ds.SegmentNumber.ToString() + ", " + node.IPEP.ToString() + ", " + node.BandWidth.ToString() + ", " + baseSpan.TotalSeconds.ToString() + ", " + elapsed.ToString() + "[s], " + timeRatio.ToString() + "倍時間, " + dataSizeRatio.ToString() + "倍");



			//manager.WriteFile(fdi.BaseHash, ds);
			taskEnd = true;
		}

	}

	public class ConnectionManager {
		int connectionNum = 0;
		Dictionary<Node, bool> connections = null;


		public ConnectionManager(int connectionNum) {
			this.connectionNum = connectionNum;
			connections = new Dictionary<Node, bool>(connectionNum);

		}

		public void AddNode(Node n) {
			this.connections.Add(n, false);
		}

		public void LockConnect(Node n) {
			this.connections[n] = true;
		}

		public bool CheckConnect(Node n) {
			return connections[n];
		}

		public void UnlockConnect(Node n) {
			this.connections[n] = false;
		}


	}

	public class DownloadSchedule {

		static public Node[] SequencialSchedule(int segmentNum, List<Node> nodes, int maxConnection) {
			Node[] ns = new Node[segmentNum];
			for (int i = 0; i < ns.Length; i++) {
				ns[i] = nodes[i % nodes.Count];
			}
			return ns;
		}

		static public Node[] WRPSSchedule(int segmentNum, List<Node> nodes, int maxConnection) {
			Node[] ns = new Node[segmentNum];

			double[] speeds = new double[nodes.Count];
			double[] speeds2 = new double[nodes.Count];


			for (int i = 0; i < speeds.Length; i++) {
				speeds2[i] = speeds[i] = 1.0d / nodes[i].BandWidth;
			}





			for (int i = 0; i < ns.Length; i++) {
				//for (int j = 0; j < speeds.Length; j++) {
				var minIndex = FindMinIndex(speeds);

				ns[i] = nodes[minIndex];

				speeds[minIndex] += speeds2[minIndex];

				//}

			}

			return ns;



		}


		static public Node[] WRPSSchedule2(int segmentNum, List<Node> nodes, int maxConnection) {
			Node[] ns = new Node[segmentNum];

			nodes.Sort((Node n1, Node n2) => {
				return n2.BandWidth - n1.BandWidth;
			});

			double[] speeds = new double[maxConnection];
			double[] speeds2 = new double[maxConnection];


			for (int i = 0; i < speeds.Length; i++) {
				speeds2[i] = speeds[i] = 1.0d / nodes[i].BandWidth;
			}





			for (int i = 0; i < ns.Length; i++) {
				//for (int j = 0; j < speeds.Length; j++) {
				var minIndex = FindMinIndex(speeds);

				ns[i] = nodes[minIndex];

				speeds[minIndex] += speeds2[minIndex];

				//}

			}

			return ns;
		}

		static public Node[] WRPSSchedule3(int segmentNum, List<Node> nodes, int maxConnection, int limit) {
			Node[] ns = new Node[limit];

			nodes.Sort((Node n1, Node n2) => {
				return n2.BandWidth - n1.BandWidth;
			});

			double[] speeds = new double[maxConnection];
			double[] speeds2 = new double[maxConnection];


			for (int i = 0; i < speeds.Length; i++) {
				speeds2[i] = speeds[i] = 1.0d / nodes[i].BandWidth;
			}





			for (int i = 0; i < limit; i++) {
				//for (int j = 0; j < speeds.Length; j++) {
				var minIndex = FindMinIndex(speeds);

				ns[i] = nodes[minIndex];

				speeds[minIndex] += speeds2[minIndex];

				//}

			}

			return ns;



		}


		static public Node[] WPPSSchedule(int segmentNum, List<Node> nodes, int maxConnection, int limit,int offset) {
			if (offset == 0) {
				return WRPSSchedule3(segmentNum, nodes, maxConnection,limit);
			}

			Node[] ns = new Node[limit];

			double speeds = 128.0 * 8 / nodes[0].BandWidth;//bps

			nodes.Sort((Node n1, Node n2) => {
				return n2.BandWidth - n1.BandWidth;
			});

			var count = 0;
			for (double i = speeds; i < offset; i += speeds) {
				ns[count] = nodes[0];
				count++;
			}

			for (int i = count; i < limit; i++) {
				ns[i] = nodes[1];
			}
			return ns;
		}

		static double FindMinValue(double[] a) {
			var min = double.MaxValue;

			for (int i = 0; i < a.Length; i++) {
				min = Math.Min(min, a[i]);
			}

			return min;
		}


		static int FindMinIndex(double[] a) {
			double min = double.MaxValue;
			int index = 0;

			for (int i = 0; i < a.Length; i++) {
				if (min > a[i]) {
					min = a[i];
					index = i;
				}
			}

			return index;


		}


	}

	public class MulticastDownloadOption {
		int limitSegmentNum = 0;

		public int LimitSegmentNum {
			get { return limitSegmentNum; }
			set { limitSegmentNum = value; }
		}

		public MulticastDownloadOption(int limit) {
			this.limitSegmentNum = limit;
		}

	}

	public class DownloadInfo {
		ContentInfoBase contentInfo;

		public ContentInfoBase ContentInfo {
			get { return contentInfo; }
			set { contentInfo = value; }
		}
		object downloadOption;

		public object DownloadOption {
			get { return downloadOption; }
			set { downloadOption = value; }
		}

		public DownloadInfo(ContentInfoBase cib) {
			this.contentInfo = cib;
			this.downloadOption = null;
		}


		public DownloadInfo(ContentInfoBase cib, object option) {
			this.contentInfo = cib;
			this.downloadOption = option;
		}
	}


	public class PeerDownloader {
		private PeerSystem system = null;
		private PeerAction action = null;
		private PeerFileManager manager = null;

		private LinkedList<DownloadInfo> downloadFileList = null;

		public delegate int? DownloadStrategyMethod(IEnumerator<int> enu);

		private DownloadStrategyMethod downloadStrategy = null;

		public delegate FileDataInfo FileSelectStrategyMethod(List<FileDataInfo> list);
		private FileSelectStrategyMethod fileSelectStrategy = null;//これから必要かも

		private Thread mainThread = null;
		private int downloadLimitNum = 0;

		bool isSimpleMode = false;

		public bool IsSimpleMode {
			get { return isSimpleMode; }
			set { isSimpleMode = value; }
		}

		static private int dn = 1;

		private Thread[] threads = new Thread[dn];
		private int downloadNum = dn;

		int wppsOffset = 0;

		public int WppsOffset {
			get { return wppsOffset; }
			set { wppsOffset = value; }
		}


		int videoRate = 1000;

		public int VideoRate {
			get { return videoRate; }
			set { videoRate = value; }
		}

		int[] selectNodeIndex = null;

		public int[] SelectNodeIndex {
			get { return selectNodeIndex; }
			set { selectNodeIndex = value; }
		}


		public PeerDownloader(PeerSystem ps, PeerAction pa, PeerFileManager pfm) {
			system = ps;
			action = pa;
			manager = pfm;

			downloadFileList = new LinkedList<DownloadInfo>();
			downloadStrategy = DownloadStrategy.SequentialDownload;
		}


		public void Start() {
			mainThread = new Thread(DownloadProcess);
			mainThread.IsBackground = true;
			mainThread.Start();
		}

		public void Stop() {
			mainThread.Abort();
		}


		private void DownloadProcess() {
			DownloadTask.masterWatch = new System.Diagnostics.Stopwatch();
			DownloadTask.masterWatch.Reset();
			DownloadTask.masterWatch.Start();
			DateTime dlStartTime = DateTime.Now;

			bool[] connections = new bool[downloadNum];
			while (true) {
				if (downloadFileList.Count != 0) {
					PeerLogger.Instance.OnMassage("ダウンロードあり 並列数" + dn.ToString());
					while (true) {
						FileDataInfo fdi2 = null;

						foreach (var di in downloadFileList) {

							var fdi = manager.GetFileData(di.ContentInfo.BaseHash);



							List<Node> contentHolderList2;
							system.ContentDictionary.TryGetValue(di.ContentInfo, out contentHolderList2);

							List<Node> contentHolderList = new List<Node>();
							int sumSpeed = 0;
							//ソート
							contentHolderList2.Sort((Node n1, Node n2) => {
								return n2.BandWidth - n1.BandWidth;
							});

							if (selectNodeIndex == null) {
								if (isSimpleMode) {
									while (sumSpeed < videoRate) {
										//評価用に追加
										if (contentHolderList.Count == 3) {
											break;
										}
										if (contentHolderList2.Count == 0) {
											break;
										}
										contentHolderList.Add(contentHolderList2[0]);
										sumSpeed += contentHolderList2[0].BandWidth;
										contentHolderList2.RemoveAt(0);
									}
								}
								else {
									//使うピアを選別する
									//最速ピアと足りなければ足りない分だけ
									bool init = false;
									while (sumSpeed < videoRate) {
										//評価用に追加
										if (contentHolderList.Count == 3) {
											break;
										}
										//0ならブレイクせざるを得ない
										if (contentHolderList2.Count == 0) {
											break;
											//throw new Exception("スケジューリングエラー");
										}

										if (contentHolderList.Count >= dn) {
											break;
										}


										int diff = videoRate - sumSpeed;

										Node node = null;
										int index = 0;
										if (!init) {
											index = contentHolderList2.FindIndex((Node n) => {
												if (n.BandWidth > diff) {
													return true;
												}
												return false;
											});

											if (node == null) {
												index = 0;
											}
											init = true;
										}
										else {
											index = contentHolderList2.FindLastIndex((Node n) => {
												if (n.BandWidth > diff) {
													return true;
												}
												return false;
											});
											if (node == null) {
												index = 0;
											}
										}

										node = contentHolderList2[index];

										contentHolderList2.RemoveAt(index);
										contentHolderList.Add(node);
										sumSpeed += node.BandWidth;
									}
								}

							}
							else {
								foreach (var i in selectNodeIndex) {
									contentHolderList.Add(contentHolderList2[i]);
								}

							}

							if(selectNodeIndex == null){
								action.PutContentNodeList();
							}

							contentHolderList.Sort((Node n1, Node n2) => {
								return n2.BandWidth - n1.BandWidth;
							});

							int limit = fdi.BlockCount;
							if (di.DownloadOption is MulticastDownloadOption) {
								MulticastDownloadOption mdo = (MulticastDownloadOption)di.DownloadOption;
								limit = mdo.LimitSegmentNum;
							}

							Node[] schedule = null;
							if (isSimpleMode) {
								schedule = DownloadSchedule.SequencialSchedule(fdi.BlockCount, contentHolderList, contentHolderList.Count);
							}
							else {
								//
								schedule = DownloadSchedule.WPPSSchedule(fdi.BlockCount, contentHolderList, contentHolderList.Count, limit,wppsOffset);
							}


							Dictionary<System.Net.IPEndPoint, List<DownloadTask>> taskdic = new Dictionary<System.Net.IPEndPoint, List<DownloadTask>>();


							bool isParallelDownload = true;

							Thread[] nodeThread = null;
							NodeTask[] nodeTasks = null;
							if (isParallelDownload) {
								for (int i = 0; i < schedule.Length; i++) {
									if (!taskdic.ContainsKey(schedule[i].IPEP)) {
										taskdic.Add(schedule[i].IPEP, new List<DownloadTask>());
										taskdic[schedule[i].IPEP].Add(new DownloadTask(fdi, schedule[i], i, manager, action));
									}
									else {
										taskdic[schedule[i].IPEP].Add(new DownloadTask(fdi, schedule[i], i, manager, action));
									}
								}

								nodeThread = new Thread[taskdic.Keys.Count];

								var enu = taskdic.Values.GetEnumerator();

								nodeTasks = new NodeTask[taskdic.Keys.Count];

								for (int i = 0; i < nodeThread.Length; i++) {
									enu.MoveNext();
									nodeTasks[i] = new NodeTask(enu.Current);
									nodeThread[i] = new Thread(nodeTasks[i].Run);
									nodeThread[i].IsBackground = true;
								}

							}
							else {

								nodeTasks = new NodeTask[1];
								nodeThread = new Thread[1];
								List<DownloadTask> downloadTaskList = new List<DownloadTask>(schedule.Length);

								for (int i = 0; i < schedule.Length; i++) {
									DownloadTask downloadTask = new DownloadTask(fdi, schedule[i], i, manager, action);
									downloadTaskList.Add(downloadTask);
								}



								for (int i = 0; i < nodeThread.Length; i++) {
									nodeTasks[i] = new NodeTask(downloadTaskList);
									nodeThread[i] = new Thread(nodeTasks[i].Run);
									nodeThread[i].IsBackground = true;
								}

							}

							System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
							dlStartTime = DateTime.Now;
							sw.Start();
							for (int i = 0; i < nodeThread.Length; i++) {
								nodeThread[i].Start();
							}


							//終了チェック
							bool flag;
							while (true) {
								flag = true;
								for (int i = 0; i < nodeThread.Length; i++) {
									flag &= nodeTasks[i].EndCheck();

								}
								if (flag) {
									//登録
									//system.AddContent(new KeyValuePair<P2P.BBase.ContentInfoBase, P2P.BBase.Node>(fdi, system.MyNodeInfo));
									//action.RegMyContent();
									break;
								}

								Thread.Sleep(50);
							}


							//system.AddContent(new KeyValuePair<ContentInfoBase, List<Node>>(fdi, contentHolderList));
							//action.PutNodeContentList();

							var span = sw.Elapsed;

							string sn = string.Empty;
							foreach (var v in schedule) {
								sn += v.BandWidth.ToString() + " ";
							}

							//PeerLogger.Instance.OnMassage("complete ダウンロード時間 " + span.ToString());
							//PeerLogger.Instance.OnMassage("complete ダウンロードnode " + sn);

							fdi2 = fdi;
							RemoveDownload(fdi2);
							

							string s = string.Empty;

							foreach (var sc in schedule) {
								s += sc.BandWidth.ToString() + " ";
							}
							PeerLogger.Instance.OnMassage(s);

							goto EXIT2;
						}

					}
				EXIT2:
					DateTime dlEndTime = DateTime.Now;


					TimeSpan ts = dlEndTime - dlStartTime;

					PeerLogger.Instance.OnMassage("ダウンロード開始時間 " + dlStartTime.ToString());
					PeerLogger.Instance.OnMassage("ダウンロード時間 " + ts.ToString());
					PeerLogger.Instance.OnMassage("ダウンロード終了時間 " + dlEndTime.ToString());

				}
				else {
					Thread.Sleep(20);
				}
			}
		}



		public class NodeTask {
			List<DownloadTask> tasks = null;
			Thread[] threads = null;
			public NodeTask(List<DownloadTask> tasks) {
				this.tasks = tasks;
				threads = new Thread[tasks.Count];
			}

			public void Run() {

				for (int i = 0; i < tasks.Count; i++) {
					tasks[i].Download();

				}

				/*
				foreach (var task in tasks) {
					while (true) {
						for (int i = 0; i < threads.Length; i++) {
							if (threads[i] == null || threads[i].IsAlive == false) {
								threads[i] = new Thread(task.Download);

								threads[i].Start();

								goto EXIT;
							}

						}
					}
				EXIT: ;

				}
				 * */
			}

			public bool EndCheck() {
				bool flag = true;
				for (int i = 0; i < tasks.Count; i++) {
					flag &= tasks[i].TaskEnd;

				}

				return flag;
			}
		}






		public PeerDownloader(PeerSystem system, PeerAction action) {
			this.system = system;
			this.action = action;
			downloadFileList = new LinkedList<DownloadInfo>();
		}


		public void AddDownload(ContentInfoBase cib) {

			downloadFileList.AddLast(new DownloadInfo(cib));
			manager.AddFileData(cib);

		}


		public void AddDownload(ContentInfoBase cib, int limitSegmentNum) {

			MulticastDownloadOption mdo = new MulticastDownloadOption(limitSegmentNum);

			downloadFileList.AddLast(new DownloadInfo(cib, mdo));
			manager.AddFileData(cib);

		}

		private void Download() {
			foreach (var di in downloadFileList) {
				var fdi = manager.GetFileData(di.ContentInfo.BaseHash);

				List<Node> contentHolderList;
				bool b = system.ContentDictionary.TryGetValue(di.ContentInfo, out contentHolderList);


				if (b) {
					foreach (var contentHolder in contentHolderList) {
						//DownloadTask(fdi, contentHolder);
						CheckComplete(fdi);
					}

				}
			}

		}


		public void CheckComplete(FileDataInfo fdi) {
			FileDataState fsd = fdi.GetState();

			if (fsd == FileDataState.Complete) {
				RemoveDownload(fdi);
				system.MyContentList.Add(new ContentInfoBase(fdi));
			}

		}

		public void RemoveDownload(FileDataInfo fdi) {
			RemoveDownload(new ContentInfoBase(fdi));
		}

		public void RemoveDownload(ContentInfoBase cib) {
			DownloadInfo di = null;
			foreach (var c in downloadFileList) {
				if (c.ContentInfo.BaseHash.Equals(cib.BaseHash)) {
					di = c;
					break;
				}

			}
			downloadFileList.Remove(di);
		}




		private class DownloadStrategy {
			static public int? SequentialDownload(IEnumerator<int> segEnu) {
				if (segEnu != null) {
					segEnu.MoveNext();
					int i = segEnu.Current;

					return i;
				}
				return null;

			}

			static public int? RandomDownload(IEnumerable<int> segEnu) {
				if (segEnu != null) {
					int[] a = new List<int>(segEnu).ToArray();
					Random rand = new Random();

					return a[rand.Next(a.Length)];
				}
				return null;

			}

			static public int? ZipDownload(IEnumerable<int> segEnu) {
				if (segEnu != null) {
					int[] a = new List<int>(segEnu).ToArray();


				}
				return null;
			}

		}

	}
}