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

    [Tooltip("Monster will spawn if player walked this far")]
    [SerializeField] private float playerWalkDistanceBeforeSpawn = 50;

    private float timePassed;
    private float playerDistanceTraveled;
    private Vector3 playerLastPosition;
    private bool spawned;
    private void Update()
    {
        if (!spawned)
        {
            timePassed += Time.deltaTime;
            playerDistanceTraveled += Vector3.Distance(playerLastPosition, player.transform.position);

            if (timePassed >= timeBeforeSpawn || playerDistanceTraveled >= playerWalkDistanceBeforeSpawn)
            {
                // Select spawn point that is at the right distance from the player
                // FIXME: this uses world distance, should use navmesh distance.
                var selectedSpawnPoint = initialSpawnPositionsParent
                    .GetChildren()
                    .MinValue(p => Mathf.Abs(Vector3.Distance(p.position, player.transform.position) - targetSpawnDistance + Random.value * targetSpawnDistanceRandomSpread));

                Spawn(selectedSpawnPoint.position);
            }

            playerLastPosition = player.transform.position;
        }
    }

    public void Spawn(Vector3 position)
	{
        spawned = true;

        monster.transform.position = position;
        monster.SetActive(true);
    }
	private void OnDrawGizmosSelected()
	{
        if (player)
		{
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(player.transform.position, targetSpawnDistance);
		}
	}
}
