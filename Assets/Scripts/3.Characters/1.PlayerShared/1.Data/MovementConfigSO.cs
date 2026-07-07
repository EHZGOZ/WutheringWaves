using UnityEngine;

namespace WutheringWaves
{
    [CreateAssetMenu(menuName = "WutheringWaves/MovementConfig", fileName = "MovementConfig", order = 2)]
    public class MovementConfigSO : ScriptableObject
    {
        [Header("===角色信息===")]
        [SerializeField] internal CharacterName characterName;

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
        [SerializeField] internal float runSpeed = 6f;
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
