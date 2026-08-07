using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

/// <summary>
/// 实现传送带路径的功能,计划支持动态调整
/// </summary>
public class BeltPath
{
    private GameManager gameManager;
    //Belt列表，从起点到终点的顺序排列
    [SerializeField] private List<GameObject> belts = new List<GameObject>();
    // 物体列表，DistanceToNext表示该物体到下一个物体或者终点的距离，Item表示该物体 
    [SerializeField] private List<(float DistanceToNext, Item item)> items = new List<(float DistanceToNext, Item item)>();

    /// <summary>
    /// 该整条传送带的总长度（实际长度）,由所有传送带的有效长度相加得到。
    /// 拐弯传送带的长度就不是1，而是Π/4（2Πr/4，r = 1/2）
    /// </summary>
    private float totalLength;

    /// <summary>
    /// 该整条传送带从起点沿着路径找到的第一个物体的距离,用于判断Item是否可以被Eject到传送带上
    /// </summary>
    private float spacingToFirstItem;

    /// <summary>
    /// 两个物体在传送带上的最小间距
    /// </summary>
    private float itemSpacingOnBelts;
    /// <summary>
    /// 基础传送带速度
    /// </summary>
    private float baseBeltSpeed;

    public float SpacingToFirstItem
    {
        get => spacingToFirstItem;
    }
    public int itemsCount
    {
        get => items.Count;
    }
    public float TotalLength
    {
        get => totalLength;
    }

    public event Action<float> itemsProgress;

    /// <summary>
    /// 初始化传送带路径
    /// </summary>
    public BeltPath(IEnumerable<GameObject> belts)
    {
        this.belts.AddRange(belts);
        Init();
    }
    void Init()
    {
        gameManager = GameObject.FindObjectOfType<GameManager>();

        //执行反向引用
        foreach (var belt in belts)
        {
            belt.GetComponent<BeltComponent>().BeltPath = this;
        }
        //根据Belts初始化空的items
        items.Clear();
        items = new List<(float DistanceToNext, Item item)>();
        //计算总长度
        totalLength = 0;
        foreach (var belt in belts)
        {
            totalLength += belt.GetComponent<BeltComponent>().GetEffectiveLength();
        }
        spacingToFirstItem = totalLength;

        itemSpacingOnBelts = gameManager.GetBaseItemSpacing();
        baseBeltSpeed = gameManager.GetBaseBeltSpeed();
    }
    public void BeltPathUpdate()
    {
        if (items.Count == 0)
        {
            return;
        }

        //举个例子，beltSpeed(每帧最多能推进的距离) 和 remainingDistance的关系就像是最大血量和当前血量的关系
        //物体通过消耗血量来推进距离，moveDistance就是这一帧该消耗的血量，
        //如果当前血量不足以消耗掉理论上该消耗的血量，就只能消耗掉当前血量，剩余血量为0，物体就不能再推进了
        //如果当前血量足够消耗掉理论上该消耗的血量，就消耗掉理论上的血量，剩余血量为当前血量减去理论消耗量

        //计算BeltSpeed : 每帧最多能推进的距离 = 基础传送带速度（每秒最多通过多少物品） * Time.deltaTime * 物体间距
        float beltSpeed = baseBeltSpeed * Time.deltaTime * itemSpacingOnBelts;
        //items该帧还剩下多少距离可以推进(remainingDistance是属于整个BeltPath的推进距离的)
        float remainingDistance = beltSpeed;
        //从终点向起点遍历物品
        for (int i = items.Count - 1; i >= 0; i--)
        {
            var (distanceToNext, item) = items[i];

            //根据是不是离终点最近的物体来决定跟前方的最小间距
            float minDistance = (i == items.Count - 1) ? 0f : itemSpacingOnBelts;

            //计算能推进的距离,先将其设定为理论上的最大推进距离
            float moveDistance = distanceToNext - minDistance;
            moveDistance = moveDistance <= 0f ? 0f : moveDistance;

            //如果剩余推进距离不足以实现理论上的最大推进距离，就将推进距离设定为剩余推进距离
            if (remainingDistance <= moveDistance)
            {
                moveDistance = remainingDistance;
            }
            if (moveDistance < 0)
            {
                Debug.LogError("出错了\n" + "当前remainingDistance: " + remainingDistance + "\n" + "当前moveDistance: " + moveDistance + "\n");
                moveDistance = 0;
            }

            remainingDistance -= moveDistance;

            //更新该物体的DistanceToNext
            float newDistanceToNext = distanceToNext - moveDistance;
            newDistanceToNext = newDistanceToNext < 0 ? 0 : newDistanceToNext;
            items[i] = (newDistanceToNext, item);

            //更新从起点到第一个物体的距离
            spacingToFirstItem += moveDistance;

            if (remainingDistance <= 1e-7)
            {
                break;
            }

        }
        //之前对remainingDistance的疑惑:
        /***
         * 如果第一个物体前方距离很大以至于他的DistanceToNext很大，这回导致它消耗完路径的所有remainingDistance
         * 这就意味着后面的物体都不能推进了，只有第一个物体在移动，这明显是不对的。
         */
        //对该问题的解答:
        /***
         * 问题出在了对于物体位置的理解上。在该套系统中，
         * 物体的位置的计算是通过spacingToFirstItem和DistanceToNext来计算的。
         * 当第一个物体消耗完了所有的remainingDistance，该物体的DistanceToNext就会变小，也就是向前移动了
         * 此时remainingDistance为0，触发了break语句，后面的物体就不会再在循环中参与计算了。
         * 也因此后面的物体的DistanceToNext并没有改变，这意味着它们与移动的第一个物体之间的距离没有改变。
         * 它们也跟随着第一个物体向前移动了
         * 一句话：物体位置的计算是相对的呀！！！从来都不是物体一个个移动，而是一堆物体一起移动
         */





        /*性能优化待实现：
        如果队首后面连续若干个物品都紧贴在最小间距上(彼此不可能再推进,因为被前车堵住),
        就把它们标记为"压缩态"直接跳过计算,
        类似 Unity 里对一整批贴在一起、相对速度为零的对象做批处理而非逐个算物理
        */

        //从终点开始计算推进物体
        //如果距离终点最近的物体距离终点的距离小于等于0，就调用函数（EjectItemFromBeltPath）将物体从传送带上弹出
        //末尾还会再检查一次末端物品是否恰好可以立即弹出,做收尾处理



    }

