using System;
using UnityEngine;

[RequireComponent(typeof(AudioSource), typeof(AudioLowPassFilter)/*, typeof(AudioReverbFilter)*/)]
public class ImmersiveAudioSource : MonoBehaviour
{
    public float MinDistance = 1;
    public float Volume = 1;
	public Func<(float, bool, Vector3)> GetDistanceVisibilityAndFirstCorner;

    private AudioSource source;
    private AudioLowPassFilter lowPassFilter;
    //private AudioReverbFilter reverbFilter;
	private float wetness;

	void Awake()
    {
		source = GetComponent<AudioSource>();
		lowPassFilter = GetComponent<AudioLowPassFilter>();
		//reverbFilter = GetComponent<AudioReverbFilter>();

		source.rolloffMode = AudioRolloffMode.Custom;
		source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, new(new(0, 1), new(1, 1)));
		//reverbFilter.decayTime = 10;
	}
	private void Start()
	{
		UpdateAudio(true);
	}

	private void Update()
    {
		UpdateAudio(false);
	}

	private void UpdateAudio(bool snap)
	{
		var (distance, visible, firstCorner) = GetDistanceVisibilityAndFirstCorner();
		source.volume = Volume * Mathf.Clamp01(MinDistance / distance);

		float deltaTime = snap ? 999999 : Time.deltaTime;

		if (visible)
		{
			lowPassFilter.cutoffFrequency = Mathf.Pow(10, Mathf.MoveTowards(Mathf.Log10(lowPassFilter.cutoffFrequency), Mathf.Log10(20000), deltaTime * 5));
			source.spread = Mathf.MoveTowards(source.spread, 0, deltaTime * 180);
			wetness = Mathf.Lerp(wetness, 0, Time.deltaTime);
		}
		else
		{
			lowPassFilter.cutoffFrequency = Mathf.Pow(10, Mathf.Lerp(Mathf.Log10(lowPassFilter.cutoffFrequency), Math.MapClamped(distance, 0, 100, Mathf.Log10(4000), Mathf.Log10(200)), deltaTime * 5));
			source.spread = Mathf.MoveTowards(source.spread, 180, deltaTime * 180);
			wetness = Mathf.Lerp(wetness, 1, deltaTime);
		}

		//reverbFilter.reverbLevel = -Mathf.Pow(10, Math.MapClamped(Mathf.Log10(0 + wetness), Mathf.Log10(0), Mathf.Log10(1), Mathf.Log10(10000), Mathf.Log10(0)));
		//reverbFilter.dryLevel    = -Mathf.Pow(10, Math.MapClamped(Mathf.Log10(1 - wetness), Mathf.Log10(0), Mathf.Log10(1), Mathf.Log10(10000), Mathf.Log10(0)));

		transform.position = Vector3.Lerp(transform.position, firstCorner, deltaTime * 5);
	}
}
