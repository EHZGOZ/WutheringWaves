using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WutheringWaves
{
    [CreateAssetMenu(menuName = "SoulLike/CharacterData",  fileName = "CharacterData",order = 0)]                 
         
    public class CharacterData : ScriptableObject
    {
        #region 静态配置

        #region 角色信息
        [Header("角色信息")]
        [Tooltip("角色名称")]
        [SerializeField] public string characterName = "None";
        #endregion

        #region 攻击力相关
        [Header("攻击力")]
        [Tooltip("基础攻击力")]
        [SerializeField] public float baseAttack = 10f;
        #endregion

        #region 生命值相关
        [Header("生命值")]
        [Tooltip("基础最大生命值")]
        [SerializeField] public float maxHealth = 100f;
        #endregion

        #region 耐力系统
        [Header("耐力系统")]
        [Tooltip("基础最大耐力值")]
        [SerializeField] public float maxStamina = 200f;
        [Tooltip("跑步时每秒消耗的耐力值")]
        [SerializeField] public float staminaCostInRun = 2f;
        [Tooltip("静止时每秒恢复的耐力值")]
        [SerializeField] public float staminaRecovery= 20f;
        [Tooltip("耐力恢复延迟时间(秒)")]
        [SerializeField] public float staminaRecoveryDelay = 0.5f;
        #endregion

        #region 共鸣技能
        [Header("共鸣技能")]
        [Tooltip("共鸣技能CD")]
        [SerializeField] public float resonanceSkillCD = 10f;
        [Tooltip("共鸣技能倍率")]
        [SerializeField] public float resonanceSkillMagnification = 3f;
        #endregion

        #region 共鸣解放
        [Header("共鸣解放")]
        [Tooltip("共鸣解放CD")]
        [SerializeField] public float resonanceOutbreakCD = 10f;
        [Tooltip("共鸣技能倍率")]
        [SerializeField] public float resonanceOutbreakMagnification = 5f;
        #endregion

        #endregion

        #region 动态存档

        #region 攻击力相关
        [Header("当前攻击力")]
        [SerializeField] public float currentAttack;
        #endregion

        #region 生命值相关
        [Header("当前生命值")]
        [SerializeField] public float currentHealth;
        #endregion

        #region 耐力系统
        [Header("当前耐力值")]
        [SerializeField] public float currentStamina;
        [Tooltip("距离上次耐力使用时间")]
        [SerializeField] public float lastStaminaCostTime;
        [Tooltip("是否可以回复耐力")]
        [SerializeField] public float canRecoveringStamina;
        #endregion

        #region 共鸣技能

        #endregion

        #region 共鸣解放

        #endregion

        #endregion

        #region 初始化
        public void Initialize()
        {

        }
        #endregion

        #region 具体逻辑

        #region 耐力系统
        // 消耗耐力
        public void ConsumeStamina(float staminacost)
        {
            if (staminacost <= 0) return;
            currentStamina = Mathf.Clamp(currentStamina - staminacost, 0, maxStamina);
        }

        // 恢复耐力
        public void RecoverStamina(float recover)
        {
            if (recover <= 0) return;
            currentStamina = Mathf.Clamp(currentStamina + recover, 0, maxStamina);
        }
        #endregion

        #endregion

    }

}

