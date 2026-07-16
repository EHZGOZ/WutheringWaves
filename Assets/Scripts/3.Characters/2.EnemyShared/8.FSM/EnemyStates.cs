using UnityEngine;

namespace WutheringWaves
{
    #region 具体状态实现类（所有敌人状态继承自抽象基类，实现生命周期方法）

    #region 待机状态
    //待机状态
    public class EnemyIdleState : EnemyStateBase
    {
        public EnemyIdleState(EnemyStateMachine stateMachine, EnemyStateFactory factory) : base(stateMachine, factory) { }

        public override void EnterState()
        {
            // 1.进入待机状态动画
            IdleEnterAnimation();

            // 2.初始化待机状态
            InitializeIdleState();
        }

        #region EnterState子状态
        // 1.进入待机状态动画
        private void IdleEnterAnimation()
        {
            // 1.按敌人动画配置获取 Idle 动画名
            string animationName = stateMachine.GetEnemyAnimationName(EnemyAnimationId.Idle);

            // 2.Animator 或动画名为空时不播放，避免临时敌人未配置动画时报错
            if (stateMachine.Animator == null || string.IsNullOrEmpty(animationName))
            {
                return;
            }

            // 3.播放待机动画
            stateMachine.Animator.CrossFadeInFixedTime(animationName, 0.3f, 0, 0);
        }

        // 2.初始化待机状态
        private void InitializeIdleState()
        {
            // 1.当前待机状态暂时没有运行时数据需要初始化
            Debug.Log($"敌人 {stateMachine.Context.name} 进入待机状态");
        }
        #endregion

        public override void UpdateState()
        {
            // 1.状态转换判断
            if (CheckStateTransitions())
            {
                return;
            }

            // 2.更新待机状态
            UpdateIdleState();
        }

        #region UpdateState子状态
        // 2.更新待机状态
        private void UpdateIdleState()
        {
            // 1.当前待机状态暂时不执行额外逻辑
        }
        #endregion

        public override void ExitState()
        {
            // 1.退出待机状态
            IdleExitState();
        }

        #region ExitState子状态
        // 1.退出待机状态
        private void IdleExitState()
        {
            // 1.当前待机状态暂时没有退出逻辑
        }
        #endregion

        #region 状态转换判断
        // 状态转换判断
        private bool CheckStateTransitions()
        {
            // 1.死亡优先级最高，由EnemyStateMachine统一提供死亡状态
            if (stateMachine.IsDead)
            {
                SwitchState(EnemyState.Dead);
                return true;
            }

            // 2.移动组件统一判断目标、导航状态和恢复追击距离
            if (stateMachine.MovementLogic.IsChaseAvailable())
            {
                SwitchState(EnemyState.Chase);
                return true;
            }

            // 3.没有发生状态转换
            return false;
        }
        #endregion
    }
    #endregion

    #region 追击状态
    //追击状态
    public class EnemyChaseState : EnemyStateBase
    {
        public EnemyChaseState(EnemyStateMachine stateMachine, EnemyStateFactory factory) : base(stateMachine, factory) { }

        public override void EnterState()
        {
            // 1.进入追击状态动画
            ChaseEnterAnimation();

            // 2.初始化追击状态
            InitializeChaseState();
        }

        #region EnterState子状态
        // 1.进入追击状态动画
        private void ChaseEnterAnimation()
        {
            // 1.按敌人动画配置获取 Chase 动画名
            string animationName = stateMachine.GetEnemyAnimationName(EnemyAnimationId.Chase);

            // 2.Animator 或动画名为空时不播放，避免临时敌人未配置动画时报错
            if (stateMachine.Animator == null || string.IsNullOrEmpty(animationName))
            {
                return;
            }

            // 3.播放追击动画
            stateMachine.Animator.CrossFadeInFixedTime(animationName, 0.3f, 0, 0);
        }

        // 2.初始化追击状态
        private void InitializeChaseState()
        {
            // 1.当前追击状态暂时没有运行时数据需要初始化
            Debug.Log($"敌人 {stateMachine.Context.name} 进入追击状态");
        }
        #endregion

        public override void UpdateState()
        {
            // 1.状态转换判断
            if (CheckStateTransitions())
            {
                return;
            }

            // 2.更新追击状态
            UpdateChaseState();
        }

        #region UpdateState子状态
        // 2.更新追击状态
        private void UpdateChaseState()
        {
            // 1.追击状态只下达靠近目标命令
            // 路径计算、停止距离、实体移动和移动转向统一由EnemyMovement处理
            stateMachine.MovementLogic.MoveToTarget();
        }
        #endregion

        public override void ExitState()
        {
            // 1.退出追击状态
            ChaseExitState();
        }

        #region ExitState子状态
        // 1.退出追击状态
        private void ChaseExitState()
        {
            // 1.离开追击状态时停止移动，避免残留移动表现
            stateMachine.MovementLogic.StopMove();
        }
        #endregion

