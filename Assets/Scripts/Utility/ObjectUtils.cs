using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObjectUtils : MonoBehaviour
{
	private static ObjectUtils The;
	private List<(Component component, float timeLeft)> delayedDestructionsWithDependencies = new();
	private List<(Component component, float timeLeft)> backbuffer = new();

	public static void DestroyWithDependencies(Component toDestroy)
	{
		var components = toDestroy.gameObject.GetComponents<Component>();

		var toDestroyType = toDestroy.GetType();

		foreach (var component in components)
		{
			foreach (var attribute in component.GetType().CustomAttributes)
			{
				if (attribute.AttributeType == typeof(RequireComponent))
				{
					var requiredType = (System.Type)attribute.ConstructorArguments[0].Value;
					if (toDestroyType == requiredType || toDestroyType.IsSubclassOf(requiredType))
					{
						DestroyWithDependencies(component);
						break;
					}
				}
			}
		}
		Destroy(toDestroy);
	}
	public static void DestroyWithDependencies(Component toDestroy, float delay)
	{
		The.delayedDestructionsWithDependencies.Add((toDestroy, delay));
	}

	private void Awake()
	{
		The = this;
	}
	private void Update()
	{
		backbuffer.Clear();
		for (int i = 0; i < delayedDestructionsWithDependencies.Count; ++i)
		{
			var x = delayedDestructionsWithDependencies[i];
			x.timeLeft -= Time.deltaTime;
			if (x.timeLeft <= 0)
				DestroyWithDependencies(x.component);
			else
				backbuffer.Add(x);
		}

		Utils.Swap(ref backbuffer, ref delayedDestructionsWithDependencies);
	}
}
