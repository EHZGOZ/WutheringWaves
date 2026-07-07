    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    namespace WutheringWaves
    {
            #region 状态抽象基类（定义所有状态的核心生命周期规范）
            // 所有角色状态的抽象基类
            public abstract class CharacterBaseState
            {
                #region 核心依赖（所有状态可访问的上下文）
                protected CharacterStateMachine stateMachine; // 状态机核心（上下文，存储所有共享数据/组件）
                protected CharacterStateFactory Factory; // 状态工厂（用于状态切换时获取目标状态实例）
                #endregion

                #region 构造函数（注入依赖，所有子类必须实现）
                protected CharacterBaseState(CharacterStateMachine currentContext, CharacterStateFactory characterStateFactory)
                {
                    stateMachine = currentContext;
                    Factory = characterStateFactory;
                }
                #endregion

                #region 状态生命周期（抽象方法，子类必须实现具体逻辑）
                /// <summary>进入状态时执行（仅调用一次，初始化状态数据/动画/参数）</summary>
                public abstract void EnterState();
                /// <summary>状态帧更新逻辑（每帧调用，处理状态核心业务）</summary>
                public abstract void UpdateState();
                /// /// <summary>退出状态时执行（仅调用一次，清理状态数据）</summary>
                public abstract void ExitState();
                #endregion

                #region 状态切换辅助方法（子类通过此方法切换状态，统一入口）
                /// <summary>切换到目标状态（子类内部调用，自动触发生命周期）</summary>
                /// <param name="targetState">目标状态枚举</param>
                protected void SwitchState(CharacterState targetState)
                {
                    stateMachine.SwitchState(Factory.GetState(targetState));
                }
                #endregion

                #region 通用判断方法（所有状态可复用的基础判断，封装在基类）
                //// 判断是否为可打断状态（非闪避/非受击/非死亡可被大部分状态打断）
                //protected bool IsInterruptible()
                //{
                //    return stateMachine.CurrentStateType != CharacterState.JinxiDodge&&stateMachine.CurrentStateType != CharacterState.JinxiHit && stateMachine.CurrentStateType != CharacterState.JinxiDead;
                //}
                //// 判断地面冲刺是否可用
                //protected bool IsDashAvailable()
                //{
                //    // 条件1：是否为可打断状态（非受击/非死亡，基础状态判断）
                //    bool isCanInterrupt = IsInterruptible();
                //    // 条件2：必须在地面
                //    bool isGrounded =!stateMachine.CheckFallTransition();
                //    // 条件3：全局CD是否结束（两次连续冲刺后，必须等全局CD才能重新开始冲刺）
                //    bool isGlobalCDOver = stateMachine.DashGlobalCDTimer <= 0;
                //    // 条件4：当前冲刺次数是否未达上限（最多连续2次，次数0/1时满足，2次则触发全局CD并重置）
                //    bool isUnderMaxCount = stateMachine.DashCount < 2;
                //    // 条件5：内置CD是否结束（两次连续冲刺之间的0.3s冷却，仅非首次冲刺需要判断）
                //    bool isInternalCDOver = stateMachine.DashInternalCDTimer <= 0;

                //    return isCanInterrupt &&isGrounded&& isGlobalCDOver && isUnderMaxCount && (stateMachine.DashCount == 0 || isInternalCDOver);
                //}
                ////判断空中冲刺是否可用
                //protected bool IsAirDashAvailable()
                //{
                //    // 条件1：是否为可打断状态（非闪避/非受击/非死亡，基础状态判断）
                //    bool isCanInterrupt = IsInterruptible();
                //    // 条件2：必须在空中
                //    bool isInAir = !stateMachine.CharacterController.isGrounded;
                //    // 条件3：本次坠落还未被使用
                //    bool hasNotUsed = !stateMachine.HasAirDashed;

                //    return isCanInterrupt && isInAir && hasNotUsed ;
                //}
                //// 判断跳跃是否可用
                //protected bool IsJumpAvailable()
                //{
                //    // 条件1：是否为可打断状态
                //    bool isCanInterrupt = IsInterruptible();
                //    return isCanInterrupt;
                //}
                #endregion
            }
            #endregion

    }




