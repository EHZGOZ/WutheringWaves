using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace WutheringWaves
{
    // 技能UI类型：用于告诉HUD当前刷新的是哪个技能槽
    public enum SkillUIType
    {
        ESkill, // E技能
        QBurst  // Q爆发
    }
    public class UIEvents : MonoBehaviour
    {
        #region 事件定义
        // 技能图标UI刷新事件：参数为角色上下文、技能类型、图标索引、剩余冷却时间
        public static event Action<CharacterContext, SkillUIType, int, float> OnSkillIconUIChanged;

        // 技能图标UI运行时刷新事件：参数为角色运行时数据、技能类型、图标索引、剩余冷却时间
        public static event Action<CharacterRuntimeData, SkillUIType, int, float> OnSkillIconUIRuntimeChanged;


        // 体力变化事件：参数为体力组件、当前值、最大值、归一化值
        public static event Action<PlayerStamina, float, float, float> OnStaminaChanged;

        // 体力条显隐事件：参数为体力组件、是否显示
        public static event Action<PlayerStamina, bool> OnStaminaVisibilityChanged;
        #endregion

        #region 事件派发
        // 派发技能图标UI刷新事件
        public static void RaiseSkillIconUIChanged(CharacterContext source, SkillUIType skillType, int iconIndex, float cooldownRemaining)
        {
            OnSkillIconUIChanged?.Invoke(source, skillType, iconIndex, cooldownRemaining);
        }
        // 派发技能图标UI运行时刷新事件
        public static void RaiseSkillIconUIRuntimeChanged(CharacterRuntimeData source, SkillUIType skillType, int iconIndex, float cooldownRemaining)
        {
            OnSkillIconUIRuntimeChanged?.Invoke(source, skillType, iconIndex, cooldownRemaining);
        }
        // 派发体力变化事件
        public static void RaiseStaminaChanged(PlayerStamina source, float current, float max, float normalized)
        {
            OnStaminaChanged?.Invoke(source, current, max, normalized);
        }

        // 派发体力条显隐事件
        public static void RaiseStaminaVisibilityChanged(PlayerStamina source, bool visible)
        {
            OnStaminaVisibilityChanged?.Invoke(source, visible);
        }
        #endregion


    }
}

