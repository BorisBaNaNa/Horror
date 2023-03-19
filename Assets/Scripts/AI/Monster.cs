using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AI;
using UnityEngine;

[RequireComponent(typeof(NavMeshAgent))]
public class Monster : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [Tooltip("Bigger values make monster rotate faster, but it also makes the agent stumble on corners. I found nothing in NavMeshAgent to do this...")]
    [SerializeField] private float rotationSpeed;
    [SerializeField] private float visionAngle = 60;
    [SerializeField] private float visionDistance = float.PositiveInfinity;
    [SerializeField] private LayerMask visionMask;
    [SerializeField] private Transform patrolPositionsParent;
    [SerializeField] private float patrolChangePeriod = 10;
    [SerializeField] private float patrolSpeed = 1;
    [SerializeField] private float patrolMaxDistanceFromPlayer = 10;
    [SerializeField] private float chaseSpeed = 2;

    [Tooltip("Go that amount of meters further of the position when monster lost the player.")]
    [SerializeField] private float lostWalkDistance = 2;

    [SerializeField] private int numberOfPointsToCheckAfterLostPlayer = 3;

    [Tooltip("NavMeshAgent.stoppingDistance multiplied by reachFactor determines if monster reached it's destination")]
    [SerializeField] private float reachFactor = 1.1f;

    [SerializeField] private float waitLookaroundSpeed = 90;
    [SerializeField] private float waitLookaroundTime = 4;

    private enum State
	{
        // Walking to patrol points randomly
        Patrolling,

        // Running to the player
        Chasing,

        // Running to the player's last seen position
        RunningToLastSeen,

        // Searching nearby places after lost player
        Searching,

        // Waiting for player to show himself
        Waiting,
    }

    private State state;
    private NavMeshAgent agent;
    private Transform[] patrolPoints;
    private Transform lastPatrolPoint;
    private float patrolTimer;
    private Vector3 lastPlayerPosition;
    private Vector3 lastSeenPlayerVelocity;
    private float waitTimer;

    private struct CalculatedPath
	{
        public float relevance;
        public Vector3 end;
	}

    private List<Vector3> predictedPointsToCheck = new();

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        patrolPoints = patrolPositionsParent.GetChildren();
    }
    private void SwitchToState(State newState)
	{
        state = newState;
        switch (newState)
		{
            case State.Patrolling:
			{
                patrolTimer = 0;
                agent.speed = patrolSpeed;
                break;
            }
            case State.Chasing:
            {
                agent.speed = chaseSpeed;
                break;
            }
            case State.RunningToLastSeen:
            {
                agent.speed = chaseSpeed;
                break;
            }
            case State.Searching:
            {
                agent.speed = chaseSpeed;
                break;
            }
            case State.Waiting:
			{
                waitTimer = 0;
                agent.speed = 0;
                break;
            }
        }
    }
    private void FixedUpdate()
    {
        switch (state)
		{
            case State.Patrolling:
            {
                if (PlayerIsVisible)
                    SwitchToState(State.Chasing);
                else
				{
                    patrolTimer += Time.deltaTime;
                    if (patrolTimer >= patrolChangePeriod || ReachedDestination)
                    {
                        patrolTimer = 0;

                        lastPatrolPoint = patrolPoints
                            .Where(p => p != lastPatrolPoint && PathLength(p.position, player.transform.position) < patrolMaxDistanceFromPlayer)
                            .IfEmpty(patrolPoints)
                            .GetRandom();

                        agent.destination = lastPatrolPoint.position;
                    }
                }
                break;
            }
            case State.Chasing:
            {
                if (PlayerIsVisible)
				{
                    agent.destination = player.transform.position;

                    if (ReachedDestination)
                        Debug.Log("GAME OVER");
                }
                else
                {
                    Vector3 nextDestination = agent.destination + Vector3.ClampMagnitude(lastSeenPlayerVelocity * 1000, 1) * lostWalkDistance;

                    // Make sure nextDestination is not to the other side of a wall
                    if (NavMesh.Raycast(agent.destination, nextDestination, out var hit, ~0))
                        nextDestination = hit.position;

                    agent.destination = nextDestination;

                    SwitchToState(State.RunningToLastSeen);
                }
                break;
            }
            case State.RunningToLastSeen:
            {
                if (PlayerIsVisible)
				{
                    SwitchToState(State.Chasing);
                }
                else
				{
                    if (ReachedDestination)
                    {
                        var notVisiblePoints = patrolPoints.Where(p => NavMesh.Raycast(transform.position, p.position, out var _, ~0));

                        predictedPointsToCheck = GetRelevantSearchPoints(notVisiblePoints.Select(p => p.position))
                            .Last(numberOfPointsToCheckAfterLostPlayer)
                            .ToList();

                        if (predictedPointsToCheck.TryPop(out var nextDestination))
                            agent.destination = nextDestination;

                        SwitchToState(State.Searching);
                    }
                }
                break;
            }
            case State.Searching:
            {
                if (PlayerIsVisible)
                    SwitchToState(State.Chasing);
                else if (ReachedDestination)
				{
                    predictedPointsToCheck.Sort(p => -PathLength(transform.position, p));

                    if (predictedPointsToCheck.TryPop(out var nextDestination))
                        agent.destination = nextDestination;
                    else
                        SwitchToState(State.Waiting);
				}
                break;
            }
            case State.Waiting:
            {
                if (PlayerIsVisible)
                    SwitchToState(State.Chasing);
                else
				{
                    transform.Rotate(0, waitLookaroundSpeed * Time.deltaTime, 0);

                    waitTimer += Time.deltaTime;
                    if (waitTimer > waitLookaroundTime)
                        SwitchToState(State.Patrolling);
				}
                break;
            }
        }

        if (state != State.Waiting)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(agent.steeringTarget - transform.position), Time.deltaTime * rotationSpeed);

        lastPlayerPosition = player.transform.position;
    }

	private void OnDrawGizmos()
	{
        if (agent)
		{
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(agent.destination, 0.5f);

            UnityEditor.Handles.Label(agent.destination + Vector3.up, "agent.destination");
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, Quaternion.Euler(0, +visionAngle, 0) * transform.forward * Mathf.Min(10000, visionDistance));
        Gizmos.DrawRay(transform.position, Quaternion.Euler(0, -visionAngle, 0) * transform.forward * Mathf.Min(10000, visionDistance));

        UnityEditor.Handles.Label(transform.position + Vector3.up, state.ToString());
    }

    private bool ReachedDestination => Vector3.Distance(transform.position, agent.destination) <= agent.stoppingDistance * reachFactor;

    private bool PlayerIsVisible
	{
        get
		{
            var dirToPlayer = player.transform.position - transform.position;
            if (Vector3.Angle(dirToPlayer, transform.forward) <= visionAngle)
            {
                if (Physics.Raycast(transform.position, dirToPlayer, out var hit, visionDistance, visionMask, QueryTriggerInteraction.Ignore))
                {
                    // FIXME: sketchy
                    if (hit.collider.name == "Player")
                    {
                        lastSeenPlayerVelocity = player.transform.position - lastPlayerPosition;

                        // TODO: implement hiding in shadows
                        return true;
                    }
                }
            }
            return false;
        }
    }
    private float PathLength(Vector3[] corners)
    {
        return corners.Pairs().Sum(p => Vector3.Distance(p.first, p.second));
    }
    private float PathLength(Vector3 a, Vector3 b)
	{
        var path = new NavMeshPath();
        if (NavMesh.CalculatePath(a, b, ~0, path))
            return PathLength(path.corners);
        return float.PositiveInfinity;
    }

    // Returns bigger values for more relevant paths, 0 for irrelevant.
    private float Relevance(Vector3[] corners)
    {
        var length = PathLength(corners);
        var direction = corners[1] - corners[0];
        var angle = Vector3.Angle(direction, lastSeenPlayerVelocity);

        var angleFactor = Mathf.Pow(Math.MapClamped(angle, 0, 180, 1, 0), 4);

        return angleFactor / length;
    }

    // Select relevant patrol points to check (which are close and in the right direction)
    private IEnumerable<Vector3> GetRelevantSearchPoints(IEnumerable<Vector3> destinations)
	{
        List<CalculatedPath> calculatedPaths = new();

        var path = new NavMeshPath();
        foreach (var point in destinations)
        {
            if (NavMesh.CalculatePath(transform.position, point, ~0, path))
            {
                var calculatedPath = new CalculatedPath()
                {
                    end = point,
                    relevance = Relevance(path.corners),
                };
                calculatedPaths.Add(calculatedPath);
            }
        }

        calculatedPaths.Sort(p => p.relevance);

        return calculatedPaths.Select(p => p.end);
    }
}
