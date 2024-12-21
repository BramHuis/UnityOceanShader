using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ocean : MonoBehaviour
{
    //require component toevoegen meshrenderer enzo
    [SerializeField] ComputeShader computeShader;
    [SerializeField] Material oceanMaterial;

    ComputeBuffer vertexBuffer;
    ComputeBuffer triangleBuffer;
    int kernelHandleFillVertices;
    int kernelHandleFillTriangles;
    int kernelHandleSimulateWaves;

    Vector3[] cpuVertices;
    int[] cpuTriangles;
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


    private void Start() {
        Init();
    }

    private void Update() {
        computeShader.SetFloat("time", Time.time);
        computeShader.Dispatch(kernelHandleSimulateWaves, Mathf.CeilToInt((float)oceanWidth / dispatchGroupSizeX), Mathf.CeilToInt((float)oceanLength / dispatchGroupSizeY), 1);
    }

    private void Init() {
        numberOfVertices = oceanWidth * oceanLength;
        vertexBuffer = new ComputeBuffer(numberOfVertices, sizeof(float) * 3);
        cpuVertices = new Vector3[numberOfVertices];

        numberOfTriangleIndices = (oceanWidth - 1) * (oceanLength - 1) * 6;
        triangleBuffer = new ComputeBuffer(numberOfTriangleIndices, sizeof(int));
        cpuTriangles = new int[numberOfTriangleIndices];

        kernelHandleFillVertices = computeShader.FindKernel("FillVertexBuffer");
        kernelHandleFillTriangles = computeShader.FindKernel("FillTriangleBuffer");
        kernelHandleSimulateWaves = computeShader.FindKernel("SimulateWaves");

        computeShader.SetBuffer(kernelHandleFillVertices, "vertexBuffer", vertexBuffer);
        computeShader.SetBuffer(kernelHandleFillTriangles, "triangleBuffer", triangleBuffer);
        computeShader.SetBuffer(kernelHandleSimulateWaves, "vertexBuffer", vertexBuffer);
        computeShader.SetFloat("vertexSpread", vertexSpread);
        computeShader.SetInt("oceanWidthVertexCount", oceanWidth);
        computeShader.SetInt("oceanLengthVertexCount", oceanLength);

        computeShader.GetKernelThreadGroupSizes(kernelHandleFillVertices, out dispatchGroupSizeX, out dispatchGroupSizeY, out _);
        computeShader.Dispatch(kernelHandleFillVertices, Mathf.CeilToInt((float)oceanWidth / dispatchGroupSizeX), Mathf.CeilToInt((float)oceanLength / dispatchGroupSizeY), 1);
        computeShader.GetKernelThreadGroupSizes(kernelHandleFillTriangles, out dispatchGroupSizeX, out dispatchGroupSizeY, out _);
        computeShader.Dispatch(kernelHandleFillTriangles, Mathf.CeilToInt((float)(oceanWidth - 1) / dispatchGroupSizeX), Mathf.CeilToInt((float)(oceanLength - 1) / dispatchGroupSizeY), 1);
        
        // Set the compute buffers to the vert/frag shaders
        oceanMaterial.SetBuffer("vertexPositions", vertexBuffer);
        oceanMaterial.SetBuffer("triangleIndices", triangleBuffer);

        // Set the threadgroupsizes for the simulate waves kernel
        computeShader.GetKernelThreadGroupSizes(kernelHandleSimulateWaves, out dispatchGroupSizeX, out dispatchGroupSizeY, out _);

        // vertexBuffer.GetData(cpuVertices);
        // triangleBuffer.GetData(cpuTriangles);

        // for (int i = 0; i < numberOfVertices; i++) { 
        //     Debug.Log(cpuVertices[i]);
        // }

        // Mesh mesh = new Mesh();
        // mesh.vertices = cpuVertices;
        // mesh.triangles = cpuTriangles;
        // MeshFilter meshFilter = GetComponent<MeshFilter>();
        // meshFilter.mesh = mesh;
    }

    private void OnRenderObject() {
        oceanMaterial.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Triangles, numberOfTriangleIndices);
    }
    
    private void OnDestroy() {
        vertexBuffer.Dispose();
        triangleBuffer.Dispose();
    }
}
