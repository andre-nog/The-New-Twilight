using System;
using System.Collections.Generic;
using UnityEngine;

// Define uma loja como dado puro — novas lojas são só novos assets, sem
// código novo. Estoque é infinito.
[CreateAssetMenu(fileName = "New Shop", menuName = "Shop/Shop")]
public class ShopSO : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public ItemSO item;
        public int price;
    }

    public string shopName = "Shop";
    public List<Entry> entries = new();

    // Preço de venda = floor(item.value * sellMultiplier).
    [Range(0f, 1f)]
    public float sellMultiplier = 0.5f;
}
