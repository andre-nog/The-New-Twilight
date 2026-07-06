using System.Collections.Generic;
using UnityEngine;

public struct ItemTooltipData
{
    public string title;
    public Color titleColor;
    public string rarityLabel;
    public string slotLabel;
    public List<(string label, string value)> statRows;
    public string description;
}
