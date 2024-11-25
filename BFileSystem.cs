using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using P2P.BBase;


namespace P2P
{
    namespace BFileSystem
    {

        #region YaneuraoGameSDK.NETから　Thanks やね氏
        #region ライセンス
        /*
		Ｑ2．ライセンスはどうなっていますか？
		Ａ2．ライセンスについては、GPLおよびLGPL(SDLがそういうライセンスだから)を採用します。
		しかし、原則的にYanesdk.NETのコード自体は煮るなり焼くなりしていただいて構いません。
		つまり、Yanesdk.NET開発チームに断ることなく、Yanesdk.NETのソースコードを部分的ないし、まるごと商用ソフトに利用してかまいません。
		もちろん、同人ソフトや教育目的で使ってもらうのも大歓迎です。
		 * */
        #endregion

        /// <summary>
        /// CurrentDirectoryHelperで使う定数
        /// </summary>
        public enum CurrentDirectoryEnum {
			StartupDirectory, // 起動時のworking directory
			WorkingDirectory, // 現在のcurrent directory
			ExecuteDirectory, // 実行ファイルの存在するdirectory相対
		}

		/// <summary>
		/// カレントフォルダを一時的に変更するためのヘルパクラス
		/// </summary>
		public class CurrentDirectoryHelper : IDisposable {
			/// <summary>
			/// 
			/// </summary>
			/// <param name="e">カレントフォルダの設定</param>
			public CurrentDirectoryHelper(CurrentDirectoryEnum e) {
				switch (e) {
					case CurrentDirectoryEnum.ExecuteDirectory:
						SetCurrentDir(global::System.AppDomain.CurrentDomain.BaseDirectory);
						break;
					case CurrentDirectoryEnum.StartupDirectory:
						SetCurrentDir(startUpDir);
						break;
					case CurrentDirectoryEnum.WorkingDirectory:
						// do nothing
						break;

					default:
						throw null; // ありえない
				}
			}

			/// <summary>
			/// カレントフォルダの設定
			/// </summary>
			/// <param name="path"></param>
			private void SetCurrentDir(string path) {
				try {
					if (path != null) {
						currentDir = Environment.CurrentDirectory;
						Environment.CurrentDirectory = path;
					}
				}
				catch // CurrentDirectoryの変更に失敗しうる。
				{
					currentDir = null;
				}
			}

			/// <summary>
			/// 変更前のカレントフォルダ
			/// </summary>
			private string currentDir = null;

			/// <summary>
			/// 起動時のフォルダ
			/// </summary>
			private static string startUpDir = Environment.CurrentDirectory;

			/// <summary>
			/// 変更していたカレントフォルダを元に戻す
			/// </summary>
			public void Dispose() {
				if (currentDir != null) {
					try {
						Environment.CurrentDirectory = currentDir;
					}
					catch { }
				}
			}

		}

		/// <summary>
		/// fileを扱うときに使うと良い。
		/// </summary>
		/// <remarks>
		///	ファイルシステムクラス。
		///
		///	path関連は、multi-threadを考慮していない。
		///	(つまりsetPathしているときに他のスレッドがgetPathするようなことは
		///	想定していない)
		///
		///	pathはたいてい起動時に一度設定するだけなので、これでいいと思われる。
		///
		///	使用例)
		///	FileSys.addPath("..");
		///	FileSys.addPath("sdl\src\");
		///	FileSys.addPath("yanesdk4d/");
		///	char[] name = FileSys.makeFullName("y4d.d");
		///
		///	./y4d.d(これはディフォルト) と ../y4d.d と sdl/src/y4d.d と
		///	yanesdk4d/y4d.d を検索して、存在するファイルの名前が返る。
		///
		///	また、アーカイバとして、 FileArchiverBase 派生クラスを
		///	addArchiver で指定できる。その Archiver も read/readRWのときに
		///	必要ならば自動的に呼び出される。
		/// </remarks>
		public class FileSys {
			#region パス関連
			/// <summary>
			/// コンストラクタ。
			/// </summary>
			/// <remarks>
			/// コンストラクタでは、addPath("./");で、カレントフォルダを
			/// 検索対象としている。
			/// </remarks>
			static FileSys() {
				AddPath("");
			}

