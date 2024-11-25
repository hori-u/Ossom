using System.Collections.Generic;
using System.Text;
using System.IO;

namespace P2P
{
    //いい名前が思いつかなかったぜ
    public class HTMLManager {
		StringBuilder buffer = null;

		private List<string> replaceList = new List<string>();
		public  List<string> ReplaceList {
			get { return replaceList; }
			set { replaceList = value; }
		}

		public HTMLManager(string path) {
			using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read)) {
				using (StreamReader sr = new StreamReader(fs, Encoding.UTF8)) {
					string tmp = sr.ReadToEnd();
					buffer = new StringBuilder(tmp);

				}
			}
		}

		public string ReplaceWord() {
			if (replaceList.Count > 0) {
				int i = 0;
				string word = "<!--" + i + "-->";
				StringBuilder sb = buffer.Replace(word, replaceList[i]);
				i++;
				while (replaceList.Count > i) {
					word = "<!--" + i + "-->";
					sb = sb.Replace(word, replaceList[i]);//効率がよくないけどコーディングは楽
					i++;
				}
				return sb.ToString();
			}
			else {
				return buffer.ToString();
			}
		}
	}
}
