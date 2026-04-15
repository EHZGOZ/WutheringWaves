using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WutheringWaves
{
    [CreateAssetMenu(menuName = "WutheringWaves/CharacterDataSO",  fileName = "CharacterDataSO", order = 0)]                 
         
    public class CharacterDataSO : ScriptableObject
    {
        internal enum ChacterName
        {
            今汐,
            卡提希娅
        }

        #region 角色信息
        [Header("===角色信息===")]
        [SerializeField] internal ChacterName characterName;
        [Header("基础攻击力")]
        [SerializeField] internal float baseAttack = 100f;
        [Header("最大生命值")]
        [SerializeField] internal float maxHealth = 1000f;
        #endregion

        #region 战斗相关
        [Header("=== 战斗配置 ===")]
        [Tooltip("角色战斗配置（攻击段、技能段等）")]
        [SerializeField] internal CombatConfigSO combatConfig;
        
        #endregion

        #region 耐力系统
        [Header("==耐力相关==")]
        [Header("最大耐力值")]
        [SerializeField] internal float maxStaminaData = 100f;
        [Header("跑步时每秒消耗的耐力值")]
        [SerializeField] internal float staminaCostInRun = 5f;
        [Header("静止时每秒恢复的耐力值")]
        [SerializeField] internal float staminaRecovery= 10f;
        [Header("耐力恢复延迟时间(秒)")]
        [SerializeField] internal float staminaRecoveryDelay = 1f;
        #endregion

        #region 缓冲相关
        [Header("=== 急停缓冲配置 ===")]
        [Header("移动缓冲的总滑行距离（米）")]
        [SerializeField] internal float moveStoppingDistance = 0.7f;
        [Header("移动缓冲的总时长（秒）")]
        [SerializeField] internal float moveStoppingTime = 1f;
        [Header("奔跑缓冲的总滑行距离（米）")]
        [SerializeField] internal float runStoppingDistance = 1f;
        [Header("奔跑缓冲的总时长（秒）")]
        [SerializeField] internal float runStoppingTime = 1f;
        [Header("急停缓冲的总滑行距离（米）")]
        [SerializeField] internal float dashStoppingDistance = 1f;
        [Header("急停缓冲的总时长（秒）")]
        [SerializeField] internal float dashStoppingTime = 0.66f;
        #endregion

        #region 地面监测相关
        [Header("重力加速度")]
        [SerializeField] internal float gravity = -30f;
        [Header("坠落检测延迟时间（秒）")]
        [SerializeField] internal float fallCheckDelay = 0.3f;
        [Header("地面检测球体半径")]
        [SerializeField] internal float groundCheckRadius = 0.2f;
        [Header("地面检测起点偏移")]
        [SerializeField] internal float groundCheckOriginOffset = 0.2f;
        [Header("地面检测最大距离")]
        [SerializeField] internal float groundCheckDistance = 0.2f;
        #endregion

        #region 位移相关
        [Header("===位移相关===")]
        [Header("旋转平滑时间")]
        [SerializeField] internal float rotationSmoothTime = 0.1f;
        [Header("速度平滑过渡速度")]
        [SerializeField] internal float speedSmoothSpeed = 5f;
        [Header("移动输入阈值")]
        [SerializeField] internal float moveThreshold = 0.1f;
        [Header("移动速度")]
        [SerializeField] internal float moveSpeed = 4f;
        [Header("奔跑速度")]
        [SerializeField]internal float runSpeed = 6f;
        [Header("跳跃移动速度系数")]
        [SerializeField] internal float jumpAirControlMultiplier = 1f;
        [Header("下坠移动速度系数")]
        [SerializeField] internal float fallingAirControlMultiplier = 1f;
        [Header("冲刺耗时（不可打断）")]
        [SerializeField] internal float dashCostTime = 0.33f;
        [Header("连续两次冲刺的内置CD（秒")]
        [SerializeField] internal float dashInternalCD = 0.2f;
        [Header("全局CD阈值：两次冲刺间隔小于此值判定为'快速连冲'（秒）")]
        [SerializeField] internal float dashGlobalCDThreshold = 1.5f;
        [Header("全局CD惩罚时长：快速连冲触发的冷却时间（秒）")]
        [SerializeField] internal float dashGlobalCDPenalty = 1.3f;
        [Header("空中冲刺距离（米）")]
        [SerializeField] internal float airDashDistance = 5f;
        [Header("空中冲刺向上距离（米）")]
        [SerializeField] internal float airDashUpDistance = 2f;
        [Header("空中冲刺耗时（秒）")]
        [SerializeField] internal float airDashCostTime = 0.3f;
        [Header("御空冲刺耗时（不可打断）")]
        [SerializeField] internal float floatDashCostTime = 1.9f;
        
        #endregion


    }

}

