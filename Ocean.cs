using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class Ocean : MonoBehaviour
{
    [SerializeField] ComputeShader computeShader;
    [SerializeField] Material oceanMaterial;
    
    ComputeBuffer vertexBuffer;
    ComputeBuffer triangleBuffer;
    ComputeBuffer normalBuffer;
    ComputeBuffer gerstnerWaveBuffer;

    int kernelHandleFillVertices;
    int kernelHandleFillTriangles;
    int kernelHandleSimulateWaves;
    int kernelHandleCalculateNormals;

    int numberOfVertices;
    int numberOfTriangleIndices;
    uint dispatchGroupSizeX, dispatchGroupSizeY;

    [Header("Ocean mesh parameters")]
    [Tooltip("Ocean width"), Range(1f, 100000.0f), SerializeField]
    float oceanWidth;
    [Tooltip("Ocean length"), Range(1f, 100000.0f), SerializeField]
    float oceanLength;
    [Tooltip("Number of vertices per side (same for x and z direction)"), Range(2, 5000), SerializeField]
    int numberOfVerticesPerSide;
    
    [Header("Ocean wave parameters")]
    [SerializeField] GerstnerWaveSO[] gerstnerWavesSO;
    [SerializeField] GerstnerWave[] gerstnerWaves;

    [Header("Ocean property parameters")]
    [Tooltip("Shallow color"), SerializeField]
    Color shallowWaterColor = new Color(0, 178, 191, 178);
    [Tooltip("Deep color"), SerializeField]
    Color deepWaterColor = new Color(16, 138, 196, 255);
    [Tooltip("Ocean color blend depth"), Range(0.1f, 200f), SerializeField]
    float oceanColorBlendDepth = 30.0f;
    [Tooltip("Ambient color"), SerializeField]
    Color ambientLight = new Color(16, 138, 196, 255);
    [Tooltip("Ambient color strength"), Range(0.0f, 1.0f),SerializeField]
    float ambientLightStrength = 0.1f;
    [Tooltip("Shininess of the specular highlights"), Range(0.0f, 100.0f), SerializeField]
    float specularHighlightShininess = 50.0f;
    [Tooltip("Fresnel color"), SerializeField]
    Color fresnelColor = new Color(204, 230, 255, 255);
    [Tooltip("Fresnel strength"), Range(0.0f, 10.0f), SerializeField]
    float fresnelPower = 3.5f;
    Vector3 mainLightDirection;
    Vector4 mainLightColor;

    [Header("Ocean foam")]
    [Tooltip("Width of foam"), Range(0.0f, 10.0f), SerializeField]
    float foamWidth = 1.0f;
    [Tooltip("Foam color"), SerializeField]
    Color foamColor = new Color(255, 255, 255, 255);
    

    private void Start() {
        OceanSOValidator.CacheOceansInScene(); // Only runs once in one of the SO's
        BindKernelHandles();
        InitializeBuffers();
        GenerateOceanMeshData();
        SetUpWaveGenerationMaterialBuffers();
    }

    private void Update() {
        DispatchSimulateWaves();
    }

    private void BindKernelHandles()
    {
        // Bind the kernel handles to the kernels
        kernelHandleFillVertices = computeShader.FindKernel("FillVertexBuffer");
        kernelHandleFillTriangles = computeShader.FindKernel("FillTriangleBuffer");
        kernelHandleSimulateWaves = computeShader.FindKernel("SimulateWaves");
        kernelHandleCalculateNormals = computeShader.FindKernel("CalculateNormals");
    }

    private void InitializeBuffers()
    {
        // Set up the vertex buffer
        numberOfVertices = numberOfVerticesPerSide * numberOfVerticesPerSide; // Always create the same number of vertices in x and z direction
        Debug.Log($"The number of vertices in the ocean is {numberOfVertices}.");
        vertexBuffer = new ComputeBuffer(numberOfVertices, sizeof(float) * 3);

        // Set up the triangle and normal buffer
        numberOfTriangleIndices = (numberOfVerticesPerSide - 1) * (numberOfVerticesPerSide - 1) * 6;
        triangleBuffer = new ComputeBuffer(numberOfTriangleIndices, sizeof(int));
        normalBuffer = new ComputeBuffer(numberOfTriangleIndices, sizeof(float) * 3);

        // Set up the gerstner waves
        gerstnerWaves = new GerstnerWave[gerstnerWavesSO.Length];
        ChangeWaveData();

        int numberOfGerstnerWaves = gerstnerWaves.Length;
        computeShader.SetInt("numberOfGerstnerWaves", numberOfGerstnerWaves);
        gerstnerWaveBuffer = new ComputeBuffer(numberOfGerstnerWaves, sizeof(int) + sizeof(float) * 7);
        gerstnerWaveBuffer.SetData(gerstnerWaves);

        // Bind the buffers to the kernel handles
        computeShader.SetBuffer(kernelHandleFillVertices, "vertexBuffer", vertexBuffer);
        computeShader.SetBuffer(kernelHandleFillTriangles, "triangleBuffer", triangleBuffer);
        computeShader.SetBuffer(kernelHandleSimulateWaves, "vertexBuffer", vertexBuffer);
        computeShader.SetBuffer(kernelHandleSimulateWaves, "gerstnerWaveBuffer", gerstnerWaveBuffer);
        computeShader.SetBuffer(kernelHandleCalculateNormals, "vertexBuffer", vertexBuffer);
        computeShader.SetBuffer(kernelHandleCalculateNormals, "triangleBuffer", triangleBuffer);
        computeShader.SetBuffer(kernelHandleCalculateNormals, "normalBuffer", normalBuffer);
    }

    private void GenerateOceanMeshData() 
    {
        computeShader.SetInt("numberOfVerticesPerSide", numberOfVerticesPerSide);
        computeShader.SetFloat("oceanWidth", oceanWidth);
        computeShader.SetFloat("oceanLength", oceanLength);
        computeShader.SetVector("oceanStartPosition", transform.position);
        
        computeShader.GetKernelThreadGroupSizes(kernelHandleFillVertices, out dispatchGroupSizeX, out dispatchGroupSizeY, out _);
        computeShader.Dispatch(kernelHandleFillVertices, Mathf.CeilToInt((float)numberOfVerticesPerSide / dispatchGroupSizeX), Mathf.CeilToInt((float)numberOfVerticesPerSide / dispatchGroupSizeY), 1);
        computeShader.GetKernelThreadGroupSizes(kernelHandleFillTriangles, out dispatchGroupSizeX, out dispatchGroupSizeY, out _);
        computeShader.Dispatch(kernelHandleFillTriangles, Mathf.CeilToInt((float)(numberOfVerticesPerSide - 1) / dispatchGroupSizeX), Mathf.CeilToInt((float)(numberOfVerticesPerSide - 1) / dispatchGroupSizeY), 1);
    }
    
    private void SetUpWaveGenerationMaterialBuffers()
    {
        mainLightDirection = -RenderSettings.sun.transform.forward;
        mainLightColor = RenderSettings.sun.color;

        // Set the data of the vert/frag shaders
        oceanMaterial.SetBuffer("vertexPositions", vertexBuffer);
        oceanMaterial.SetBuffer("triangleIndices", triangleBuffer);
        oceanMaterial.SetBuffer("normalBuffer", normalBuffer);
        oceanMaterial.SetColor("shallowWaterColor", shallowWaterColor);
        oceanMaterial.SetColor("deepWaterColor", deepWaterColor);
        oceanMaterial.SetFloat("oceanColorBlendDepth", oceanColorBlendDepth);
        oceanMaterial.SetFloat("specularHighlightShininess", specularHighlightShininess);
        oceanMaterial.SetVector("mainLightDirection", mainLightDirection);
        oceanMaterial.SetVector("mainLightColor", mainLightColor);
        oceanMaterial.SetVector("ambientLight", ambientLight);
        oceanMaterial.SetFloat("ambientLightStrength", ambientLightStrength);
        oceanMaterial.SetVector("fresnelColor", fresnelColor);   
        oceanMaterial.SetFloat("fresnelPower", fresnelPower);
        oceanMaterial.SetFloat("foamWidth", foamWidth);
        oceanMaterial.SetVector("foamColor", foamColor);
    }

    public static Vector2 AngleToDirection(float angleDegrees) {
        // Convert the angle from degrees to radians
        float angleRadians = Mathf.PI * angleDegrees / 180f;  // MathF.PI for single precision (float)

        // Calculate the normalized direction vector
        float x = Mathf.Cos(angleRadians);
        float y = Mathf.Sin(angleRadians);

        return new Vector2(x, y);
    }

    private void DispatchSimulateWaves() {
        // Simulate the waves and afterwards recalculate the normals
        computeShader.SetFloat("time", Time.time);
        computeShader.GetKernelThreadGroupSizes(kernelHandleSimulateWaves, out dispatchGroupSizeX, out dispatchGroupSizeY, out _);
        computeShader.Dispatch(kernelHandleSimulateWaves, Mathf.CeilToInt((float)numberOfVerticesPerSide / dispatchGroupSizeX), Mathf.CeilToInt((float)numberOfVerticesPerSide / dispatchGroupSizeY), 1);
    
        computeShader.GetKernelThreadGroupSizes(kernelHandleCalculateNormals, out dispatchGroupSizeX, out dispatchGroupSizeY, out _);    
        computeShader.Dispatch(kernelHandleCalculateNormals, Mathf.CeilToInt((float)(numberOfVerticesPerSide - 1) / dispatchGroupSizeX), Mathf.CeilToInt((float)(numberOfVerticesPerSide - 1) / dispatchGroupSizeY), 1);
    }

    private void ChangeWaveData() {
        for (int i = 0; i < gerstnerWavesSO.Length; i++) {
            // Change the data for the Gerstner Wave
            gerstnerWaves[i] = new GerstnerWave {
                toggle = gerstnerWavesSO[i].toggle ? 1 : 0,
                waveAmplitude = gerstnerWavesSO[i].waveAmplitude,
                waveSteepness = gerstnerWavesSO[i].waveSteepness,
                wavePhaseShift = gerstnerWavesSO[i].wavePhaseShift,
                waveLength = gerstnerWavesSO[i].waveLength,
                waveSpeed = gerstnerWavesSO[i].waveSpeed,
                windDirection = AngleToDirection(gerstnerWavesSO[i].windDirectionDegrees)
            };
        }
    }

    // Unity methods
    private void OnRenderObject() {
        SetUpWaveGenerationMaterialBuffers();
        oceanMaterial.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Triangles, numberOfTriangleIndices);
    }
     
    public void OnValidate() {
        SetUpWaveGenerationMaterialBuffers();
        if (gerstnerWaves != null) {
            ChangeWaveData();
            gerstnerWaveBuffer.SetData(gerstnerWaves);
            computeShader.SetBuffer(kernelHandleSimulateWaves, "gerstnerWaveBuffer", gerstnerWaveBuffer);
        }
    }

    private void OnDestroy() {
        vertexBuffer.Dispose();
        triangleBuffer.Dispose();
        normalBuffer.Dispose();
        gerstnerWaveBuffer.Dispose();
    }
}


public struct GerstnerWave
{
    public int toggle;
    public float waveAmplitude;
    public float waveSteepness;
    public float wavePhaseShift;
    public float waveLength;
    public float waveSpeed;
    public Vector2 windDirection;

    public GerstnerWave(int toggle, float waveAmplitude, float waveSteepness, float wavePhaseShift, float waveLength, float waveSpeed, Vector2 windDirection) {
        this.toggle = toggle;
        this.waveAmplitude = waveAmplitude;
        this.waveSteepness = waveSteepness;
        this.wavePhaseShift = wavePhaseShift;
        this.waveLength = waveLength;
        this.waveSpeed = waveSpeed;
        this.windDirection = windDirection;
    }
}