using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Gerstner wave", menuName = "Gerstner wave")]
public class GerstnerWaveSO : ScriptableObject
{
    [Tooltip("Toggle")]
    public bool toggle;
    [Tooltip("Wave amplitude"), Range(0.0f, 50.0f)]
    public float waveAmplitude;
    [Tooltip("Wave steepness"), Range(0.0f, 5.0f)]
    public float waveSteepness;
    [Tooltip("Wave phase shift"), Range(0.0f, 2 * Mathf.PI)]
    public float wavePhaseShift;
    [Tooltip("Wave length"), Range(0.01f, 25.0f)]
    public float waveLength;
    [Tooltip("Wave speed"), Range(0.0f, 10.0f)]
    public float waveSpeed;
    [Tooltip("Wind direction (in degrees)"), Range(0.0f, 360.0f)]
    public float windDirectionDegrees;

    public  void OnValidate()
    {
        // This will run when a value is changed in the Inspector or manually during runtime
        OceanSOValidator.ValidateAllCachedOceans();
    }
}
