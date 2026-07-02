using System;
using System.Collections.Generic;
using UnityEngine;

namespace WutheringWaves
{
    // 单件物品的数据结构
    [Serializable]
    public class ItemData
    {
        public string itemId;          // 物品唯一ID（如 "weapon_001"）
        public string itemName;        // 物品显示名称（如 "流刃"）
        public string itemType;        // 物品类型（"Weapon" / "Material" / "Consumable" / "Relic"）
        public int quantity;           // 数量（材料类叠加，武器类通常是 1）
        public int rarity;             // 稀有度（1=普通, 2=精良, 3=稀有, 4=传说, 5=限定）
        public int level;              // 等级（武器/圣遗物类有等级，材料类为 0）
    }

    // 玩家的背包：包含一个物品列表
    [Serializable]
    public class InventoryData
    {
        public List<ItemData> items = new List<ItemData>();
    }
}