using System;

namespace WutheringWaves
{
    // 全局事件总线：用于模块间低耦合通信，避免系统之间直接硬引用
    public static class GameEvents
    {
        #region 事件定义
        // 游戏会话状态变化事件
        public static event Action<GameSessionState> OnGameSessionStateChanged;

        // 切人事件：参数为切出角色、切入角色
        public static event Action<CharacterContext, CharacterContext> OnCharacterSwitched;

        // 角色状态变化事件：参数为状态机、旧状态、新状态
        public static event Action<CharacterStateMachine, CharacterState, CharacterState> OnCharacterStateChanged;

        // 生命值变化事件：参数为角色上下文、当前值、最大值、归一化值
        public static event Action<CharacterContext, float, float, float> OnHealthChanged;

        #endregion

        #region 事件派发
        // 派发游戏会话状态变化
        public static void RaiseGameSessionStateChanged(GameSessionState state)
        {
            OnGameSessionStateChanged?.Invoke(state);
        }
        // 派发切人事件：通知 UI、特效、小地图等外围系统同步当前角色
        public static void RaiseCharacterSwitched(CharacterContext previous, CharacterContext current)
        {
            OnCharacterSwitched?.Invoke(previous, current);
        }

        // 派发角色状态变化事件
        public static void RaiseCharacterStateChanged(CharacterStateMachine stateMachine, CharacterState oldState, CharacterState newState)
        {
            OnCharacterStateChanged?.Invoke(stateMachine, oldState, newState);
        }
        // 派发生命值变化事件
        public static void RaiseHealthChanged(CharacterContext source, float current, float max, float normalized)
        {
            OnHealthChanged?.Invoke(source, current, max, normalized);
        }

        #endregion
    }
}
