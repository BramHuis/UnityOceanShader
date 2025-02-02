using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class Ocean : MonoBehaviour
{
    [SerializeField] ComputeShader computeShader;
    [SerializeField] Material oceanMaterial;
    [SerializeField] OceanSO oceanSO;

    ComputeBuffer vertexBuffer;
    ComputeBuffer triangleBuffer;
    ComputeBuffer normalBuffer;
    ComputeBuffer atomicNormalBuffer;
    ComputeBuffer gerstnerWaveBuffer;
    ComputeBuffer positionsToRetrieveHeightFromBuffer;

    int kernelHandleFillVertices;
    int kernelHandleFillTriangles;
    int kernelHandleSimulateWaves;
    int kernelHandleCalculateNormals;
    int kernelHandleCalculateNormalsSmooth;
    int kernelHandleNormalizeNormalsSmooth;
    int kernelHandleRetrieveOceanHeight;

    int numberOfVertices;
    int numberOfTriangleIndices;
    uint dispatchGroupSizeX, dispatchGroupSizeY;

    float oceanWidth;
    float oceanLength;
    int numberOfVerticesPerSide;
    
    GerstnerWaveSO[] gerstnerWavesSO;
    GerstnerWave[] gerstnerWaves;

    Color shallowWaterColor; 
    Color deepWaterColor;
    float oceanColorBlendDepth;

    Color ambientLight;
    float ambientLightStrength;
    float specularHighlightShininess; 

    Color fresnelColor;
    float fresnelPower; 
    Vector3 mainLightDirection;
    Vector4 mainLightColor;

    float foamWidth;
    Color foamColor;

    bool applySmoothShading;
    [SerializeField] Transform[] transformsToRetrieveHeightFrom;
    Vector2[] pointsToRetrieveHeightFrom;
    int heightPointsArrayLength;


    private void Start() {
        OceanSOValidator.CacheOceansInScene(); // Only runs once even with multiple Ocean.cs scripts
        ChangeOceanSODataOnce();
        ChangeOceanSOData();
        BindKernelHandles();
        InitializeBuffers();
        GenerateOceanMeshData();
        SetUpWaveGenerationMaterialBuffers();
        
        
    }

    private void Update() {
        DispatchSimulateWaves();
        
        for (int i = 0; i < heightPointsArrayLength; i++) {
            Vector3 transformPosition = transformsToRetrieveHeightFrom[i].position;
            pointsToRetrieveHeightFrom[i] = new Vector2(transformPosition.x, transformPosition.z);
        }

        positionsToRetrieveHeightFromBuffer.SetData(pointsToRetrieveHeightFrom);
        computeShader.GetKernelThreadGroupSizes(kernelHandleFillVertices, out dispatchGroupSizeX, out _, out _);
        computeShader.Dispatch(kernelHandleRetrieveOceanHeight, Mathf.CeilToInt((float)heightPointsArrayLength / dispatchGroupSizeX), 1, 1);
        positionsToRetrieveHeightFromBuffer.GetData(pointsToRetrieveHeightFrom);

        for (int i = 0; i < heightPointsArrayLength; i++) {
            Vector3 position = transformsToRetrieveHeightFrom[i].position;
            position.y = pointsToRetrieveHeightFrom[i].x;
            transformsToRetrieveHeightFrom[i].position = position;
        }
    }

    void ChangeOceanSODataOnce() {
        applySmoothShading = oceanSO.applySmoothShading;
        oceanWidth = oceanSO.oceanWidth;
        oceanLength = oceanSO.oceanLength;
        numberOfVerticesPerSide = oceanSO.numberOfVerticesPerSide;
        gerstnerWavesSO = oceanSO.gerstnerWavesSO;
    }

    void ChangeOceanSOData() {
        shallowWaterColor = oceanSO.shallowWaterColor;
        deepWaterColor = oceanSO.deepWaterColor;
        oceanColorBlendDepth = oceanSO.oceanColorBlendDepth;
        ambientLight = oceanSO.ambientLight;
        ambientLightStrength = oceanSO.ambientLightStrength;
        specularHighlightShininess = oceanSO.specularHighlightShininess;
        fresnelColor = oceanSO.fresnelColor;
        fresnelPower = oceanSO.fresnelPower;
        foamWidth = oceanSO.foamWidth;
        foamColor = oceanSO.foamColor;
    }

    private void BindKernelHandles()
    {
        // Bind the kernel handles to the kernels
        kernelHandleFillVertices = computeShader.FindKernel("FillVertexBuffer");
        kernelHandleFillTriangles = computeShader.FindKernel("FillTriangleBuffer");
        kernelHandleSimulateWaves = computeShader.FindKernel("SimulateWaves");
        kernelHandleCalculateNormals = computeShader.FindKernel("CalculateNormals");
        kernelHandleCalculateNormalsSmooth = computeShader.FindKernel("CalculateNormalsSmooth");
        kernelHandleNormalizeNormalsSmooth = computeShader.FindKernel("NormalizeNormalsSmooth");
        kernelHandleRetrieveOceanHeight = computeShader.FindKernel("RetrieveOceanHeight");
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
        if (!applySmoothShading) {
            normalBuffer = new ComputeBuffer(numberOfTriangleIndices, sizeof(float) * 3);
        } else {
            normalBuffer = new ComputeBuffer(numberOfVertices, sizeof(float) * 3);
            atomicNormalBuffer = new ComputeBuffer(numberOfVertices * 3, sizeof(int));
        }
        
        heightPointsArrayLength = transformsToRetrieveHeightFrom.Length;
        computeShader.SetInt("heightPointsArrayLength", heightPointsArrayLength);
        pointsToRetrieveHeightFrom = new Vector2[heightPointsArrayLength];
        positionsToRetrieveHeightFromBuffer = new ComputeBuffer(heightPointsArrayLength, sizeof(float) * 2);
        

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
        computeShader.SetBuffer(kernelHandleRetrieveOceanHeight, "positionsToRetrieveHeightFromBuffer", positionsToRetrieveHeightFromBuffer);
        computeShader.SetBuffer(kernelHandleRetrieveOceanHeight, "vertexBuffer", vertexBuffer);
        if (!applySmoothShading) {
            computeShader.SetBuffer(kernelHandleCalculateNormals, "vertexBuffer", vertexBuffer);
            computeShader.SetBuffer(kernelHandleCalculateNormals, "triangleBuffer", triangleBuffer);
            computeShader.SetBuffer(kernelHandleCalculateNormals, "normalBuffer", normalBuffer);
        } else {
            computeShader.SetBuffer(kernelHandleCalculateNormalsSmooth, "vertexBuffer", vertexBuffer);
            computeShader.SetBuffer(kernelHandleCalculateNormalsSmooth, "triangleBuffer", triangleBuffer);
            computeShader.SetBuffer(kernelHandleCalculateNormalsSmooth, "normalBuffer", normalBuffer);
            computeShader.SetBuffer(kernelHandleCalculateNormalsSmooth, "atomicNormalBuffer", atomicNormalBuffer);
            computeShader.SetBuffer(kernelHandleNormalizeNormalsSmooth, "normalBuffer", normalBuffer);
            computeShader.SetBuffer(kernelHandleNormalizeNormalsSmooth, "atomicNormalBuffer", atomicNormalBuffer);
        }
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

        if (!applySmoothShading) {
            computeShader.GetKernelThreadGroupSizes(kernelHandleCalculateNormals, out dispatchGroupSizeX, out dispatchGroupSizeY, out _);    
            computeShader.Dispatch(kernelHandleCalculateNormals, Mathf.CeilToInt((float)(numberOfVerticesPerSide - 1) / dispatchGroupSizeX), Mathf.CeilToInt((float)(numberOfVerticesPerSide - 1) / dispatchGroupSizeY), 1);
        } else {
            computeShader.GetKernelThreadGroupSizes(kernelHandleCalculateNormalsSmooth, out dispatchGroupSizeX, out dispatchGroupSizeY, out _);    
            computeShader.Dispatch(kernelHandleCalculateNormalsSmooth, Mathf.CeilToInt((float)numberOfVerticesPerSide / dispatchGroupSizeX), Mathf.CeilToInt((float)numberOfVerticesPerSide / dispatchGroupSizeY), 1);
            computeShader.Dispatch(kernelHandleNormalizeNormalsSmooth, Mathf.CeilToInt((float)numberOfVerticesPerSide / dispatchGroupSizeX), Mathf.CeilToInt((float)numberOfVerticesPerSide / dispatchGroupSizeY), 1);
        }
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
        int pass = applySmoothShading ? 1 : 0;
        oceanMaterial.SetPass(pass);
        Graphics.DrawProceduralNow(MeshTopology.Triangles, numberOfTriangleIndices);
    }
     
    public void OnValidate() {
        SetUpWaveGenerationMaterialBuffers();
        if (oceanSO != null) {
            ChangeOceanSOData();
        }

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
        positionsToRetrieveHeightFromBuffer.Dispose();
        atomicNormalBuffer?.Dispose();
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