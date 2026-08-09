using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

// ============================================
// 轨道监视器 EditorWindow
// 将 BeltManager.beltPaths 作为轨道列表
// 每个 BeltPath 是一条轨道直线
// BeltPath.items 中的每个 item 是轨道上的小圆
// 小圆位置 = item 在整个 BeltPath 上的进度 (0~1)
// ============================================

public class BeltMonitorWindow : EditorWindow
{
    private BeltManager targetManager;
    private Vector2 scrollPosition;

    // 布局参数
    private const float TRACK_ROW_HEIGHT = 90f;
    private const float TRACK_LINE_MARGIN = 50f;
    private const float TRACK_LINE_THICKNESS = 4f;
    private const float CIRCLE_RADIUS = 8f;
    private const float TICK_COUNT = 5;          // 刻度数量

    [MenuItem("Tools/传送带轨道监视器")]
    public static void ShowWindow()
    {
        BeltMonitorWindow window = GetWindow<BeltMonitorWindow>();
        window.titleContent = new GUIContent("传送带监视器");
        window.minSize = new Vector2(500, 350);
    }

    private void OnEnable()
    {
        // 窗口打开时自动查找
        AutoFindBeltManager();
    }

    private void OnFocus()
    {
        // 窗口获得焦点时自动查找
        AutoFindBeltManager();
    }

