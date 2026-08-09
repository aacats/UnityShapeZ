using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 使用 GPU Instancing 根据二维坐标批量绘制图片实例。
/// </summary>
public class GPUInstanceItems : MonoBehaviour
{
    [SerializeField] private BeltManager beltManager;
    [Header("Render")]
    public Texture2D spriteTexture;        // 要绘制的图片
    public Material instancedMaterial;     // 使用带 Instancing 的材质
    public Vector3 itemScale = Vector3.one;
    public float zOffset = 0f;
    [Header("Layer")]
    [Tooltip("设置这批实例的渲染层级，数值越大越靠前")]
    public int orderInLayer = 10;
    [Tooltip("用于让 Sprite 走更高的渲染队列，避免被同层物体遮挡")]
    public int renderQueue = 3000;
    public int LayerName = 0;

    [Header("Runtime")]
    private Mesh quadMesh;
    private Matrix4x4[] matrices;
    private readonly List<Vector2> positions = new List<Vector2>();

    private void Start()
    {
        Debug.Log("[GPUInstanceItems] 开始初始化实例化渲染。");

        if (quadMesh == null)
        {
            quadMesh = CreateQuadMesh();
        }

        if (instancedMaterial == null)
        {
            instancedMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        if (spriteTexture != null)
        {
            instancedMaterial.mainTexture = spriteTexture;
        }

        instancedMaterial.enableInstancing = true;
        instancedMaterial.renderQueue = renderQueue;
        instancedMaterial.SetInt("_SortingLayerID", 0);
        instancedMaterial.SetInt("_SortingOrder", orderInLayer);
        LayerName = LayerMask.NameToLayer("GPUInstance");
        Debug.Log($"[GPUInstanceItems] 已启用 GPU Instancing，OrderInLayer={orderInLayer}。");
    }

    private void Update()
    {
        SetPositions(); // 每帧更新位置列表
        if (positions.Count == 0)
        {
            return;
        }

        Debug.Log($"[GPUInstanceItems] 开始更新 {positions.Count} 个实例矩阵。");
        UpdateMatrices();
        Graphics.DrawMeshInstanced(quadMesh, 0, instancedMaterial, matrices, positions.Count);
        Debug.Log($"[GPUInstanceItems] 已调用 DrawMeshInstanced，实例数：{positions.Count}");
    }

    /// <summary>
    /// 外部脚本每帧调用，传入当前帧所有二维坐标。
    /// </summary>
    public void SetPositions()
    {

        if (beltManager == null)
        {
            Debug.LogError("[GPUInstanceItems] BeltManager 引用为空。");
            return;
        }

        List<Vector3> newPositions = beltManager.GetAllItemsPositions();
        positions.Clear();
        if (newPositions == null)
        {
            Debug.LogWarning("[GPUInstanceItems] BeltManager 返回了空坐标列表。");
            return;
        }

        foreach (var p in newPositions)
        {
            positions.Add(p);
        }

        if (positions.Count > 0)
        {
            Debug.Log($"[GPUInstanceItems] 第一个坐标：{positions[0]}");
        }
    }

    public void SetSinglePosition(Vector2 position)
    {
        positions.Clear();
        positions.Add(position);
    }

    private void UpdateMatrices()
    {
        if (matrices == null || matrices.Length < positions.Count)
        {
            matrices = new Matrix4x4[positions.Count];
            Debug.Log($"[GPUInstanceItems] 已分配 {matrices.Length} 个矩阵缓存。");
        }

        for (int i = 0; i < positions.Count; i++)
        {
            Vector2 p = positions[i];
            Vector3 worldPosition = new Vector3(p.x, p.y, zOffset);
            matrices[i] = Matrix4x4.TRS(worldPosition, Quaternion.identity, itemScale);
        }

        Debug.Log($"[GPUInstanceItems] 已更新 {positions.Count} 个实例矩阵。");
    }

    private Mesh CreateQuadMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3(0.5f, -0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f)
        };
        mesh.uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };
        mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateNormals();
        return mesh;
    }
}
