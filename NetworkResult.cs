using System;
using System.Collections.Generic;

namespace P2P
{
    public class NetworkResultList {
		private LinkedList<NetworkResult> resultList = null;
		private int listMax = 1000;

		public NetworkResultList() {
            resultList = new LinkedList<NetworkResult>();
		}

		public NetworkResultList(int limit) {
            resultList = new LinkedList<NetworkResult>();
            listMax = limit;
		}

		/// <summary>
		/// 追加
		/// </summary>
		/// <param name="nr"></param>
		public void Enqueue(NetworkResult nr) {
			ListCutOff();
			resultList.AddLast(nr);
		}

		/// <summary>
		/// 削除
		/// </summary>
		/// <returns></returns>
		public NetworkResult Dequeue() {
			ListCutOff();
			NetworkResult nr = this.resultList.First.Value;
			resultList.RemoveFirst();
			return nr;
		}

		public NetworkResult FindResult(Predicate<NetworkResult> pred) {
			ListCutOff();
			return LinkedListExtension.FindAndRemove(resultList,pred);
		}

		private void ListCutOff() {
			if (resultList.Count > listMax) {
				while (resultList.Count > listMax) {
					resultList.RemoveFirst();
				}
			}
		}

		static public NetworkResultList Instance = new NetworkResultList();

	}

	public class NetworkResult {
		public enum ResultType {
			Success,
			TimeOut,
			NotFound,
			Diffuse,
			Unknown
		}

		private ResultType result = ResultType.Unknown;

		private BBase.Node node = null;

		public NetworkResult(BBase.Node node, ResultType result) {
			this.node = node;
			this.result = result;
		}

	}
}
