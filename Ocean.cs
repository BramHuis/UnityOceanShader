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

    int kernelHandleFillVertices;
    int kernelHandleFillTriangles;
    int kernelHandleSimulateWaves;
    int kernelHandleCalculateNormals;


    int numberOfVertices;
    int numberOfTriangleIndices;
    uint dispatchGroupSizeX, dispatchGroupSizeY;

    [Header("Ocean mesh parameters")]
    [Tooltip("Ocean mesh width"), Range(2, 5000), SerializeField]
    int oceanWidth;
    [Tooltip("Ocean mesh length"), Range(2, 5000), SerializeField]
    int oceanLength;
    [Tooltip("Ocean mesh length"), Range(0.1f, 10), SerializeField]
    float vertexSpread;

    [Header("Ocean wave parameters")]
    [Tooltip("Wave amplitude"), Range(0.0f, 50.0f), SerializeField]
    float waveAmplitude;

    [Header("Ocean property parameters")]
    [Tooltip("Shallow color"), SerializeField]
    Color shallowWaterColor;
    [Tooltip("Deep color"), SerializeField]
    Color deepWaterColor;
    [Tooltip("Ocean color blend depth"), Range(0.1f, 200f), SerializeField]
    float oceanColorBlendDepth;
    

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
        numberOfVertices = oceanWidth * oceanLength;
        vertexBuffer = new ComputeBuffer(numberOfVertices, sizeof(float) * 3);

        // Set up the triangle buffer
        numberOfTriangleIndices = (oceanWidth - 1) * (oceanLength - 1) * 6;
        triangleBuffer = new ComputeBuffer(numberOfTriangleIndices, sizeof(int));

        normalBuffer = new ComputeBuffer(numberOfVertices, sizeof(float) * 3);

        // Bind the buffers to the kernel handles
        computeShader.SetBuffer(kernelHandleFillVertices, "vertexBuffer", vertexBuffer);
        computeShader.SetBuffer(kernelHandleFillTriangles, "triangleBuffer", triangleBuffer);
        computeShader.SetBuffer(kernelHandleSimulateWaves, "vertexBuffer", vertexBuffer);
        computeShader.SetTexture(kernelHandleSimulateWaves, "noiseTexture", noiseTexture);
        computeShader.SetBuffer(kernelHandleCalculateNormals, "vertexBuffer", vertexBuffer);
        computeShader.SetBuffer(kernelHandleCalculateNormals, "normalBuffer", normalBuffer);
    }

    private void GenerateOceanMeshData()
    {
        computeShader.SetInt("oceanWidthVertexCount", oceanWidth);
        computeShader.SetInt("oceanLengthVertexCount", oceanLength);
        computeShader.SetFloat("vertexSpread", vertexSpread);
        
        computeShader.GetKernelThreadGroupSizes(kernelHandleFillVertices, out dispatchGroupSizeX, out dispatchGroupSizeY, out _);
        computeShader.Dispatch(kernelHandleFillVertices, Mathf.CeilToInt((float)oceanWidth / dispatchGroupSizeX), Mathf.CeilToInt((float)oceanLength / dispatchGroupSizeY), 1);
        computeShader.GetKernelThreadGroupSizes(kernelHandleFillTriangles, out dispatchGroupSizeX, out dispatchGroupSizeY, out _);
        computeShader.Dispatch(kernelHandleFillTriangles, Mathf.CeilToInt((float)(oceanWidth - 1) / dispatchGroupSizeX), Mathf.CeilToInt((float)(oceanLength - 1) / dispatchGroupSizeY), 1);
    }
    
    private void SetUpWaveGenerationMaterialBuffers()
    {
        // Set the compute buffers to the vert/frag shaders
        oceanMaterial.SetBuffer("vertexPositions", vertexBuffer);
        oceanMaterial.SetBuffer("triangleIndices", triangleBuffer);
        oceanMaterial.SetBuffer("normalBuffer", normalBuffer);
        oceanMaterial.SetColor("shallowWaterColor", shallowWaterColor);
        oceanMaterial.SetColor("deepWaterColor", deepWaterColor);
        oceanMaterial.SetFloat("oceanColorBlendDepth", oceanColorBlendDepth);
    }

    private void SetSimulateWaveParameters() {
        computeShader.SetFloat("waveAmplitude", waveAmplitude);
    }

    private void DispatchSimulateWaves() {
        // Simulate the waves and afterwards recalculate the normals
        computeShader.SetFloat("time", Time.time);
        computeShader.GetKernelThreadGroupSizes(kernelHandleSimulateWaves, out dispatchGroupSizeX, out dispatchGroupSizeY, out _);
        computeShader.Dispatch(kernelHandleSimulateWaves, Mathf.CeilToInt((float)oceanWidth / dispatchGroupSizeX), Mathf.CeilToInt((float)oceanLength / dispatchGroupSizeY), 1);
    
        computeShader.GetKernelThreadGroupSizes(kernelHandleCalculateNormals, out dispatchGroupSizeX, out dispatchGroupSizeY, out _);    
        computeShader.Dispatch(kernelHandleCalculateNormals, Mathf.CeilToInt((float)oceanWidth / dispatchGroupSizeX), Mathf.CeilToInt((float)oceanLength / dispatchGroupSizeY), 1);
    }

    // Unity methods
    private void OnRenderObject() {
        oceanMaterial.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Triangles, numberOfTriangleIndices);
    }
    
    private void OnValidate() {
        SetSimulateWaveParameters();
    }

    private void OnDestroy() {
        vertexBuffer.Dispose();
        triangleBuffer.Dispose();
        normalBuffer.Dispose();
    }
}