			/// <summary>
			/// フォルダ名とファイル名をくっつける。
			/// </summary>
			/// <remarks>
			///	1. / は \ に置き換える
			///	2.左辺の終端の \ は自動補間する
			///	"a"と"b"をつっつければ、"a\b"が返ります。
			///	3.駆け上りpathをサポートする
			///	"dmd/bin/"と"../src/a.d"をつくっければ、"dmd\src\a.d"が返ります。
			///	4.カレントpathをサポートする
			///	"./bin/src"と"./a.d"をくっつければ、"bin\src\a.d"が返ります
			///	"../src/a.d"と"../src.d"をつっつければ"..\src\src\src.d"が返ります
			///	5.途中のかけあがりもサポート
			///	"./src/.././bin/"と"a.c"をつっつければ、"bin\a.c"が返ります。
			///	6.左辺が ".."で始まるならば、それは除去されない
			///	"../src/bin"と"../test.c"をつっつければ、"../src/test.c"が返ります。
			/// 7.network driveを考慮
			/// "\\my_network\test"と"../test.c"をつっつければ"\\my_network\test.c"が返る。
			/// "\\my_network\"と"../test.c"は"\\my_network\..\test.c"が返る
			/// (\\の直後の名前はnetwork名なのでvolume letter扱いをしなくてはならない)
			/// </remarks>
			///	<code>
			/// // 動作検証用コード
			/// string name;
			///	name = FileSys.concatPath("", "b");  // b
			///	name = FileSys.concatPath("a", "b");  // a\b
			///	name = FileSys.concatPath("dmd/bin", "../src/a.d"); // dmd\src\a.d
			///	name = FileSys.concatPath("dmd\\bin", "../src\\a.d"); // dmd\src\a.d
			///	name = FileSys.concatPath("../src/a.d", "..\\src\\src.d"); // ..\src\src\src.d
			///	name = FileSys.concatPath("../src/bin", "../test.c"); // ../src/test.c
			///	name = FileSys.concatPath("./src/.././bin", "a.c"); // bin\a.c
			///	name = FileSys.concatPath("../../test2/../test3", "test.d"); // ..\..\test3\test.d
			/// name = FileSys.concatPath("\\\\my_network\\test", "../test.c"); // bin\a.c
			///	name = FileSys.concatPath("\\\\my_network\\", "../test.d"); // ..\..\test3\test.d
			/// </code>
			/// <param name="a"></param>
			/// <param name="b"></param>
			/// <returns></returns>
			public static string ConcatPath(string path1, string path2) {
				//	bが絶対pathならば、そのまま返す。
				if (IsAbsPath(path2))
					return ConcatPathHelper("", path2);
				string s = ConcatPathHelper("", path1);
				//	終端が \ でないなら \ を入れておく。
				if (s.Length != 0 && !s.EndsWith("\\") && !s.EndsWith("/"))
					s += Path.DirectorySeparatorChar;
				return ConcatPathHelper(s, path2);
			}

			/// <summary>
			/// 絶対pathかどうかを判定する
			/// </summary>
			/// <param name="path"></param>
			/// <returns></returns>
			private static bool IsAbsPath(string path) {
				if (path == null || path.Length == 0)
					return false;
				if (path[0] == '\\' || path[0] == '/')
					return true;
				return path.Contains(Path.VolumeSeparatorChar.ToString());
				// volume separator(':')があるということは、
				//	絶対pathと考えて良いだろう
			}

			/// <summary>
			/// path名を連結するときのヘルパ。駆け上がりpath等の処理を行なう。
			/// </summary>
			/// <param name="a"></param>
			/// <param name="b"></param>
			/// <returns></returns>
			private static string ConcatPathHelper(string path1, string path2) {
				if (path2 == null || path2.Length == 0)
					return path1; // aが空なのでこのまま帰る

				StringBuilder res = new StringBuilder(path1);
				for (int i = 0; i < path2.Length; ++i) {
					char c = path2[i];
					bool bSep = false;
					if (c == '/' || c == '\\') {
						bSep = true;
					}
					else if (c == '.') {
						//	次の文字をスキャンする
						char n = (i + 1 < path2.Length) ? path2[i + 1] : '\0';
						//	./ か .\
						if (n == '/' || n == '\\') { i++; continue; }
						//	../ か ..\
						if (n == '.') {
							char n2 = (i + 2 < path2.Length) ? path2[i + 2] : '\0';
							if (n2 == '/' || n2 == '\\') {
								string nn = GetDirName(res.ToString());
								if (nn == res.ToString()) {
									//	".."分の除去失敗。
									//	rootフォルダかのチェックを
									//	したほうがいいのだろうか..
									res.Append("..");
									res.Append(Path.DirectorySeparatorChar);
								}
								else {
									res = new StringBuilder(nn);
								}
								i += 2;
								continue;
							}
						}
					}
					if (bSep) {
						res.Append(Path.DirectorySeparatorChar);
					}
					else {
						res.Append(c);
					}
				}
				return res.ToString();
			}


