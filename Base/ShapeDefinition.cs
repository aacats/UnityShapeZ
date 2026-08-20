using System;
using System.Collections.Generic;
using UnityEngine;

public enum SubShape
{
    Rect,
    Circle,
    Star,
    Windmill
}

public enum ShapeColor
{
    Uncolored,
    Red,
    Green,
    Blue,
    Yellow,
    Cyan,
    Purple,
    White
}

[Serializable]
public class ShapeSlot
{
    public SubShape subShape;
    public ShapeColor color;
}

[Serializable]
public class ShapeLayer
{
    public ShapeSlot[] slots = new ShapeSlot[4];
}

[Serializable]
public class ShapeDefinition
{
    public List<ShapeLayer> layers = new List<ShapeLayer>();

    public ShapeDefinition Clone()
    {
        var copy = new ShapeDefinition();
        foreach (var layer in layers)
        {
            var newLayer = new ShapeLayer();
            for (int i = 0; i < 4; i++)
            {
                if (layer.slots != null && i < layer.slots.Length && layer.slots[i] != null)
                {
                    newLayer.slots[i] = new ShapeSlot
                    {
                        subShape = layer.slots[i].subShape,
                        color = layer.slots[i].color
                    };
                }
            }
            copy.layers.Add(newLayer);
        }
        return copy;
    }
}