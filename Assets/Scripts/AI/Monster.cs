using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AI;
using UnityEngine;
using UnityEditor.Purchasing;
using UnityEngine.UIElements.Experimental;

[RequireComponent(typeof(NavMeshAgent))]
public class Monster : MonoBehaviour, ISoundListener
{
    [SerializeField] private GameObject player;
    [Tooltip("Bigger values make monster rotate faster, but it also makes the agent stumble on corners. I found nothing in NavMeshAgent to do this...")]
    [SerializeField] private float rotationSpeed;
    [SerializeField] private float visionAngle = 90;
    [SerializeField] private float visionDistance = 20;
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

    [SerializeField] private float listenSensitivity = 1f;
    [SerializeField] private float listenThreshold = 1.5f;

    [Header("Audio")]
	[SerializeField] private float screamMinDistance = 5;
	[SerializeField] private float screamVolume = 0.5f;
	[SerializeField] private AudioClip[] screamClips;
	[SerializeField] private float patrolScreamPeriodMin = 30;
	[SerializeField] private float patrolScreamPeriodMax = 60;
	[SerializeField] private float chaseScreamPeriodMin = 5;
	[SerializeField] private float chaseScreamPeriodMax = 10;

	private enum State
	{
		Initial,

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
    private float listenMeter;
	private int screamClipIndex;

	public Vector3 ListenerPosition => transform.position;
    public void Listen(Vector3 position, float volume)
    {
        listenMeter += volume * listenSensitivity;
        if (listenMeter > listenThreshold)
        {
            listenMeter = listenThreshold;

			agent.destination = position;
            SwitchToState(State.RunningToLastSeen);
        }
	}

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
        AudioSystem.AddSoundListener(this);
        screamClips.Shuffle();
        StartCoroutine(RandomScream());

        SwitchToState(State.Patrolling);
	}
    private IEnumerator RandomScream()
	{
        while (true)
        {
            bool chasing = state == State.Chasing || state == State.RunningToLastSeen;
            
            var randomScreamPeriodMin = chasing ? chaseScreamPeriodMin : patrolScreamPeriodMin;
            var randomScreamPeriodMax = chasing ? chaseScreamPeriodMax : patrolScreamPeriodMax;

			yield return new WaitForSeconds(Random.Range(randomScreamPeriodMin, randomScreamPeriodMax));

            Scream();
        }
    }
    private void SwitchToState(State newState)
	{
        if (state == newState)
            return;

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
                Scream();
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
    private void Scream()
    {
        var source = AudioSystem.Play(screamClips[screamClipIndex], transform.position, limiter: this);

        if (source)
		{
			if (++screamClipIndex >= screamClips.Length)
			{
				screamClipIndex = 0;
				screamClips.Shuffle();
			}

            var muffler = source.gameObject.AddComponent<ImmersiveAudioSource>();
            muffler.MinDistance = screamMinDistance;
            muffler.Volume = screamVolume;
            muffler.GetDistanceVisibilityAndFirstCorner = () =>
            {
                var path = NavMeshUtils.Path(transform.position, player.transform.position);
                if (path is null)
                    return (Vector3.Distance(transform.position, player.transform.position), PlayerCanBeSeen, transform.position);
				return (NavMeshUtils.PathLength(path), PlayerCanBeSeen, path.Length >= 2 ? path[path.Length - 2] : transform.position);
            };
        }
	}
	private void FixedUpdate()
    {
        listenMeter -= Time.deltaTime;
        listenMeter = Mathf.Max(0, listenMeter);


		switch (state)
		{
            case State.Patrolling:
            {
                if (PlayerIsInVision)
                {
					SwitchToState(State.Chasing);
                }
                else
				{
                    patrolTimer += Time.deltaTime;
                    if (patrolTimer >= patrolChangePeriod || ReachedDestination)
                    {
                        patrolTimer = 0;

                        var sortedPatrolPoints = patrolPoints.Select(p => (transform: p, distance: NavMeshUtils.PathLength(p.position, player.transform.position))).OrderBy(p => p.distance);

                        var relevantPatrolPoints = sortedPatrolPoints.Where(p => p.distance < patrolMaxDistanceFromPlayer);

						if (relevantPatrolPoints.Count() == 0)
							lastPatrolPoint = sortedPatrolPoints.Take(3).GetRandom().transform;
                        else
							lastPatrolPoint = relevantPatrolPoints.GetRandom().transform;

                        agent.destination = lastPatrolPoint.position;
                    }
                }
                break;
            }
            case State.Chasing:
            {
                if (PlayerIsInVision)
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
				if (PlayerIsInVision)
				{
					SwitchToState(State.Chasing);
				}
				else
				{
					if (ReachedDestination)
					{
						var notVisiblePoints = patrolPoints.Where(p => NavMesh.Raycast(transform.position, p.position, out var _, ~0));

						predictedPointsToCheck = GetRelevantSearchPoints(notVisiblePoints.Select(p => p.position))
							.SomeLast(numberOfPointsToCheckAfterLostPlayer)
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
                if (PlayerIsInVision)
                    SwitchToState(State.Chasing);
                else if (ReachedDestination)
				{
                    predictedPointsToCheck.Sort(p => -NavMeshUtils.PathLength(transform.position, p));

                    if (predictedPointsToCheck.TryPop(out var nextDestination))
                        agent.destination = nextDestination;
                    else
                        SwitchToState(State.Waiting);
				}
                break;
            }
            case State.Waiting:
            {
                if (PlayerIsInVision)
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
		
		DebugGizmos.DrawPie(transform.position, transform.rotation, visionDistance, visionAngle, Color.yellow);

        UnityEditor.Handles.Label(transform.position + Vector3.up, $"state: {state}\nlistenMeter: {listenMeter}\nPlayerCanBeSeen: {PlayerCanBeSeen}");
    }

    private bool ReachedDestination => Vector3.Distance(transform.position, agent.destination) <= agent.stoppingDistance * reachFactor;
    private Vector3 EyePosition => transform.position + Vector3.up;

	private bool PlayerCanBeSeen
    {
        get
		{
            var dirToPlayer = player.transform.position - EyePosition;
			if (Physics.Raycast(EyePosition, dirToPlayer, out var hit, visionDistance, visionMask, QueryTriggerInteraction.Ignore))
			{
				// FIXME: sketchy
				if (hit.collider.name == "Player")
				{
					lastSeenPlayerVelocity = player.transform.position - lastPlayerPosition;

					// TODO: implement hiding in shadows
					return true;
				}
			}
            return false;
		}
    }
    private bool PlayerIsInVision
	{
        get
		{
            var dirToPlayer = player.transform.position - EyePosition;
            if (Vector3.Angle(dirToPlayer, transform.forward) <= visionAngle)
            {
                return PlayerCanBeSeen;
			}
            return false;
        }
    }

    // Returns bigger values for more relevant paths, 0 for irrelevant.
    private float Relevance(Vector3[] corners)
    {
        var length = NavMeshUtils.PathLength(corners);
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
