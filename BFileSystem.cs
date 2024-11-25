using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using P2P.BBase;


namespace P2P
{
    namespace BFileSystem
    {

        #region YaneuraoGameSDK.NET����@Thanks ��ˎ�
        #region ���C�Z���X
        /*
		�p2�D���C�Z���X�͂ǂ��Ȃ��Ă��܂����H
		�`2�D���C�Z���X�ɂ��ẮAGPL�����LGPL(SDL�������������C�Z���X������)���̗p���܂��B
		�������A�����I��Yanesdk.NET�̃R�[�h���͎̂ς�Ȃ�Ă��Ȃ肵�Ă��������č\���܂���B
		�܂�AYanesdk.NET�J���`�[���ɒf�邱�ƂȂ��AYanesdk.NET�̃\�[�X�R�[�h�𕔕��I�Ȃ����A�܂邲�Ə��p�\�t�g�ɗ��p���Ă��܂��܂���B
		�������A���l�\�t�g�⋳��ړI�Ŏg���Ă��炤�̂��劽�}�ł��B
		 * */
        #endregion

        /// <summary>
        /// CurrentDirectoryHelper�Ŏg���萔
        /// </summary>
        public enum CurrentDirectoryEnum {
			StartupDirectory, // �N������working directory
			WorkingDirectory, // ���݂�current directory
			ExecuteDirectory, // ���s�t�@�C���̑��݂���directory����
		}

		/// <summary>
		/// �J�����g�t�H���_���ꎞ�I�ɕύX���邽�߂̃w���p�N���X
		/// </summary>
		public class CurrentDirectoryHelper : IDisposable {
			/// <summary>
			/// 
			/// </summary>
			/// <param name="e">�J�����g�t�H���_�̐ݒ�</param>
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
						throw null; // ���肦�Ȃ�
				}
			}

			/// <summary>
			/// �J�����g�t�H���_�̐ݒ�
			/// </summary>
			/// <param name="path"></param>
			private void SetCurrentDir(string path) {
				try {
					if (path != null) {
						currentDir = Environment.CurrentDirectory;
						Environment.CurrentDirectory = path;
					}
				}
				catch // CurrentDirectory�̕ύX�Ɏ��s������B
				{
					currentDir = null;
				}
			}

			/// <summary>
			/// �ύX�O�̃J�����g�t�H���_
			/// </summary>
			private string currentDir = null;

			/// <summary>
			/// �N�����̃t�H���_
			/// </summary>
			private static string startUpDir = Environment.CurrentDirectory;

			/// <summary>
			/// �ύX���Ă����J�����g�t�H���_�����ɖ߂�
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
		/// file�������Ƃ��Ɏg���Ɨǂ��B
		/// </summary>
		/// <remarks>
		///	�t�@�C���V�X�e���N���X�B
		///
		///	path�֘A�́Amulti-thread���l�����Ă��Ȃ��B
		///	(�܂�setPath���Ă���Ƃ��ɑ��̃X���b�h��getPath����悤�Ȃ��Ƃ�
		///	�z�肵�Ă��Ȃ�)
		///
		///	path�͂����Ă��N�����Ɉ�x�ݒ肷�邾���Ȃ̂ŁA����ł����Ǝv����B
		///
		///	�g�p��)
		///	FileSys.addPath("..");
		///	FileSys.addPath("sdl\src\");
		///	FileSys.addPath("yanesdk4d/");
		///	char[] name = FileSys.makeFullName("y4d.d");
		///
		///	./y4d.d(����̓f�B�t�H���g) �� ../y4d.d �� sdl/src/y4d.d ��
		///	yanesdk4d/y4d.d ���������āA���݂���t�@�C���̖��O���Ԃ�B
		///
		///	�܂��A�A�[�J�C�o�Ƃ��āA FileArchiverBase �h���N���X��
		///	addArchiver �Ŏw��ł���B���� Archiver �� read/readRW�̂Ƃ���
		///	�K�v�Ȃ�Ύ����I�ɌĂяo�����B
		/// </remarks>
		public class FileSys {
			#region �p�X�֘A
			/// <summary>
			/// �R���X�g���N�^�B
			/// </summary>
			/// <remarks>
			/// �R���X�g���N�^�ł́AaddPath("./");�ŁA�J�����g�t�H���_��
			/// �����ΏۂƂ��Ă���B
			/// </remarks>
			static FileSys() {
				AddPath("");
			}

