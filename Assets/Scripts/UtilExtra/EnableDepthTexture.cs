using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class EnableDepthTexture : MonoBehaviour {

    void Start() {
        GetComponent<Camera>().depthTextureMode = DepthTextureMode.Depth;
    }
}