			/// <summary>
			/// ファイル名に、setPathで指定されているpathを結合する(存在チェック付き)
			/// </summary>
			/// <remarks>
			///	ただし、ファイルが存在していなければ、
			///	pathは、設定されているものを先頭から順番に調べる。
			///	ファイルが見つからない場合は、元のファイル名をそのまま返す
			///	fullname := setPathされているpath + localpath + filename;
			/// </remarks>
			/// <see cref=""/>
			/// <param name="localpath"></param>
			/// <param name="filename"></param>
			/// <returns></returns>
			public static string MakeFullName(string localpath, string filename) {
				return MakeFullName(ConcatPath(localpath, filename));
			}

			/// <summary>
			/// ファイル名に、setPathで指定されているpathを結合する(存在チェック付き)
			/// </summary>
			/// <remarks>
			/// 
			///	ただし、ファイルが存在していなければ、
			///	pathは、設定されているものを先頭から順番に調べる。
			///	ファイルが見つからない場合は、元のファイル名をそのまま返す
			///
			///	fullname := setPathされているpath + filename;
			/// </remarks>
			/// <param name="filename"></param>
			/// <returns></returns>
			public static string MakeFullName(string filename) {
				foreach (string path in pathlist) {
					string fullname = ConcatPath(path, filename);
					//	くっつけて、これが実在するファイルか調べる
					if (IsExist(fullname))
						return fullname;
				}

				/*
				//	アーカイバを用いて調べてみる
				foreach (FileArchiverBase arc in archiver) {
					foreach (string path in pathlist) {
						string fullname = ConcatPath(path, filename);
						if (arc.IsExist(fullname)) {
							return fullname;
						}
					}
				}
				 * */
				return filename;	//	not found..
			}

			/// <summary>
			/// FileSysが読み込むときのディレクトリポリシー。
			/// 
			/// あるファイルを読み込むとき、
			///		・実行ファイル相対で読み込む(default)
			///		・起動時のworking directory相対で読み込む
			///		・現在のworking directory相対で読み込む
			/// の3種から選択できる。
			/// </summary>
			public static CurrentDirectoryEnum DirectoryPolicy = CurrentDirectoryEnum.ExecuteDirectory;

			/// <summary>
			///	ファイルの存在確認
			/// </summary>
			/// <remarks>
			///	指定したファイル名のファイルが実在するかどうかを調べる
			///	setPathで指定されているpathは考慮に入れない。
			/// </remarks>
			/// <param name="filename"></param>
			/// <returns></returns>
			public static bool IsExist(string filename) {
				using (CurrentDirectoryHelper helper = new CurrentDirectoryHelper(FileSys.DirectoryPolicy))
					return File.Exists(filename);
			}

			/// <summary>
			/// ファイルの生存確認(pathも込み)
			/// </summary>
			/// <remarks>
			///	setPathで指定したpathも含めてファイルを探す。
			///	アーカイブ内のファイルは含まない。
			///	あくまでファイルが実在するときのみそのファイル名が返る。
			/// </remarks>
			/// <param name="filename"></param>
			/// <returns>ファイルが見つからない場合はnull。</returns>
			public static string IsRealExist(string filename) {
				foreach (string path in pathlist) {
					string fullname = ConcatPath(path, filename);
					//	くっつけて、これが実在するファイルか調べる
					if (IsExist(fullname))
						return fullname;
				}
				return null;
			}

			/// <summary>
			/// pathを取得。
			/// </summary>
			/// <returns></returns>
			public static List<string> PathList { get { return pathlist; } }


