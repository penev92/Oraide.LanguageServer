using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.MiniYamlParser
{
	public static class Exts
	{
        public static V GetOrAdd<K, V>(this Dictionary<K, V> d, K k)
			where V : new()
		{
			return d.GetOrAdd(k, new V());
		}

		public static V GetOrAdd<K, V>(this Dictionary<K, V> d, K k, V v)
		{
			if (!d.TryGetValue(k, out var ret))
				d.Add(k, ret = v);
			return ret;
		}

		public static V GetOrAdd<K, V>(this Dictionary<K, V> d, K k, Func<K, V> createFn)
		{
			if (!d.TryGetValue(k, out var ret))
				d.Add(k, ret = createFn(k));
			return ret;
		}

		public static string JoinWith<T>(this IEnumerable<T> ts, string j)
		{
			return string.Join(j, ts);
		}

		public static IEnumerable<T> Append<T>(this IEnumerable<T> ts, params T[] moreTs)
		{
			return ts.Concat(moreTs);
		}

		public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
		{
			return new HashSet<T>(source);
		}

		public static Dictionary<TKey, TSource> ToDictionaryWithConflictLog<TSource, TKey>(
			this IEnumerable<TSource> source, Func<TSource, TKey> keySelector,
			string debugName, Func<TKey, string> logKey, Func<TSource, string> logValue)
		{
			return ToDictionaryWithConflictLog(source, keySelector, x => x, debugName, logKey, logValue);
		}

		public static Dictionary<TKey, TElement> ToDictionaryWithConflictLog<TSource, TKey, TElement>(
			this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector,
			string debugName, Func<TKey, string> logKey = null, Func<TElement, string> logValue = null)
		{
			// Fall back on ToString() if null functions are provided:
			logKey = logKey ?? (s => s.ToString());
			logValue = logValue ?? (s => s.ToString());

			// Try to build a dictionary and log all duplicates found (if any):
			var dupKeys = new Dictionary<TKey, List<string>>();
			var d = new Dictionary<TKey, TElement>();
			foreach (var item in source)
			{
				var key = keySelector(item);
				var element = elementSelector(item);

				// Discard elements with null keys
				if (!typeof(TKey).IsValueType && key == null)
					continue;

				// Check for a key conflict:
				if (d.ContainsKey(key))
				{
					if (!dupKeys.TryGetValue(key, out var dupKeyMessages))
					{
						// Log the initial conflicting value already inserted:
						dupKeyMessages = new List<string>();
						dupKeyMessages.Add(logValue(d[key]));
						dupKeys.Add(key, dupKeyMessages);
					}

					// Log this conflicting value:
					dupKeyMessages.Add(logValue(element));
					continue;
				}

				d.Add(key, element);
			}

			// If any duplicates were found, throw a descriptive error
			if (dupKeys.Count > 0)
			{
				var badKeysFormatted = string.Join(", ", dupKeys.Select(p => $"{logKey(p.Key)}: [{string.Join(",", p.Value)}]"));
				var msg = $"{debugName}, duplicate values found for the following keys: {badKeysFormatted}";
				throw new ArgumentException(msg);
			}

			// Return the dictionary we built:
			return d;
		}
    }
}
