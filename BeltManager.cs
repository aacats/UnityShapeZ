using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 用来管理所有的传送带路径，包括动态添加、删除和修改相关BeltPath。
/// </summary>
public class BeltManager : MonoBehaviour
{
    [SerializeField] private PlaceManager placeManager;
    [SerializeField] private GridManager gridManager;
    public List<BeltPath> beltPaths = new List<BeltPath>();

    //放置传送带时检查传送带前后是否已经属于某个路径,再决定是创建新的路径还是加入已有的路径

    //删除时只有一个就删除路径，两个就调用BeltPath相关方法删掉目标，如果传送带路径内传送带个数大于等于三个就检查删除的传送带是起点、中间还是终点再决定

    void Start()
    {
        placeManager = FindObjectOfType<PlaceManager>();
        if (placeManager == null)
        {
            Debug.LogError("BeltManager: PlaceManager not found!");
        }
        gridManager = FindObjectOfType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogError("BeltManager: GridManager not found!");
        }
        placeManager.onSelectNum += OnPlaceBuilding;
    }

    private void OnPlaceBuilding(GameObject gameObject)
    {
        //检查放置的物体是否是传送带，如果是就调用AddBeltToPath方法
        if (gameObject == null)
        {
            Debug.Log("BeltManager: 放置的物体为空");
            return;
        }

        if (gameObject.GetComponent<BeltComponent>() != null)
        {
            Debug.Log("放置的物体是传送带: " + gameObject.name);
            AddBeltToPath(gameObject);
        }
        else
        {
            Debug.Log("放置的物体不是传送带: " + gameObject.name);
        }
    }

    void Update()
    {
        //更新所有的传送带路径
        foreach (var beltPath in beltPaths)
        {
            beltPath.BeltPathUpdate();
        }
    }

    //根据传送带是否有相邻传送带来决定是创建新的路径还是加入已有的路径
    private void AddBeltToPath(GameObject belt)
    {
        Debug.Log("BeltManager: 添加传送带" + belt.name);

        //先查找前后是否有传送带
        (GameObject, GameObject) frontBackBelt = FindBeltAtAdjacency(belt);
        GameObject frontBelt = frontBackBelt.Item1;
        GameObject backBelt = frontBackBelt.Item2;

        //如果物体不是传送带，赋值为null
        if (frontBelt != null && frontBelt.GetComponent<BeltComponent>() == null)
        {
            frontBelt = null;
        }
        if (backBelt != null && backBelt.GetComponent<BeltComponent>() == null)
        {
            backBelt = null;
        }

        //如果前面有传送带，加入前面传送带的路径
        if (frontBelt != null)
        {
            //找到前面(终点处)传送带的BeltPath
            BeltPath frontBeltPath = frontBelt.GetComponent<BeltComponent>().BeltPath;
            //将当前传送带加入到前面传送带的路径中
            frontBeltPath.ExtendByBeltOnBeginning(belt);
            Debug.Log("BeltManager: 将传送带" + belt.name + "加入到前面传送带" + frontBelt.name + "的路径中");

            //检查后面传送带是否属于不同的路径，如果是就合并路径
            if (backBelt != null)
            {
                BeltPath backBeltPath = backBelt.GetComponent<BeltComponent>().BeltPath;
                if (backBeltPath != frontBeltPath)
                {
                    //合并路径
                    frontBeltPath.ExtendByPathOnEnd(backBeltPath);
                    Debug.Log("BeltManager: 合并传送带路径，前面传送带" + frontBelt.name + "的路径和后面传送带" + backBelt.name + "的路径合并");
                    beltPaths.Remove(backBeltPath);
                }
            }
        }
        else if (backBelt != null)
        {
            //如果前面没有传送带，后面有传送带，就加入后面传送带的路径
            BeltComponent backBeltComponent = backBelt.GetComponent<BeltComponent>();
            BeltPath backBeltPath = backBeltComponent.BeltPath;
            backBeltPath.ExtendByBeltOnEnd(belt);
            Debug.Log("BeltManager: 将传送带" + belt.name + "加入到后面传送带" + backBelt.name + "的路径中");
        }
        else
        {
            //前后都没有传送带，创建新的路径
            Debug.Log("BeltManager: 创建新的传送带路径");
            BeltPath newBeltPath = new BeltPath(new List<GameObject> { belt.gameObject });
            beltPaths.Add(newBeltPath);
        }

    }

    ///根据传送带找到相邻的、可以相连的传送带（即使有传送带在其前后位置，但是它们的方向是相对的话，也不能返回GameObject）
    public (GameObject, GameObject) FindBeltAtAdjacency(GameObject belt)
    {
        GameObject frontBelt = null;
        GameObject backBelt = null;
        //先拿到BeltComponent
        BeltComponent beltComponent = belt.GetComponent<BeltComponent>();
        if (beltComponent == null)
        {
            return (null, null);
        }
        //拿到BeltType
        BeltType type = beltComponent.Type;

        //根据type和rotation找到前后目标相对格子位置
        (Vector2Int frontGridPosition, Vector2Int backGridPosition) = GetFrontBackGridPosition(type, belt.transform.eulerAngles.z);

        //根据GridManager、传送带本身的网格坐标和前后目标相对格子位置，找到前后目标格子坐标
        Vector2Int beltGridPosition = GridManager.WorldPositionToGridPosition(belt.transform.position);
        Vector2Int frontTargetGridPosition = beltGridPosition + frontGridPosition;
        Vector2Int backTargetGridPosition = beltGridPosition + backGridPosition;

        //根据前后目标格子坐标，找到前后目标格子上的物体
        frontBelt = gridManager.GetGameObjectAtGridPosition(frontTargetGridPosition);
        backBelt = gridManager.GetGameObjectAtGridPosition(backTargetGridPosition);

        //判断belt和frontBelt、backBelt是否可以相连，如果不可以就返回null
        //如果forntBelt的后面不为空、该物体是传送带，则可以相连
        if (frontBelt != null && frontBelt.GetComponent<BeltComponent>() != null)
        {
            // 进行进一步的连接判断
            BeltComponent frontBeltComponent = frontBelt.GetComponent<BeltComponent>();
            BeltType frontBeltType = frontBeltComponent.Type;
            Vector2Int itsBack = GetFrontBackGridPosition(frontBeltType, frontBelt.transform.eulerAngles.z).Item2;
            GameObject itsBackBelt = gridManager.GetGameObjectAtGridPosition(frontTargetGridPosition + itsBack);
            if (itsBackBelt != null)
            {
                if (itsBackBelt.GetInstanceID() != belt.GetInstanceID())
                {
                    Debug.Log("frontBeltID:" + frontBelt.GetInstanceID() + "的后方不是当前传送带" + belt.GetInstanceID() + "，不能相连");
                    frontBelt = null; // 前方传送带的后方不是当前传送带，不能相连
                }
            }
        }
        //如果backBelt的前面不为空、该物体是传送带，则可以相连
        if (backBelt != null && backBelt.GetComponent<BeltComponent>() != null)
        {
            // 进行进一步的连接判断
            BeltComponent backBeltComponent = backBelt.GetComponent<BeltComponent>();
            BeltType backBeltType = backBeltComponent.Type;
            Vector2Int itsFront = GetFrontBackGridPosition(backBeltType, backBelt.transform.eulerAngles.z).Item1;
            GameObject itsFrontBelt = gridManager.GetGameObjectAtGridPosition(backTargetGridPosition + itsFront);
            if (itsFrontBelt != null)
            {
                if (itsFrontBelt.GetInstanceID() != belt.GetInstanceID())
                {
                    Debug.Log("backBeltID:" + backBelt.GetInstanceID() + "的前方不是当前传送带" + belt.GetInstanceID() + "，不能相连");
                    backBelt = null; // 后方传送带的前方不是当前传送带，不能相连
                }
            }
        }

        return (frontBelt, backBelt);
    }

    /// <summary>
    /// 根据传送带类型和旋转角度，获取前后目标格子位置
    /// 前方格子位置是指传送带终点处的格子位置，后方格子位置是指传送带起点处的格子位置
    /// </summary>
    /// <param name="type"></param>
    /// <param name="rotation"></param>
    /// <returns></returns>
    public (Vector2Int, Vector2Int) GetFrontBackGridPosition(BeltType type, float rotation)
    {
        Vector2Int frontGridPosition = new Vector2Int();
        Vector2Int backGridPosition = new Vector2Int();

        //根据type和rotation计算前后目标格子位置
        switch (type)
        {
            case BeltType.top:
                //从下往上
                switch (rotation)
                {
                    case 0:
                        //前方格子在上方，后方格子在下方
                        frontGridPosition = new Vector2Int(0, 1);
                        backGridPosition = new Vector2Int(0, -1);
                        break;
                    case 90:
                        //前方格子在左方，后方格子在右方
                        frontGridPosition = new Vector2Int(-1, 0);
                        backGridPosition = new Vector2Int(1, 0);
                        break;
                    case 180:
                        //前方格子在下方，后方格子在上方
                        frontGridPosition = new Vector2Int(0, -1);
                        backGridPosition = new Vector2Int(0, 1);
                        break;
                    case 270:
                        //前方格子在右方，后方格子在左方
                        frontGridPosition = new Vector2Int(1, 0);
                        backGridPosition = new Vector2Int(-1, 0);
                        break;
                }
                break;
            case BeltType.left:
                switch (rotation)
                {
                    case 0:
                        //前方格子在左方，后方格子在下方
                        frontGridPosition = new Vector2Int(-1, 0);
                        backGridPosition = new Vector2Int(0, -1);
                        break;
                    case 90:
                        //前方格子在下方，后方格子在右方
                        frontGridPosition = new Vector2Int(0, -1);
                        backGridPosition = new Vector2Int(1, 0);
                        break;
                    case 180:
                        //前方格子在右方，后方格子在上方
                        frontGridPosition = new Vector2Int(1, 0);
                        backGridPosition = new Vector2Int(0, 1);
                        break;
                    case 270:
                        //前方格子在上方，后方格子在左方
                        frontGridPosition = new Vector2Int(0, 1);
                        backGridPosition = new Vector2Int(-1, 0);
                        break;
                }
                break;
            case BeltType.right:
                switch (rotation)
                {
                    case 0:
                        //前方格子在左方，后方格子在右方
                        frontGridPosition = new Vector2Int(1, 0);
                        backGridPosition = new Vector2Int(-1, 0);
                        break;
                    case 90:
                        //前方格子在上方，后方格子在下方
                        frontGridPosition = new Vector2Int(0, 1);
                        backGridPosition = new Vector2Int(0, -1);
                        break;
                    case 180:
                        //前方格子在右方，后方格子在左方
                        frontGridPosition = new Vector2Int(-1, 0);
                        backGridPosition = new Vector2Int(1, 0);
                        break;
                    case 270:
                        //前方格子在左方，后方格子在右方
                        frontGridPosition = new Vector2Int(0, -1);
                        backGridPosition = new Vector2Int(0, 1);
                        break;
                }
                break;
        }


        return (frontGridPosition, backGridPosition);
    }

    public int Debugfunc()
    {
        return beltPaths.Count;
    }
    public List<Vector3> GetAllItemsPositions()
    {
        List<Vector3> allItemsPositions = new List<Vector3>();
        allItemsPositions.Clear();
        foreach (var beltPath in beltPaths)
        {
            allItemsPositions.AddRange(beltPath.GetVector3sFromItems());
        }
        return allItemsPositions;
    }
    public int GetTotalItemCount()
    {
        int totalCount = 0;
        foreach (var beltPath in beltPaths)
        {
            totalCount += beltPath.itemsCount;
        }
        return totalCount;
    }

}
