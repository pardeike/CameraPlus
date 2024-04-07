using System;
using System.Collections.Generic;

namespace CameraPlus
{
	public class QuotaCache<S, T>(int maxRetrievals, Func<S, T> fetchCallback)
	{
		private readonly Dictionary<S, (T value, int count)> cache = [];
		private readonly int maxRetrievals = maxRetrievals;
		private readonly Func<S, T> fetchCallback = fetchCallback;

		public T Get(S key)
		{
			if (key == null)
				return default;

			if (!cache.ContainsKey(key) || cache[key].count >= maxRetrievals)
			{
				T newValue = fetchCallback(key);
				cache[key] = (newValue, 0);
			}

			var (value, count) = cache[key];
			cache[key] = (value, count + 1);

			return value;
		}
	}

}