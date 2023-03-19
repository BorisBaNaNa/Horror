using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class PlayWebGLVideo : MonoBehaviour
{
    public VideoPlayer videoPlay;
    public string videoClipName;
    private string url;

    private void Awake()
    {
        url = System.IO.Path.Combine(Application.streamingAssetsPath, videoClipName);
        videoPlay.url = url;
    }
}
