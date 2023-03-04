using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

public struct Pair<T>
{
	public T first, second;
}

public static class Extensions
{
	////////////////////////////
	// IEnumerable extensions //
	////////////////////////////

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T MinValue<T>(this IEnumerable<T> enumerable, System.Func<T, float> func)
	{
		T result = default;
		float minValue = float.MaxValue;
		bool found = false;

		foreach (var t in enumerable)
		{
			var d = func(t);
			if (d < minValue)
			{
				minValue = d;
				result = t;
				found = true;
			}
		}

		if (!found)
			throw new System.InvalidOperationException("IEnumerable.MinValue: collection has no elements");

		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T MaxValue<T>(this IEnumerable<T> enumerable, System.Func<T, float> func)
	{
		T result = default;
		float maxValue = float.MinValue;
		bool found = false;

		foreach (var t in enumerable)
		{
			var d = func(t);
			if (d > maxValue)
			{
				maxValue = d;
				result = t;
				found = true;
			}
		}

		if (!found)
			throw new System.InvalidOperationException("IEnumerable.MinValue: collection has no elements");

		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Sort<T>(this List<T> list, System.Func<T, float> func)
	{
		list.Sort((a, b) => (int)Mathf.Sign(func(a) - func(b)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static List<T> SortedBy<T>(this IEnumerable<T> enumerable, System.Func<T, float> func)
	{
		var result = enumerable.ToList();
		result.Sort((a, b) => (int)Mathf.Sign(func(a) - func(b)));
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T GetRandom<T>(this IEnumerable<T> enumerable)
	{
		var array = enumerable.ToArray();
		return array[Random.Range(0, array.Length)];
	}

	/// <summary>
	/// Returns a collection of adjacent pairs. For example:
	/// [1, 2, 3, 4] => [[1, 2], [2, 3], [3, 4]]
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="enumerable"></param>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static IEnumerable<Pair<T>> Pairs<T>(this IEnumerable<T> enumerable)
	{
		var array = enumerable.ToArray();

		for (int i = 1; i < array.Length; ++i)
			yield return new Pair<T>() { first = array[i - 1], second = array[i] };
	}

	/// <summary>
	/// If this collection is empty, returns fallback collection.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="enumerable"></param>
	/// <param name="fallback"></param>
	/// <returns></returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static IEnumerable<T> IfEmpty<T>(this IEnumerable<T> enumerable, IEnumerable<T> fallback)
	{
		if (enumerable.Count() == 0)
			return fallback;
		return enumerable;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static IEnumerable<T> First<T>(this IEnumerable<T> enumerable, int n)
	{
		foreach (var item in enumerable)
		{
			if (n-- <= 0)
				break;

			yield return item;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static IEnumerable<T> Last<T>(this IEnumerable<T> enumerable, int n)
	{
		var array = enumerable.ToArray();
		for (int i = 0; i < n; ++i)
		{
			yield return array[array.Length - n + i];
		}
	}

	/////////////////////
	// List extensions //
	/////////////////////

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T Pop<T>(this List<T> list)
	{
		var result = list[list.Count - 1];
		list.RemoveAt(list.Count - 1);
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryPop<T>(this List<T> list, out T result)
	{
		if (list.Count == 0)
		{
			result = default;
			return false;
		}

		result = list[list.Count - 1];
		list.RemoveAt(list.Count - 1);
		return true;
	}


	////////////////////////////
	// IEnumerator extensions //
	////////////////////////////
	public static IEnumerable<T> ToEnumerable<T>(this IEnumerator<T> enumerator)
	{
		while (enumerator.MoveNext())
			yield return enumerator.Current;
	}

	public static IEnumerable ToEnumerable(this IEnumerator enumerator)
	{
		while (enumerator.MoveNext())
			yield return enumerator.Current;
	}

	//////////////////////////
	// Transform extensions //
	//////////////////////////

	public static Transform[] GetChildren(this Transform transform)
	{
		return transform.GetEnumerator().ToEnumerable().OfType<Transform>().ToArray();
	}
}