			/// <summary>
			/// pathを設定。ここで設定したものは、makeFullPathのときに使われる
			/// </summary>
			/// <remarks>
			///	設定するpathの終端は、\ および / でなくとも構わない。
			///	(\ および / で あっても良い)
			/// 
			/// ディフォルトでは、""のみが設定されている。
			/// </remarks>
			/// <param name="pathlist_"></param>
			public static void SetPath(string[] pathlist_) {
				pathlist.Clear();
				pathlist.AddRange(pathlist_);
			}

			/// <summary>
			/// pathを追加。ここで設定したpathを追加する。
			/// </summary>
			/// <remarks>
			/// 
			///	設定するpathの終端は、\ および / でなくとも構わない。
			///	(\ および / で あっても良い)
			///	ただし、"."や".."などを指定するときは、"\" か "/"を
			///	付与してないといけない。(こんなのを指定しないで欲しいが)
			///
			///	ディフォルトでは""のみがaddPathで追加されている。
			///	(カレントフォルダを検索するため)
			/// </remarks>
			/// <param name="path"></param>
			public static void AddPath(string path) { pathlist.Add(path); }

			/// <summary>
			/// pathのリスト
			/// </summary>
			private static List<string> pathlist = new List<string>();

			/// <summary>
			/// 親フォルダ名を返す。
			/// </summary>
			/// <param name="fname"></param>
			/// <returns></returns>
			/// <remarks>
			/// ディレクトリ名の取得。
			/// 例えば、 "d:\path\foo.bat" なら "d:\path\" を返します。
			///		"d:\path"や"d:\path\"ならば、"d:\"を返します。
			///	※　windows環境においては、'/' は使えないことに注意。
			///		(Path.DirectorySeparatorChar=='\\'であるため)
			///	※　終端は必ず'\'のついている状態になる
			/// 　　ただし、返し値の文字列が空になる場合は除く。
			/// 　　つまり、getDirName("src\")==""である。
			///	※　終端が ".." , "..\" , "../"ならば、これは駆け上がり不可。
			///		(".."の場合も終端は必ず'\'のついている状態になる)
			/// ※  network driveを考慮して、"\\network computer名\"までは、
			/// 　ドライブレター扱い。
			/// </remarks>
			public static string GetDirName(string path) {
				//	コピーしておく
				string path2 = path;

				//	終端が'\'か'/'ならば1文字削る
				int l = path2 == null ? 0 : path2.Length;
				if (l != 0) {
					bool bNetwork = (path2.Length >= 2) && (path2.Substring(0, 2) == "\\\\");

					int vpos = -1;
					// volume separator(networkフォルダの場合、
					// computer名の直後の\マーク)のあるpos
					if (bNetwork) {
						for (int i = 2; i < path2.Length; ++i) {
							char c = path2[i];
							if (c == '\\' || c == '/') {
								vpos = i;
								break;
							}
						}
					}

					if ((path2.EndsWith("\\") || path2.EndsWith("/")) && vpos != path2.Length - 1)
						path2 = path2.Remove(--l);

					//	もし終端が".."ならば、これは駆け上がり不可。
					if (path2.EndsWith("..")) {
						//	かけ上がれねーよヽ(`Д´)ノ
					}
					else {
						// fullname = Path.GetDirectoryName(fullname);
						// 動作が期待するものではないので自前で書く。
						for (int pos = path2.Length - 1; ; --pos) {
							char c = path2[pos];
							if (c == Path.VolumeSeparatorChar || pos == vpos) {
								// c: なら c:
								// c:/abc なら c: に。
								// c:abc なら c:に。
								if (pos != path2.Length - 1)
									path2 = path2.Remove(pos + 1);
								break;
							}
							if (c == '\\' || c == '/') {
								// そこ以下を除去
								path2 = path2.Remove(pos);
								break;
							}
							if (pos == 0) { // 単体フォルダ名なので空の文字を返すのが正しい
								path2 = "";
								break;
							}
						}
					}
					if (!path2.EndsWith("\\") && !path2.EndsWith("/") && path2.Length != 0) { path2 += Path.DirectorySeparatorChar; }
				}
				return path2;
			}

