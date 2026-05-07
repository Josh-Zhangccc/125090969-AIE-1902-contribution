using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct LocalizedString
{
    public string zh;
    public string en;
}

[Serializable]
public struct LocalizedText
{
    public string title;
    public string file;
}

[Serializable]
public struct NodeImage
{
    public string file;
    public LocalizedString description;
}

[Serializable]
public struct LocalizedAudio
{
    public string file;
    [Range(0f, 1f)]
    public float volume;
    public bool autoPlay;
}

[Serializable]
public struct CameraOverrideData
{
    public bool enabled;
    public Vector3 target;
    [Min(0f)]
    public float blendTime;
    [Tooltip("true = 相机停留在目标朝向不动；false = 数秒后自动恢复原朝向")]
    public bool stayOnTarget;
    [Tooltip("stayOnTarget 为 false 时，在 N 秒后自动恢复原朝向")]
    [Min(0f)]
    public float restoreAfterSeconds;
}

[Serializable]
public struct ConnectedNode
{
    public string id;
    public bool required;
}

[Serializable]
public struct ConnectedNodesData
{
    public List<ConnectedNode> to;
}

[Serializable]
public class NodeData
{
    public string id;
    public LocalizedString displayName;
    public LocalizedString shortDescription;
    public string campusLocation;
    public bool isStartingNode;
    public bool isVisible = true;
    public Vector3 position;
    public Vector3 rotation;
    public ConnectedNodesData connectedNodes;
    public float radius = 5f;
    public string thumbnail;
    public LocalizedText zhText;
    public LocalizedText enText;
    public List<NodeImage> images;
    public LocalizedAudio zhAudio;
    public LocalizedAudio enAudio;
    public CameraOverrideData cameraOverride;
    public float autoDismissDuration;
}
