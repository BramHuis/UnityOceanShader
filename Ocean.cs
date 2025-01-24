using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    [SerializeField] GerstnerWave[] gerstnerWaves;

    [Header("Ocean property parameters")]
    [Tooltip("Shallow color"), SerializeField]
    Color shallowWaterColor;
    [Tooltip("Deep color"), SerializeField]
    Color deepWaterColor;
    [Tooltip("Ocean color blend depth"), Range(0.1f, 200f), SerializeField]
    float oceanColorBlendDepth;
    [Tooltip("Ambient color"), SerializeField]
    Color ambientLight;
    [Tooltip("Ambient color strength"), Range(0.0f, 1.0f),SerializeField]
    float ambientLightStrength;
    [Tooltip("Shininess of the specular highlights"), Range(0.0f, 100.0f), SerializeField]
    float specularHighlightShininess;
    [Tooltip("Fresnel color"), SerializeField]
    Color fresnelColor;
    [Tooltip("Fresnel strength"), Range(0.0f, 10.0f), SerializeField]
    float fresnelPower;
    Vector3 mainLightDirection;
    Vector4 mainLightColor;

    [Header("Ocean refraction")]
    [Tooltip("Refraction affected by depth"), SerializeField]
    bool isDepthBased;
    [Tooltip("Refraction strength"), Range(0.0f, 1000.0f), SerializeField]
    float refractionStrength;




    private void Start() {
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
        if (isDepthBased) {
            oceanMaterial.SetInt("isDepthBased", 1);
        } else {
            oceanMaterial.SetInt("isDepthBased", 0);
        }
        oceanMaterial.SetFloat("refractionStrength", refractionStrength);
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

    // Unity methods
    private void OnRenderObject() {
        SetUpWaveGenerationMaterialBuffers();
        oceanMaterial.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Triangles, numberOfTriangleIndices);
    }
    
    private void OnValidate() {
        SetUpWaveGenerationMaterialBuffers();

        if (gerstnerWaveBuffer != null) {
            gerstnerWaveBuffer.Dispose(); // Dispose of the old buffer to avoid memory leaks
        }
        
        if (gerstnerWaves != null) {
            int numberOfGerstnerWaves = gerstnerWaves.Length;
            computeShader.SetInt("numberOfGerstnerWaves", numberOfGerstnerWaves);
            gerstnerWaveBuffer = new ComputeBuffer(numberOfGerstnerWaves, sizeof(int) +  sizeof(float) * 7); // Update buffer size if needed
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

[System.Serializable]
public struct GerstnerWave
{
    [Tooltip("Toggle"), Range(0, 1), SerializeField]
    int toggle;
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
    [Tooltip("Wind direction (in degrees)"), SerializeField]
    Vector2 windDirectionDegrees;

    public GerstnerWave(int toggle, float waveAmplitude, float waveSteepness, float wavePhaseShift, float waveLength, float waveSpeed, Vector2 windDirectionDegrees) {
        this.toggle = toggle;
        this.waveAmplitude = waveAmplitude;
        this.waveSteepness = waveSteepness;
        this.wavePhaseShift = wavePhaseShift;
        this.waveLength = waveLength;
        this.waveSpeed = waveSpeed;
        this.windDirectionDegrees = windDirectionDegrees;
    }
}