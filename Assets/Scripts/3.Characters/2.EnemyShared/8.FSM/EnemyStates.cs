using UnityEngine;

namespace WutheringWaves
{
    #region 待机状态
    // 敌人待机状态：等待玩家进入发现范围
    public class EnemyIdleState : EnemyStateBase
    {
        public EnemyIdleState(EnemyStateMachine stateMachine, EnemyStateFactory factory) : base(stateMachine, factory) { }

        public override void EnterState()
        {
            // 1.进入待机状态，按敌人动画配置播放 Idle 动画
            string animationName = stateMachine.GetEnemyAnimationName(EnemyAnimationId.Idle);
            if (stateMachine.Animator != null && !string.IsNullOrEmpty(animationName))
            {
                stateMachine.Animator.CrossFadeInFixedTime(animationName, 0.3f, 0, 0);
            }

            // 2.输出调试，确认状态切换正常
            Debug.Log($"敌人 {stateMachine.Context.name} 进入待机状态");
        }

        public override void UpdateState()
        {
            // 1.优先检查状态转换
            if (CheckStateTransitions())
            {
                return;
            }

            // 2.待机状态当前不执行额外逻辑
        }

        // 检查待机状态转换：死亡、发现玩家
        private bool CheckStateTransitions()
        {
            // 1.死亡优先级最高
            if (stateMachine.RuntimeData != null && stateMachine.RuntimeData.IsDead)
            {
                SwitchState(EnemyState.Dead);
                return true;
            }

            // 2.发现玩家后进入追击状态
            if (stateMachine.Context.MovementLogic != null && stateMachine.Context.MovementLogic.IsTargetInDetectRange())
            {
                SwitchState(EnemyState.Chase);
                return true;
            }

            // 3.没有发生状态转换
            return false;
        }

        public override void ExitState()
        {

        }
    }
    #endregion

    #region 追击状态
    // 敌人追击状态：发现玩家后靠近目标，到达停止距离后保持朝向
    public class EnemyChaseState : EnemyStateBase
    {
        public EnemyChaseState(EnemyStateMachine stateMachine, EnemyStateFactory factory) : base(stateMachine, factory) { }

        public override void EnterState()
        {
            // 1.进入追击状态，按敌人动画配置播放 Chase 动画
            string animationName = stateMachine.GetEnemyAnimationName(EnemyAnimationId.Chase);
            if (stateMachine.Animator != null && !string.IsNullOrEmpty(animationName))
            {
                stateMachine.Animator.CrossFadeInFixedTime(animationName, 0.1f, 0, 0);
            }

            // 2.输出调试，确认状态切换正常
            Debug.Log($"敌人 {stateMachine.Context.name} 进入追击状态");
        }
        public override void UpdateState()
        {
            // 1.优先检查状态转换，发生转换后不继续执行追击逻辑
            if (CheckStateTransitions())
            {
                return;
            }

            // 2.进入停止距离后先停住并朝向目标，后续这里可以切攻击状态
            if (stateMachine.Context.MovementLogic.IsInStopDistance())
            {
                stateMachine.Context.MovementLogic.StopMove();
                stateMachine.Context.MovementLogic.RotateToTarget();
                return;
            }

            // 3.未到停止距离时继续追击目标
            stateMachine.Context.MovementLogic.MoveToTarget();
        }

        // 检查追击状态转换：死亡、移动组件缺失、目标离开发现范围
        private bool CheckStateTransitions()
        {
            // 1.死亡优先级最高
            if (stateMachine.RuntimeData != null && stateMachine.RuntimeData.IsDead)
            {
                SwitchState(EnemyState.Dead);
                return true;
            }

            // 2.移动组件为空时回到待机，避免空引用
            if (stateMachine.Context.MovementLogic == null)
            {
                SwitchState(EnemyState.Idle);
                return true;
            }

            // 3.目标离开发现范围时回到待机
            if (!stateMachine.Context.MovementLogic.IsTargetInDetectRange())
            {
                stateMachine.Context.MovementLogic.StopMove();
                SwitchState(EnemyState.Idle);
                return true;
            }

            // 4.没有发生状态转换
            return false;
        }

        public override void ExitState()
        {
            // 1.离开追击状态时停止移动，避免残留移动表现
            stateMachine.Context.MovementLogic?.StopMove();
        }
    }
    #endregion

    #region 受击状态
    // 敌人受击状态：第一版只停留短时间，后续可接受击动画和击退
    public class EnemyHitState : EnemyStateBase
    {
        private float stateTimer; // 受击状态计时器

        public EnemyHitState(EnemyStateMachine stateMachine, EnemyStateFactory factory) : base(stateMachine, factory) { }

        public override void EnterState()
        {
            // 1.初始化受击状态计时
            stateTimer = 0f;

            // 2.进入受击状态，按敌人动画配置播放 Hit 动画
            string animationName = stateMachine.GetEnemyAnimationName(EnemyAnimationId.Hit);
            if (stateMachine.Animator != null && !string.IsNullOrEmpty(animationName))
            {
                stateMachine.Animator.CrossFadeInFixedTime(animationName, 0.05f, 0, 0);
            }

            // 3.输出调试，确认状态切换正常
            Debug.Log($"敌人 {stateMachine.Context.name} 进入受击状态");
        }

        public override void UpdateState()
        {
            // 1.优先检查死亡转换
            if (CheckStateTransitions())
            {
                return;
            }

            // 2.受击状态持续一小段时间
            stateTimer += Time.deltaTime;
            if (stateTimer < stateMachine.HitStateDuration)
            {
                return;
            }

            // 3.受击结束后，如果玩家仍在发现范围内，继续追击
            if (stateMachine.Context.MovementLogic != null && stateMachine.Context.MovementLogic.IsTargetInDetectRange())
            {
                SwitchState(EnemyState.Chase);
                return;
            }

            // 4.否则回到待机
            SwitchState(EnemyState.Idle);
        }

        // 检查受击状态转换：死亡
        private bool CheckStateTransitions()
        {
            // 1.死亡优先级最高
            if (stateMachine.RuntimeData != null && stateMachine.RuntimeData.IsDead)
            {
                SwitchState(EnemyState.Dead);
                return true;
            }

            // 2.没有发生状态转换
            return false;
        }

        public override void ExitState()
        {

        }
    }
    #endregion

    #region 死亡状态
    // 敌人死亡状态：第一版交给 EnemyContext 统一处理死亡表现和禁用逻辑
    public class EnemyDeadState : EnemyStateBase
    {
        private bool hasEnteredDead; // 防止死亡逻辑重复执行

        public EnemyDeadState(EnemyStateMachine stateMachine, EnemyStateFactory factory) : base(stateMachine, factory) { }

        public override void EnterState()
        {
            // 1.死亡逻辑只执行一次
            if (hasEnteredDead)
            {
                return;
            }

            hasEnteredDead = true;

            // 2.进入死亡状态，按敌人动画配置播放 Dead 动画
            string animationName = stateMachine.GetEnemyAnimationName(EnemyAnimationId.Dead);
            if (stateMachine.Animator != null && !string.IsNullOrEmpty(animationName))
            {
                stateMachine.Animator.CrossFadeInFixedTime(animationName, 0.1f, 0, 0);
            }

            // 3.通知 EnemyContext 处理死亡后的碰撞体、隐藏、掉落等
            stateMachine.Context.OnDeadStateEntered();
        }

        public override void UpdateState()
        {

        }

        public override void ExitState()
        {

        }
    }
    #endregion
}