			/// <summary>
			/// �t�H���_���ƃt�@�C��������������B
			/// </summary>
			/// <remarks>
			///	1. / �� \ �ɒu��������
			///	2.���ӂ̏I�[�� \ �͎�����Ԃ���
			///	"a"��"b"��������΁A"a\b"���Ԃ�܂��B
			///	3.�삯���path���T�|�[�g����
			///	"dmd/bin/"��"../src/a.d"����������΁A"dmd\src\a.d"���Ԃ�܂��B
			///	4.�J�����gpath���T�|�[�g����
			///	"./bin/src"��"./a.d"����������΁A"bin\src\a.d"���Ԃ�܂�
			///	"../src/a.d"��"../src.d"���������"..\src\src\src.d"���Ԃ�܂�
			///	5.�r���̂�����������T�|�[�g
			///	"./src/.././bin/"��"a.c"��������΁A"bin\a.c"���Ԃ�܂��B
			///	6.���ӂ� ".."�Ŏn�܂�Ȃ�΁A����͏�������Ȃ�
			///	"../src/bin"��"../test.c"��������΁A"../src/test.c"���Ԃ�܂��B
			/// 7.network drive���l��
			/// "\\my_network\test"��"../test.c"���������"\\my_network\test.c"���Ԃ�B
			/// "\\my_network\"��"../test.c"��"\\my_network\..\test.c"���Ԃ�
			/// (\\�̒���̖��O��network���Ȃ̂�volume letter���������Ȃ��Ă͂Ȃ�Ȃ�)
			/// </remarks>
			///	<code>
			/// // ���쌟�ؗp�R�[�h
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
				//	b�����path�Ȃ�΁A���̂܂ܕԂ��B
				if (IsAbsPath(path2))
					return ConcatPathHelper("", path2);
				string s = ConcatPathHelper("", path1);
				//	�I�[�� \ �łȂ��Ȃ� \ �����Ă����B
				if (s.Length != 0 && !s.EndsWith("\\") && !s.EndsWith("/"))
					s += Path.DirectorySeparatorChar;
				return ConcatPathHelper(s, path2);
			}

			/// <summary>
			/// ���path���ǂ����𔻒肷��
			/// </summary>
			/// <param name="path"></param>
			/// <returns></returns>
			private static bool IsAbsPath(string path) {
				if (path == null || path.Length == 0)
					return false;
				if (path[0] == '\\' || path[0] == '/')
					return true;
				return path.Contains(Path.VolumeSeparatorChar.ToString());
				// volume separator(':')������Ƃ������Ƃ́A
				//	���path�ƍl���ėǂ����낤
			}