    private void AutoFindBeltManager()
    {
        // 1. 先尝试从选中对象上找
        if (Selection.activeGameObject != null)
        {
            var mgr = Selection.activeGameObject.GetComponent<BeltManager>();
            if (mgr != null)
            {
                targetManager = mgr;
                return;
            }
        }

        // 2. 选中对象上没有，全局查找
        targetManager = FindObjectOfType<BeltManager>();
        if (targetManager != null)
        {
            Repaint();
        }
    }
    private void OnGUI()
    {
        // --- 顶部工具栏 ---
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        EditorGUILayout.LabelField("BeltManager:", GUILayout.Width(80));
        targetManager = (BeltManager)EditorGUILayout.ObjectField(
            targetManager, typeof(BeltManager), true, GUILayout.Width(200));
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(50)))
        {
            Repaint();
        }
        EditorGUILayout.EndHorizontal();

        if (targetManager == null)
        {
            EditorGUILayout.HelpBox(
                "请将场景中的 BeltManager 拖到上方字段中", MessageType.Info);
            return;
        }

        // --- 统计信息 ---
        int totalPaths = targetManager.beltPaths.Count;
        int totalItems = targetManager.GetTotalItemCount();
        EditorGUILayout.LabelField(
            $"  轨道数: {totalPaths}    |    物品总数: {totalItems}",
            EditorStyles.miniLabel);

        // --- 滚动区域 ---
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        for (int i = 0; i < targetManager.beltPaths.Count; i++)
        {
            DrawTrackRow(targetManager.beltPaths[i], i);
            GUILayout.Space(8);
        }

        EditorGUILayout.EndScrollView();

        // 持续刷新
        Repaint();
    }

    // ============================================
    // 绘制一行轨道
    // ============================================
    private void DrawTrackRow(BeltPath beltPath, int index)
    {
        Rect rowRect = GUILayoutUtility.GetRect(
            GUIContent.none, GUIStyle.none, GUILayout.Height(TRACK_ROW_HEIGHT));

        // 背景
        EditorGUI.DrawRect(rowRect, new Color(0.12f, 0.12f, 0.14f, 0.7f));

        // --- 左侧：轨道名称 ---
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
        };
        Rect titleRect = new Rect(rowRect.x + 6, rowRect.y + 4, 120, 18);
        EditorGUI.LabelField(titleRect, $"BeltPath [{index}]", titleStyle);

        // 物品数量
        GUIStyle countStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
        };
        Rect countRect = new Rect(rowRect.x + 6, rowRect.y + 22, 120, 14);
        EditorGUI.LabelField(countRect, $"items: {beltPath.itemsCount}", countStyle);

        // --- 轨道直线的 Y 坐标 ---
        float lineY = rowRect.y + rowRect.height * 0.6f;
        float lineStartX = rowRect.x + TRACK_LINE_MARGIN;
        float lineEndX = rowRect.x + rowRect.width - TRACK_LINE_MARGIN * 0.5f;
        float lineWidth = lineEndX - lineStartX;

        // --- 绘制轨道直线 ---
        Rect lineRect = new Rect(
            lineStartX, lineY - TRACK_LINE_THICKNESS / 2f,
            lineWidth, TRACK_LINE_THICKNESS);
        EditorGUI.DrawRect(lineRect, new Color(0.35f, 0.35f, 0.40f, 0.9f));

        // --- 绘制刻度 ---
        DrawTickMarks(lineStartX, lineEndX, lineY, beltPath);

        // --- 绘制每个物品的小圆 ---
        DrawItemsOnTrack(lineStartX, lineEndX, lineY, beltPath);

        // --- 右侧：总长度 ---
        GUIStyle lenStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { textColor = new Color(0.5f, 0.7f, 0.5f) },
            alignment = TextAnchor.MiddleRight
        };
        Rect lenRect = new Rect(lineEndX - 60, rowRect.y + 4, 60, 14);
        // 用反射访问 totalLength（private 字段）
        float totalLen = GetTotalLength(beltPath);
        EditorGUI.LabelField(lenRect, $"L:{totalLen:F2}", lenStyle);
    }

    // ============================================
    // 绘制刻度标记
    // ============================================
    private void DrawTickMarks(float startX, float endX, float lineY, BeltPath beltPath)
    {
        float lineWidth = endX - startX;
        float totalLen = GetTotalLength(beltPath);

        GUIStyle tickStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.55f, 0.55f, 0.55f) }
        };

        for (int i = 0; i <= TICK_COUNT; i++)
        {
            float t = (float)i / TICK_COUNT;
            float x = startX + t * lineWidth;

            // 刻度竖线
            Rect tickRect = new Rect(x - 1, lineY - 7, 2, 14);
            EditorGUI.DrawRect(tickRect, new Color(0.5f, 0.5f, 0.5f, 0.5f));

            // 刻度文字（显示实际距离）
            float distance = t * totalLen;
            Rect labelR = new Rect(x - 20, lineY + 10, 40, 14);
            EditorGUI.LabelField(labelR, distance.ToString("F1"), tickStyle);
        }
    }

    // ============================================
    // 绘制轨道上的所有物品（小圆）
    // ============================================
    private void DrawItemsOnTrack(float startX, float endX, float lineY, BeltPath beltPath)
    {
        float lineWidth = endX - startX;
        float totalLen = GetTotalLength(beltPath);

        if (totalLen <= 0f) return;

        // 获取 spacingToFirstItem
        float spacingToFirst = GetSpacingToFirstItem(beltPath);

        // 获取 items 列表
        var items = GetItems(beltPath);
        if (items == null || items.Count == 0) return;

        // 累计进度
        float accumulatedProgress = spacingToFirst;

        for (int i = 0; i < items.Count; i++)
        {
            var (distanceToNext, item) = items[i];

            // 计算该物品在整条轨道上的进度 (0~1)
            float progress = accumulatedProgress / totalLen;
            // 限制在 0~1 范围内（可能在轨道的起点之前或终点之后）
            progress = Mathf.Clamp01(progress);

            float circleX = startX + progress * lineWidth;

            // 为每个物品分配颜色
            Color itemColor = GetItemColor(i, items.Count);

            // 如果物品在轨道范围外（进度<0或>1），用半透明显示
            if (accumulatedProgress < 0 || accumulatedProgress > totalLen)
            {
                itemColor.a = 0.3f;
            }

            // 绘制实心圆
            DrawFilledCircle(new Vector2(circleX, lineY), CIRCLE_RADIUS, itemColor);

            // 物品名称（上方）
            GUIStyle nameStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = itemColor }
            };
            string itemName = item != null ? item.GetCopyableKey() : $"item[{i}]";
            Rect nameRect = new Rect(circleX - 35, lineY - CIRCLE_RADIUS - 22, 70, 14);
            EditorGUI.LabelField(nameRect, itemName, nameStyle);

            // 进度数值（下方）
            GUIStyle valStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
            };
            Rect valRect = new Rect(circleX - 30, lineY + CIRCLE_RADIUS + 4, 60, 14);
            EditorGUI.LabelField(valRect, progress.ToString("F3"), valStyle);

            // 累加进度
            accumulatedProgress += distanceToNext;
        }
    }

    // ============================================
    // 绘制实心圆
    // ============================================
    private void DrawFilledCircle(Vector2 center, float radius, Color color)
    {
        Handles.BeginGUI();
        Handles.color = color;

        int segments = 32;
        Vector3 center3 = new Vector3(center.x, center.y, 0);

        for (int i = 0; i < segments; i++)
        {
            float a1 = (float)i / segments * Mathf.PI * 2f;
            float a2 = (float)(i + 1) / segments * Mathf.PI * 2f;

            Vector3 p1 = center3 + new Vector3(Mathf.Cos(a1) * radius, Mathf.Sin(a1) * radius, 0);
            Vector3 p2 = center3 + new Vector3(Mathf.Cos(a2) * radius, Mathf.Sin(a2) * radius, 0);

            Handles.DrawAAConvexPolygon(center3, p1, p2);
        }

        Handles.color = Color.white;
        Handles.EndGUI();
    }

    // ============================================
    // 颜色分配
    // ============================================
    private Color GetItemColor(int index, int total)
    {
        // 在色相环上均匀分布
        float hue = (float)index / Mathf.Max(total, 1);
        return Color.HSVToRGB(hue, 0.85f, 0.9f);
    }

    // ============================================
    // 反射访问 BeltPath 的私有字段
    // ============================================

    private float GetTotalLength(BeltPath beltPath)
    {
        var field = typeof(BeltPath).GetField("totalLength",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);
        if (field != null)
            return (float)field.GetValue(beltPath);
        return 0f;
    }

    private float GetSpacingToFirstItem(BeltPath beltPath)
    {
        var field = typeof(BeltPath).GetField("spacingToFirstItem",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);
        if (field != null)
            return (float)field.GetValue(beltPath);
        return 0f;
    }

    private List<(float, Item)> GetItems(BeltPath beltPath)
    {
        var field = typeof(BeltPath).GetField("items",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);
        if (field != null)
            return field.GetValue(beltPath) as List<(float, Item)>;
        return null;
    }
}