			/// <summary>
			/// 正味のファイル名だけを返す(フォルダ名部分を除去する)
			/// </summary>
			/// <param name="filename"></param>
			/// <returns></returns>
			public static string GetPureFileName(string filename) {
				if (filename == null)
					return "";
				return Path.GetFileName(filename);
			}

			#endregion

			/// <summary>
			/// Streamをランダムアクセスするためのwrapper
			/// </summary>
			/// <remarks>
			/// Zipファイルを扱うときに用いる。
			/// little endianだと仮定している。(zip headerがそうなので)
			/// </remarks>
			public class StreamRandAccessor {

				/// <summary>
				/// openしたストリームを渡すナリよ！
				/// </summary>
				/// <param name="f_"></param>
				public void SetStream(Stream f_) {
					stream_length = f_.Length;
					f = f_;
					// 読み込んでいないので。
					pos = 0;
					readsize = 0;
					bWrite = false;
				}

				/// <summary>
				/// ストリームの先頭からiのoffsetの位置のデータbyteを読み込む
				/// </summary>
				/// <param name="i"></param>
				/// <returns></returns>
				public byte GetByte(long i) {
					check(ref i, 1);
					return data[i];
				}

				/// <summary>
				/// ストリームの先頭からiのoffsetの位置に、データbyteを書き込み
				/// </summary>
				/// <param name="i"></param>
				/// <param name="b"></param>
				public void PushByte(long i, byte b) {
					check(ref i, 1);
					data[i] = b;
					bWrite = true;
				}


				/// <summary>
				///	ストリームの先頭からiのoffsetの位置のデータushortを読み込む
				/// </summary>
				/// <remarks>
				/// little endian固定。
				/// </remarks>
				/// <param name="i"></param>
				/// <returns></returns>
				public ushort GetUshort(long i) {
					check(ref i, 2);

					/*
					#if BigEndian
								// BigEndianのコードは、すべて未検証
								// Zipファイルからの読み込みにしか使わない。
								// Zipファイル内の値はlittle endianなので
								// この #if BigEndianのコードが必要になることはない。

								byte b0 = data[i];
								byte b1 = data[i + 1];
								return (b0 << 8) | b1;
					#else
					 */
					//	return *(ushort*)&data[i];
					// return BitConverter.ToUInt16(data , ( int ) i);

					// ↑BitConverterはLE/BEが環境依存。

					byte b0 = data[i];
					byte b1 = data[i + 1];
					return (ushort)((b1 << 8) | b0);

					//#endif
				}

				/// <summary>
				///	ストリームの先頭からiのoffsetの位置のデータuintを読み込む
				/// </summary>
				/// <remarks>
				/// little endian固定。
				/// </remarks>
				/// <param name="i"></param>
				/// <returns></returns>
				public uint GetUint(long i) {
					check(ref i, 4);

					//#if BigEndian 
					//			return bswap(*(uint *)&data[i]);
					//#else
					//			return BitConverter.ToUInt32(data , ( int ) i);
					//			return *(uint*)&data[i];

					// ↑BitConverterはLE/BEが環境依存。

					byte b0 = data[i];
					byte b1 = data[i + 1];
					byte b2 = data[i + 2];
					byte b3 = data[i + 3];
					return (uint)((b3 << 24) | (b2 << 16) | (b1 << 8) | b0);

					//#endif
				}

				/// <summary>
				///	ストリームの先頭からiのoffsetの位置のデータushortを書き込む
				/// </summary>
				/// <remarks>
				/// little endian固定。
				/// </remarks>
				/// <param name="i"></param>
				/// <returns></returns>
				public void PutUshort(long i, ushort us) {
					check(ref i, 2);

					//#if BigEndian 
					//			data[0] = (byte)us;
					//			data[1] = (byte)(us >> 8);
					//#else
					//			*(ushort*)&data[i] = us;
					data[i + 0] = (byte)((us) & 0xff);
					data[i + 1] = (byte)((us >> 8));
					//#endif
					bWrite = true;
				}

