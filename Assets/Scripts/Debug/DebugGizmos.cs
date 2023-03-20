using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public class DebugGizmos : MonoBehaviour
{
	public enum Kind
	{
		Sphere,
		Circle,
		Box,
	}
	public Kind kind;
	public Color color = Color.white;
	public float radius = 1;
	public Vector3 size = Vector3.one;

	private void Update()
	{
		switch (kind)
		{
			case Kind.Circle:
			{
				DrawCircle(transform.position, transform.rotation, radius, color);
				break;
			}
		}
	}


	public static void DrawArc(Vector3 position, Quaternion rotation, float radius, float angle, Color color)
	{
		Vector3 GetPoint(float angle) => position + rotation * new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * radius;

		Vector3 prev = GetPoint(-0.5f * angle * Mathf.Deg2Rad);
		for (int i = -15; i <= 16; ++i)
		{
			Vector3 curr = GetPoint(i / 32f * angle * Mathf.Deg2Rad);
			Debug.DrawLine(prev, curr, color);
			prev = curr;
		}
	}
	public static void DrawCircle(Vector3 position, Quaternion rotation, float radius, Color color)
	{
		DrawArc(position, rotation, radius, 360, color);
	}
	public static void DrawPie(Vector3 position, Quaternion rotation, float radius, float angle, Color color)
	{
		Vector3 GetPoint (float angle) => position + rotation * new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * radius;

		Vector3 prev = GetPoint(-0.5f * angle * Mathf.Deg2Rad);
		Debug.DrawLine(position, prev, color);
		for (int i = -15; i <= 16; ++i)
		{
			Vector3 curr = GetPoint(i / 32f * angle * Mathf.Deg2Rad);
			Debug.DrawLine(prev, curr, color);
			prev = curr;
		}
		Debug.DrawLine(position, prev, color);
	}
}
