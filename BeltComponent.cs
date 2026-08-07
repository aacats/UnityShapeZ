using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 将物体Belt和BeltComponent区分开
/// </summary>
public class BeltComponent : MonoBehaviour
{
    /// <summary>
    /// 传送带的方向类型,是左转弯还是右转弯还是直线
    /// </summary>
    private BeltType type;

    /// <summary>
    /// Belt归属于哪个BeltPath
    /// </summary>
    private BeltPath beltPath;

    public BeltType Type
    {
        get => type;
        set => type = value;
    }
    public BeltPath BeltPath
    {
        get => beltPath;
        set => beltPath = value;
    }
    public float GetEffectiveLength()
    {
        //判断传送带是否是直线传送带，如果是就返回1，否则返回四分之Π
        return type == BeltType.top ? 1.0f : Mathf.PI / 4;
    }
    // 函数：把在Belt上的线性进度转换成该段的局部坐标(直线段是简单插值,弯道段用三角函数算圆弧插值),最后再转换到世界坐标
    public Vector3 GetLocalPosition(float localProgress)
    {
        // 这里需要根据传送带的类型和形状来计算局部坐标
        // 简单示例，实际实现会更复杂
        if (type == BeltType.top)
        {
            // 弯道传送带，使用圆弧插值
            return new Vector3(Mathf.Sin(localProgress * Mathf.PI / 2), 0, Mathf.Cos(localProgress * Mathf.PI / 2));
        }
        else
        {
            // 直线传送带，使用简单插值
            return new Vector3(localProgress, 0, 0);
        }
    }
    /// <summary>
    /// 将局部进度转换为局部坐标的函数
    /// localProgress是该物体从该传送带的起点沿着传送带走了多远的距离,范围是0~1
    /// </summary>
    /// <param name="localProgress"></param>
    public Vector2 LocalProgressToLocalPosition(float localProgress)
    {
        localProgress = Mathf.Clamp01(localProgress);

        if (type == BeltType.top)//从下往上
        {
            return new Vector2(0, localProgress - 0.5f);
        }
        else if (type == BeltType.left)//从下往左
        {
            //角度进度
            float arcProgress = localProgress * Mathf.PI / 2;
            return new Vector2(0.5f * Mathf.Cos(arcProgress) - 0.5f, 0.5f * Mathf.Sin(arcProgress) - 0.5f);
        }
        else if (type == BeltType.right)//从下往右
        {
            //角度进度
            float arcProgress = localProgress * Mathf.PI / 2;
            return new Vector2(0.5f - 0.5f * Mathf.Cos(arcProgress) - 0.5f, 0.5f * Mathf.Sin(arcProgress) - 0.5f);
        }
        else
        {
            //默认返回0,0
            Debug.LogWarning("BeltComponent.LocalProgressToLocalPosition: 未知的传送带类型，无法计算局部坐标");
            return Vector2.zero;
        }
    }

}
