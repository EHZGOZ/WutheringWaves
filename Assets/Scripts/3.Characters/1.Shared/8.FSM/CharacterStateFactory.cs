using System;
using System.Collections.Generic;

namespace WutheringWaves
{
    #region 状态工厂类（管理所有状态实例，支持基础状态默认注册与角色特殊状态补注册）
    // 负责创建并缓存当前角色可用的状态实例，状态机只从工厂读取，不再假设所有角色都有同一套状态
    public class CharacterStateFactory
    {
        private readonly Dictionary<CharacterState, CharacterBaseState> registeredStates = new Dictionary<CharacterState, CharacterBaseState>();
        private readonly CharacterStateMachine context;

        public CharacterStateFactory(CharacterStateMachine stateMachine)
        {
            context = stateMachine;
            RegisterBaseStates();
        }

        #region 注册接口
        // 注册单个状态：重复注册时以后注册的实现覆盖旧实现，方便角色驱动替换默认状态
        public void RegisterState(CharacterState stateType, CharacterBaseState state)
        {
            if (state == null)
            {
                return;
            }

            registeredStates[stateType] = state;
        }

        // 判断当前角色是否已注册指定状态
        public bool HasState(CharacterState stateType)
        {
            return registeredStates.ContainsKey(stateType);
        }

        // 获取指定状态实例：未注册时返回 null，由状态机决定如何兜底
        public CharacterBaseState GetState(CharacterState stateType)
        {
            return registeredStates.TryGetValue(stateType, out CharacterBaseState state) ? state : null;
        }
        #endregion

        #region 基础状态注册
        // 注册所有角色默认都会具备的基础状态
        private void RegisterBaseStates()
        {
            RegisterState(CharacterState.Idle, new CharacterIdleState(context, this));
            RegisterState(CharacterState.Moving, new CharacterMovingState(context, this));
            RegisterState(CharacterState.Jumping, new CharacterJumpingState(context, this));
            RegisterState(CharacterState.Falling, new CharacterFallingState(context, this));
            RegisterState(CharacterState.Dashing, new CharacterDashingState(context, this));
            RegisterState(CharacterState.AirDashing, new CharacterAirDashingState(context, this));
            RegisterState(CharacterState.Dodging, new CharacterDodgingState(context, this));
            RegisterState(CharacterState.Hit, new CharacterHitState(context, this));
            RegisterState(CharacterState.Dead, new CharacterDeadState(context, this));
            RegisterState(CharacterState.Transition, new CharacterTransitionState(context, this));
        }
        #endregion

        #region 默认状态
        /// <summary>获取角色默认初始状态（待机）</summary>
        public CharacterBaseState GetDefaultState()
        {
            CharacterBaseState defaultState = GetState(CharacterState.Idle);
            if (defaultState == null)
            {
                throw new ArgumentException("状态工厂未注册 Idle 状态，无法获取默认状态。");
            }

            return defaultState;
        }
        #endregion
    }
    #endregion
}
