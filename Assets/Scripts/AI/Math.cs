using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class Math
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float MapUnclamped(float v, float sn, float sx, float dn, float dx)
	{
		return (v - sn) / (sx - sn) * (dx - dn) + dn;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float MapClamped(float v, float sn, float sx, float dn, float dx)
	{
		return (Mathf.Clamp(v, Mathf.Min(sn, sx), Mathf.Max(sn, sx)) - sn) / (sx - sn) * (dx - dn) + dn;
	}
}