				/// <summary>
				///	ストリームの先頭からiのoffsetの位置のデータuintを書き込む
				/// </summary>
				/// <remarks>
				/// little endian固定。
				/// </remarks>
				/// <param name="i"></param>
				/// <returns></returns>
				public void PutUint(long i, uint ui) {
					check(ref i, 4);

					//#if BigEndian 
					//			ui = bswap(ui);
					//#else
					//			*(uint *)&data[i] = ui;
					data[i + 0] = (byte)((ui) & 0xff);
					data[i + 1] = (byte)((ui >> 8) & 0xff);
					data[i + 2] = (byte)((ui >> 16) & 0xff);
					data[i + 3] = (byte)((ui >> 24));
					//#endif
					bWrite = true;
				}

				/// <summary>
				/// 書き込みを行なう
				/// </summary>
				/// <remarks>
				///	pushUint等でデータに対して書き込みを行なったものを
				///	ストリームに戻す。このメソッドを明示的に呼び出さなくとも
				///	256バイトのストリーム読み込み用バッファが内部的に用意されており、
				///	現在読み込んでいるストリームバッファの外にアクセスしたときには
				///	自動的に書き込まれる。
				/// </remarks>
				public void Flush() {
					if (bWrite) {
						//	writebackしなくては
						f.Write(data, 0, readsize);
						bWrite = false;
					}
				}

				/// <summary>
				///	posからsize分読み込む(バッファリング等はしない)
				/// </summary>
				/// <returns>読み込まれたサイズを返す</returns>
				public long Read(byte[] data, long pos, uint size) {
					Flush();
					//	ファイルのシークを行なう前にはflushさせておかないと、
					//	あとで戻ってきて書き込むとシーク時間がもったいない

					f.Seek(pos, SeekOrigin.Begin);
					int s;
					try { s = f.Read(data, 0, (int)size); }
					catch { s = 0; }
					// これでは2GBまでしか扱われへんやん..しょぼん..
					return (long)s;
				}

				/// <summary>
				/// setStreamも兼ねるコンストラクタ
				/// </summary>
				/// <param name="f"></param>

				public StreamRandAccessor(Stream f) { SetStream(f); }
				public StreamRandAccessor() { }

				public void Dispose() { Flush(); }

				private Stream f;

				private long stream_length;
				private byte[] data = new byte[256]; // 読み込みバッファ
				private int readsize;	 // バッファに読み込めたサイズ。
				private long pos;		 // 現在読み込んでいるバッファのストリーム上の位置
				private bool bWrite;	 // このバッファに対して書き込みを行なったか？
				//	書き込みを行なったら、他のバッファを読み込んだときに
				//	この分をwritebackする必要がある

				/// <summary>
				///	このストリームのiの位置にsizeバイトのアクセスをしたいのだが、
				///	バッファに読まれているかチェックして読み込まれていなければ読み込む。
				///	渡されたiは、data[i]で目的のところにアクセスできるように調整される。
				/// </summary>
				/// <param name="i"></param>
				/// <param name="size"></param>
				private void check(ref long i, uint size) {
					if (i < pos || pos + readsize < i + size) {
						//	バッファ外でんがな
						Flush();
						// size<128と仮定して良い
						//	アクセスする場所がbufferの中央に来るように調整。
						int offset = (int)((data.Length - size) / 2);
						if (i < offset) {
							pos = 0;
						}
						else {
							pos = i - offset;
						}
						f.Seek(pos, SeekOrigin.Begin);
						readsize = f.Read(data, 0, data.Length);
					}
					i -= pos;
				}

				/// <summary>
				/// ストリーム長を返す
				/// </summary>
				public long Length {
					get {
						if (f == null)
							return 0;
						return stream_length;
					}
				}
			}
		}

		#endregion

		//071216
		//後つくるもの，FileInfo,BlockStateとかをまとめるFileSystemクラスをつくりゃー

		/*
		 * ListとLinkedListのパフォーマンス
		 * 線形アクセスなどでは10倍くらいちがう．
		 * Listが超はやいならLinkedListは早いってかんじでどっちも早いけどListの方がとっても早い
		 * 要素の削除は10倍以上LinkedListが早い
		 * LinkedListは早いけListは遅いという感覚
		 * Listは削除とか途中に挿入とか以外はとても優秀
		 * LinkedListは全般的に優秀
		 * */


		/*
		 *ファイルシステムの考え方メモ
		 * ファイルとして状態をもつのは自分のPCの中にファイルとして作成するもののみ
		 * Downloadキューに入れられたものとか、自分がファイルとして持っているものとか
		 * 
		 * 
         */


