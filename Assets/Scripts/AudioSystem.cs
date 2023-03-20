using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class AudioSystem : MonoBehaviour
{
	private static AudioSystem The;
	private List<ISoundListener> AllSoundListeners = new();
	private Dictionary<object, AudioSource> limiterMap = new();
	public static void AddSoundListener(ISoundListener listener)
	{
		The.AllSoundListeners.Add(listener);
	}
	public static AudioSource CreateGameObjectWithAudio(AudioClip clip, float volume = 1, float pitch = 1, object limiter = null, float extraDestroyDelay = 0.1f)
	{
		if (limiter is not null)
		{
			if (The.limiterMap.TryGetValue(limiter, out var existingSource))
			{
				if (existingSource)
					return null;
			}
			else
			{
				The.limiterMap.Add(limiter, null);
			}
		}

		var go = new GameObject();
		Destroy(go, clip.length + extraDestroyDelay);

		var source = go.AddComponent<AudioSource>();

		source.volume = volume;
		source.pitch = pitch;
		source.clip = clip;
		source.loop = false;
		source.dopplerLevel = 0;
		source.spatialBlend = 1;
		source.spread = 0;

		source.Play();

		if (limiter is not null)
			The.limiterMap[limiter] = source;

		return source;
	}
	public static AudioSource Play(AudioClip clip, Vector3 position, float volume = 1, float pitch = 1, object limiter = null, float extraDestroyDelay = 0.1f)
	{
		var source = CreateGameObjectWithAudio(clip: clip, volume: volume, pitch: pitch, limiter: limiter, extraDestroyDelay: extraDestroyDelay);
		if (source)
			source.transform.position = position;
		return source;
	}
	public static AudioSource Play(AudioClip clip, Transform parent, float volume = 1, float pitch = 1, object limiter = null, float extraDestroyDelay = 0.1f)
	{
		var source = CreateGameObjectWithAudio(clip: clip, volume: volume, pitch: pitch, limiter: limiter, extraDestroyDelay: extraDestroyDelay);
		if (source)
		{
			source.transform.parent = parent;
			source.transform.SetLocalPositionAndRotation(default, Quaternion.identity);
		}
		return source;
	}
	public static void PlayListenable(AudioClip clip, Vector3 position, float radius, float volume = 1, float pitch = 1, object limiter = null, float extraDestroyDelay = 0.1f)
	{
		var source = Play(clip: clip, position: position, volume: volume, pitch: pitch, extraDestroyDelay: extraDestroyDelay);
		if (source is null)
			return;

		var gizmos = source.gameObject.AddComponent<DebugGizmos>();
		gizmos.kind = DebugGizmos.Kind.Circle;
		gizmos.radius = radius;
		gizmos.color = Color.yellow;

		foreach (var listener in The.AllSoundListeners)
		{
			var distance = NavMeshUtils.PathLength(listener.ListenerPosition, position);

			float listenVolume = Math.MapClamped(distance, 0, radius, 1, 0);

			if (listenVolume > 0)
			{
				listener.Listen(position, listenVolume);
			}
		}
	}

	private void Awake()
	{
		The = this;
	}
}

public interface ISoundListener
{
	Vector3 ListenerPosition { get; }
	void Listen(Vector3 position, float volume);
}