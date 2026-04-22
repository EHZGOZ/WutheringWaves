using System;

namespace WutheringWaves
{
    // 全局事件总线：用于模块间低耦合通信，避免系统之间直接硬引用
    public static class GameEvents
    {
        #region 事件定义
        // 玩家相关
        // 切人事件：参数为切出角色、切入角色
        public static event Action<CharacterContext, CharacterContext> OnCharacterSwitched;

        // 角色通用相关
        // 角色状态变化事件：参数为状态机、旧状态、新状态
        public static event Action<CharacterStateMachine, CharacterState, CharacterState> OnCharacterStateChanged;

        // 技能 UI 刷新事件：参数为当前角色攻击逻辑
        public static event Action<CharacterAttack> OnSkillUIStateChanged;

        // 体力变化事件：参数为体力组件、当前值、最大值、归一化值
        public static event Action<PlayerStamina, float, float, float> OnStaminaChanged;

        // 体力条显隐事件：参数为体力组件、是否显示
        public static event Action<PlayerStamina, bool> OnStaminaVisibilityChanged;

        // 角色专属：今汐
        // 御空状态变化事件：参数为攻击逻辑、是否处于御空
        public static event Action<CharacterAttack, bool> OnFloatingChanged;
        #endregion

        #region 事件派发

        #region 玩家相关
        // 派发切人事件：通知 UI、特效、小地图等外围系统同步当前角色
        public static void RaiseCharacterSwitched(CharacterContext previous, CharacterContext current)
        {
            OnCharacterSwitched?.Invoke(previous, current);
        }
        #endregion

        #region 角色通用相关
        // 派发角色状态变化事件
        public static void RaiseCharacterStateChanged(CharacterStateMachine source, CharacterState oldState, CharacterState newState)
        {
            OnCharacterStateChanged?.Invoke(source, oldState, newState);
        }

        // 派发技能 UI 刷新事件
        public static void RaiseSkillUIStateChanged(CharacterAttack source)
        {
            OnSkillUIStateChanged?.Invoke(source);
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

        #region 角色专属：今汐
        // 派发御空状态变化事件
        public static void RaiseFloatingChanged(CharacterAttack source, bool isFloating)
        {
            OnFloatingChanged?.Invoke(source, isFloating);
        }
        #endregion

        #endregion
    }
}