		public class SegmentIO {
			/// <summary>
			/// ファイルセグメントをファイルに書き込む
			/// 書き込むファイルはFileInfoで指定する
			/// </summary>
			/// <param name="fs"></param>
			/// <param name="fi"></param>
			static public void WriteFileSegment(string folderPath, DataSegment seg, FileDataInfo fi, IFileOpen fo) {
				string filePath = Path.Combine(folderPath, fi.FileName);
				FileStream fs = fo.Get(filePath);

				if (seg.Data == null) return;
				if (seg.Data.Length == 1 && seg.Data[0] == 0) return;
				lock (fs) {
					int writePosition = seg.SegmentNumber * FileDividePolicy.FileDivideSize;
					//ExtendFileSize(fs, writePosition + FileDividePolicy.FileDivideSize);
					ExtendFileSize(fs, writePosition);
					fs.Seek(writePosition, SeekOrigin.Begin);
					fs.Write(seg.Data, 0, seg.Data.Length);
					fs.Flush();
					fi.PutBlock(seg.SegmentNumber);
				}
			}


			/// <summary>
			/// 読めなければnullが返るから気をつけてね
			/// </summary>
			/// <param name="segnum"></param>
			/// <param name="fi"></param>
			/// <param name="fo"></param>
			/// <returns></returns>
			static public DataSegment ReadFileSegment(string folderPath,int segnum,FileDataInfo fi, IFileOpen fo) {
				bool exist = fi.CheckBlock(segnum);

				
				if (exist) {
					string filePath = Path.Combine(folderPath, fi.FileName);
					//このブロックが存在するということはfsの大きさもそれだけあると仮定できるからその例外は対処しない
					FileStream fs = fo.Get(filePath);
					lock (fs) {
						
						int readPosition = segnum * FileDividePolicy.FileDivideSize;
						fs.Seek(readPosition, SeekOrigin.Begin);
						int readLength = (readPosition + FileDividePolicy.FileDivideSize) > (int)(fs.Length) ?
							(int)fs.Length - readPosition  :  FileDividePolicy.FileDivideSize;

						byte[] buffer = new byte[readLength];
						fs.Read(buffer, 0, buffer.Length);
						DataSegment fseg = new DataSegment(fi.BaseHash.ByteData, segnum, buffer);
						return fseg;
					}
				}
				//なければ読めません
				return null;
			}

			/// <summary>
			/// Streamをsizeまでかくちょうする
			/// しかし普通はFileStream専用になるんじゃない
			/// そもそもprivateメソッドです
			/// 本当はint sizeでなくてlong sizeだけどキャストが面倒なので
			/// </summary>
			/// <param name="fs"></param>
			/// <param name="size"></param>
			static private void ExtendFileSize(Stream fs, int size) {
				//拡張する必要性を問う
				if (fs.Length > size) {
					return;
				}
				else {
					fs.SetLength((long)size);
				}
			}
		}

		public class NSManager : IDisposable {
			#region IDisposable メンバ

			public void Dispose() {
				throw new Exception("The method or operation is not implemented.");
			}

			#endregion

		}

		/// <summary>
		/// ファイルをオープンするインタフェース
		/// </summary>
		public interface IFileOpen {
			FileStream Get(string path);
		}

		/// <summary>
		/// ファイルの作成方法を指定するインタフェース
		/// </summary>
		public interface FSFactory {
			FileStream Create(string s);
		}

		/// <summary>
		/// とりあえずこれが今回は必要だからこれだけ実装
		/// </summary>
		public class RWShareOpenCreateFSFactory : FSFactory {
			#region FSFactory メンバ
			public FileStream Create(string s) {
				return new FileStream(s, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
			}
			#endregion

		}

		/// <summary>
		/// ごく普通にオープン．クローズとかは勝手にやってね
		/// 今回は使わないぽいけど実装しとかないと役割を忘れるかもしれないので
		/// </summary>
		public class SimpleOpen : IFileOpen {
			public delegate FileStream FileOpenMethod(string s);

			private FileOpenMethod fileOpen = null;
			SimpleOpen(FileOpenMethod fileOpen) {
				this.fileOpen = fileOpen;
			}
			public FileStream Get(string path) {
				return fileOpen(path);
			}
		}


