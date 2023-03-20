using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshUtils : MonoBehaviour
{
	public static float PathLength(Vector3[] corners)
	{
		return corners.Pairs().Sum(p => Vector3.Distance(p.first, p.second));
	}
	public static float PathLength(Vector3 source, Vector3 destination)
	{
		var path = Path(source, destination);
		if (path is not null)
			return PathLength(path);
		return float.PositiveInfinity;
	}
	public static Vector3[] Path(Vector3 source, Vector3 destination)
	{
		var path = new NavMeshPath();
		if (NavMesh.CalculatePath(source, destination, ~0, path))
			return path.corners;
		return null;
	}

}
