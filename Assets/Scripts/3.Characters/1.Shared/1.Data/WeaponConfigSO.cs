using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WutheringWaves
{
    [System.Serializable]
    //御剑动画数据
    public class SwordAnimation
    {
        [Header("御剑动画总时长")]
        public float swordAnimationCostTime = 1f;
        [Header("御剑动画延迟")]
        public float DelayTime = 1f;
        [Header("是否有御剑动画")]
        public bool hasSwordAnimation = false;
        [Header("御剑对象（优先使用此处御剑对象，如为空使用通用御剑对象）")]
        public GameObject floatingSword;

        [Header("初始剑柄位置（相对于角色的本地位置/旋转）")]
        public Vector3 initSwordHandlePos = Vector3.zero;
        [Header("初始剑朝向（相对角色本地欧拉角）")]
        public Vector3 initSwordRotationEuler = Vector3.zero;
        public Quaternion InitSwordRotation => Quaternion.Euler(initSwordRotationEuler);

        [Header("轨迹列表（逐段飞行）")]
        public List<SwordTrajectory> swordTrajectory = new List<SwordTrajectory>();
    }

    [System.Serializable]
    //单段御剑轨迹
    public class SwordTrajectory
    {
        [Header("本段轨迹剑柄最终位置（相对角色本地坐标）")]
        public Vector3 targeSwordHandlelPos = Vector3.zero;
        [Header("本段轨迹剑最终朝向（相对角色本地欧拉角）")]
        public Vector3 targetSwordRotationEuler = Vector3.zero;
        public Quaternion TargetSwordRotation => Quaternion.Euler(targetSwordRotationEuler);
        [Header("时间权重/权重越大，耗时越长")]
        public float timeWeight = 1f;
    }

    [System.Serializable]
    //武器动作配置
    public class WeaponActionConfig
    {
        [Header("攻击步骤ID")]
        public AttackId attackId = AttackId.None;

        [Header("武器表现配置")]
        public SwordAnimation swordAnimation = new SwordAnimation();

        public override string ToString()
        {
            return $"武器动作：{attackId}";
        }
    }

    [CreateAssetMenu(menuName = "WutheringWaves/WeaponConfig", fileName = "WeaponConfig", order = 3)]
    //武器配置：负责维护通用武器资源与攻击步骤到武器表现的映射
    public class WeaponConfigSO : ScriptableObject
    {
        [Header("===角色信息===")]
        [SerializeField] internal CharacterName characterName;
        [Header("===通用武器配置===")]
        [Tooltip("当动作没有单独覆盖时，默认使用的武器预制体")]
        [SerializeField] internal GameObject floatingSword;

        [Tooltip("武器长度，当前用于调试与后续扩展")]
        [SerializeField] internal float swordLength = 1f;

        [Tooltip("所有武器动作统一附加的本地位置偏移")]
        [SerializeField] internal Vector3 swordPositionOffset = Vector3.zero;

        [Tooltip("所有武器动作统一附加的本地旋转偏移")]
        [SerializeField] internal Vector3 swordRotationOffset = Vector3.zero;

        [Header("===武器动作列表===")]
        [Tooltip("按 AttackId 映射不同攻击步骤对应的武器表现")]
        [SerializeField] internal List<WeaponActionConfig> weaponActions = new List<WeaponActionConfig>();

        //根据攻击步骤ID获取对应的武器动作配置
        public WeaponActionConfig GetWeaponActionConfig(AttackId attackId)
        {
            return weaponActions.FirstOrDefault(config => config != null && config.attackId == attackId);
        }
    }
}
