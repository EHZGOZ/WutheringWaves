using System;

namespace WutheringWaves
{
    // 技能UI类型：用于告诉HUD当前刷新的是哪个技能槽
    public enum SkillUIType
    {
        ESkill, // E技能
        QBurst  // Q爆发
    }
    // 外观事件类型：用于描述这次表现操作的类型
    public enum VisualEventType
    {
        ShowFade,        // 淡入显示
        HideFade,        // 淡出隐藏
        ShowInstantly,   // 立即显示
        HideInstantly    // 立即隐藏
    }

    // 外观目标类型：用于区分这次事件要作用到哪个表现对象
    public enum VisualTargetType
    {
        DecorationSword, // 装饰剑
        BattleWeapon,    // 战斗武器
        DragonHorn       // 龙角
    }


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

        // 生命值变化事件：参数为角色上下文、当前值、最大值、归一化值
        public static event Action<CharacterContext, float, float, float> OnHealthChanged;


        // 技能图标UI刷新事件：参数为角色上下文、技能类型、图标索引、剩余冷却时间
        public static event Action<CharacterContext, SkillUIType, int, float> OnSkillIconUIChanged;

        // 技能图标UI运行时刷新事件：参数为角色运行时数据、技能类型、图标索引、剩余冷却时间
        public static event Action<CharacterRuntimeData, SkillUIType, int, float> OnSkillIconUIRuntimeChanged;


        // 体力变化事件：参数为体力组件、当前值、最大值、归一化值
        public static event Action<PlayerStamina, float, float, float> OnStaminaChanged;

        // 体力条显隐事件：参数为体力组件、是否显示
        public static event Action<PlayerStamina, bool> OnStaminaVisibilityChanged;
        //// 外观变化事件：参数为角色上下文、外观目标类型、外观事件类型
        //public static event Action<CharacterContext, VisualTargetType, VisualEventType> OnVisualChanged;


        // 角色专属：今汐
        // 御空状态变化事件：参数为攻击逻辑、是否处于御空
        public static event Action<bool> OnFloatingChanged;
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
        public static void RaiseCharacterStateChanged(CharacterStateMachine stateMachine, CharacterState oldState, CharacterState newState)
        {
            OnCharacterStateChanged?.Invoke(stateMachine, oldState, newState);
        }
        // 派发生命值变化事件
        public static void RaiseHealthChanged(CharacterContext source, float current, float max, float normalized)
        {
            OnHealthChanged?.Invoke(source, current, max, normalized);
        }


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
        //// 派发外观变化事件
        //public static void RaiseVisualChanged(CharacterContext source, VisualTargetType targetType, VisualEventType eventType)
        //{
        //    OnVisualChanged?.Invoke(source, targetType, eventType);
        //}

        #endregion

        #region 角色专属：今汐
        // 派发御空状态变化事件
        public static void RaiseFloatingChanged(bool isFloating)
        {
            OnFloatingChanged?.Invoke(isFloating);
        }
        #endregion

        #endregion
    }
}
