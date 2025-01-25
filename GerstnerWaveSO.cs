using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Gerstner wave", menuName = "Gerstner wave")]
public class GerstnerWaveSO : ScriptableObject
{
    [Tooltip("Toggle"), SerializeField]
    bool toggle;
    [Tooltip("Wave amplitude"), Range(0.0f, 50.0f), SerializeField]
    float waveAmplitude;
    [Tooltip("Wave steepness"), Range(0.0f, 5.0f), SerializeField]
    float waveSteepness;
    [Tooltip("Wave phase shift"), Range(0.0f, 2 * Mathf.PI), SerializeField]
    float wavePhaseShift;
    [Tooltip("Wave length"), Range(0.01f, 25.0f), SerializeField]
    float waveLength;
    [Tooltip("Wave speed"), Range(0.0f, 10.0f), SerializeField]
    float waveSpeed;
    [Tooltip("Wind direction (in degrees)"), Range(0, 360), SerializeField]
    float windDirectionDegrees;
}
