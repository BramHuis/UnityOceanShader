using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "OceanSO", menuName = "Ocean")]
public class OceanSO : ScriptableObject
{
    [Header("Ocean mesh parameters")]
    [Tooltip("Ocean width"), Range(1f, 100000.0f)]
    public float oceanWidth;
    [Tooltip("Ocean length"), Range(1f, 100000.0f)]
    public float oceanLength;
    [Tooltip("Number of vertices per side (same for x and z direction)"), Range(2, 5000)]
    public int numberOfVerticesPerSide;
    
    [Header("Ocean wave parameters")]
    public GerstnerWaveSO[] gerstnerWavesSO;

    [Header("Ocean property parameters")]
    [Tooltip("Shallow color")]
    public Color shallowWaterColor;
    [Tooltip("Deep color")]
    public Color deepWaterColor;
    [Tooltip("Ocean color blend depth"), Range(0.1f, 200f)]
    public float oceanColorBlendDepth = 30.0f;
    [Tooltip("Ambient color")]
    public Color ambientLight;
    [Tooltip("Ambient color strength"), Range(0.0f, 1.0f)]
    public float ambientLightStrength = 0.1f;
    [Tooltip("Shininess of the specular highlights"), Range(0.0f, 100.0f)]
    public float specularHighlightShininess = 50.0f;
    [Tooltip("Fresnel color")]
    public Color fresnelColor;
    [Tooltip("Fresnel strength"), Range(0.0f, 10.0f)]
    public float fresnelPower = 3.5f;

    [Header("Ocean foam")]
    [Tooltip("Width of foam"), Range(0.0f, 10.0f)]
    public float foamWidth = 1.0f;
    [Tooltip("Foam color")]
    public Color foamColor;

    [Tooltip("Apply smoothshading")]
    public bool applySmoothShading;

    public  void OnValidate()
    {
        // This will run when a value is changed in the Inspector or manually during runtime
        OceanSOValidator.ValidateAllCachedOceans();
    }
}
