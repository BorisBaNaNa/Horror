using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject monster;

    [Tooltip("Monster will spawn in one of these locations")]
    [SerializeField] private Transform initialSpawnPositionsParent;

    [Tooltip("When monster is spawned, it will be about this distance far from player")]
    [SerializeField] private float targetSpawnDistance = 10;
    [SerializeField] private float targetSpawnDistanceRandomSpread = 2;

    [Tooltip("Monster will always spawn when this time has passed")]
    [SerializeField] private float timeBeforeSpawn = 60;

	private bool spawned;

    private IEnumerator Start()
    {
		yield return new WaitForSecondsRealtime(timeBeforeSpawn);

		if (spawned)
			yield break;

		SpawnAtDistance(player.transform.position, targetSpawnDistance, targetSpawnDistanceRandomSpread);
	}

	public void Spawn(Vector3 position)
	{
		spawned = true;

		monster.transform.position = position;
		monster.SetActive(true);
	}
	public void SpawnAtDistance(Vector3 position, float distance, float randomFactor = 0)
	{
		// FIXME: this uses world distance, maybe should use navmesh distance?

		var selectedSpawnPoint = initialSpawnPositionsParent
			.GetChildren()
			.MinValue(p => Mathf.Abs(Vector3.Distance(p.position, position) - distance + Random.value * randomFactor));

		Spawn(selectedSpawnPoint.position);
	}
	public void SpawnInRange(Vector3 position, float minDistance, float maxDistance)
	{
		// FIXME: this uses world distance, maybe should use navmesh distance?

		var closeSpawnPoints = initialSpawnPositionsParent
			.GetChildren()
			.Where(p => InRange(Vector3.Distance(p.position, position), minDistance, maxDistance));

		if (closeSpawnPoints.Count() != 0)
			Spawn(closeSpawnPoints.GetRandom().position);
		else
			SpawnAtDistance(position, minDistance);
	}
	private bool InRange(float value, float min, float max)
	{
		return min <= value && value <= max;
	}
	private void OnDrawGizmosSelected()
	{
        if (player)
            DebugGizmos.DrawCircle(player.transform.position, Quaternion.identity, targetSpawnDistance, Color.yellow);
	}
}
