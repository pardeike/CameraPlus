using System;
using System.Collections.Generic;

namespace CameraPlus
{
	public class WeakQuotaCache<KEY, KEYID, VALUE>(int maxRetrievals, Func<KEY, KEYID> keyConverter, Func<KEY, VALUE> fetchCallback) where VALUE : class
	{
		private readonly Dictionary<KEYID, (WeakReference<VALUE> value, int count)> cache = [];
		private readonly int maxRetrievals = maxRetrievals;
		private readonly Func<KEY, KEYID> keyConverter = keyConverter;
		private readonly Func<KEY, VALUE> fetchCallback = fetchCallback;

		public VALUE Get(KEY key)
		{
			if (key == null)
				return default;

			var rawKey = keyConverter(key);

			if (!cache.ContainsKey(rawKey) || cache[rawKey].count >= maxRetrievals)
			{
				VALUE newValue = fetchCallback(key);
				cache[rawKey] = (new WeakReference<VALUE>(newValue), 0);
			}

			var (value, count) = cache[rawKey];
			cache[rawKey] = (value, count + 1);

			if (value.TryGetTarget(out VALUE returnValue))
				return returnValue;
			return default;
		}
	}

	public class QuotaCache<KEY, KEYID, VALUE>(int maxRetrievals, Func<KEY, KEYID> keyConverter, Func<KEY, VALUE> fetchCallback)
	{
		private readonly Dictionary<KEYID, (VALUE value, int count)> cache = [];
		private readonly int maxRetrievals = maxRetrievals;
		private readonly Func<KEY, KEYID> keyConverter = keyConverter;
		private readonly Func<KEY, VALUE> fetchCallback = fetchCallback;

		public VALUE Get(KEY key)
		{
			if (key == null)
				return default;

			var rawKey = keyConverter(key);

			if (!cache.ContainsKey(rawKey) || cache[rawKey].count >= maxRetrievals)
			{
				VALUE newValue = fetchCallback(key);
				cache[rawKey] = (newValue, 0);
			}

			var (value, count) = cache[rawKey];
			cache[rawKey] = (value, count + 1);

			return value;
		}
	}
}