    //动态放置和删除传送带的函数，更新spacingToFirstItem和totalLength

    /// <summary>
    /// 将传送带加入到传送带路径的终点
    /// </summary>
    /// <param name="belt">要加入的传送带</param>
    public void ExtendByBeltOnEnd(GameObject belt)
    {
        //获取传送带的BeltComponent
        BeltComponent beltComp = belt.GetComponent<BeltComponent>();
        if (beltComp == null)
        {
            return;
        }
        //将传送带加入到路径的末尾，Add函数类似与Push函数。
        belts.Add(belt);
        //回填这段传送带所属的路径
        beltComp.BeltPath = this;
        //更新totalLength
        float additionalLength = beltComp.GetEffectiveLength();
        totalLength += additionalLength;

        //此处应该妥善处理spacingToFirstItem的更新
        if (items.Count == 0)
        {
            spacingToFirstItem = totalLength;
        }
        else
        {
            //如果传送带路径上有物体，就不需要更新spacingToFirstItem
            //但是需要更新items中最后一个物体的DistanceToNext
            items[items.Count - 1] = (items[items.Count - 1].Item1 + additionalLength, items[items.Count - 1].Item2);
        }
    }
    /// <summary>
    /// 将传送带加入到传送带路径的起点
    /// </summary>
    /// <param name="belt">要加入的传送带</param>
    public void ExtendByBeltOnBeginning(GameObject belt)
    {
        //获取传送带的BeltComponent
        BeltComponent beltComp = belt.GetComponent<BeltComponent>();
        if (beltComp == null)
        {
            return;
        }
        //将传送带加入到路径的开头
        belts.Insert(0, belt);
        //回填这段传送带所属的路径
        beltComp.BeltPath = this;
        // 更新spacingToFirstItem和totalLength
        float additionalLength = beltComp.GetEffectiveLength();
        spacingToFirstItem += additionalLength;
        totalLength += additionalLength;
    }
    /// <summary>
    /// 将另一个传送带路径加入到当前传送带路径的起点
    /// </summary>
    /// <param name="otherPath"></param>
    public void ExtendByPathOnEnd(BeltPath otherPath)
    {
        //避免环形
        if (otherPath == this)
        {
            Debug.LogError("不能将传送带路径加入到自身");
            return;
        }
        //将另一个路径的传送带加入到当前路径的末尾
        //或者说，将传送带的起点和另一个传送带的终点连接起来，形成一条新的传送带路径
        float oldLength = totalLength;
        for (int i = otherPath.belts.Count - 1; i >= 0; i--)
        {
            GameObject belt = otherPath.belts[i];
            BeltComponent beltComp = belt.GetComponent<BeltComponent>();
            belts.Insert(0, belt);
            beltComp.BeltPath = this;

            float additionalLength = beltComp.GetEffectiveLength();
            totalLength += additionalLength;
        }
        if (otherPath.items.Count != 0)
        {
            //另一个BeltPath终点的物体距离更新
            otherPath.items[otherPath.items.Count - 1] = (otherPath.items[otherPath.items.Count - 1].Item1 + spacingToFirstItem, otherPath.items[otherPath.items.Count - 1].Item2);
        }
        else
        {
            //另一个BeltPath没有物体，更新spacingToFirstItem
            spacingToFirstItem += otherPath.totalLength;
        }
        for (int i = otherPath.items.Count - 1; i >= 0; i--)
        {
            //将另一个路径的物体加入到当前路径的起点
            items.Insert(0, otherPath.items[i]);
        }
    }

