using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Dolores.Utility.Extension
{
    public static class StringExtension {
        public static IEnumerable<string> EachLine(this TextReader reader) {
            string s;
            while((s = reader.ReadLine()) != null)
                yield return s;
        }
        public static IEnumerable<string> EachLine(this string str) {
            return str.Split('\n');
        }
        public static string FormatWith(this string str, params object[] args) {
            return string.Format(str, args);
        }
        public static char ToChar(this int i) {
            if(i < 0 || 255 < i) throw new ArgumentException();
            return (char)i;
        }
        public static string Repeat(this string s, int count) {
            if(count < 0) throw new ArgumentOutOfRangeException();
            var sb = new StringBuilder(s.Length * count);
            count.Times(() => sb.Append(s));
            return sb.ToString();
        }
        public static string FirstMatch(this string s, Regex r) {
            var m = r.Match(s);
            if(!m.Success) return null;
            if(m.Groups.Count > 1) return m.Groups[1].Value;
            else return m.Value;
        }
        public static int GetBytes(this string s) {
            return Encoding.UTF8.GetByteCount(s);
        }
        public static string Replace(this string s, Regex pattern, MatchEvaluator filter) {
            return pattern.Replace(s, filter);
        }
        public static string[] Split(this string s, Regex r) {
            return r.Split(s);
        }
        public static bool IsNullOrEmpty(this string s) {
            return string.IsNullOrEmpty(s);
        }
        public static string ToStringLiteral(this string s) {
            return "@\"" + s.Replace("\"", "\"\"") + "\"";
        }
    }
    public static class StreamExtension {
        public static void Write(this Stream dest, Stream src) { //Write whole content in src to dest
            var buffer = new byte[1024];
            int count;
            while((count = src.Read(buffer, 0, buffer.Length)) > 0) {
                dest.Write(buffer, 0, count);
            }
        }
    }
    public static class CollectionExtension {
        public static T[] Slice<T>(this T[] arr, int index, int length) {
            if(length < 0) length = arr.Length - index;
            if(length < 0 || arr.Length < index + length) throw new ArgumentOutOfRangeException();
            T[] result = new T[length];
            Array.Copy(arr, index, result, 0, length);
            return result;
        }
        public static T[] Shifted<T>(this T[] arr, int shift) {
            return arr.Slice(shift, -1);
        }
        public static T[] Fill<T>(this T[] arr, int min_length, T padding) {
            var result = new T[Math.Max(arr.Length, min_length)];
            Array.Copy(arr, result, arr.Length);
            for(var i = arr.Length; i < result.Length; i++)
                result[i] = padding;
            return result;
        }
        public static string Join<S>(this System.Collections.IEnumerable arr, S sep) { //TODO: refactor
            var i = arr.GetEnumerator();
            if(!i.MoveNext()) return string.Empty;
            var sb = new StringBuilder();
            sb.Append(i.Current);
            while(i.MoveNext()) {
                var x = i.Current;
                sb.Append(sep);
                sb.Append(x);
            }
            return sb.ToString();
        }
        public static string Join<T, S>(this IEnumerable<T> arr, S sep) {
            var i = arr.GetEnumerator();
            if(!i.MoveNext()) return string.Empty;
            var sb = new StringBuilder();
            sb.Append(i.Current);
            while(i.MoveNext()) {
                var x = i.Current;
                sb.Append(sep);
                sb.Append(x);
            }
            return sb.ToString();
        }
        public static IEnumerable<To> Map<From, To>(this IEnumerable<From> arr, Func<From, To> map) {
            foreach(var item in arr)
                yield return map(item);
        }
        public static To[] Map<From, To>(this From[] arr, Func<From, To> map) {
            To[] result = new To[arr.Length];
            for(var i = 0; i < result.Length; i++) result[i] = map(arr[i]);
            return result;
        }
        public static void Each<T>(this IEnumerable<T> arr, Action<T> func) {
            foreach(var a in arr) func(a);
        }
        public static void Each<T>(this IEnumerable<T[]> arr, Action<T, T> func) {
            foreach(var item in arr) {
                if(item.Length != 2)
                    throw new Exception("Each: invalid item length");
                func(item[0], item[1]);
            }
        }
        public static IEnumerable<T> Each<T>(this IEnumerable<T> arr) { return arr; }
        public static void WithIndex<T>(this IEnumerable<T> enm, Action<int, T> func) {
            int index = 0;
            foreach(var item in enm) func(index++, item);
        }
        public static void EachTuple<T>(this T[] arr, Action<T, T> func) {
            for(var i = 0; i < arr.Length - 1; i += 2) //ugg, bad code.
                func(arr[i], arr[i + 1]);
            if(arr.Length % 2 == 1)
                func(arr[arr.Length - 1], default(T));
        }
        public static IList<T> ToList<T>(this System.Collections.IList list) {
            var result = new List<T>(list.Count);
            foreach(T item in list) result.Add(item);
            return result;
        }

        public static bool IsNullOrEmpty<T>(this ICollection<T> tgt) {
            return tgt == null || tgt.Count == 0;
        }
        public static Dictionary<string, string> ToDictionary(this System.Collections.Specialized.NameValueCollection nvc) {
            var dic = new Dictionary<string, string>(nvc.Count);
            foreach(string key in nvc.Keys)
                dic.Add(key, nvc[key]);
            return dic;
        }
    }
    public static class NumberExtension {
        public static void Times(this int count, Action func) {
            if(count < 0) throw new ArgumentOutOfRangeException();
            for(var i = 0; i < count; i++) func();
        }
    }
    public static class TypeExtension {
        public static T CreateInstance<T>(this Type t) {
            return (T)Activator.CreateInstance(t);
        }
    }
}
