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
	public static AudioSource CreateGameObjectWithAudio(AudioClip clip, float volume = 1, float pitch = 1, object limiter = null, float extraDestroyDelay = 0.1f, bool raytraced = false)
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

		if (raytraced)
		{
			source.spatialize = true;
			var steamSource = source.gameObject.AddComponent<SteamAudio.SteamAudioSource>();
			steamSource.distanceAttenuation = true;
			steamSource.distanceAttenuationInput = SteamAudio.DistanceAttenuationInput.PhysicsBased;
			steamSource.airAbsorption = true;
			steamSource.airAbsorptionInput = SteamAudio.AirAbsorptionInput.SimulationDefined;
			steamSource.occlusion = true;
			steamSource.occlusionInput = SteamAudio.OcclusionInput.SimulationDefined;
			steamSource.transmission = true;
			steamSource.transmissionInput = SteamAudio.TransmissionInput.SimulationDefined;
			steamSource.transmissionType = SteamAudio.TransmissionType.FrequencyDependent;
			steamSource.reflections = true;
			steamSource.reflectionsType = SteamAudio.ReflectionsType.Realtime;
			steamSource.applyHRTFToReflections = true;
		}

		source.Play();

		if (limiter is not null)
			The.limiterMap[limiter] = source;

		return source;
	}
	public static AudioSource Play(AudioClip clip, Vector3 localPosition = default, Transform parent = null, float volume = 1, float pitch = 1, object limiter = null, float extraDestroyDelay = 0.1f, bool raytraced = false, bool listenable = false, float radius = 50)
	{
		var source = CreateGameObjectWithAudio(clip: clip, volume: volume, pitch: pitch, limiter: limiter, extraDestroyDelay: extraDestroyDelay, raytraced: raytraced);
		if (source)
		{
			source.transform.parent = parent;
			source.transform.localPosition = localPosition;

			if (listenable)
			{
				var gizmos = source.gameObject.AddComponent<DebugGizmos>();
				gizmos.kind = DebugGizmos.Kind.Circle;
				gizmos.radius = radius;
				gizmos.color = Color.yellow;

				foreach (var listener in The.AllSoundListeners)
				{
					var distance = NavMeshUtils.PathLength(listener.ListenerPosition, source.transform.position);

					float listenVolume = Math.MapClamped(distance, 0, radius, 1, 0);

					if (listenVolume > 0)
					{
						listener.Listen(source.transform.position, listenVolume);
					}
				}
			}
		}
		return source;
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