    /// <summary>
    /// 移除传送带路径中的传送带以至于将原来的传送带路径一分为二
    /// </summary>
    /// <param name="belt"></param>
    public void DeleteEntityOnPathSplitIntoTwo(GameObject belt)
    {
        BeltComponent beltComp = belt.GetComponent<BeltComponent>();
        beltComp.BeltPath = null;
        float beltLength = beltComp.GetEffectiveLength();

        //确保传送带不属于两头
        if (belts[0] == belt || belts[belts.Count - 1] == belt)
        {
            Debug.LogError("传送带不应该属于两头");
            return;
        }
        //从起点开始，应该移除的传送带的前面一段传送带路径的 传送带 个数
        int firstPathEntityCount = 0;
        //应该移除的传送带的前面一段传送带路径的 传送带 长度
        float firstPathLength = 0;
        //从起点到终点，应该移除的传送带的前面一个传送带
        GameObject firstPathEndEntity = null;
        for (int i = 0; i < belts.Count; i++)
        {
            GameObject currentBelt = belts[i];
            if (currentBelt == belt)
            {
                break;
            }
            firstPathEntityCount++;
            firstPathEndEntity = currentBelt;
            firstPathLength += currentBelt.GetComponent<BeltComponent>().GetEffectiveLength();
        }
        //第二段传送带路径的长度
        float secondPathLength = totalLength - firstPathLength - beltLength;
        //第二段传送带路径的起点的在路径中的进度
        float secondPathStart = firstPathLength + beltLength;
        //第一段传送带路径的传送带列表
        List<GameObject> firstEntities = belts.Take(firstPathEntityCount).ToList();
        //第二段传送带路径的传送带列表
        List<GameObject> secondEntities = belts.Skip(firstPathEntityCount + 1).ToList();
        //移除传送带路径中的 目标传送带
        firstEntities.RemoveAt(firstEntities.Count - 1);
        belts = firstEntities;
        //创建新的传送带路径
        BeltPath SecondPath = new BeltPath(secondEntities);


        //差分物品
        for (int i = 0; i < items.Count; i++)
        {
            var (distanceToNext, item) = items[i];
            if (spacingToFirstItem >= firstPathLength)
            {
                //如果物体不位于当前传送带（原先的前半段传送带路径），就移除
                items.RemoveAt(i);
                i--;

                //判断物体是否位于新路径上，如果是就加入新路径
                if (spacingToFirstItem >= secondPathStart)
                {
                    //将物体加入新路径起点
                    SecondPath.items.Add((distanceToNext, item));
                    if (SecondPath.items.Count == 1)
                    {
                        SecondPath.spacingToFirstItem = spacingToFirstItem - secondPathStart;
                    }
                }
                else
                {
                    //物体不在新路径上，直接丢弃
                    Debug.LogWarning("物体不在新路径上，直接丢弃");
                }
            }
            else
            {
                //如果物体位于当前传送带（原先的前半段传送带路径），就保留
                //先获取这个物体现在最多能前进多少
                float maxDistance = Mathf.Min(spacingToFirstItem + distanceToNext, firstPathLength) - spacingToFirstItem;
                if (distanceToNext > maxDistance)
                {
                    //如果物体的距离大于最大距离，就更新物体的距离
                    items[i] = (maxDistance, item);
                }
            }
            spacingToFirstItem += distanceToNext;
        }
        totalLength = firstPathLength;
        if (items.Count == 0)
        {
            spacingToFirstItem = totalLength;
        }
    }



