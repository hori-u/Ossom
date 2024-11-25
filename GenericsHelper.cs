using System;
using System.Collections.Generic;

namespace P2P
{
    public static class ListExtension {
		public static T Last<T>( List<T> list) {
			return list[list.Count - 1];
		}
	}

	public static class LinkedListExtension {
		public static T Find<T>( LinkedList<T> list, Predicate<T> pred) {
			LinkedListNode<T> node = list.First;
			while (node != null) {
				LinkedListNode<T> next = node.Next;
				if (pred(node.Value)) {
					return node.Value;
				}
				node = next;
			}
			return default(T);
		}

		public static LinkedList<T> FindAll<T>( LinkedList<T> list, Predicate<T> pred) {
			LinkedList<T> retList = new LinkedList<T>();
			LinkedListNode<T> node = list.First;
			while (node != null) {
				LinkedListNode<T> next = node.Next;
				if (pred(node.Value)) {
					retList.AddLast(node.Value);
				}
				node = next;
			}
			return retList;
		}

		public static T FindAndRemove<T>( LinkedList<T> list, Predicate<T> pred) {
			LinkedList<T> retList = new LinkedList<T>();
			T t = default(T);
			LinkedListNode<T> node = list.First;
			while (node != null) {
				LinkedListNode<T> next = node.Next;
				if (pred(node.Value)) {
					t = node.Value;
					list.Remove(node);
				}
				node = next;
			}
			return t;
		}

		public static void Remove<T>( LinkedList<T> list, Predicate<T> pred) {
			LinkedListNode<T> node = list.First;
			while (node != null) {
				LinkedListNode<T> next = node.Next;
				if (pred(node.Value)) {
					list.Remove(node);
					break;
				}
				node = next;
			}
		}

		public static void RemoveAll<T>( LinkedList<T> list,Predicate<T> pred) {
			LinkedListNode<T> node = list.First;
			while (node != null) {
				LinkedListNode<T> next = node.Next;
				if (pred(node.Value)) {
					list.Remove(node);
				}
				node = next;
			}
		}
	}
	
	public static class GenericsHelper {
		public static IEnumerable<T> Remove<T>( IEnumerable<T> objs, Predicate<T> pred) {
			foreach (T obj in objs)
				if (!pred(obj))
					yield return obj;
		}
	}	
}