        #region 状态转换判断
        // 状态转换判断
        private bool CheckStateTransitions()
        {
            // 1.死亡优先级最高，由EnemyStateMachine统一提供死亡状态
            if (stateMachine.IsDead)
            {
                SwitchState(EnemyState.Dead);
                return true;
            }

            // 2.目标无效时停止移动并回到待机
            // 目标是否存在由EnemyTargeting统一判断
            if (!stateMachine.Targeting.HasTarget)
            {
                stateMachine.MovementLogic.StopMove();
                SwitchState(EnemyState.Idle);
                return true;
            }

            // 3.到达停止距离时停止移动并回到待机
            if (stateMachine.MovementLogic.IsInStopDistance())
            {
                stateMachine.MovementLogic.StopMove();
                SwitchState(EnemyState.Idle);
                return true;
            }

            // 4.没有发生状态转换
            return false;
        }
        #endregion
    }
    #endregion

    #region 受击状态
    //受击状态
    public class EnemyHitState : EnemyStateBase
    {
        private float stateTimer; // 受击状态计时器
        private float HitStateDuration = 4f;

        public EnemyHitState(EnemyStateMachine stateMachine, EnemyStateFactory factory) : base(stateMachine, factory) { }

        public override void EnterState()
        {
            // 1.进入受击状态动画
            HitEnterAnimation();

            // 2.初始化受击状态
            InitializeHitState();
        }

        #region EnterState子状态
        // 1.进入受击状态动画
        private void HitEnterAnimation()
        {
            // 1.按敌人动画配置获取 Hit 动画名
            string animationName = stateMachine.GetEnemyAnimationName(EnemyAnimationId.Hit);

            // 2.Animator 或动画名为空时不播放，避免临时敌人未配置动画时报错
            if (stateMachine.Animator == null || string.IsNullOrEmpty(animationName))
            {
                return;
            }

            // 3.播放受击动画
            stateMachine.Animator.CrossFadeInFixedTime(animationName, 0.3f, 0, 0);
        }

        // 2.初始化受击状态
        private void InitializeHitState()
        {
            // 1.初始化受击状态计时
            stateTimer = 0f;

            // 2.输出调试，确认状态切换正常
            Debug.Log($"敌人 {stateMachine.Context.name} 进入受击状态");
        }
        #endregion

        public override void UpdateState()
        {
            // 1.状态转换判断
            if (CheckStateTransitions())
            {
                return;
            }

            // 2.更新受击状态
            UpdateHitState();
        }

        #region UpdateState子状态
        // 2.更新受击状态
        private void UpdateHitState()
        {
            // 1.受击状态持续一小段时间
            stateTimer += Time.deltaTime;
            if (stateTimer < HitStateDuration)
            {
                return;
            }

            // 2.受击结束后，如果当前仍然存在玩家目标，则继续追击
            // 目标是否存在由EnemyTargeting统一判断
            if (stateMachine.Targeting.HasTarget)
            {
                SwitchState(EnemyState.Chase);
                return;
            }

            // 3.没有有效目标时回到待机
            SwitchState(EnemyState.Idle);
        }
        #endregion

        public override void ExitState()
        {
            // 1.退出受击状态
            HitExitState();
        }

        #region ExitState子状态
        // 1.退出受击状态
        private void HitExitState()
        {
            // 1.当前受击状态暂时没有退出逻辑
        }
        #endregion

        #region 状态转换判断
        // 状态转换判断
        private bool CheckStateTransitions()
        {
            // 1.死亡优先级最高，由EnemyStateMachine统一提供死亡状态
            if (stateMachine.IsDead)
            {
                SwitchState(EnemyState.Dead);
                return true;
            }

            // 2.没有发生状态转换
            return false;
        }
        #endregion
    }
    #endregion

    #region 死亡状态
    //死亡状态
    public class EnemyDeadState : EnemyStateBase
    {
        private bool hasEnteredDead; // 防止死亡逻辑重复执行

        public EnemyDeadState(EnemyStateMachine stateMachine, EnemyStateFactory factory) : base(stateMachine, factory) { }

        public override void EnterState()
        {
            // 1.进入死亡状态
            EnterDeadState();
        }

        #region EnterState子状态
        // 1.进入死亡状态
        private void EnterDeadState()
        {
            // 1.死亡逻辑只执行一次
            if (hasEnteredDead)
            {
                return;
            }

            hasEnteredDead = true;

            // 2.进入死亡状态动画
            DeadEnterAnimation();

            // 3.通知 EnemyContext 处理死亡后的碰撞体、隐藏、掉落等
            stateMachine.Context.OnDeadStateEntered();
        }

        // 2.进入死亡状态动画
        private void DeadEnterAnimation()
        {
            // 1.按敌人动画配置获取 Dead 动画名
            string animationName = stateMachine.GetEnemyAnimationName(EnemyAnimationId.Dead);

            // 2.Animator 或动画名为空时不播放，避免临时敌人未配置动画时报错
            if (stateMachine.Animator == null || string.IsNullOrEmpty(animationName))
            {
                return;
            }

            // 3.播放死亡动画
            stateMachine.Animator.CrossFadeInFixedTime(animationName, 0.1f, 0, 0);
        }
        #endregion

        public override void UpdateState()
        {
            // 1.更新死亡状态
            UpdateDeadState();
        }

        #region UpdateState子状态
        // 1.更新死亡状态
        private void UpdateDeadState()
        {
            // 1.死亡状态暂时不执行额外逻辑
        }
        #endregion

        public override void ExitState()
        {
            // 1.退出死亡状态
            DeadExitState();
        }

        #region ExitState子状态
        // 1.退出死亡状态
        private void DeadExitState()
        {
            // 1.死亡状态正常不会退出，暂时不执行额外逻辑
        }
        #endregion
    }
    #endregion

    #endregion
}