    // progress怎样获得？spacingToFirstItem + DistanceToNext得到从起点到该物体的总距离。
    // 再通过比较GetEffectiveLength()来判断该物体在哪段Belt上。
    // 再调用该 belt 的 GetLocalPosition 函数,将进度值转换为局部坐标,最后再转换到世界坐标
    // 目的：找到物体应该画在BeltPath上的哪段Belt上的哪个位置，返回其世界坐标以便于绘制物体
    public Vector3 GetWorldPositionFromProgress(float progress)
    {
        progress = Mathf.Clamp(progress, 0f, totalLength);

        //遍历传送带列表，先找到对应的传送带
        float currentLength = 0;
        for (int i = 0; i < belts.Count; i++)
        {
            BeltComponent belt = belts[i].GetComponent<BeltComponent>();
            float beltLength = belt.GetEffectiveLength();

            if (currentLength + beltLength >= progress)
            {
                //该物体从该传送带的起点沿着传送带走了多远的距离,范围是0~1
                float localProgress = Mathf.Clamp01((progress - currentLength) / beltLength);
                //调用该传送带的GetLocalPosition函数，将进度值转换为局部坐标
                Vector2 localPosition = belt.LocalProgressToLocalPosition(localProgress);
                //最后再转换到世界坐标
                //函数待实现
                Vector2 worldPosition = belts[i].transform.TransformPoint(localPosition);
                return worldPosition;
            }
            currentLength += beltLength;
        }
        return Vector3.zero;
    }
    public List<Vector3> GetVector3sFromItems()
    {
        List<Vector3> positions = new List<Vector3>();
        positions.Clear();
        float currentProgress = 0;
        for (int i = 0; i < items.Count; i++)
        {
            if (i == 0)
            {
                currentProgress = spacingToFirstItem;
            }
            else
            {
                currentProgress += items[i - 1].DistanceToNext;
            }
            Vector3 worldPosition = GetWorldPositionFromProgress(currentProgress);
            positions.Add(worldPosition);
        }

        return positions;
    }


    //ShapeZ代码中有事件：每当路径结构变化(建造/拆除/连接改变)时,重新计算这条路径末端物品要交给谁的问题
    //该函数待建


    /// <summary>
    /// 测试用添加物体
    /// </summary>
    public bool AddItemToBeltPath(Item item)
    {
        Debug.Log("SpacingToFirstItem: " + spacingToFirstItem);
        if (spacingToFirstItem < gameManager.GetMinSpaceingToFirstItem())
        {
            Debug.LogWarning("[BeltPath] 物体间距不足，无法添加物体到传送带路径上");
            return false;
        }
        //将物体加入到传送带路径的末尾
        items.Insert(0, (spacingToFirstItem, item));
        //更新spacingToFirstItem
        spacingToFirstItem = 0;

        return true;
    }

}