using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ocean : MonoBehaviour
{
    [SerializeField] ComputeShader computeShader;
    [SerializeField] Material oceanMaterial;
    [SerializeField] Texture2D noiseTexture;

    ComputeBuffer vertexBuffer;
    ComputeBuffer triangleBuffer;
    ComputeBuffer normalBuffer;
    Vector3 [] cpuNormals;
    int [] cpuTriangleIndices;

    int kernelHandleFillVertices;
    int kernelHandleFillTriangles;
    int kernelHandleSimulateWaves;
    int kernelHandleCalculateNormals;

    int numberOfVertices;
    int numberOfTriangleIndices;
    uint dispatchGroupSizeX, dispatchGroupSizeY;

    [Header("Ocean mesh parameters")]
    [Tooltip("Ocean width"), Range(0.1f, 10000), SerializeField]
    float oceanWidth;
    [Tooltip("Ocean length"), Range(0.1f, 10000), SerializeField]
    float oceanLength;
    [Tooltip("Number of ocean subdivisions"), Range(0, 12), SerializeField]
    int oceanSubdivisions;
    int numberOfVerticesPerSide;
    
    [Header("Ocean wave parameters")]
    [Tooltip("Wave amplitude"), Range(0.0f, 50.0f), SerializeField]
    float waveAmplitude;
    [Tooltip("Wave frequency"), Range(0.01f, 20.0f), SerializeField]
    float waveFrequency;
    [Tooltip("Wind direction (in degrees)"), Range(0.0f, 360.0f), SerializeField]
    float windDirectionDegrees;
    [Tooltip("Wind strength"), Range(0.0f, 5.0f), SerializeField]
    float windStrength;

    [Header("Ocean property parameters")]
    [Tooltip("Shallow color"), SerializeField]
    Color shallowWaterColor;
    [Tooltip("Deep color"), SerializeField]
    Color deepWaterColor;
    [Tooltip("Ocean color blend depth"), Range(0.1f, 200f), SerializeField]
    float oceanColorBlendDepth;

    [Header("Ocean lighting parameters")]
    [Tooltip("Ambient color"), SerializeField]
    Color ambientLight;
    [Tooltip("Ambient color strength"), Range(0.0f, 1.0f),SerializeField]
    float ambientLightStrength;
    [Tooltip("Shininess of the specular highlights"), Range(0.1f, 40), SerializeField]
    float specularHighlightShininess;
    [Tooltip("Fresnel color"), SerializeField]
    Color fresnelColor;
    [Tooltip("Fresnel strength"), SerializeField]
    float fresnelPower;
    Vector3 mainLightDirection;
    Vector4 mainLightColor;


    private void Start() {
        BindKernelHandles();
        InitializeBuffers();
        GenerateOceanMeshData();
        SetUpWaveGenerationMaterialBuffers();
    }

    private void Update() {
        DispatchSimulateWaves();
        if(Input.GetKeyDown(KeyCode.Space)) {
            CheckNormals();
        }
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
        numberOfVerticesPerSide = (1 << oceanSubdivisions) + 1;
        numberOfVertices = numberOfVerticesPerSide * numberOfVerticesPerSide; // Always create the same number of vertices in x and z direction
        Debug.Log($"The number of vertices in the ocean is {numberOfVertices}.");
        vertexBuffer = new ComputeBuffer(numberOfVertices, sizeof(float) * 3);

        // Set up the triangle buffer
        numberOfTriangleIndices = (numberOfVerticesPerSide - 1) * (numberOfVerticesPerSide - 1) * 6;
        triangleBuffer = new ComputeBuffer(numberOfTriangleIndices, sizeof(int));
        cpuTriangleIndices = new int[numberOfTriangleIndices];
        normalBuffer = new ComputeBuffer(numberOfTriangleIndices, sizeof(float) * 3);
        Debug.Log(numberOfTriangleIndices);
        cpuNormals = new Vector3[numberOfTriangleIndices];
        // Bind the buffers to the kernel handles
        computeShader.SetBuffer(kernelHandleFillVertices, "vertexBuffer", vertexBuffer);
        computeShader.SetBuffer(kernelHandleFillTriangles, "triangleBuffer", triangleBuffer);
        computeShader.SetBuffer(kernelHandleSimulateWaves, "vertexBuffer", vertexBuffer);
        computeShader.SetTexture(kernelHandleSimulateWaves, "noiseTexture", noiseTexture);
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
        mainLightDirection = RenderSettings.sun.transform.forward;
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
    }

    private void SetSimulateWaveParameters() {
        computeShader.SetFloat("waveAmplitude", waveAmplitude);
        computeShader.SetFloat("waveFrequency", waveFrequency);
        computeShader.SetVector("windDirection", AngleToDirection(windDirectionDegrees));
        computeShader.SetFloat("windStrength", windStrength);
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
        SetSimulateWaveParameters();
        SetUpWaveGenerationMaterialBuffers();

    }

    private void OnDestroy() {
        vertexBuffer.Dispose();
        triangleBuffer.Dispose();
        normalBuffer.Dispose();
    }

    private void CheckNormals() {
        normalBuffer.GetData(cpuNormals);
        Debug.Log($"Number of normals: {cpuNormals.Length}");
        for (int i = 0; i < cpuNormals.Length; i++) {
            Debug.Log(cpuNormals[i]);
        }

        triangleBuffer.GetData(cpuTriangleIndices);
        Debug.Log($"Number of triangle indices: {cpuTriangleIndices.Length}");
        for (int i = 0; i < cpuTriangleIndices.Length; i++) {
            Debug.Log(cpuTriangleIndices[i]);
        }
    }
}