using System;
using UnityEngine;

public enum ItemType
{
    Shape,
    Color,
    Boolean
}

[Serializable]
public abstract class Item
{
    [SerializeField] private string key;
    public string Key => key;

    public abstract ItemType Type { get; }

    protected Item(string key)
    {
        this.key = key;
    }

    public abstract string GetCopyableKey();
    public abstract bool EqualsItem(Item other);
}

[Serializable]
public class ShapeItem : Item
{
    [SerializeField] private ShapeDefinition definition;
    public ShapeDefinition Definition => definition;

    public override ItemType Type => ItemType.Shape;

    public ShapeItem(string key, ShapeDefinition definition) : base(key)
    {
        this.definition = definition;
    }

    public override string GetCopyableKey()
    {
        return Key;
    }

    public override bool EqualsItem(Item other)
    {
        if (other is not ShapeItem shape)
        {
            return false;
        }

        return shape.Key == Key;
    }
}

[Serializable]
public class ColorItem : Item
{
    [SerializeField] private ShapeColor color;
    public ShapeColor Color => color;

    public override ItemType Type => ItemType.Color;

    public ColorItem(string key, ShapeColor color) : base(key)
    {
        this.color = color;
    }

    public override string GetCopyableKey()
    {
        return color.ToString().ToLower();
    }

    public override bool EqualsItem(Item other)
    {
        if (other is not ColorItem colorItem)
        {
            return false;
        }

        return colorItem.Color == Color;
    }
}