			/// <summary>
			/// path����A������Ƃ��̃w���p�B�삯�オ��path���̏������s�Ȃ��B
			/// </summary>
			/// <param name="a"></param>
			/// <param name="b"></param>
			/// <returns></returns>
			private static string ConcatPathHelper(string path1, string path2) {
				if (path2 == null || path2.Length == 0)
					return path1; // a����Ȃ̂ł��̂܂܋A��

				StringBuilder res = new StringBuilder(path1);
				for (int i = 0; i < path2.Length; ++i) {
					char c = path2[i];
					bool bSep = false;
					if (c == '/' || c == '\\') {
						bSep = true;
					}
					else if (c == '.') {
						//	���̕������X�L��������
						char n = (i + 1 < path2.Length) ? path2[i + 1] : '\0';
						//	./ �� .\
						if (n == '/' || n == '\\') { i++; continue; }
						//	../ �� ..\
						if (n == '.') {
							char n2 = (i + 2 < path2.Length) ? path2[i + 2] : '\0';
							if (n2 == '/' || n2 == '\\') {
								string nn = GetDirName(res.ToString());
								if (nn == res.ToString()) {
									//	".."���̏������s�B
									//	root�t�H���_���̃`�F�b�N��
									//	�����ق��������̂��낤��..
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
			/// �t�@�C�����ɁAsetPath�Ŏw�肳��Ă���path����������(���݃`�F�b�N�t��)
			/// </summary>
			/// <remarks>
			///	�������A�t�@�C�������݂��Ă��Ȃ���΁A
			///	path�́A�ݒ肳��Ă�����̂�擪���珇�Ԃɒ��ׂ�B
			///	�t�@�C����������Ȃ��ꍇ�́A���̃t�@�C���������̂܂ܕԂ�
			///	fullname := setPath����Ă���path + localpath + filename;
			/// </remarks>
			/// <see cref=""/>
			/// <param name="localpath"></param>
			/// <param name="filename"></param>
			/// <returns></returns>
			public static string MakeFullName(string localpath, string filename) {
				return MakeFullName(ConcatPath(localpath, filename));
			}

			/// <summary>
			/// �t�@�C�����ɁAsetPath�Ŏw�肳��Ă���path����������(���݃`�F�b�N�t��)
			/// </summary>
			/// <remarks>
			/// 
			///	�������A�t�@�C�������݂��Ă��Ȃ���΁A
			///	path�́A�ݒ肳��Ă�����̂�擪���珇�Ԃɒ��ׂ�B
			///	�t�@�C����������Ȃ��ꍇ�́A���̃t�@�C���������̂܂ܕԂ�
			///
			///	fullname := setPath����Ă���path + filename;
			/// </remarks>
			/// <param name="filename"></param>
			/// <returns></returns>
			public static string MakeFullName(string filename) {
				foreach (string path in pathlist) {
					string fullname = ConcatPath(path, filename);
					//	�������āA���ꂪ���݂���t�@�C�������ׂ�
					if (IsExist(fullname))
						return fullname;
				}

				/*
				//	�A�[�J�C�o��p���Ē��ׂĂ݂�
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
			/// FileSys���ǂݍ��ނƂ��̃f�B���N�g���|���V�[�B
			/// 
			/// ����t�@�C����ǂݍ��ނƂ��A
			///		�E���s�t�@�C�����΂œǂݍ���(default)
			///		�E�N������working directory���΂œǂݍ���
			///		�E���݂�working directory���΂œǂݍ���
			/// ��3�킩��I���ł���B
			/// </summary>
			public static CurrentDirectoryEnum DirectoryPolicy = CurrentDirectoryEnum.ExecuteDirectory;

			/// <summary>
			///	�t�@�C���̑��݊m�F
			/// </summary>
			/// <remarks>
			///	�w�肵���t�@�C�����̃t�@�C�������݂��邩�ǂ����𒲂ׂ�
			///	setPath�Ŏw�肳��Ă���path�͍l���ɓ���Ȃ��B
			/// </remarks>
			/// <param name="filename"></param>
			/// <returns></returns>
			public static bool IsExist(string filename) {
				using (CurrentDirectoryHelper helper = new CurrentDirectoryHelper(FileSys.DirectoryPolicy))
					return File.Exists(filename);
			}

			/// <summary>
			/// �t�@�C���̐����m�F(path������)
			/// </summary>
			/// <remarks>
			///	setPath�Ŏw�肵��path���܂߂ăt�@�C����T���B
			///	�A�[�J�C�u���̃t�@�C���͊܂܂Ȃ��B
			///	�����܂Ńt�@�C�������݂���Ƃ��݂̂��̃t�@�C�������Ԃ�B
			/// </remarks>
			/// <param name="filename"></param>
			/// <returns>�t�@�C����������Ȃ��ꍇ��null�B</returns>
			public static string IsRealExist(string filename) {
				foreach (string path in pathlist) {
					string fullname = ConcatPath(path, filename);
					//	�������āA���ꂪ���݂���t�@�C�������ׂ�
					if (IsExist(fullname))
						return fullname;
				}
				return null;
			}

			/// <summary>
			/// path���擾�B
			/// </summary>
			/// <returns></returns>
			public static List<string> PathList { get { return pathlist; } }


			/// <summary>
			/// path��ݒ�B�����Őݒ肵�����̂́AmakeFullPath�̂Ƃ��Ɏg����
			/// </summary>
			/// <remarks>
			///	�ݒ肷��path�̏I�[�́A\ ����� / �łȂ��Ƃ��\��Ȃ��B
			///	(\ ����� / �� �����Ă��ǂ�)
			/// 
			/// �f�B�t�H���g�ł́A""�݂̂��ݒ肳��Ă���B
			/// </remarks>
			/// <param name="pathlist_"></param>
			public static void SetPath(string[] pathlist_) {
				pathlist.Clear();
				pathlist.AddRange(pathlist_);
			}

			/// <summary>
			/// path��ǉ��B�����Őݒ肵��path��ǉ�����B
			/// </summary>
			/// <remarks>
			/// 
			///	�ݒ肷��path�̏I�[�́A\ ����� / �łȂ��Ƃ��\��Ȃ��B
			///	(\ ����� / �� �����Ă��ǂ�)
			///	�������A"."��".."�Ȃǂ��w�肷��Ƃ��́A"\" �� "/"��
			///	�t�^���ĂȂ��Ƃ����Ȃ��B(����Ȃ̂��w�肵�Ȃ��ŗ~������)
			///
			///	�f�B�t�H���g�ł�""�݂̂�addPath�Œǉ�����Ă���B
			///	(�J�����g�t�H���_���������邽��)
			/// </remarks>
			/// <param name="path"></param>
			public static void AddPath(string path) { pathlist.Add(path); }

			/// <summary>
			/// path�̃��X�g
			/// </summary>
			private static List<string> pathlist = new List<string>();

			/// <summary>
			/// �e�t�H���_����Ԃ��B
			/// </summary>
			/// <param name="fname"></param>
			/// <returns></returns>
			/// <remarks>
			/// �f�B���N�g�����̎擾�B
			/// �Ⴆ�΁A "d:\path\foo.bat" �Ȃ� "d:\path\" ��Ԃ��܂��B
			///		"d:\path"��"d:\path\"�Ȃ�΁A"d:\"��Ԃ��܂��B
			///	���@windows���ɂ����ẮA'/' �͎g���Ȃ����Ƃɒ��ӁB
			///		(Path.DirectorySeparatorChar=='\\'�ł��邽��)
			///	���@�I�[�͕K��'\'�̂��Ă����ԂɂȂ�
			/// �@�@�������A�Ԃ��l�̕����񂪋�ɂȂ�ꍇ�͏����B
			/// �@�@�܂�AgetDirName("src\")==""�ł���B
			///	���@�I�[�� ".." , "..\" , "../"�Ȃ�΁A����͋삯�オ��s�B
			///		(".."�̏ꍇ���I�[�͕K��'\'�̂��Ă����ԂɂȂ�)
			/// ��  network drive���l�����āA"\\network computer��\"�܂ł́A
			/// �@�h���C�u���^�[�����B
			/// </remarks>
			public static string GetDirName(string path) {
				//	�R�s�[���Ă���
				string path2 = path;

				//	�I�[��'\'��'/'�Ȃ��1�������
				int l = path2 == null ? 0 : path2.Length;
				if (l != 0) {
					bool bNetwork = (path2.Length >= 2) && (path2.Substring(0, 2) == "\\\\");

					int vpos = -1;
					// volume separator(network�t�H���_�̏ꍇ�A
					// computer���̒����\�}�[�N)�̂���pos
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

					//	�����I�[��".."�Ȃ�΁A����͋삯�オ��s�B
					if (path2.EndsWith("..")) {
						//	�����オ��ˁ[��R(`�D�L)�m
					}
					else {
						// fullname = Path.GetDirectoryName(fullname);
						// ���삪���҂�����̂ł͂Ȃ��̂Ŏ��O�ŏ����B
						for (int pos = path2.Length - 1; ; --pos) {
							char c = path2[pos];
							if (c == Path.VolumeSeparatorChar || pos == vpos) {
								// c: �Ȃ� c:
								// c:/abc �Ȃ� c: �ɁB
								// c:abc �Ȃ� c:�ɁB
								if (pos != path2.Length - 1)
									path2 = path2.Remove(pos + 1);
								break;
							}
							if (c == '\\' || c == '/') {
								// �����ȉ�������
								path2 = path2.Remove(pos);
								break;
							}
							if (pos == 0) { // �P�̃t�H���_���Ȃ̂ŋ�̕�����Ԃ��̂�������
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
			/// �����̃t�@�C����������Ԃ�(�t�H���_����������������)
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
			/// Stream�������_���A�N�Z�X���邽�߂�wrapper
			/// </summary>
			/// <remarks>
			/// Zip�t�@�C���������Ƃ��ɗp����B
			/// little endian���Ɖ��肵�Ă���B(zip header�������Ȃ̂�)
			/// </remarks>
			public class StreamRandAccessor {

				/// <summary>
				/// open�����X�g���[����n���i����I
				/// </summary>
				/// <param name="f_"></param>
				public void SetStream(Stream f_) {
					stream_length = f_.Length;
					f = f_;
					// �ǂݍ���ł��Ȃ��̂ŁB
					pos = 0;
					readsize = 0;
					bWrite = false;
				}

				/// <summary>
				/// �X�g���[���̐擪����i��offset�̈ʒu�̃f�[�^byte��ǂݍ���
				/// </summary>
				/// <param name="i"></param>
				/// <returns></returns>
				public byte GetByte(long i) {
					check(ref i, 1);
					return data[i];
				}

				/// <summary>
				/// �X�g���[���̐擪����i��offset�̈ʒu�ɁA�f�[�^byte����������
				/// </summary>
				/// <param name="i"></param>
				/// <param name="b"></param>
				public void PushByte(long i, byte b) {
					check(ref i, 1);
					data[i] = b;
					bWrite = true;
				}


				/// <summary>
				///	�X�g���[���̐擪����i��offset�̈ʒu�̃f�[�^ushort��ǂݍ���
				/// </summary>
				/// <remarks>
				/// little endian�Œ�B
				/// </remarks>
				/// <param name="i"></param>
				/// <returns></returns>
				public ushort GetUshort(long i) {
					check(ref i, 2);

					/*
					#if BigEndian
								// BigEndian�̃R�[�h�́A���ׂĖ�����
								// Zip�t�@�C������̓ǂݍ��݂ɂ����g��Ȃ��B
								// Zip�t�@�C�����̒l��little endian�Ȃ̂�
								// ���� #if BigEndian�̃R�[�h���K�v�ɂȂ邱�Ƃ͂Ȃ��B

								byte b0 = data[i];
								byte b1 = data[i + 1];
								return (b0 << 8) | b1;
					#else
					 */
					//	return *(ushort*)&data[i];
					// return BitConverter.ToUInt16(data , ( int ) i);

					// ��BitConverter��LE/BE�����ˑ��B

					byte b0 = data[i];
					byte b1 = data[i + 1];
					return (ushort)((b1 << 8) | b0);

					//#endif
				}

				/// <summary>
				///	�X�g���[���̐擪����i��offset�̈ʒu�̃f�[�^uint��ǂݍ���
				/// </summary>
				/// <remarks>
				/// little endian�Œ�B
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

					// ��BitConverter��LE/BE�����ˑ��B

					byte b0 = data[i];
					byte b1 = data[i + 1];
					byte b2 = data[i + 2];
					byte b3 = data[i + 3];
					return (uint)((b3 << 24) | (b2 << 16) | (b1 << 8) | b0);

					//#endif
				}

				/// <summary>
				///	�X�g���[���̐擪����i��offset�̈ʒu�̃f�[�^ushort����������
				/// </summary>
				/// <remarks>
				/// little endian�Œ�B
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
				///	�X�g���[���̐擪����i��offset�̈ʒu�̃f�[�^uint����������
				/// </summary>
				/// <remarks>
				/// little endian�Œ�B
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
				/// �������݂��s�Ȃ�
				/// </summary>
				/// <remarks>
				///	pushUint���Ńf�[�^�ɑ΂��ď������݂��s�Ȃ������̂�
				///	�X�g���[���ɖ߂��B���̃��\�b�h�𖾎��I�ɌĂяo���Ȃ��Ƃ�
				///	256�o�C�g�̃X�g���[���ǂݍ��ݗp�o�b�t�@�������I�ɗp�ӂ���Ă���A
				///	���ݓǂݍ���ł���X�g���[���o�b�t�@�̊O�ɃA�N�Z�X�����Ƃ��ɂ�
				///	�����I�ɏ������܂��B
				/// </remarks>
				public void Flush() {
					if (bWrite) {
						//	writeback���Ȃ��Ă�
						f.Write(data, 0, readsize);
						bWrite = false;
					}
				}

				/// <summary>
				///	pos����size���ǂݍ���(�o�b�t�@�����O���͂��Ȃ�)
				/// </summary>
				/// <returns>�ǂݍ��܂ꂽ�T�C�Y��Ԃ�</returns>
				public long Read(byte[] data, long pos, uint size) {
					Flush();
					//	�t�@�C���̃V�[�N���s�Ȃ��O�ɂ�flush�����Ă����Ȃ��ƁA
					//	���ƂŖ߂��Ă��ď������ނƃV�[�N���Ԃ����������Ȃ�

					f.Seek(pos, SeekOrigin.Begin);
					int s;
					try { s = f.Read(data, 0, (int)size); }
					catch { s = 0; }
					// ����ł�2GB�܂ł��������ւ���..����ڂ�..
					return (long)s;
				}

				/// <summary>
				/// setStream�����˂�R���X�g���N�^
				/// </summary>
				/// <param name="f"></param>

				public StreamRandAccessor(Stream f) { SetStream(f); }
				public StreamRandAccessor() { }

				public void Dispose() { Flush(); }

				private Stream f;

				private long stream_length;
				private byte[] data = new byte[256]; // �ǂݍ��݃o�b�t�@
				private int readsize;	 // �o�b�t�@�ɓǂݍ��߂��T�C�Y�B
				private long pos;		 // ���ݓǂݍ���ł���o�b�t�@�̃X�g���[����̈ʒu
				private bool bWrite;	 // ���̃o�b�t�@�ɑ΂��ď������݂��s�Ȃ������H
				//	�������݂��s�Ȃ�����A���̃o�b�t�@��ǂݍ��񂾂Ƃ���
				//	���̕���writeback����K�v������

				/// <summary>
				///	���̃X�g���[����i�̈ʒu��size�o�C�g�̃A�N�Z�X���������̂����A
				///	�o�b�t�@�ɓǂ܂�Ă��邩�`�F�b�N���ēǂݍ��܂�Ă��Ȃ���Γǂݍ��ށB
				///	�n���ꂽi�́Adata[i]�ŖړI�̂Ƃ���ɃA�N�Z�X�ł���悤�ɒ��������B
				/// </summary>
				/// <param name="i"></param>
				/// <param name="size"></param>
				private void check(ref long i, uint size) {
					if (i < pos || pos + readsize < i + size) {
						//	�o�b�t�@�O�ł񂪂�
						Flush();
						// size<128�Ɖ��肵�ėǂ�
						//	�A�N�Z�X����ꏊ��buffer�̒����ɗ���悤�ɒ����B
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
				/// �X�g���[������Ԃ�
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
		//�������́CFileInfo,BlockState�Ƃ����܂Ƃ߂�FileSystem�N���X�������[

		/*
		 * List��LinkedList�̃p�t�H�[�}���X
		 * ���`�A�N�Z�X�Ȃǂł�10�{���炢�������D
		 * List�����͂₢�Ȃ�LinkedList�͑������Ă��񂶂łǂ�������������List�̕����Ƃ��Ă�����
		 * �v�f�̍폜��10�{�ȏ�LinkedList������
		 * LinkedList�͑�����List�͒x���Ƃ������o
		 * List�͍폜�Ƃ��r���ɑ}���Ƃ��ȊO�͂ƂĂ��D�G
		 * LinkedList�͑S�ʓI�ɗD�G
		 * */


		/*
		 *�t�@�C���V�X�e���̍l��������
		 * �t�@�C���Ƃ��ď�Ԃ����͎̂�����PC�̒��Ƀt�@�C���Ƃ��č쐬������̂̂�
		 * Download�L���[�ɓ����ꂽ���̂Ƃ��A�������t�@�C���Ƃ��Ď����Ă�����̂Ƃ�
		 * 
		 * 
         */


		public class SegmentIO {
			/// <summary>
			/// �t�@�C���Z�O�����g���t�@�C���ɏ�������
			/// �������ރt�@�C����FileInfo�Ŏw�肷��
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
			/// �ǂ߂Ȃ����null���Ԃ邩��C�����Ă�
			/// </summary>
			/// <param name="segnum"></param>
			/// <param name="fi"></param>
			/// <param name="fo"></param>
			/// <returns></returns>
			static public DataSegment ReadFileSegment(string folderPath,int segnum,FileDataInfo fi, IFileOpen fo) {
				bool exist = fi.CheckBlock(segnum);

				
				if (exist) {
					string filePath = Path.Combine(folderPath, fi.FileName);
					//���̃u���b�N�����݂���Ƃ������Ƃ�fs�̑傫�������ꂾ������Ɖ���ł��邩�炻�̗�O�͑Ώ����Ȃ�
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
				//�Ȃ���Γǂ߂܂���
				return null;
			}

			/// <summary>
			/// Stream��size�܂ł������傤����
			/// ���������ʂ�FileStream��p�ɂȂ�񂶂�Ȃ�
			/// ��������private���\�b�h�ł�
			/// �{����int size�łȂ���long size�����ǃL���X�g���ʓ|�Ȃ̂�
			/// </summary>
			/// <param name="fs"></param>
			/// <param name="size"></param>
			static private void ExtendFileSize(Stream fs, int size) {
				//�g������K�v����₤
				if (fs.Length > size) {
					return;
				}
				else {
					fs.SetLength((long)size);
				}
			}
		}

		public class NSManager : IDisposable {
			#region IDisposable �����o

			public void Dispose() {
				throw new Exception("The method or operation is not implemented.");
			}

			#endregion

		}

		/// <summary>
		/// �t�@�C�����I�[�v������C���^�t�F�[�X
		/// </summary>
		public interface IFileOpen {
			FileStream Get(string path);
		}

		/// <summary>
		/// �t�@�C���̍쐬���@���w�肷��C���^�t�F�[�X
		/// </summary>
		public interface FSFactory {
			FileStream Create(string s);
		}

		/// <summary>
		/// �Ƃ肠�������ꂪ����͕K�v�����炱�ꂾ������
		/// </summary>
		public class RWShareOpenCreateFSFactory : FSFactory {
			#region FSFactory �����o
			public FileStream Create(string s) {
				return new FileStream(s, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
			}
			#endregion

		}

		/// <summary>
		/// �������ʂɃI�[�v���D�N���[�Y�Ƃ��͏���ɂ���Ă�
		/// ����͎g��Ȃ��ۂ����ǎ������Ƃ��Ȃ��Ɩ�����Y��邩������Ȃ��̂�
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
		/// �t�@�C���v�[�����g���ăI�[�v��
		/// �����̎����Ă�t�@�C���A�_�E�����[�h���̃t�@�C�����ׂĂ��v�[������
		/// ������ƕ��ʂł͂��Ȃ����Ƃ�����
		/// �X�g���[���Đ��Ń��f�B�A�v���C���[��Open��Brossom��Open�Ƃ������ꂪ�u���b�N�����̂�
		/// �˂�Brossom��Open�����f�B�A�v���C���[��Open�Ƃ�������ɂ��邽�߂�
		/// ���Brossom���t�@�C���n���h�����͂Ȃ��Ȃ��Ƃ������Ƃɂ���
		/// FileStream�̏�Ԃ͕ۏ؂���Ȃ��̂Ŗ���Seek�Ƃ�����悤��
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
			/// Get��Get(string,FSFactory)�Ƃ������ɂ����������������ˁi����)
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
			/// ���Ԃ�g��Ȃ���
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

			#region IDisposable �����o

			public void Dispose() {
				foreach (FileStream fs in fsDic.Values) {
					fs.Close();
				}
				fsDic.Clear();
			}

			#endregion
		}


		//����
		//���낢��Z�ʂ��������ăt�@�C���X�g���[����Http���X�|���X�̃X�g���[���̗����ɁC�ǂ��炩�I�����ď������߂�l�ɂ���D
		//�t�b�N��������

		/// <summary>
		/// FileInfo��FileState����������A���S���Y���͂����ɂ���
		/// </summary>
		public class FileStateManager {

			/// <summary>
			/// successList��fi�ɏ�������
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
		/// �}���`�X���b�h�Ή�
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
			/// Success�ƕ񍐂��ꂽ�ԍ��̃��X�g������ŐV�����C���X�^���X������ĕԂ��܂��D
			/// ������SuccessList�̓N���A�[������
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
			/// �}���`�X���b�h�Ȃ̂œ����I�u�W�F�N�g��Ԃ����Ƃ͂��܂���
			/// �V�����C���X�^���X�ɃR�s�[�ł�
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