		/// <summary>
		/// ファイルプールを使ってオープン
		/// 自分の持ってるファイル、ダウンロード中のファイルすべてをプールする
		/// ちょっと普通ではやらないことだけど
		/// ストリーム再生でメディアプレイヤーがOpen→BrossomがOpenという流れがブロックされるので
		/// つねにBrossomがOpen→メディアプレイヤーがOpenという流れにするために
		/// 常にBrossomがファイルハンドルをはなさないということにする
		/// FileStreamの状態は保証されないので毎回Seekとかするように
		/// </summary>
		public class FileStreamPool : IFileOpen,IDisposable {
			private Dictionary<string, FileStream> fsDic = null;
			public delegate FileStream FileOpenMethod(string s);
			private FileOpenMethod fileOpen = null;

			public FileStreamPool(FileOpenMethod openMethod) {
				fileOpen = openMethod;
				fsDic = new Dictionary<string, FileStream>();
			}


			/// <summary>
			/// GetはGet(string,FSFactory)という風にした方がいいかもね（メモ)
			/// </summary>
			/// <param name="path"></param>
			/// <returns></returns>
			public FileStream Get(string path) {
				FileStream fs;

				bool exist = fsDic.TryGetValue(path, out fs);

				if (exist) {
					return fs;
				}
				else {
					fs = fileOpen(path);
					fsDic.Add(path, fs);
					return fs;
				}
			}

			/// <summary>
			/// たぶん使わないよ
			/// </summary>
			/// <param name="path"></param>
			public void Close(string path){
				FileStream fs;

				bool exist = fsDic.TryGetValue(path,out fs);

				if(exist){
					fs.Close();
					fsDic.Remove(path);
				}
			}

			#region IDisposable メンバ

			public void Dispose() {
				foreach (FileStream fs in fsDic.Values) {
					fs.Close();
				}
				fsDic.Clear();
			}

			#endregion
		}


		//メモ
		//いろいろ融通をきかせてファイルストリームとHttpレスポンスのストリームの両方に，どちらか選択して書き込める様にする．
		//フックをかける

		/// <summary>
		/// FileInfoとFileStateをいじくるアルゴリズムはここにかく
		/// </summary>
		public class FileStateManager {

			/// <summary>
			/// successListをfiに書き込む
			/// </summary>
			/// <param name="fi"></param>
			/// <param name="fs"></param>
			public static void WriteFileState(FileDataInfo fi, BlockState fs) {
				List<short> successList = fs.GetSuccessListandClear();
				foreach (short s in successList) {
					fi.PutBlock((int)s);
				}
			}
		}

		/// <summary>
		/// マルチスレッド対応
		/// </summary>
		public class BlockState {
			private LinkedList<short> reserveList = null;
			private List<short>       successList = null;
			private object            sync        = new object();

			public void ReserveSegment(int segnum) {
				lock (sync) {
					reserveList.AddLast((short)segnum);
				}
			}

			public bool CheckReserve(int segnum) {
				lock (sync) {
					return reserveList.Contains((short)segnum);
				}
			}

			public void ReportFail(int segnum) {
				lock (sync) {
					reserveList.Remove((short)segnum);
				}
			}

			public void ReportSuccess(int segnum) {
				lock (sync) {
					reserveList.Remove((short)segnum);
					successList.Add((short)segnum);
				}
			}

			/// <summary>
			/// Successと報告された番号のリストを内部で新しいインスタンスを作って返します．
			/// 内部のSuccessListはクリアーされるよ
			/// </summary>
			/// <returns></returns>
			public List<short> GetSuccessListandClear() {
				lock (sync) {
					List<short> retList = new List<short>(successList);
					successList.Clear();
					return retList;
				}
			}

			/// <summary>
			/// マルチスレッドなので内部オブジェクトを返すことはしません
			/// 新しいインスタンスにコピーです
			/// </summary>
			/// <returns></returns>
			public List<short> GetSuccessList() {
				lock (sync) {
					return new List<short>(successList);
				}
			}

			public void Clear() {
				lock (sync) {
					reserveList.Clear();
					ClearSuccessList();
				}
			}

			public void ClearSuccessList() {
				lock (sync) {
					successList.Clear();
				}
			}
		}
	}
}
