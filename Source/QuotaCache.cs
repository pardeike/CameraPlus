using System;
using System.Collections.Generic;

namespace CameraPlus
{
	public class WeakQuotaCache<KEY, KEYID, VALUE>(int maxRetrievals, Func<KEY, KEYID> keyConverter, Func<KEY, VALUE> fetchCallback) where VALUE : class
	{
		class Entry(VALUE value)
		{
			public readonly WeakReference<VALUE> value = new(value);
			public int count;
		}

		readonly Dictionary<KEYID, Entry> cache = [];
		readonly int maxRetrievals = maxRetrievals;
		readonly Func<KEY, KEYID> keyConverter = keyConverter;
		readonly Func<KEY, VALUE> fetchCallback = fetchCallback;

		public VALUE Get(KEY key)
		{
			if (key == null)
				return default;

			var rawKey = keyConverter(key);

			if (cache.TryGetValue(rawKey, out var entry) == false || entry.count >= maxRetrievals)
			{
				VALUE newValue = fetchCallback(key);
				entry = new Entry(newValue);
				cache[rawKey] = entry;
			}

			entry.count++;

			if (entry.value.TryGetTarget(out VALUE returnValue))
				return returnValue;
			return default;
		}
	}

	public class QuotaCache<KEY, KEYID, VALUE>(int maxRetrievals, Func<KEY, KEYID> keyConverter, Func<KEY, VALUE> fetchCallback)
	{
		class Entry(VALUE value)
		{
			public VALUE value = value;
			public int count;
		}

		readonly Dictionary<KEYID, Entry> cache = [];
		readonly int maxRetrievals = maxRetrievals;
		readonly Func<KEY, KEYID> keyConverter = keyConverter;
		readonly Func<KEY, VALUE> fetchCallback = fetchCallback;
		readonly string metricName = typeof(VALUE).Name;

		public void Clear() => cache.Clear();

		public VALUE Get(KEY key)
		{
			if (key == null)
				return default;

			var rawKey = keyConverter(key);
			PerfMetrics.Count($"quota_cache.{metricName}.requests");

			if (cache.TryGetValue(rawKey, out var entry) == false || entry.count >= maxRetrievals)
			{
				PerfMetrics.Count($"quota_cache.{metricName}.refreshes");
				VALUE newValue = fetchCallback(key);
				entry = new Entry(newValue);
				cache[rawKey] = entry;
			}
			else
				PerfMetrics.Count($"quota_cache.{metricName}.hits");

			entry.count++;

			return entry.value;
		}
	}
}
