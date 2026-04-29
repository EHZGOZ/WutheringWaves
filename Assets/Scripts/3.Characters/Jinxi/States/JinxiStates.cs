
using DG.Tweening;
using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    namespace WutheringWaves
{
    #region 具体状态实现类（所有状态继承自抽象基类，实现生命周期方法）

     #region 待机状态
    //待机状态
    public class JinxiIdleState : CharacterBaseState
    {
        public JinxiIdleState(CharacterStateMachine stateMachine, CharacterStateFactory factory) : base(stateMachine, factory) { }
        private float _stateTime;
        public override void EnterState()
        {
            // 1. 进入待机状态动画
            IdleEnterAnimation();
            // 2.初始化待机状态
            InitializeIdleState();
        }

        #region EnterState子状态
        //1. 进入待机状态动画
        private void IdleEnterAnimation()
        {
            //1.切换到 Locomotion 混合树
            stateMachine.Animator.CrossFadeInFixedTime(stateMachine.GetLocomotionAnimationName(LocomotionAnimationId.Idle), 0.3f, 0, 0);
            //2.展示背负装饰剑
            stateMachine.manifestation.ShowDecorationSwordFade();
        }
        //2.初始化待机状态
        private void InitializeIdleState()
        {
            _stateTime = 0f;
        }
        #endregion

        public override void UpdateState()
        {
            //1.常态重力
            stateMachine.movementLogic.ApplyGroundingForce();
            //2. 更新待机状态动画
            IdleUpdateAnimation();
            //3.更新状态
            UpdateIdleState();
            //3.状态转换判断
            CheckStateTransitions();
        }

        #region UpdateState子状态
        //2. 更新待机状态动画
        private void IdleUpdateAnimation()
        {
            //待机动画 传入参数
            stateMachine.movementLogic.UpdateFreeMoveAnimation(stateMachine.MoveInput, stateMachine.IsHoldingRun);
        }

        private void UpdateIdleState()
        {
            _stateTime += Time.deltaTime;
        }
        #endregion

        public override void ExitState()
        {
            // 1. 退出待机状态动画
            IdleExitAnimation();

            //2. 重置垂直速度
            stateMachine.movementLogic.ResetVerticalVelocity();
        }

        #region ExitState子状态
        // 1. 退出待机状态动画
        private void IdleExitAnimation()
        {

        }

        #endregion

        //状态转换判断
        private void CheckStateTransitions()
        {
            //死亡状态
            if (stateMachine.runtimeData.currentHealth <= 0)
            {
                SwitchState(CharacterState.JinxiDead);
                return;
            }
            //爆发状态
            if (stateMachine.JinxiSpecialSkillLinker.IsQBurstable() && stateMachine.CheckAndConsumeQBurstRequest())
            {
                stateMachine.IsStateLocked = false;
                SwitchState(CharacterState.JinxiQBurst);
                return;
            }
            //受击状态
            if (stateMachine.TryConsumeHitRequest())
            {
                SwitchState(CharacterState.JinxiHit);
                return;
            }
            //战技状态
            if (stateMachine.CheckAndConsumeESkillRequest() && stateMachine.JinxiSpecialSkillLinker.IsESkillable())
            {
                SwitchState(CharacterState.JinxiESkill);
                return;
            }
            //延奏状态
            if (stateMachine.CheckAndConsumeQteSkillRequest() && stateMachine.JinxiSpecialSkillLinker.IsQteSkillable())
            {
                SwitchState(CharacterState.JinxiQteSkill);
                return;
            }

            //冲刺状态
            if (stateMachine.CheckAndConsumeDashRequest() && stateMachine.movementLogic.IsDashAvailable())
            {
                SwitchState(CharacterState.JinxiDash);
                return;
            }
            //坠落状态
            if (!stateMachine.movementLogic.CustomCheckGrounded())
            {
                SwitchState(CharacterState.JinxiFall);
                return;
            }
            // 跳跃状态
            if (stateMachine.CheckAndConsumeJumpRequest() && stateMachine.movementLogic.IsJumpAvailable())
            {
                SwitchState(CharacterState.JinxiJump);
                return;
            }
            //御空攻击状态
            if (stateMachine.CheckAndConsumeAirAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsAirAttackable())
            {
                SwitchState(CharacterState.JinxiAirAttack);
                return;
            }
            //重击状态
            if (stateMachine.CheckAndConsumeHeavyAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsHeavyAttackable())
            {
                SwitchState(CharacterState.JinxiHeavyAttack);
                return;
            }
            //攻击状态
            if (stateMachine.CheckAndConsumeAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsAttackable())
            {
                SwitchState(CharacterState.JinxiAttack);
                return;
            }
            //移动状态
            if (stateMachine.MoveInput.magnitude > stateMachine.movementLogic.moveThreshold && stateMachine.movementLogic.CustomCheckGrounded())
            {
                SwitchState(CharacterState.JinxiMove);
                return;
            }
            ////过渡状态
            //if (stateMachine.MoveInput.magnitude < stateMachine.movementLogic.moveThreshold && _stateTime >= 10f)
            //{
            //    // 统一先切到 JinxiTransition，由 JinxiTransition 处理后续
            //    SwitchState(CharacterState.JinxiTransition);
            //    return;
            //}
        }
    }
    #endregion

     #region 移动状态
    //移动状态
    public class JinxiMoveState : CharacterBaseState
    {
        private float _stateTimer;//已处于移动状态的时间
        public JinxiMoveState(CharacterStateMachine stateMachine, CharacterStateFactory factory) : base(stateMachine, factory) { }

        public override void EnterState()
        {
            //1.初始化移动状态
            InitlizeMovingState();
            //2. 进入移动状态动画
            MovingEnterAnimation();
        }

        #region EnterState子方法
        //1.初始化移动状态
        private void InitlizeMovingState()
        {
            _stateTimer = 0f;
        }
        //2.进入移动状态动画
        private void MovingEnterAnimation()
        {
            //1.切换到 Locomotion 混合树
            stateMachine.Animator.CrossFadeInFixedTime(stateMachine.GetLocomotionAnimationName(LocomotionAnimationId.Move), 0.3f, 0, 0);
        }

        #endregion

        public override void UpdateState()
        {
            //1.常态重力
            stateMachine.movementLogic.ApplyGroundingForce();
            //2.实际移动与旋转
            stateMachine.movementLogic.UpdateMovement(stateMachine.MoveInput, stateMachine.IsHoldingRun);
            //3.移动动画
            MovingUpdateAnimation();
            //4.状态更新
            UpdateMovingState();
            //5.状态转换判断
            CheckStateTransitions();
        }

        #region UpdateState子方法
        //3.移动动画
        private void MovingUpdateAnimation()
        {
            //移动动画
            stateMachine.movementLogic.UpdateFreeMoveAnimation(stateMachine.MoveInput, stateMachine.IsHoldingRun);
        }
        //4.状态更新
        private void UpdateMovingState()
        {
            _stateTimer += Time.deltaTime;
        }
        #endregion

        public override void ExitState()
        {
            //1.清除移动动画残存
            MovingExitAnimation();

            //2. 重置垂直速度
            stateMachine.movementLogic.ResetVerticalVelocity();
        }

        #region ExitState子方法
        // 1. 退出移动状态动画
        private void MovingExitAnimation()
        {

        }

        #endregion

        private void CheckStateTransitions()
        {
            //死亡状态
            if (stateMachine.runtimeData.currentHealth <= 0)
            {
                SwitchState(CharacterState.JinxiDead);
                return;
            }
            //爆发状态
            if (stateMachine.JinxiSpecialSkillLinker.IsQBurstable() && stateMachine.CheckAndConsumeQBurstRequest())
            {
                stateMachine.IsStateLocked = false;
                SwitchState(CharacterState.JinxiQBurst);
                return;
            }
            //受击状态
            if (stateMachine.TryConsumeHitRequest())
            {
                SwitchState(CharacterState.JinxiHit);
                return;
            }
            //战技状态
            if (stateMachine.CheckAndConsumeESkillRequest() && stateMachine.JinxiSpecialSkillLinker.IsESkillable())
            {
                SwitchState(CharacterState.JinxiESkill);
                return;
            }
            //延奏状态
            if (stateMachine.CheckAndConsumeQteSkillRequest() && stateMachine.JinxiSpecialSkillLinker.IsQteSkillable())
            {
                SwitchState(CharacterState.JinxiQteSkill);
                return;
            }
            //冲刺状态
            if (stateMachine.CheckAndConsumeDashRequest() && stateMachine.movementLogic.IsDashAvailable())
            {
                SwitchState(CharacterState.JinxiDash);
                return;
            }
            //坠落状态
            if (!stateMachine.movementLogic.CustomCheckGrounded())
            {
                SwitchState(CharacterState.JinxiFall);
                return;
            }
            // 跳跃状态
            if (stateMachine.CheckAndConsumeJumpRequest() && stateMachine.movementLogic.IsJumpAvailable())
            {
                SwitchState(CharacterState.JinxiJump);
                return;
            }
            //御空攻击状态
            if (stateMachine.CheckAndConsumeAirAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsAirAttackable())
            {
                SwitchState(CharacterState.JinxiAirAttack);
                return;
            }
            //重击状态
            if (stateMachine.CheckAndConsumeHeavyAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsHeavyAttackable())
            {
                SwitchState(CharacterState.JinxiHeavyAttack);
                return;
            }
            //攻击状态
            if (stateMachine.CheckAndConsumeAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsAttackable())
            {
                SwitchState(CharacterState.JinxiAttack);
                return;
            }
            // 收步状态
            if (stateMachine.MoveInput.magnitude < stateMachine.movementLogic.moveThreshold && stateMachine.movementLogic.CustomCheckGrounded())
            {
                SwitchState(CharacterState.JinxiStop);
                return;
            }

        }
    }
    #endregion

     #region 收步状态
    // 收步状态
    public class JinxiStopState : CharacterBaseState
    {
        private float _stopTimer; // 收步计时器
        private bool _isRunStop;  // 是否是奔跑后的收步

        public JinxiStopState(CharacterStateMachine stateMachine, CharacterStateFactory factory)
            : base(stateMachine, factory) { }

        public override void EnterState()
        {
            //1.初始化收步数据
            InitializeStopData();

            //2.播放收步动画
            StopEnterAnimation();

            //3.初始化收步滑行
            InitializeStopMovement();
        }

        #region EnterState子方法
        //1.初始化收步数据
        private void InitializeStopData()
        {
            _isRunStop = stateMachine.movementLogic.HasPressedShift();
            _stopTimer = _isRunStop ? stateMachine.movementLogic.runStoppingTime : stateMachine.movementLogic.moveStoppingTime;
        }

        //2.播放收步动画
        private void StopEnterAnimation()
        {
            if (_isRunStop)
            {
                stateMachine.Animator.CrossFadeInFixedTime(stateMachine.GetLocomotionAnimationName(LocomotionAnimationId.Stop_Run), 0.1f, 0, 0);
            }
            else
            {
                stateMachine.Animator.CrossFadeInFixedTime(stateMachine.GetLocomotionAnimationName(LocomotionAnimationId.Idle), 0.1f, 0, 0);
            }
        }

        //3.初始化收步滑行
        private void InitializeStopMovement()
        {
            if (_isRunStop)
            {
                stateMachine.movementLogic.InitializeStopping(
                    stateMachine.movementLogic.runStoppingDistance,
                    stateMachine.movementLogic.runStoppingTime
                );
            }
            else
            {
                stateMachine.movementLogic.InitializeStopping(
                    stateMachine.movementLogic.moveStoppingDistance,
                    stateMachine.movementLogic.moveStoppingTime
                );
            }
        }
        #endregion

        public override void UpdateState()
        {
            //1.常态重力
            stateMachine.movementLogic.ApplyGroundingForce();

            //2.实现收步滑行
            RealizationStopMovement();

            //3.状态转换
            CheckStateTransitions();

            //4.更新收步计时
            UpdateStopTimer();
        }

        #region UpdateState子方法
        //2.实现收步滑行
        private void RealizationStopMovement()
        {
            //1.实现急停逻辑
            stateMachine.movementLogic.HandleStoppingMovement();

            //2.动画传参
            if (_isRunStop)
            {
                stateMachine.movementLogic.UpdateFreeMoveAnimation(stateMachine.MoveInput, stateMachine.IsHoldingRun);
            }
            else
            {
                stateMachine.movementLogic.UpdateStopMoveAnimation(stateMachine.MoveInput, stateMachine.IsHoldingRun);
            }
        }

        //4.更新收步计时
        private void UpdateStopTimer()
        {
            _stopTimer -= Time.deltaTime;

            if (_stopTimer <= 0f)
            {
                SwitchState(CharacterState.JinxiIdle);
            }
        }
        #endregion

        public override void ExitState()
        {
            //1.退出收步状态动画
            StopExitAnimation();

            //2.重置垂直速度
            stateMachine.movementLogic.ResetVerticalVelocity();
        }

        #region ExitState子方法
        //1.退出收步状态动画
        private void StopExitAnimation()
        {
            //防止动画参数残留
            stateMachine.movementLogic.UpdateFreeMoveAnimation(stateMachine.MoveInput, stateMachine.IsHoldingRun);
        }
        #endregion

        //状态转换
        private void CheckStateTransitions()
        {
            //死亡状态
            if (stateMachine.runtimeData.currentHealth <= 0)
            {
                SwitchState(CharacterState.JinxiDead);
                return;
            }

            //爆发状态
            if (stateMachine.JinxiSpecialSkillLinker.IsQBurstable() && stateMachine.CheckAndConsumeQBurstRequest())
            {
                stateMachine.IsStateLocked = false;
                SwitchState(CharacterState.JinxiQBurst);
                return;
            }

            //受击状态
            if (stateMachine.TryConsumeHitRequest())
            {
                SwitchState(CharacterState.JinxiHit);
                return;
            }

            //战技状态
            if (stateMachine.CheckAndConsumeESkillRequest() && stateMachine.JinxiSpecialSkillLinker.IsESkillable())
            {
                SwitchState(CharacterState.JinxiESkill);
                return;
            }
            //延奏状态
            if (stateMachine.CheckAndConsumeQteSkillRequest() && stateMachine.JinxiSpecialSkillLinker.IsQteSkillable())
            {
                SwitchState(CharacterState.JinxiQteSkill);
                return;
            }

            //冲刺状态
            if (stateMachine.CheckAndConsumeDashRequest() && stateMachine.movementLogic.IsDashAvailable())
            {
                SwitchState(CharacterState.JinxiDash);
                return;
            }

            //跳跃状态
            if (stateMachine.CheckAndConsumeJumpRequest() && stateMachine.movementLogic.IsJumpAvailable())
            {
                SwitchState(CharacterState.JinxiJump);
                return;
            }

            //坠落状态
            if (!stateMachine.movementLogic.CustomCheckGrounded())
            {
                SwitchState(CharacterState.JinxiFall);
                return;
            }

            //御空攻击状态
            if (stateMachine.CheckAndConsumeAirAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsAirAttackable())
            {
                SwitchState(CharacterState.JinxiAirAttack);
                return;
            }

            //重击状态
            if (stateMachine.CheckAndConsumeHeavyAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsHeavyAttackable())
            {
                SwitchState(CharacterState.JinxiHeavyAttack);
                return;
            }

            //攻击状态
            if (stateMachine.CheckAndConsumeAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsAttackable())
            {
                SwitchState(CharacterState.JinxiAttack);
                return;
            }

            //移动状态
            if (stateMachine.MoveInput.magnitude > stateMachine.movementLogic.moveThreshold && stateMachine.movementLogic.CustomCheckGrounded())
            {
                SwitchState(CharacterState.JinxiMove);
                return;
            }
        }
    }
    #endregion

     #region 跳跃状态
    //跳跃状态
    public class JinxiJumpState : CharacterBaseState
    {
        private const float JumpLockTime = 0.33f;
        private float _stateTimer;//已处于跳跃状态的时间

        public JinxiJumpState(CharacterStateMachine stateMachine, CharacterStateFactory factory) : base(stateMachine, factory) { }

        public override void EnterState()
        {
            //1.进入跳跃状态动画
            JumpingEnterAnimation();
            //2.初始化跳跃状态
            InitializeJumpingState();
            //3.重置连招
        }

        #region EnterState子状态
        // 1. 进入跳跃状态动画
        private void JumpingEnterAnimation()
        {
            LocomotionAnimationId jumpAnimationId = stateMachine.IsHoldingRun
                ? LocomotionAnimationId.Jump_Run
                : LocomotionAnimationId.Jump_Walk;
            stateMachine.Animator.CrossFadeInFixedTime(stateMachine.GetLocomotionAnimationName(jumpAnimationId), 0f, 0, 0);
        }
        //2.初始化跳跃状态
        private void InitializeJumpingState()
        {
            //1.消费跳跃请求
            stateMachine.CleanWantsToJumpRequest();//消费跳跃请求
            //2.锁定状态：禁止切换其他状态
            stateMachine.IsStateLocked = true;
            //3.重置计时器，开始计时
            _stateTimer = 0f;
        }
        #endregion

        public override void UpdateState()
        {
            //1.跳跃初始锁定与解锁
            UpdateJumpLockTime();
            //2.状态转换判断
            CheckInterruptions();
            stateMachine.movementLogic.HandleJumpMovement(stateMachine.MoveInput);
        }

        #region UpdateState子状态
        //跳跃阶段状态解锁
        private void UpdateJumpLockTime()
        {
            _stateTimer += Time.deltaTime;
            if (_stateTimer >= JumpLockTime)
                stateMachine.IsStateLocked = false;
        }
        #endregion

        public override void ExitState()
        {
            // 1. 退出跳跃状态动画
            JumpingExitAnimation();
            //2. 重置垂直速度
            stateMachine.movementLogic.ResetVerticalVelocity();
        }

        #region ExitState子状态
        // 1. 退出跳跃状态动画
        private void JumpingExitAnimation()
        {
        }
        #endregion

        //状态转换判断
        private void CheckInterruptions()
        {
            //死亡状态
            if (stateMachine.runtimeData.currentHealth <= 0)
            {
                SwitchState(CharacterState.JinxiDead);
                return;
            }
            //爆发状态
            if (stateMachine.JinxiSpecialSkillLinker.IsQBurstable() && stateMachine.CheckAndConsumeQBurstRequest())
            {
                stateMachine.IsStateLocked = false;
                SwitchState(CharacterState.JinxiQBurst);
                return;
            }
            //受击状态
            if (stateMachine.TryConsumeHitRequest())
            {
                SwitchState(CharacterState.JinxiHit);
                return;
            }
            // 御空冲刺状态
            if (stateMachine.CheckAndConsumeDashRequest() && stateMachine.movementLogic.IsFloatDashAvailable())
            {
                SwitchState(CharacterState.JinxiFloatDash);
                return;
            }
            //战技状态
            if (stateMachine.CheckAndConsumeESkillRequest() && stateMachine.JinxiSpecialSkillLinker.IsESkillable())
            {
                SwitchState(CharacterState.JinxiESkill);
                return;
            }
            //御空攻击状态
            if (stateMachine.CheckAndConsumeAirAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsAirAttackable())
            {
                SwitchState(CharacterState.JinxiAirAttack);
                return;
            }
            //延奏状态
            if (stateMachine.CheckAndConsumeQteSkillRequest() && stateMachine.JinxiSpecialSkillLinker.IsQteSkillable())
            {
                SwitchState(CharacterState.JinxiQteSkill);
                return;
            }
            //下落攻击状态
            if (stateMachine.CheckAndConsumeFallAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsFallAttackable())
            {
                SwitchState(CharacterState.JinxiFallAttack);
                return;
            }
            //坠落状态
            if (_stateTimer >= JumpLockTime)
            {
                SwitchState(CharacterState.JinxiFall);
                return;
            }
        }
    }
    #endregion

     #region 坠落状态
    //坠落状态
    public class JinxiFallState : CharacterBaseState
    {
        //已处于坠落状态的时间
        private float _stateTimer;
        public JinxiFallState(CharacterStateMachine stateMachine, CharacterStateFactory factory) : base(stateMachine, factory) { }
        public override void EnterState()
        {
            //1.坠落动画
            FallingEnterAnimation();
            //2.重置坠落时间
            InitializeFallingState();
            //3.重置连招
        }

        #region EnterState子状态
        // 1. 进入坠落状态动画
        private void FallingEnterAnimation()
        {
            if (stateMachine.PreviousStateType == CharacterState.JinxiAirDash 
                || stateMachine.PreviousStateType == CharacterState.JinxiFloatDash 
                || stateMachine.PreviousStateType == CharacterState.JinxiJump
                || stateMachine.PreviousStateType == CharacterState.JinxiQBurst
                )
            {
                stateMachine.Animator.CrossFadeInFixedTime(stateMachine.GetLocomotionAnimationName(LocomotionAnimationId.Fall), 0.2f, 0, 0);
            }
            else
            {
                stateMachine.Animator.CrossFadeInFixedTime(stateMachine.GetLocomotionAnimationName(LocomotionAnimationId.Fall), 0, 0, 0);
            }

        }
        //2.初始化坠落状态
        private void InitializeFallingState()
        {
            //2.重置下坠时间
            _stateTimer = 0f;
        }
        #endregion

        public override void UpdateState()
        {
            //1.更新坠落时间
            _stateTimer += Time.deltaTime;
            //2.坠落逻辑
            stateMachine.movementLogic.HandleFallingMovement(stateMachine.MoveInput);
            stateMachine.movementLogic.ApplyGroundingForce();

            //3.状态转换判断
            CheckInterruptions();
        }

        #region UpdateState子状态

        #endregion

        public override void ExitState()
        {
            //1.退出坠落状态动画
            FallingExitAnimation();
            //2.落地垂直速度重置
            stateMachine.movementLogic.ResetVerticalVelocity();

        }

        #region ExitState子状态
        // 1. 退出坠落状态动画
        private void FallingExitAnimation()
        {

        }

        #endregion

        //状态转换判断
        private void CheckInterruptions()
        {
            // 死亡状态
            if (stateMachine.runtimeData.currentHealth <= 0)
            {
                SwitchState(CharacterState.JinxiDead);
                return;
            }
            //爆发状态
            if (stateMachine.JinxiSpecialSkillLinker.IsQBurstable() && stateMachine.CheckAndConsumeQBurstRequest())
            {
                stateMachine.IsStateLocked = false;
                SwitchState(CharacterState.JinxiQBurst);
                return;
            }
            //受击状态
            if (stateMachine.TryConsumeHitRequest())
            {
                SwitchState(CharacterState.JinxiHit);
                return;
            }
            //战技状态
            if (stateMachine.CheckAndConsumeESkillRequest() && stateMachine.JinxiSpecialSkillLinker.IsESkillable())
            {
                SwitchState(CharacterState.JinxiESkill);
                return;
            }
            //延奏状态
            if (stateMachine.CheckAndConsumeQteSkillRequest() && stateMachine.JinxiSpecialSkillLinker.IsQteSkillable())
            {
                SwitchState(CharacterState.JinxiQteSkill);
                return;
            }
            // 御空冲刺状态
            if (stateMachine.CheckAndConsumeDashRequest() && stateMachine.movementLogic.IsFloatDashAvailable())
            {
                SwitchState(CharacterState.JinxiFloatDash);
                return;
            }
            // 空中冲刺状态
            if (stateMachine.CheckAndConsumeDashRequest() && stateMachine.movementLogic.IsAirDashAvailable())
            {
                SwitchState(CharacterState.JinxiAirDash);
                return;
            }
            //御空攻击状态
            if (stateMachine.CheckAndConsumeAirAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsAirAttackable())
            {
                SwitchState(CharacterState.JinxiAirAttack);
                return;
            }
            //下落攻击状态
            if (stateMachine.CheckAndConsumeFallAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsFallAttackable())
            {
                SwitchState(CharacterState.JinxiFallAttack);
                return;
            }
            // 着地状态
            if (stateMachine.movementLogic.CustomCheckGrounded())
            {
                SwitchState(CharacterState.JinxiLand);
                return;
            }

        }
    }
    #endregion

     #region 着地状态
    // 着地状态
    public class JinxiLandState : CharacterBaseState
    {
        private float _landTimer; // 着地计时器

        public JinxiLandState(CharacterStateMachine stateMachine, CharacterStateFactory factory)
            : base(stateMachine, factory) { }

        public override void EnterState()
        {
            //1.初始化着地数据
            InitializeLandData();

            //2.播放着地动画
            LandEnterAnimation();
        }

        #region EnterState子方法
        //1.初始化着地数据
        private void InitializeLandData()
        {
            _landTimer = stateMachine.GetLocomotionAnimationLength(LocomotionAnimationId.Land);
        }

        //2.播放着地动画
        private void LandEnterAnimation()
        {if(stateMachine.PreviousPreviousStateType!=CharacterState.JinxiQBurst)
            {
                stateMachine.Animator.CrossFadeInFixedTime(stateMachine.GetLocomotionAnimationName(LocomotionAnimationId.Land), 0.1f, 0, 0);
            }else
            {
                stateMachine.Animator.CrossFadeInFixedTime(stateMachine.GetLocomotionAnimationName(LocomotionAnimationId.Land), 0.3f, 0, 0);
            }
           
        }
        #endregion

        public override void UpdateState()
        {
            //1.常态重力
            stateMachine.movementLogic.ApplyGroundingForce();

            //2.动画传参，防止BlendTree参数残留
            LandUpdateAnimation();

            //3.状态转换
            CheckStateTransitions();

            //4.更新着地计时
            UpdateLandTimer();
        }

        #region UpdateState子方法
        //2.更新着地动画参数
        private void LandUpdateAnimation()
        {
            stateMachine.movementLogic.UpdateFreeMoveAnimation(stateMachine.MoveInput, stateMachine.IsHoldingRun);
        }

        //4.更新着地计时
        private void UpdateLandTimer()
        {
            _landTimer -= Time.deltaTime;

            if (_landTimer > 0f)
            {
                return;
            }

            if (stateMachine.MoveInput.magnitude > stateMachine.movementLogic.moveThreshold)
            {
                SwitchState(CharacterState.JinxiMove);
                return;
            }

            SwitchState(CharacterState.JinxiIdle);
        }
        #endregion

        public override void ExitState()
        {
            //1.退出着地状态动画
            LandExitAnimation();

            //2.重置垂直速度
            stateMachine.movementLogic.ResetVerticalVelocity();
        }

        #region ExitState子方法
        //1.退出着地状态动画
        private void LandExitAnimation()
        {
            // CrossFade 直切动画后无需再清理 Trigger
        }
        #endregion

        //状态转换
        private void CheckStateTransitions()
        {
            //死亡状态
            if (stateMachine.runtimeData.currentHealth <= 0)
            {
                SwitchState(CharacterState.JinxiDead);
                return;
            }

            //爆发状态
            if (stateMachine.JinxiSpecialSkillLinker.IsQBurstable() && stateMachine.CheckAndConsumeQBurstRequest())
            {
                stateMachine.IsStateLocked = false;
                SwitchState(CharacterState.JinxiQBurst);
                return;
            }

            //受击状态
            if (stateMachine.TryConsumeHitRequest())
            {
                SwitchState(CharacterState.JinxiHit);
                return;
            }

            //战技状态
            if (stateMachine.CheckAndConsumeESkillRequest() && stateMachine.JinxiSpecialSkillLinker.IsESkillable())
            {
                SwitchState(CharacterState.JinxiESkill);
                return;
            }
            //延奏状态
            if (stateMachine.CheckAndConsumeQteSkillRequest() && stateMachine.JinxiSpecialSkillLinker.IsQteSkillable())
            {
                SwitchState(CharacterState.JinxiQteSkill);
                return;
            }

            //冲刺状态
            if (stateMachine.CheckAndConsumeDashRequest() && stateMachine.movementLogic.IsDashAvailable())
            {
                SwitchState(CharacterState.JinxiDash);
                return;
            }

            //跳跃状态
            if (stateMachine.CheckAndConsumeJumpRequest() && stateMachine.movementLogic.IsJumpAvailable())
            {
                SwitchState(CharacterState.JinxiJump);
                return;
            }

            //坠落状态
            if (!stateMachine.movementLogic.CustomCheckGrounded())
            {
                SwitchState(CharacterState.JinxiFall);
                return;
            }

            //御空攻击状态
            if (stateMachine.CheckAndConsumeAirAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsAirAttackable())
            {
                SwitchState(CharacterState.JinxiAirAttack);
                return;
            }

            //重击状态
            if (stateMachine.CheckAndConsumeHeavyAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsHeavyAttackable())
            {
                SwitchState(CharacterState.JinxiHeavyAttack);
                return;
            }

            //攻击状态
            if (stateMachine.CheckAndConsumeAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsAttackable())
            {
                SwitchState(CharacterState.JinxiAttack);
                return;
            }

            //移动状态
            if (stateMachine.MoveInput.magnitude > stateMachine.movementLogic.moveThreshold)
            {
                SwitchState(CharacterState.JinxiMove);
                return;
            }
        }
    }
    #endregion

     #region 攻击状态
    //攻击状态
    public class JinxiAttackState : CharacterBaseState
    {
        private enum AttackPhase
        {
            // 攻击执行阶段：核心攻击动作 不可打断 造成伤害 判定命中 
            Execution,
            //攻击恢复阶段：收招/后摇动画 可打断 自由退出攻击状态 
            Recovery
        }
        private AttackPhase _phase;
        private AttackStep _attackStep;

        private float _stateTime;//处于攻击状态的时长
        private bool hasUpataAttackingUpdateAnimation;
        public JinxiAttackState(CharacterStateMachine stateMachine, CharacterStateFactory factory) : base(stateMachine, factory) { }

        public override void EnterState()
        {
            //1.初始化攻击数据
            InitializeAttackData();
            //2.初始化攻击状态
            InitializeAttackState();
            //3.进入攻击状态动画
            AttackingEnterAnimation();
        }

        #region EnterState子方法
        //1.初始化攻击数据
        private void InitializeAttackData()
        {
            //1.获取攻击段数
            _attackStep = stateMachine.JinxiSpecialSkillLinker.InitializeNormalAttackStep();
            //2.同步攻击阶段信息
            stateMachine.currentStep = _attackStep;
        }
        //2.初始化攻击状态
        private void InitializeAttackState()
        {
            //清理攻击缓存
            stateMachine.CleanWantsToAttackRequest();
            //状态切换锁定
            stateMachine.IsStateLocked = true;
            //初始化攻击状态
            _phase = AttackPhase.Execution;
            //初始化阶段时长
            _stateTime = 0;
            //初始化数据
            hasUpataAttackingUpdateAnimation = false;
        }
        // 3. 进入攻击状态动画
        private void AttackingEnterAnimation()
        {
            //装饰剑隐藏
            stateMachine.manifestation.HideDecorationSwordFade();
            //龙隐藏
            stateMachine.JinxiSpecialSkillLinker.HideDragonInstantly();
            //攻击动画
            stateMachine.Animator.CrossFadeInFixedTime(stateMachine.GetCombatAnimationName(_attackStep.attackId), 0f, 0, 0);
            //御剑动画
            stateMachine.context?.WeaponController?.PlayWeaponAction(_attackStep);
            //龙动画
            stateMachine.JinxiSpecialSkillLinker.PlayDragonAction(_attackStep);
            //特效动画
            stateMachine.effectController?.PlayEffectAction(_attackStep);
        }
        #endregion

        public override void UpdateState()
        {
            //更新状态锁定
            UpdateStateTimeAndChangePhase();
            //更新攻击状态动画
            AttackingUpdateAnimation();
            //状态转换
            CheckStateTransitions();
        }

        #region UpdateState子方法
        //更新状态锁定
        private void UpdateStateTimeAndChangePhase()
        {
            _stateTime += Time.deltaTime;
            if (_stateTime > stateMachine.attackLogic.GetExecutionDuration(_attackStep))
            {
                if (_phase == AttackPhase.Execution)
                {
                    stateMachine.JinxiSpecialSkillLinker.StartNormalComboWindow();

                    stateMachine.IsStateLocked = false;
                }
                _phase = AttackPhase.Recovery;

            }
        }
        //更新攻击状态动画
        private void AttackingUpdateAnimation()
        {
            //防止动画出错
            stateMachine.movementLogic.UpdateFreeMoveAnimation(stateMachine.MoveInput, stateMachine.IsHoldingRun);
            if (_stateTime * 1.5f > stateMachine.GetCombatAnimationLength(_attackStep.attackId) && !hasUpataAttackingUpdateAnimation)
            {
                hasUpataAttackingUpdateAnimation = true;
                stateMachine.manifestation.ShowDecorationSwordFade();
            }
        }
        #endregion

        public override void ExitState()
        {
            //1.退出攻击状态动画
            AttackingExitAnimation();
            //2. 重置垂直速度
            stateMachine.movementLogic.ResetVerticalVelocity();
        }

        #region ExitState子状态
        // 1. 退出攻击状态动画
        private void AttackingExitAnimation()
        {
            // CrossFade 直切动画后无需再清理 Trigger
            //剑隐藏
            stateMachine.context?.WeaponController?.EndWeaponAction();
            //龙隐藏
            stateMachine.JinxiSpecialSkillLinker.HideDragonInstantly();
            //隐藏特效
            stateMachine.context?.EffectController?.EndEffectAction();
        }
        #endregion

        private void CheckStateTransitions()
        {
            // 死亡状态
            if (stateMachine.runtimeData.currentHealth <= 0)
            {
                SwitchState(CharacterState.JinxiDead);
                return;
            }
            //爆发状态
            if (stateMachine.JinxiSpecialSkillLinker.IsQBurstable() && stateMachine.CheckAndConsumeQBurstRequest())
            {
                stateMachine.IsStateLocked = false;
                SwitchState(CharacterState.JinxiQBurst);
                return;
            }
            // 受击状态
            if (stateMachine.TryConsumeHitRequest())
            {
                SwitchState(CharacterState.JinxiHit);
                return;
            }
            //战技状态
            if (stateMachine.CheckAndConsumeESkillRequest() && stateMachine.JinxiSpecialSkillLinker.IsESkillable())
            {
                SwitchState(CharacterState.JinxiESkill);
                return;
            }
            //延奏状态
            if (stateMachine.CheckAndConsumeQteSkillRequest() && stateMachine.JinxiSpecialSkillLinker.IsQteSkillable())
            {
                SwitchState(CharacterState.JinxiQteSkill);
                return;
            }
            //冲刺状态
            if (stateMachine.CheckAndConsumeDashRequest() && stateMachine.movementLogic.IsDashAvailable())
            {
                stateMachine.IsStateLocked = false;
                SwitchState(CharacterState.JinxiDash);
                return;
            }
            // 跳跃状态
            if (stateMachine.CheckAndConsumeJumpRequest() && stateMachine.movementLogic.IsJumpAvailable())
            {
                SwitchState(CharacterState.JinxiJump);
                return;
            }
            //重击状态
            if (stateMachine.CheckAndConsumeHeavyAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsHeavyAttackable())
            {
                SwitchState(CharacterState.JinxiHeavyAttack);
                return;
            }
            //攻击状态
            if (stateMachine.CheckAndConsumeAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsAttackable())
            {
                SwitchState(CharacterState.JinxiAttack);
                return;
            }

            //移动状态
            if (stateMachine.MoveInput.magnitude > stateMachine.movementLogic.moveThreshold && stateMachine.movementLogic.CustomCheckGrounded() && _stateTime >= 1f)
            {
                SwitchState(CharacterState.JinxiMove);
                return;
            }
            // 待机状态
            if (stateMachine.MoveInput.magnitude < stateMachine.movementLogic.moveThreshold && _stateTime >= stateMachine.GetCombatAnimationLength(_attackStep.attackId))
            {
                SwitchState(CharacterState.JinxiIdle);
                return;
            }
        }
    }
    #endregion

     #region 重击状态
    //重击状态
    public class JinxiHeavyAttackState : CharacterBaseState
    {
        private enum HeavyAttackPhase
        {
            Execution, // 重击执行阶段：核心动作，不可打断
            Recovery   // 重击恢复阶段：收招后摇，可打断
        }

        private HeavyAttackPhase _phase;
        private AttackStep _heavyAttackStep;

        private float _stateTime;//处于重击状态的时长
        private float _executionDuration;//重击执行阶段时长
        private float _animationLength;//重击动画总时长

        public JinxiHeavyAttackState(CharacterStateMachine stateMachine, CharacterStateFactory factory)
            : base(stateMachine, factory) { }

        public override void EnterState()
        {
            //1.初始化重击数据
            InitializeHeavyAttackData();

            //2.初始化重击状态
            InitializeHeavyAttackState();

            //3.进入重击状态动画
            HeavyAttackEnterAnimation();

            //4.消耗重击体力
            if (!stateMachine.JinxiSpecialSkillLinker.TryConsumeHeavyAttackStamina())
            {
                stateMachine.IsStateLocked = false;
                SwitchState(CharacterState.JinxiIdle);
                return;
            }
        }

        #region EnterState子方法
        //1.初始化重击数据
        private void InitializeHeavyAttackData()
        {
            //1.获取当前重击攻击段
            _heavyAttackStep = stateMachine.JinxiSpecialSkillLinker.InitializeHeavyAttackStep();

            //2.同步攻击阶段信息
            stateMachine.currentStep = _heavyAttackStep;

            //3.初始化重击执行阶段时长
            _executionDuration = stateMachine.attackLogic.GetExecutionDuration(_heavyAttackStep);

            //4.初始化重击动画总时长
            _animationLength = stateMachine.GetCombatAnimationLength(_heavyAttackStep.attackId);
        }

        //2.初始化重击状态
        private void InitializeHeavyAttackState()
        {
            //1.清理重击输入请求，避免重复触发
            stateMachine.CleanWantsToHeavyAttackRequest();

            //2.初始化状态时间
            _stateTime = 0f;

            //3.初始化重击阶段
            _phase = HeavyAttackPhase.Execution;

            //4.锁定状态，执行阶段不能随意切走
            stateMachine.IsStateLocked = true;
        }

        //3.进入重击状态动画
        private void HeavyAttackEnterAnimation()
        {
            //播放角色重击动画
            stateMachine.Animator.CrossFadeInFixedTime(stateMachine.GetCombatAnimationName(_heavyAttackStep.attackId), 0f, 0, 0);

            //播放武器表现
            stateMachine.context?.WeaponController?.PlayWeaponAction(_heavyAttackStep);

            //播放重击特效
            stateMachine.effectController?.PlayEffectAction(_heavyAttackStep);

            //隐藏装饰剑
            stateMachine.manifestation.HideDecorationSwordFade();
        }
        #endregion

        public override void UpdateState()
        {
            //1.更新重击阶段
            UpdateHeavyAttackPhase();

            //2.更新重击状态动画
            HeavyAttackUpdateAnimation();

            //3.状态转换
            CheckStateTransitions();
        }

        #region UpdateState子方法
        //1.更新重击阶段
        private void UpdateHeavyAttackPhase()
        {
            _stateTime += Time.deltaTime;

            //执行阶段结束后进入恢复阶段
            if (_phase == HeavyAttackPhase.Execution && _stateTime >= _executionDuration)
            {
                _phase = HeavyAttackPhase.Recovery;
                stateMachine.IsStateLocked = false;
            }
        }

        //2.更新重击状态动画
        private void HeavyAttackUpdateAnimation()
        {
            //同步移动混合树参数，防止退出重击时动画参数突变
            stateMachine.movementLogic.UpdateFreeMoveAnimation(stateMachine.MoveInput, stateMachine.IsHoldingRun);
        }
        #endregion

        public override void ExitState()
        {
            //1.退出重击状态动画
            HeavyAttackExitAnimation();

            //2.重置垂直速度
            stateMachine.movementLogic.ResetVerticalVelocity();
        }

        #region ExitState子方法
        //1.退出重击状态动画
        private void HeavyAttackExitAnimation()
        {
            //结束武器表现
            stateMachine.context?.WeaponController?.EndWeaponAction();

            //结束重击特效
            stateMachine.context?.EffectController?.EndEffectAction();

            //显示装饰剑
            stateMachine.manifestation.ShowDecorationSwordFade();
        }
        #endregion

        private void CheckStateTransitions()
        {
            //死亡状态
            if (stateMachine.runtimeData.currentHealth <= 0)
            {
                stateMachine.IsStateLocked = false;
                SwitchState(CharacterState.JinxiDead);
                return;
            }
            //爆发状态
            if (stateMachine.JinxiSpecialSkillLinker.IsQBurstable() && stateMachine.CheckAndConsumeQBurstRequest())
            {
                stateMachine.IsStateLocked = false;
                SwitchState(CharacterState.JinxiQBurst);
                return;
            }
            //受击状态
            if (stateMachine.TryConsumeHitRequest())
            {
                SwitchState(CharacterState.JinxiHit);
                return;
            }

            //战技状态
            if (stateMachine.CheckAndConsumeESkillRequest() && stateMachine.JinxiSpecialSkillLinker.IsESkillable())
            {
                SwitchState(CharacterState.JinxiESkill);
                return;
            }

            //延奏状态
            if (stateMachine.CheckAndConsumeQteSkillRequest() && stateMachine.JinxiSpecialSkillLinker.IsQteSkillable())
            {
                SwitchState(CharacterState.JinxiQteSkill);
                return;
            }

            //冲刺状态
            if (stateMachine.CheckAndConsumeDashRequest() && stateMachine.movementLogic.IsDashAvailable())
            {
                SwitchState(CharacterState.JinxiDash);
                return;
            }

            //跳跃状态
            if (stateMachine.CheckAndConsumeJumpRequest() && stateMachine.movementLogic.IsJumpAvailable())
            {
                SwitchState(CharacterState.JinxiJump);
                return;
            }

            //重击状态
            if (stateMachine.CheckAndConsumeHeavyAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsHeavyAttackable())
            {
                SwitchState(CharacterState.JinxiHeavyAttack);
                return;
            }

            //攻击状态
            if (stateMachine.CheckAndConsumeAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsAttackable())
            {
                SwitchState(CharacterState.JinxiAttack);
                return;
            }

            //移动状态
            if (stateMachine.MoveInput.magnitude > stateMachine.movementLogic.moveThreshold
                && stateMachine.movementLogic.CustomCheckGrounded()
                )
            {
                SwitchState(CharacterState.JinxiMove);
                return;
            }

            //待机状态
            if (stateMachine.MoveInput.magnitude < stateMachine.movementLogic.moveThreshold
                && _stateTime >= _animationLength)
            {
                SwitchState(CharacterState.JinxiIdle);
                return;
            }
        }
    }
    #endregion

     #region 下落攻击状态
    // 下落攻击状态
    public class JinxiFallAttackState : CharacterBaseState
    {
        private enum FallAttackPhase
        {
            start,  // 启动动作（正常重力）
            loop,   // 持续下落攻击（强重力，快速下砸）
            end,    // 收刀动作（接地后）
            over    // 收刀完成，可切换状态
        }
        public JinxiFallAttackState(CharacterStateMachine stateMachine, CharacterStateFactory factory)
        : base(stateMachine, factory) { }

        private float _stateTime;         // 总状态时长
        private float _endStateTime;      // 收刀阶段时长
        private bool hasUpdateFallAttackingAnimation;

        private FallAttackPhase _phase;   // 当前阶段
        private AttackStep _fallattackStepStart; // 启动动作数据
        private AttackStep _fallattackStepLoop;  // 持续下落数据
        private AttackStep _fallattackStepEnd;   // 收刀动作数据
        public override void EnterState()
        {
            //1.初始化化下落攻击数据
            InitliazeFallAttackingData();
            //2.初始化化下落攻击状态
            InitliazeFallAttackingState();
            //3.进入下落攻击状态动画
            FallAttackingEnterAnimation();
            //4.重置连招
        }

        #region EnterState子状态
        //1.初始化化下落攻击数据
        private void InitliazeFallAttackingData()
        {
            // 1. 动画数据赋值（确保 fallAttackSteps 有至少3个元素）
            if (stateMachine.JinxiSpecialSkillLinker.FallAttackSteps.Count >= 3)
            {
                _fallattackStepStart = stateMachine.JinxiSpecialSkillLinker.FallAttackSteps[0];
                _fallattackStepLoop = stateMachine.JinxiSpecialSkillLinker.FallAttackSteps[1];
                _fallattackStepEnd = stateMachine.JinxiSpecialSkillLinker.FallAttackSteps[2];
            }
            else
            {
                Debug.LogError("fallAttackSteps 列表元素不足3个！请在 Inspector 中配置 Start/Loop/End 三段数据。");
            }
            //2.同步攻击阶段信息
            stateMachine.currentStep = _fallattackStepStart;
        }
        //2.初始化化下落攻击状态
        private void InitliazeFallAttackingState()
        {
            //1.初始化数据
            _stateTime = 0f;
            _endStateTime = 0f;
            hasUpdateFallAttackingAnimation = false;
            //2.状态锁定
            stateMachine.IsStateLocked = true;
            //3.初始化下落攻击状态 
            _phase = FallAttackPhase.start;

        }
        // 3. 进入下落攻击状态动画
        private void FallAttackingEnterAnimation()
        {
            //下落攻击开始动画
            stateMachine.Animator.CrossFadeInFixedTime(stateMachine.GetCombatAnimationName(_fallattackStepStart.attackId), 0f, 0, 0);
            //装饰剑隐藏
            stateMachine.manifestation.HideDecorationSwordFade();
            //御剑
            stateMachine.context?.WeaponController?.PlayWeaponAction(_fallattackStepStart);
        }
        #endregion

        public override void UpdateState()
        {
            // 1. 三个阶段转换逻辑
            UpdateFallAttackingPhase();
            // 2.常态重力判断
            HandleGroundForce();
            // 3. 更新下落攻击状态动画
            FallAttackingUpdateAnimation();
            // 4. 状态切换判断
            CheckStateTransitions();
        }

        #region UpdateState子状态
        //1. 三个阶段转换逻辑
        private void UpdateFallAttackingPhase()
        {
            //1.状态时长递增
            _stateTime += Time.deltaTime;
            // 1. 强制守门：Start 动画没播完，什么都不做
            if (_stateTime <= stateMachine.GetCombatAnimationLength(_fallattackStepStart.attackId))
                return;
            // 2. 状态：start -> loop
            if (_phase == FallAttackPhase.start)
            {
                stateMachine.IsStateLocked = false;
                stateMachine.Animator.CrossFadeInFixedTime(stateMachine.GetCombatAnimationName(_fallattackStepLoop.attackId), 0f, 0, 0);
                //同步攻击阶段信息
                stateMachine.currentStep = _fallattackStepLoop;
                //御剑
                stateMachine.context?.WeaponController?.PlayWeaponAction(_fallattackStepLoop);
                _phase = FallAttackPhase.loop;
            }  // 3. 状态：loop -> end (着地)
            else if (_phase == FallAttackPhase.loop)
            {

                if (stateMachine.movementLogic.CustomCheckGrounded())
                {
                    //落地特效
                    stateMachine.effectController?.PlayEffectAction(_fallattackStepEnd);
                    _endStateTime = 0f;
                    stateMachine.Animator.CrossFadeInFixedTime(stateMachine.GetCombatAnimationName(_fallattackStepEnd.attackId), 0f, 0, 0);
                    //同步攻击阶段信息
                    stateMachine.currentStep = _fallattackStepEnd;
                    _phase = FallAttackPhase.end;
                }
            }  // 4. 状态：end -> over (计时)
            else if (_phase == FallAttackPhase.end)
            {
                _endStateTime += Time.deltaTime;
                if (_endStateTime > stateMachine.GetCombatAnimationLength(_fallattackStepEnd.attackId))
                {
                    _phase = FallAttackPhase.over;
                }
            }
        }
        // 2.常态重力判断
        private void HandleGroundForce()
        {
            if (_phase == FallAttackPhase.start && _stateTime >= 0.3f)
            {
                stateMachine.movementLogic.ApplyGroundingForce();
            }
            else if (_phase == FallAttackPhase.loop)
            {
                stateMachine.movementLogic.ApplyGroundingForce();
            }
            else if (_phase == FallAttackPhase.end)
            {

                stateMachine.movementLogic.ResetVerticalVelocity();// 落地垂直速度重置
            }
            else if (_phase == FallAttackPhase.over)
            {
                stateMachine.movementLogic.ApplyGroundingForce();
            }
        }
        // 3. 更新下落攻击状态动画
        private void FallAttackingUpdateAnimation()
        {
            if (_stateTime * 1.5f > stateMachine.GetCombatAnimationLength(_fallattackStepEnd.attackId) && !hasUpdateFallAttackingAnimation)
            {
                hasUpdateFallAttackingAnimation = true;
                //显示装饰剑
                stateMachine.manifestation.ShowDecorationSwordFade();
            }
        }
        #endregion

        public override void ExitState()
        {
            //1.退出下落攻击状态动画
            FallAttackingExitAnimation();
            //2.确保退出时重置垂直速度
            stateMachine.movementLogic.ResetVerticalVelocity();
        }

        #region ExitState子状态
        // 1. 退出下落攻击状态动画
        private void FallAttackingExitAnimation()
        {
            // CrossFade 直切动画后无需再清理 Trigger
            //隐藏特效
            stateMachine.context?.EffectController?.EndEffectAction();
        }
        #endregion

        private void CheckStateTransitions()
        {
            // 死亡状态
            if (stateMachine.runtimeData.currentHealth <= 0)
            {
                SwitchState(CharacterState.JinxiDead);
                return;
            }
            //爆发状态
            if (stateMachine.JinxiSpecialSkillLinker.IsQBurstable() && stateMachine.CheckAndConsumeQBurstRequest())
            {
                stateMachine.IsStateLocked = false;
                SwitchState(CharacterState.JinxiQBurst);
                return;
            }
            // 受击状态
            if (stateMachine.TryConsumeHitRequest())
            {
                SwitchState(CharacterState.JinxiHit);
                return;
            }
            //战技状态
            if (stateMachine.CheckAndConsumeESkillRequest() && stateMachine.JinxiSpecialSkillLinker.IsESkillable())
            {
                SwitchState(CharacterState.JinxiESkill);
                return;
            }
            //延奏状态
            if (stateMachine.CheckAndConsumeQteSkillRequest() && stateMachine.JinxiSpecialSkillLinker.IsQteSkillable())
            {
                SwitchState(CharacterState.JinxiQteSkill);
                return;
            }
            //冲刺状态
            if ((_phase == FallAttackPhase.end || _phase == FallAttackPhase.over) && stateMachine.CheckAndConsumeDashRequest() && stateMachine.movementLogic.IsDashAvailable())
            {
                SwitchState(CharacterState.JinxiDash);
                return;
            }
            // 空中冲刺状态
            if (stateMachine.CheckAndConsumeDashRequest() && stateMachine.movementLogic.IsAirDashAvailable() && _phase == FallAttackPhase.loop)
            {
                SwitchState(CharacterState.JinxiAirDash);
                return;
            }

            // 跳跃状态
            if ((_phase == FallAttackPhase.end || _phase == FallAttackPhase.over) && stateMachine.CheckAndConsumeJumpRequest() && stateMachine.movementLogic.IsJumpAvailable())
            {
                SwitchState(CharacterState.JinxiJump);
                return;
            }
            //重击状态
            if ((_phase == FallAttackPhase.end || _phase == FallAttackPhase.over) && stateMachine.CheckAndConsumeHeavyAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsHeavyAttackable())
            {
                SwitchState(CharacterState.JinxiHeavyAttack);
                return;
            }
            //攻击状态
            if ((_phase == FallAttackPhase.end || _phase == FallAttackPhase.over) && stateMachine.CheckAndConsumeAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsAttackable())
            {
                SwitchState(CharacterState.JinxiAttack);
                return;
            }
            //移动状态
            if ((_phase == FallAttackPhase.end || _phase == FallAttackPhase.over) && stateMachine.MoveInput.magnitude > stateMachine.movementLogic.moveThreshold)
            {
                SwitchState(CharacterState.JinxiMove);
                return;
            }
            // 待机状态
            if (stateMachine.MoveInput.magnitude < stateMachine.movementLogic.moveThreshold && _phase == FallAttackPhase.over)
            {
                SwitchState(CharacterState.JinxiIdle);
                return;
            }
        }
    }
    #endregion

     #region 御空攻击状态
    // 御空攻击状态
    public class JinxiAirAttackState : CharacterBaseState
    {
        private enum AirAttackPhase
        {
            Execution, // 攻击执行阶段：不可打断，造成伤害
            Recovery   // 攻击恢复阶段：可打断，开启连击窗口
        }
        private enum isGrouded
        {
            T,//是否在地面
            F
        }
        private AirAttackPhase _phase;
        private AttackStep _attackStep;
        private isGrouded _isGrouded;
        private float _stateTime;
        private bool _hasUpdatedExitAnimation;

        public JinxiAirAttackState(CharacterStateMachine stateMachine, CharacterStateFactory factory) : base(stateMachine, factory) { }

        public override void EnterState()
        {
            //1. 初始化御空攻击数据
            InitializeAttackData();
            //2. 初始化攻击状态
            InitializeAttackState();
            //3. 播放攻击动画与表现
            AirAttackingEnterAnimation();
        }

        #region EnterState子方法
        //1. 初始化御空攻击数据
        private void InitializeAttackData()
        {
            // 御空攻击使用SkillAttackSteps配置
            if (stateMachine.movementLogic.CustomCheckGrounded())
            {
                //御空攻击(地面模组)
                _attackStep = stateMachine.JinxiSpecialSkillLinker.InitializeAirAttackStep();
                _isGrouded = isGrouded.T;
            }
            else
            {
                //御空攻击(空中模组)
                _attackStep = stateMachine.JinxiSpecialSkillLinker.InitializeAirAttackStep();
                _isGrouded = isGrouded.F;
            }

            stateMachine.currentStep = _attackStep;
        }
        //2. 初始化攻击状态
        private void InitializeAttackState()
        {
            // 清理攻击缓存
            stateMachine.CleanWantsToAirAttackRequest();
            // 状态锁定
            stateMachine.IsStateLocked = true;
            // 初始化阶段与计时
            _phase = AirAttackPhase.Execution;
            _stateTime = 0f;
            _hasUpdatedExitAnimation = false;
        }
        //3. 播放攻击动画与表现
        private void AirAttackingEnterAnimation()
        {
            // 装饰剑隐藏
            stateMachine.manifestation.HideDecorationSwordFade();
            // 播放攻击动画
            stateMachine.Animator.CrossFadeInFixedTime(stateMachine.GetCombatAnimationName(_attackStep.attackId), 0f, 0, 0);
            // 御剑/龙/特效表现
            stateMachine.context?.WeaponController?.PlayWeaponAction(_attackStep);
            stateMachine.JinxiSpecialSkillLinker.PlayDragonAction(_attackStep);
            stateMachine.effectController?.PlayEffectAction(_attackStep);
        }
        #endregion

        public override void UpdateState()
        {
            //1. 更新状态阶段与锁定
            UpdateStateTimeAndChangePhase();
            //2. 动画参数同步
            AirAttackingUpdateAnimation();
            //4. 状态切换判断
            CheckStateTransitions();
        }

        #region UpdateState子方法
        //1. 更新状态阶段与锁定
        private void UpdateStateTimeAndChangePhase()
        {
            _stateTime += Time.deltaTime;
            if (_phase == AirAttackPhase.Execution && _stateTime > stateMachine.attackLogic.GetExecutionDuration(_attackStep))
            {
                // 进入恢复阶段，开启连击窗口
                stateMachine.JinxiSpecialSkillLinker.StartAirComboWindow();
                stateMachine.IsStateLocked = false;
                _phase = AirAttackPhase.Recovery;
            }
        }
        //2. 动画参数同步
        private void AirAttackingUpdateAnimation()
        {
            // 防止动画出错
            stateMachine.movementLogic.UpdateFreeMoveAnimation(stateMachine.MoveInput, stateMachine.IsHoldingRun);
            // 动画快结束时显示装饰剑
            if (_stateTime * 1.2f > stateMachine.GetCombatAnimationLength(_attackStep.attackId) && !_hasUpdatedExitAnimation)
            {
                _hasUpdatedExitAnimation = true;
                stateMachine.manifestation.ShowDecorationSwordFade();
            }
        }
        #endregion

        public override void ExitState()
        {
            //1.退出动画相关
            AirAttackingExitAnimation();
            //2.重置垂直速度
            stateMachine.movementLogic.ResetVerticalVelocity();
        }

        #region ExitState子方法
        //1.退出动画相关
        private void AirAttackingExitAnimation()
        {
            // CrossFade 直切动画后无需再清理 Trigger
            //结束御剑表现
            stateMachine.context?.WeaponController?.EndWeaponAction();
            //隐藏龙
            stateMachine.JinxiSpecialSkillLinker.HideDragonInstantly();
            //隐藏特效
            stateMachine.context?.EffectController?.EndEffectAction();

        }
        #endregion

        private void CheckStateTransitions()
        {
            //地面
            if (_isGrouded == isGrouded.T)
            {
                // 死亡状态
                if (stateMachine.runtimeData.currentHealth <= 0)
                {
                    stateMachine.IsStateLocked = false;
                    SwitchState(CharacterState.JinxiDead);
                    return;
                }
                // 爆发状态
                if (stateMachine.JinxiSpecialSkillLinker.IsQBurstable() && stateMachine.CheckAndConsumeQBurstRequest())
                {
                    stateMachine.IsStateLocked = false;
                    SwitchState(CharacterState.JinxiQBurst);
                    return;
                }
                // 受击状态
                if (stateMachine.TryConsumeHitRequest())
                {
                    stateMachine.IsStateLocked = false;
                    SwitchState(CharacterState.JinxiHit);
                    return;
                }
                //战技状态
                if (stateMachine.CheckAndConsumeESkillRequest() && stateMachine.JinxiSpecialSkillLinker.IsESkillable() && _phase == AirAttackPhase.Recovery)
                {
                    SwitchState(CharacterState.JinxiESkill);
                    return;
                }
                //延奏状态
                if (stateMachine.CheckAndConsumeQteSkillRequest() && stateMachine.JinxiSpecialSkillLinker.IsQteSkillable())
                {
                    SwitchState(CharacterState.JinxiQteSkill);
                    return;
                }
                // 冲刺状态
                if (stateMachine.CheckAndConsumeDashRequest() && stateMachine.movementLogic.IsDashAvailable())
                {
                    stateMachine.IsStateLocked = false;
                    SwitchState(CharacterState.JinxiDash);
                    return;
                }
                // 跳跃状态
                if (stateMachine.CheckAndConsumeJumpRequest() && stateMachine.movementLogic.IsJumpAvailable() && _phase == AirAttackPhase.Recovery)
                {
                    SwitchState(CharacterState.JinxiJump);
                    return;
                }
                //御空攻击状态
                if (stateMachine.CheckAndConsumeAirAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsAirAttackable())
                {
                    SwitchState(CharacterState.JinxiAirAttack);
                    return;
                }
                //重击状态
                if (stateMachine.CheckAndConsumeHeavyAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsHeavyAttackable() && _phase == AirAttackPhase.Recovery)
                {
                    SwitchState(CharacterState.JinxiHeavyAttack);
                    return;
                }
                // 攻击
                if (stateMachine.CheckAndConsumeAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsAttackable() && _phase == AirAttackPhase.Recovery)
                {
                    SwitchState(CharacterState.JinxiAttack);
                    return;
                }
                //移动状态
                if (stateMachine.MoveInput.magnitude > stateMachine.movementLogic.moveThreshold && stateMachine.movementLogic.CustomCheckGrounded() && _phase == AirAttackPhase.Recovery)
                {
                    SwitchState(CharacterState.JinxiMove);
                    return;
                }
                // 待机状态
                if (stateMachine.MoveInput.magnitude < stateMachine.movementLogic.moveThreshold && _stateTime >= stateMachine.GetCombatAnimationLength(_attackStep.attackId))
                {
                    SwitchState(CharacterState.JinxiIdle);
                    return;
                }
            }
            //空中
            if (_isGrouded == isGrouded.F)
            {
                // 死亡状态
                if (stateMachine.runtimeData.currentHealth <= 0)
                {
                    stateMachine.IsStateLocked = false;
                    SwitchState(CharacterState.JinxiDead);
                    return;
                }
                // 爆发状态
                if (stateMachine.JinxiSpecialSkillLinker.IsQBurstable() && stateMachine.CheckAndConsumeQBurstRequest())
                {
                    stateMachine.IsStateLocked = false;
                    SwitchState(CharacterState.JinxiQBurst);
                    return;
                }
                // 受击状态
                if (stateMachine.TryConsumeHitRequest())
                {
                    stateMachine.IsStateLocked = false;
                    SwitchState(CharacterState.JinxiHit);
                    return;
                }
                //战技状态
                if (stateMachine.CheckAndConsumeESkillRequest() && stateMachine.JinxiSpecialSkillLinker.IsESkillable() && _phase == AirAttackPhase.Recovery)
                {
                    SwitchState(CharacterState.JinxiESkill);
                    return;
                }
                //御空冲刺状态
                if (stateMachine.CheckAndConsumeDashRequest() && stateMachine.movementLogic.IsFloatDashAvailable())
                {
                    stateMachine.IsStateLocked = false;
                    SwitchState(CharacterState.JinxiFloatDash);
                    return;
                }
                //坠落状态
                if (!stateMachine.movementLogic.CustomCheckGrounded() && _stateTime >= stateMachine.GetCombatAnimationLength(_attackStep.attackId))
                {
                    SwitchState(CharacterState.JinxiFall);
                    return;
                }
                //御空攻击状态
                if (stateMachine.CheckAndConsumeAirAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsAirAttackable())
                {
                    SwitchState(CharacterState.JinxiAirAttack);
                    return;
                }
                //移动状态
                if (stateMachine.MoveInput.magnitude > stateMachine.movementLogic.moveThreshold && stateMachine.movementLogic.CustomCheckGrounded() && _phase == AirAttackPhase.Recovery)
                {
                    SwitchState(CharacterState.JinxiMove);
                    return;
                }
                // 待机状态
                if (stateMachine.MoveInput.magnitude < stateMachine.movementLogic.moveThreshold && _stateTime >= stateMachine.GetCombatAnimationLength(_attackStep.attackId))
                {
                    SwitchState(CharacterState.JinxiIdle);
                    return;
                }
            }

        }
    }
    #endregion

     #region 冲刺状态
    //冲刺状态
    public class JinxiDashState : CharacterBaseState
    {
        private enum DashPhase
        {
            Displacement, // 位移阶段：代码驱动移动，仅再次冲刺可打断
            Stopping       // 急停阶段：纯动画表演，任意动作可打断
        }
        //private DashPhase _phase;//冲刺状态的两个阶段
        private float DashLockTime;//冲刺方向锁定时间
        private bool _dashDirection;//冲刺方向
        private float _stateTimer;//已处于冲刺状态的时间
        public JinxiDashState(CharacterStateMachine stateMachine, CharacterStateFactory factory) : base(stateMachine, factory) { }
        public override void EnterState()
        {
            //1.初始化阶段 时长 时间 清理冲刺输入缓存 方向 
            InitializeDashData();  
            //2.计算方向（调用CharacterMovement）
            _dashDirection = stateMachine.movementLogic.CalculateDirection(stateMachine.MoveInput);
            //3.计算上次冲刺间隔  重置连冲资格 消耗1次冲刺次数 启动内置 CD 并更新时间戳（调用CharacterMovement）
            stateMachine.movementLogic.CalculateAndUpdateDashCounter();
            //4.检查并施加全局 CD 惩罚（调用CharacterMovement）
            stateMachine.movementLogic.ApplyPenaltyIfNecessary();
            //5.进入冲刺状态动画
            DashingEnterAnimation();
            //6.消耗体力
            if (!stateMachine.movementLogic.TryConsumeDashStamina())
            {
                stateMachine.IsStateLocked = false;
                SwitchState(stateMachine.MoveInput.magnitude > stateMachine.movementLogic.moveThreshold ? CharacterState.JinxiMove : CharacterState.JinxiIdle);
                return;
            }
        }

        #region EnterState子方法  
        //1.初始化阶段 时长 时间 清理冲刺输入缓存 方向 
        private void InitializeDashData()
        {
            // 清理冲刺输入缓存
            stateMachine.CleanWantsToDashRequest();
            ////初始化阶段
            //_phase = DashPhase.Displacement;
            //冲刺阶段状态锁定
            stateMachine.IsStateLocked = true;
            //冲刺时长
            DashLockTime = stateMachine.movementLogic.dashCostTime;
            //重置已处于冲刺状态的时间
            _stateTimer = 0f;
        }
        //5.进入冲刺状态动画
        private void DashingEnterAnimation()
        {
            if (_dashDirection)
                stateMachine.Animator.CrossFadeInFixedTime(stateMachine.GetLocomotionAnimationName(LocomotionAnimationId.DashForward), 0f, 0, 0);
            else
                stateMachine.Animator.CrossFadeInFixedTime(stateMachine.GetLocomotionAnimationName(LocomotionAnimationId.DashBackward), 0f, 0, 0);
        }
        #endregion

        public override void UpdateState()
        {
            //1.状态时间更新与解锁
            UpdateAndUnlocked();
            //2.更新冲刺状态动画
            DashingUpdateAnimation();
            //3.常态重力
            stateMachine.movementLogic.ApplyGroundingForce();
            //4.状态转换判断
            CheckStateTransitions();
        }

        #region UpdateState子方法
        //1.状态时间更新与解锁
        private void UpdateAndUnlocked()
        {
            _stateTimer += Time.deltaTime;
            if (_stateTimer >= DashLockTime)
            {
                stateMachine.IsStateLocked = false;
                //_phase = DashPhase.Stopping;
            }
        }
        //2.更新冲刺状态动画
        private void DashingUpdateAnimation()
        {
            //1.传入参数防止错误
            stateMachine.movementLogic.UpdateFreeMoveAnimation(stateMachine.MoveInput, stateMachine.IsHoldingRun);
        }
        #endregion

        public override void ExitState()
        {
            //1.退出冲刺状态动画
            DashingExitAnimation();
            //2.确保退出时重置垂直速度
            stateMachine.movementLogic.ResetVerticalVelocity();
        }

        #region ExitState子方法
        //1.退出冲刺状态动画
        private void DashingExitAnimation()
        {
            // CrossFade 直切动画后无需再清理 Trigger
        }
        #endregion

        private void CheckStateTransitions()
        {
            // 死亡状态
            if (stateMachine.runtimeData.currentHealth <= 0)
            {
                SwitchState(CharacterState.JinxiDead);
                return;
            }
            //爆发状态
            if (stateMachine.JinxiSpecialSkillLinker.IsQBurstable() && stateMachine.CheckAndConsumeQBurstRequest())
            {
                stateMachine.IsStateLocked = false;
                SwitchState(CharacterState.JinxiQBurst);
                return;
            }
            //受击状态
            if (stateMachine.TryConsumeHitRequest())
            {
                SwitchState(CharacterState.JinxiHit);
                return;
            }
            //战技状态
            if (stateMachine.CheckAndConsumeESkillRequest() && stateMachine.JinxiSpecialSkillLinker.IsESkillable())
            {
                SwitchState(CharacterState.JinxiESkill);
                return;
            }
            //延奏状态
            if (stateMachine.CheckAndConsumeQteSkillRequest() && stateMachine.JinxiSpecialSkillLinker.IsQteSkillable())
            {
                SwitchState(CharacterState.JinxiQteSkill);
                return;
            }
            //冲刺状态
            if ((stateMachine.WantsToDash && stateMachine.movementLogic.IsDashAvailable()))
            {
                SwitchState(CharacterState.JinxiDash);
                return;
            }
            // 跳跃状态
            if (stateMachine.CheckAndConsumeJumpRequest() && stateMachine.movementLogic.IsJumpAvailable())
            {
                SwitchState(CharacterState.JinxiJump);
                return;
            }
            //御空攻击状态
            if (stateMachine.CheckAndConsumeAirAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsAirAttackable())
            {
                SwitchState(CharacterState.JinxiAirAttack);
                return;
            }
            //重击状态
            if (stateMachine.CheckAndConsumeHeavyAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsHeavyAttackable())
            {
                SwitchState(CharacterState.JinxiHeavyAttack);
                return;
            }
            //攻击状态
            if (stateMachine.CheckAndConsumeAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsAttackable())
            {
                SwitchState(CharacterState.JinxiAttack);
                return;
            }
            // 移动状态
            if (stateMachine.MoveInput.magnitude > stateMachine.movementLogic.moveThreshold && stateMachine.movementLogic.CustomCheckGrounded())
            {
                SwitchState(CharacterState.JinxiMove);
                return;
            }
            // 待机状态
            if (stateMachine.MoveInput.magnitude < stateMachine.movementLogic.moveThreshold && _stateTimer >= 1.33f && stateMachine.movementLogic.CustomCheckGrounded())
            {
                SwitchState(CharacterState.JinxiIdle);
                return;
            }
        }
    }
    #endregion

     #region 空中冲刺状态
    //空中冲刺状态
    public class JinxiAirDashState : CharacterBaseState
    {
        private float _stateTime; //空中冲刺状态时长
        private float _airdashTimer = 0.5f; // 空中冲刺持续时间计时器
        private bool _airDashDirection;// 冲刺朝向标识：true=向前 false=向后

        public JinxiAirDashState(CharacterStateMachine stateMachine, CharacterStateFactory factory) : base(stateMachine, factory) { }

        public override void EnterState()
        {
            //1.初始化空中冲刺数据 计算空中冲刺方向与朝向
            InitializeAirDashingData();
            //2.初始化空中冲刺状态
            InitializeAirDashingState();
            //3.进入空中冲刺状态动画
            AirDashingEnterAnimation();
            //4.消耗空中冲刺体力
            if (!stateMachine.movementLogic.TryConsumeAirDashStamina())
            {
                stateMachine.IsStateLocked = false;
                SwitchState(CharacterState.JinxiFall);
                return;
            }
        }

        #region EnterState子方法
        //1.初始化空中冲刺数据 计算空中冲刺方向与朝向
        private void InitializeAirDashingData()
        {
            _airDashDirection = stateMachine.movementLogic.CalculateDirection(stateMachine.MoveInput);
        }
        //2.初始化空中冲刺状态
        private void InitializeAirDashingState()
        {
            // 清理冲刺输入请求，避免重复触发
            stateMachine.CleanWantsToDashRequest();
            //初始化状态时间
            _stateTime = 0f;
            // 锁定状态：冲刺期间禁止切换其他状态
            stateMachine.IsStateLocked = true;
            // 标记空中冲刺已使用（落地自动重置）
            stateMachine.movementLogic.HasAirDashed = true;
        }
        //3.进入空中冲刺状态动画
        private void AirDashingEnterAnimation()
        {
            // 播放对应方向的空中冲刺动画
            if (_airDashDirection)
            {
                //stateMachine.Animator.SetTrigger("AirDashF");
                stateMachine.Animator.CrossFadeInFixedTime(stateMachine.GetLocomotionAnimationName(LocomotionAnimationId.AirDashForward), 0f, 0, 0);
            }
            else
            {
                //stateMachine.Animator.SetTrigger("AirDashB");
                stateMachine.Animator.CrossFadeInFixedTime(stateMachine.GetLocomotionAnimationName(LocomotionAnimationId.AirDashBackward), 0f, 0, 0);
            }
        }
        #endregion

        public override void UpdateState()
        {
            //1.计时器更新
            UpdateAndUnlockStateTime();
            //2.状态转换
            CheckStateTransitions();
        }

        #region UpdateState子方法
        //1.计时器更新
        private void UpdateAndUnlockStateTime()
        {
            // 空中冲刺计时器更新
            _stateTime += Time.deltaTime;
            if (_stateTime >= _airdashTimer)
                stateMachine.IsStateLocked = false;
        }
        #endregion
        public override void ExitState()
        {
            //1.退出空中冲刺状态动画
            AirDashingExitAnimation();
            //2.确保退出时重置垂直速度
            stateMachine.movementLogic.ResetVerticalVelocity();
        }

        #region ExitState子方法
        //1.退出空中冲刺状态动画
        private void AirDashingExitAnimation()
        {
            // 清理空中冲刺动画 Trigger
            if (_airDashDirection)
            {
                //stateMachine.Animator.ResetTrigger("AirDashF");
            }
            else
            {
                //stateMachine.Animator.ResetTrigger("AirDashB");
            }
        }
        #endregion
        //状态转换
        private void CheckStateTransitions()
        {
            //爆发状态
            if (stateMachine.JinxiSpecialSkillLinker.IsQBurstable() && stateMachine.CheckAndConsumeQBurstRequest())
            {
                stateMachine.IsStateLocked = false;
                SwitchState(CharacterState.JinxiQBurst);
                return;
            }
            //战技状态
            if (stateMachine.CheckAndConsumeESkillRequest() && stateMachine.JinxiSpecialSkillLinker.IsESkillable())
            {
                SwitchState(CharacterState.JinxiESkill);
                return;
            }
            //延奏状态
            if (stateMachine.CheckAndConsumeQteSkillRequest() && stateMachine.JinxiSpecialSkillLinker.IsQteSkillable())
            {
                SwitchState(CharacterState.JinxiQteSkill);
                return;
            }
            //御空攻击状态
            if (stateMachine.CheckAndConsumeAirAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsAirAttackable())
            {
                SwitchState(CharacterState.JinxiAirAttack);
                return;
            }
            //下落攻击状态
            if (stateMachine.CheckAndConsumeFallAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsFallAttackable())
            {
                SwitchState(CharacterState.JinxiFallAttack);
                return;
            }
            //下落状态
            if (!stateMachine.movementLogic.CustomCheckGrounded())
            {
                SwitchState(CharacterState.JinxiFall);
                return;
            }
            // 着地状态
            if (stateMachine.movementLogic.CustomCheckGrounded())
            {
                SwitchState(CharacterState.JinxiLand);
                return;
            }
        }
    }
    #endregion

     #region 御空冲刺状态
    // 御空冲刺状态
    public class JinxiFloatDashState : CharacterBaseState
    {
        private float _stateTime;//御空冲刺状态时长
        private float _floatDashTimer;//御空冲刺持续时间计时器
        private bool _floatDashDirection;//御空冲刺方向
        public JinxiFloatDashState(CharacterStateMachine stateMachine, CharacterStateFactory factory) : base(stateMachine, factory) { }
        public override void EnterState()
        {
            //1.初始化御空冲刺数据
            InitializeFloatDashingData();
            //2.初始化御空冲刺状态
            InitializeFloatDashingState();
            //3.进入御空冲刺状态动画
            FloatDashingEnterAnimation();
            //4.消耗体力
            if (!stateMachine.movementLogic.TryConsumeFloatDashStamina())
            {
                stateMachine.IsStateLocked = false;
                SwitchState(CharacterState.JinxiFall);
                return;
            }
        }

        #region EnterState子方法
        //1.初始化御空冲刺数据
        private void InitializeFloatDashingData()
        {
            // 计算御空冲刺方向 + 旋转至八方向(前/后/左/右/四斜向)
            _floatDashDirection = stateMachine.movementLogic.CalculateFloatDashDirection(stateMachine.MoveInput);
            //初始化御空冲刺时长数据
            _floatDashTimer = stateMachine.movementLogic.floatDashCostTime;
        }
        //2.初始化御空冲刺状态
        private void InitializeFloatDashingState()
        {
            // 清理冲刺输入请求，避免重复触发
            stateMachine.CleanWantsToDashRequest();
            //初始化状态时间
            _stateTime = 0f;
            // 锁定状态：冲刺期间禁止切换其他状态
            stateMachine.IsStateLocked = true;
        }
        //3.进入御空冲刺状态动画
        private void FloatDashingEnterAnimation()
        {
            if (_floatDashDirection)
            {
                stateMachine.Animator.CrossFadeInFixedTime(stateMachine.GetLocomotionAnimationName(LocomotionAnimationId.FloatDashingForward), 0f, 0, 0);
            }
            else
            {
                stateMachine.Animator.CrossFadeInFixedTime(stateMachine.GetLocomotionAnimationName(LocomotionAnimationId.FloatDashingBackward), 0f, 0, 0);
            }
        }
        #endregion
        public override void UpdateState()
        {
            //1.计时器更新
            UpdateAndUnlockStateTime();
            //2.同步动画参数
            FloatDashingUpdateAnimation();
            //3.状态转换
            CheckStateTransitions();
        }

        #region UpdateState子方法
        //1.计时器更新
        private void UpdateAndUnlockStateTime()
        {
            _stateTime += Time.deltaTime;
            if (_stateTime >= _floatDashTimer)
            {
                stateMachine.IsStateLocked = false;
            }
        }
        //2.同步动画参数
        private void FloatDashingUpdateAnimation()
        {
            stateMachine.movementLogic.UpdateFreeMoveAnimation(stateMachine.MoveInput, stateMachine.IsHoldingRun);
        }
        #endregion

        public override void ExitState()
        {
            //1.退出御空冲刺状态
            FloatDashingExitAnimation();
            //2.确保退出时重置垂直速度
            stateMachine.movementLogic.ResetVerticalVelocity();
        }

        #region ExitState子方法
        //1.退出御空冲刺状态
        private void FloatDashingExitAnimation()
        {
        }
        #endregion

        //状态转换
        private void CheckStateTransitions()
        {
            // 死亡状态
            if (stateMachine.runtimeData.currentHealth <= 0)
            {
                stateMachine.IsStateLocked = false;
                SwitchState(CharacterState.JinxiDead);
                return;
            }
            //爆发状态
            if (stateMachine.JinxiSpecialSkillLinker.IsQBurstable() && stateMachine.CheckAndConsumeQBurstRequest())
            {
                stateMachine.IsStateLocked = false;
                SwitchState(CharacterState.JinxiQBurst);
                return;
            }
            //受击状态
            if (stateMachine.TryConsumeHitRequest())
            {
                stateMachine.IsStateLocked = false;
                SwitchState(CharacterState.JinxiHit);
                return;
            }
            //战技状态
            if (stateMachine.CheckAndConsumeESkillRequest() && stateMachine.JinxiSpecialSkillLinker.IsESkillable())
            {
                SwitchState(CharacterState.JinxiESkill);
                return;
            }
            //延奏状态
            if (stateMachine.CheckAndConsumeQteSkillRequest() && stateMachine.JinxiSpecialSkillLinker.IsQteSkillable())
            {
                SwitchState(CharacterState.JinxiQteSkill);
                return;
            }
            //御空冲刺状态
            if (stateMachine.WantsToDash && stateMachine.movementLogic.IsFloatDashAvailable() && _stateTime >= 0.5f)
            {
                stateMachine.IsStateLocked = false;
                SwitchState(CharacterState.JinxiFloatDash);
                return;
            }
            //御空攻击状态
            if (stateMachine.CheckAndConsumeAirAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsAirAttackable())
            {
                SwitchState(CharacterState.JinxiAirAttack);
                return;
            }
            //坠落状态
            if (!stateMachine.JinxiSpecialSkillLinker.IsFloating || _stateTime >= 1.9f)
            {
                SwitchState(CharacterState.JinxiFall);
                return;
            }
        }
    }
    #endregion

     #region 闪避状态
    // 闪避状态
    public class JinxiDodgeState : CharacterBaseState
    {
        public JinxiDodgeState(CharacterStateMachine stateMachine, CharacterStateFactory factory) : base(stateMachine, factory) { }

        public override void EnterState()
        {
            // 初始化：锁死状态+标记闪避+开启无敌+播放闪避动画+停止冲刺
            stateMachine.IsStateLocked = true;

            stateMachine.Animator.CrossFadeInFixedTime(stateMachine.GetLocomotionAnimationName(LocomotionAnimationId.Dodge), 0f, 0, 0);

        }

        public override void UpdateState()
        {

            // 仅判断动画结束，无其他逻辑（闪避期间禁止所有操作/状态切换）
            CheckStateTransitions();
        }



        public override void ExitState()
        {
            // 清理：解锁状态+清除标记+关闭无敌+重置触发器
            stateMachine.IsStateLocked = false;

            // CrossFade 直切动画后无需再清理 Trigger
        }

        private void CheckStateTransitions()
        {
            // 唯一切换条件：闪避动画播放完成（无敌期间不处理任何死亡/受击）
            AnimatorStateInfo stateInfo = stateMachine.Animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsTag("Dodge") && stateInfo.normalizedTime >= 1f)
            {
                // 动画结束后，根据移动输入切回Idle/JinxiMove
                if (stateMachine.MoveInput.magnitude > stateMachine.movementLogic.moveThreshold)
                {
                    SwitchState(CharacterState.JinxiMove);
                }
                else
                {
                    SwitchState(CharacterState.JinxiIdle);
                }
            }
        }
    }
    #endregion

     #region 御空闪避状态
    // 御空闪避状态
    public class JinxiFloatDodgeState : CharacterBaseState
    {
        public JinxiFloatDodgeState(CharacterStateMachine stateMachine, CharacterStateFactory factory) : base(stateMachine, factory) { }
        public override void EnterState() { }
        public override void UpdateState() { }

        public override void ExitState() { }
    }
    #endregion

     #region 战技状态（流光夕影）（神霓飞芒）（逐天取月）（乘岁凌霄）
    // 战技状态
    public class JinxiESkillState : CharacterBaseState
    {
        public JinxiESkillState(CharacterStateMachine stateMachine, CharacterStateFactory factory) : base(stateMachine, factory) { }
        private enum ESkillType
        {
            ESkill1,//普通战技（无前置，CD好了就能用）流光夕影
            ESkill2,//普攻第四段解锁的战技（地面空中都能用，进御空状态）神霓飞芒
            ESkill3,//御空战技（ESkill2后窗口内可用）逐天取月
            ESkill4//御空强化战技（御空普攻第四段后窗口内可用）惊龙破空
        }
        private enum ESkillStatePhase
        {
            Execution, // 技能执行阶段（不可打断）
            Recovery   // 技能恢复阶段（可打断）
        }

        private ESkillType _eSkillType;
        private ESkillStatePhase _statePhase;
        private AttackStep _currentSkillStep;
        private float _stateTime;
        private float _executionDuration; // 执行阶段时长

        public override void EnterState()
        {
            //1.初始化战技状态数据
            InitializeESkillData();
            //2.初始化战技状态
            InitializeState();
            //3. 播放技能动画与表现
            ESkillEnterAnimation();
        }

        #region EnterState子方法
        //1.初始化战技状态数据
        private void InitializeESkillData()
        {
            //初始化攻击阶段
            _currentSkillStep = stateMachine.JinxiSpecialSkillLinker.InitializeESkillStep();
            // 初始化执行阶段时长
            _executionDuration = stateMachine.attackLogic.GetExecutionDuration(_currentSkillStep);
            //初始化战技类型
            switch (_currentSkillStep.attackId)
            {
                case AttackId.ESkill04:
                    _eSkillType = ESkillType.ESkill4;
                    stateMachine.JinxiSpecialSkillLinker.OnESkill4Used();
                    return;
                case AttackId.ESkill03:
                    _eSkillType = ESkillType.ESkill3;
                    stateMachine.JinxiSpecialSkillLinker.OnESkill3Used();
                    return;
                case AttackId.ESkill02:
                    _eSkillType = ESkillType.ESkill2;
                    stateMachine.JinxiSpecialSkillLinker.OnESkill2Used();
                    return;
                default:
                    _eSkillType = ESkillType.ESkill1;
                    stateMachine.JinxiSpecialSkillLinker.OnESkill1Used();
                    return;
            }
        }
        //2.初始化战技状态
        private void InitializeState()
        {
            //1. 消费E技能请求
            stateMachine.CleanWantsToESkillRequest();
            //2.数据初始化
            _statePhase = ESkillStatePhase.Execution;
            _stateTime = 0f;
            //3.切换锁定
            stateMachine.IsStateLocked = true;
        }
        //3. 播放技能动画与表现
        private void ESkillEnterAnimation()
        {
            // 播放角色动画
            stateMachine.Animator.CrossFadeInFixedTime(stateMachine.GetCombatAnimationName(_currentSkillStep.attackId), 0f, 0, 0);
            // 御剑/龙/特效表现
            stateMachine.context?.WeaponController?.PlayWeaponAction(_currentSkillStep);
            stateMachine.JinxiSpecialSkillLinker.PlayDragonAction(_currentSkillStep);
            stateMachine.effectController?.PlayEffectAction(_currentSkillStep);
            // 隐藏装饰剑
            stateMachine.manifestation.HideDecorationSwordFade();
        }
        #endregion

        public override void UpdateState()
        {
            // 1. 更新状态阶段
            UpdateStatePhase();
            //2. 更新技能动画与表现
            ESkillUpdateAnimation();
            // 3. 状态切换判断
            CheckStateTransitions();
        }

        #region UpdateState子方法
        // 1. 更新状态阶段
        private void UpdateStatePhase()
        {
            _stateTime += Time.deltaTime;
            if (_statePhase == ESkillStatePhase.Execution && _stateTime >= _executionDuration)
            {
                _statePhase = ESkillStatePhase.Recovery;
                stateMachine.IsStateLocked = false;
            }
        }
        //2. 更新技能动画与表现
        private void ESkillUpdateAnimation()
        {
            // 动画参数同步
            stateMachine.movementLogic.UpdateFreeMoveAnimation(stateMachine.MoveInput, stateMachine.IsHoldingRun);
        }

        private void ApplyDifferentGravity()
        {
            if (_eSkillType == ESkillType.ESkill3)
            {

            }
        }
        #endregion

        public override void ExitState()
        {
            //1. 退出技能动画
            ESkillExitAnimation();
            ////2.重置御空状态
            //if (_currentSkillStep == stateMachine.JinxiSpecialSkillLinker.ESkillAttackSteps[3])
            //stateMachine.attackLogic.SetFloating(false);
            //3.重置垂直速度
            stateMachine.movementLogic.ResetVerticalVelocity();

        }

        #region ExitState子方法
        //1. 退出技能动画
        private void ESkillExitAnimation()
        {
            // CrossFade 直切动画后无需再清理 Trigger
            // 结束御剑表现
            stateMachine.context?.WeaponController?.EndWeaponAction();
            // 结束龙
            stateMachine.JinxiSpecialSkillLinker.HideDragonInstantly();
            //隐藏特效
            stateMachine.context?.EffectController?.EndEffectAction();
        }
        #endregion

        private void CheckStateTransitions()
        {
            //惊龙破空
            if (_eSkillType == ESkillType.ESkill4)
            {
                // 死亡状态
                if (stateMachine.runtimeData.currentHealth <= 0 && _statePhase == ESkillStatePhase.Recovery)
                {
                    stateMachine.IsStateLocked = false;
                    SwitchState(CharacterState.JinxiDead);
                    return;
                }
                // 爆发状态
                if (_statePhase == ESkillStatePhase.Recovery && stateMachine.JinxiSpecialSkillLinker.IsQBurstable() && stateMachine.CheckAndConsumeQBurstRequest())
                {
                    stateMachine.IsStateLocked = false;
                    SwitchState(CharacterState.JinxiQBurst);
                    return;
                }
                // 受击状态
                if (stateMachine.TryConsumeHitRequest() && _statePhase == ESkillStatePhase.Recovery)
                {
                    stateMachine.IsStateLocked = false;
                    SwitchState(CharacterState.JinxiHit);
                    return;
                }
                //战技状态
                if (stateMachine.CheckAndConsumeESkillRequest() && stateMachine.JinxiSpecialSkillLinker.IsESkillable() && _statePhase == ESkillStatePhase.Recovery)
                {
                    SwitchState(CharacterState.JinxiESkill);
                    return;
                }
                //延奏状态
                if (stateMachine.CheckAndConsumeQteSkillRequest() && stateMachine.JinxiSpecialSkillLinker.IsQteSkillable())
                {
                    SwitchState(CharacterState.JinxiQteSkill);
                    return;
                }
                //御空冲刺状态
                if (stateMachine.CheckAndConsumeDashRequest() && stateMachine.movementLogic.IsFloatDashAvailable() && _statePhase == ESkillStatePhase.Recovery)
                {
                    SwitchState(CharacterState.JinxiFloatDash);
                    return;
                }
                //御空攻击状态
                if (stateMachine.CheckAndConsumeAirAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsAirAttackable())
                {
                    SwitchState(CharacterState.JinxiAirAttack);
                    return;
                }
                //重击状态
                if (stateMachine.CheckAndConsumeHeavyAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsHeavyAttackable() && _statePhase == ESkillStatePhase.Recovery)
                {
                    SwitchState(CharacterState.JinxiHeavyAttack);
                    return;
                }
                // 攻击状态
                if (stateMachine.CheckAndConsumeAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsAttackable() && _statePhase == ESkillStatePhase.Recovery)
                {
                    SwitchState(CharacterState.JinxiAttack);
                }
                //坠落状态
                if (!stateMachine.movementLogic.CustomCheckGrounded() && _stateTime >= 6.3f)
                {
                    SwitchState(CharacterState.JinxiFall);
                    return;
                }
                //移动状态
                if (stateMachine.MoveInput.magnitude > stateMachine.movementLogic.moveThreshold && stateMachine.movementLogic.CustomCheckGrounded() && _statePhase == ESkillStatePhase.Recovery)
                {
                    SwitchState(CharacterState.JinxiMove);
                    return;
                }
                // 待机状态
                if (stateMachine.MoveInput.magnitude < stateMachine.movementLogic.moveThreshold && _stateTime >= stateMachine.GetCombatAnimationLength(_currentSkillStep.attackId))
                {
                    SwitchState(CharacterState.JinxiIdle);
                    return;
                }
            }
            //逐天取月
            if (_eSkillType == ESkillType.ESkill3)
            {
                // 死亡状态
                if (stateMachine.runtimeData.currentHealth <= 0 && _statePhase == ESkillStatePhase.Recovery)
                {
                    stateMachine.IsStateLocked = false;
                    SwitchState(CharacterState.JinxiDead);
                    return;
                }
                // 爆发状态
                if (_statePhase == ESkillStatePhase.Recovery && stateMachine.JinxiSpecialSkillLinker.IsQBurstable() && stateMachine.CheckAndConsumeQBurstRequest())
                {
                    stateMachine.IsStateLocked = false;
                    SwitchState(CharacterState.JinxiQBurst);
                    return;
                }
                // 受击状态
                if (stateMachine.TryConsumeHitRequest() && _statePhase == ESkillStatePhase.Recovery)
                {
                    stateMachine.IsStateLocked = false;
                    SwitchState(CharacterState.JinxiHit);
                    return;
                }
                //战技状态
                if (stateMachine.CheckAndConsumeESkillRequest() && stateMachine.JinxiSpecialSkillLinker.IsESkillable() && _statePhase == ESkillStatePhase.Recovery)
                {
                    SwitchState(CharacterState.JinxiESkill);
                    return;
                }
                //延奏状态
                if (stateMachine.CheckAndConsumeQteSkillRequest() && stateMachine.JinxiSpecialSkillLinker.IsQteSkillable())
                {
                    SwitchState(CharacterState.JinxiQteSkill);
                    return;
                }
                //御空冲刺状态
                if (stateMachine.CheckAndConsumeDashRequest() && stateMachine.movementLogic.IsFloatDashAvailable() && _statePhase == ESkillStatePhase.Recovery)
                {
                    SwitchState(CharacterState.JinxiFloatDash);
                    return;
                }
                //御空攻击状态
                if (stateMachine.CheckAndConsumeAirAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsAirAttackable())
                {
                    SwitchState(CharacterState.JinxiAirAttack);
                    return;
                }
                //重击状态
                if (stateMachine.CheckAndConsumeHeavyAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsHeavyAttackable() && _statePhase == ESkillStatePhase.Recovery)
                {
                    SwitchState(CharacterState.JinxiHeavyAttack);
                    return;
                }
                // 攻击状态
                if (stateMachine.CheckAndConsumeAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsAttackable() && _statePhase == ESkillStatePhase.Recovery)
                {
                    SwitchState(CharacterState.JinxiAttack);
                    return;
                }
                //坠落状态
                if (!stateMachine.movementLogic.CustomCheckGrounded() && _stateTime >= 2.3f)
                {
                    SwitchState(CharacterState.JinxiFall);
                    return;
                }
                // 待机状态
                if (stateMachine.MoveInput.magnitude < stateMachine.movementLogic.moveThreshold && _stateTime >= stateMachine.GetCombatAnimationLength(_currentSkillStep.attackId))
                {
                    SwitchState(CharacterState.JinxiIdle);
                    return;
                }
            }
            //神霓飞芒
            if (_eSkillType == ESkillType.ESkill2)
            {
                // 死亡状态
                if (stateMachine.runtimeData.currentHealth <= 0 && _statePhase == ESkillStatePhase.Recovery)
                {
                    stateMachine.IsStateLocked = false;
                    SwitchState(CharacterState.JinxiDead);
                    return;
                }
                // 爆发状态
                if (_statePhase == ESkillStatePhase.Recovery && stateMachine.JinxiSpecialSkillLinker.IsQBurstable() && stateMachine.CheckAndConsumeQBurstRequest())
                {
                    stateMachine.IsStateLocked = false;
                    SwitchState(CharacterState.JinxiQBurst);
                    return;
                }
                // 受击状态
                if (stateMachine.TryConsumeHitRequest() && _statePhase == ESkillStatePhase.Recovery)
                {
                    stateMachine.IsStateLocked = false;
                    SwitchState(CharacterState.JinxiHit);
                    return;
                }
                //战技状态
                if (stateMachine.CheckAndConsumeESkillRequest() && stateMachine.JinxiSpecialSkillLinker.IsESkillable() && _statePhase == ESkillStatePhase.Recovery)
                {
                    SwitchState(CharacterState.JinxiESkill);
                    return;
                }
                //延奏状态
                if (stateMachine.CheckAndConsumeQteSkillRequest() && stateMachine.JinxiSpecialSkillLinker.IsQteSkillable())
                {
                    SwitchState(CharacterState.JinxiQteSkill);
                    return;
                }
                //御空冲刺状态
                if (stateMachine.CheckAndConsumeDashRequest() && stateMachine.movementLogic.IsFloatDashAvailable() && _statePhase == ESkillStatePhase.Recovery)
                {
                    SwitchState(CharacterState.JinxiFloatDash);
                    return;
                }
                //御空攻击状态
                if (stateMachine.CheckAndConsumeAirAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsAirAttackable())
                {
                    SwitchState(CharacterState.JinxiAirAttack);
                    return;
                }
                //重击状态
                if (stateMachine.CheckAndConsumeHeavyAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsHeavyAttackable() && _statePhase == ESkillStatePhase.Recovery)
                {
                    SwitchState(CharacterState.JinxiHeavyAttack);
                    return;
                }
                // 攻击
                if (stateMachine.CheckAndConsumeAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsAttackable() && _statePhase == ESkillStatePhase.Recovery)
                {
                    SwitchState(CharacterState.JinxiAttack);
                    return;
                }
                //坠落状态
                if (!stateMachine.movementLogic.CustomCheckGrounded() && stateMachine.MoveInput.magnitude > stateMachine.movementLogic.moveThreshold && _statePhase == ESkillStatePhase.Recovery)
                {
                    SwitchState(CharacterState.JinxiFall);
                    return;
                }
                // 待机状态
                if (stateMachine.MoveInput.magnitude < stateMachine.movementLogic.moveThreshold && _stateTime >= stateMachine.GetCombatAnimationLength(_currentSkillStep.attackId))
                {
                    SwitchState(CharacterState.JinxiIdle);
                    return;
                }
            }
            //流光夕影
            if (_eSkillType == ESkillType.ESkill1)
            {
                // 死亡状态
                if (stateMachine.runtimeData.currentHealth <= 0 && _statePhase == ESkillStatePhase.Recovery)
                {
                    stateMachine.IsStateLocked = false;
                    SwitchState(CharacterState.JinxiDead);
                    return;
                }
                // 爆发状态
                if (_statePhase == ESkillStatePhase.Recovery && stateMachine.JinxiSpecialSkillLinker.IsQBurstable() && stateMachine.CheckAndConsumeQBurstRequest())
                {
                    stateMachine.IsStateLocked = false;
                    SwitchState(CharacterState.JinxiQBurst);
                    return;
                }
                // 受击状态
                if (stateMachine.TryConsumeHitRequest() && _statePhase == ESkillStatePhase.Recovery)
                {
                    stateMachine.IsStateLocked = false;
                    SwitchState(CharacterState.JinxiHit);
                    return;
                }
                //战技状态
                if (stateMachine.CheckAndConsumeESkillRequest() && stateMachine.JinxiSpecialSkillLinker.IsESkillable() && _statePhase == ESkillStatePhase.Recovery)
                {
                    SwitchState(CharacterState.JinxiESkill);
                    return;
                }
                //延奏状态
                if (stateMachine.CheckAndConsumeQteSkillRequest() && stateMachine.JinxiSpecialSkillLinker.IsQteSkillable())
                {
                    SwitchState(CharacterState.JinxiQteSkill);
                    return;
                }
                // 冲刺状态
                if (stateMachine.CheckAndConsumeDashRequest() && stateMachine.movementLogic.IsDashAvailable() && _statePhase == ESkillStatePhase.Recovery)
                {
                    SwitchState(CharacterState.JinxiDash);
                    return;
                }
                // 跳跃状态
                if (stateMachine.CheckAndConsumeJumpRequest() && stateMachine.movementLogic.IsJumpAvailable() && _statePhase == ESkillStatePhase.Recovery)
                {
                    SwitchState(CharacterState.JinxiJump);
                    return;
                }
                //坠落状态
                if (!stateMachine.movementLogic.CustomCheckGrounded() && _statePhase == ESkillStatePhase.Recovery)
                {
                    SwitchState(CharacterState.JinxiFall);
                    return;
                }
                //御空攻击状态
                if (stateMachine.CheckAndConsumeAirAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsAirAttackable())
                {
                    SwitchState(CharacterState.JinxiAirAttack);
                    return;
                }
                //重击状态
                if (stateMachine.CheckAndConsumeHeavyAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsHeavyAttackable() && _statePhase == ESkillStatePhase.Recovery)
                {
                    SwitchState(CharacterState.JinxiHeavyAttack);
                    return;
                }
                // 攻击
                if (stateMachine.CheckAndConsumeAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsAttackable() && _statePhase == ESkillStatePhase.Recovery)
                {
                    SwitchState(CharacterState.JinxiAttack);
                    return;
                }
                //移动状态
                if (stateMachine.MoveInput.magnitude > stateMachine.movementLogic.moveThreshold && stateMachine.movementLogic.CustomCheckGrounded() && _statePhase == ESkillStatePhase.Recovery)
                {
                    SwitchState(CharacterState.JinxiMove);
                    return;
                }
                // 待机状态
                if (stateMachine.MoveInput.magnitude < stateMachine.movementLogic.moveThreshold && _stateTime >= stateMachine.GetCombatAnimationLength(_currentSkillStep.attackId))
                {
                    SwitchState(CharacterState.JinxiIdle);
                    return;
                }
            }
        }
    }
    #endregion

     #region 爆发状态
    // 爆发状态
    public class JinxiQBurstState : CharacterBaseState
    {
        private enum QBurstPhase
        {
            Execution, // 爆发执行阶段：核心动作，不可打断
            Recovery   // 爆发恢复阶段：收招后摇，可打断
        }

        private QBurstPhase _statePhase;
        private AttackStep _currentQBurstStep;

        private float _stateTime;//处于爆发状态的时长
        private float _executionDuration;//爆发执行阶段时长
        private float _animationLength;//爆发动画总时长

        public JinxiQBurstState(CharacterStateMachine stateMachine, CharacterStateFactory factory) : base(stateMachine, factory) { }

        public override void EnterState()
        {
            //1.初始化爆发数据
            InitializeQBurstData();

            //爆发数据为空时直接回待机，防止后续空引用
            if (_currentQBurstStep == null)
            {
                stateMachine.IsStateLocked = false;
                SwitchState(CharacterState.JinxiIdle);
                return;
            }

            //2.初始化爆发状态
            InitializeQBurstState();

            //3.进入爆发状态动画
            QBurstEnterAnimation();
        }

        #region EnterState子方法
        //1.初始化爆发数据
        private void InitializeQBurstData()
        {
            //1.获取当前爆发攻击段
            if (stateMachine.JinxiSpecialSkillLinker.QBurstAttackSteps.Count == 0)
            {
                Debug.LogError("今汐 QBurstAttackSteps 配置为空！");
                _currentQBurstStep = null;
                _executionDuration = 0f;
                _animationLength = 0f;
                return;
            }

            _currentQBurstStep = stateMachine.JinxiSpecialSkillLinker.QBurstAttackSteps[0];

            //爆发数据为空时直接返回，防止后续空引用
            if (_currentQBurstStep == null)
            {
                _executionDuration = 0f;
                _animationLength = 0f;
                return;
            }

            //2.同步攻击阶段信息
            stateMachine.currentStep = _currentQBurstStep;

            //3.初始化爆发执行阶段时长
            _executionDuration = stateMachine.attackLogic.GetExecutionDuration(_currentQBurstStep);

            //4.初始化爆发动画总时长
            _animationLength = stateMachine.GetCombatAnimationLength(_currentQBurstStep.attackId);

            //5.进入爆发冷却
            stateMachine.JinxiSpecialSkillLinker.OnQBurstUsed();
        }

        //2.初始化爆发状态
        private void InitializeQBurstState()
        {
            //1.清理爆发输入请求，避免重复触发
            stateMachine.CleanWantsToQBurstRequest();

            //2.初始化状态时间
            _stateTime = 0f;

            //3.初始化爆发阶段
            _statePhase = QBurstPhase.Execution;

            //4.锁定状态，执行阶段不能随意切走
            stateMachine.IsStateLocked = true;
        }

        //3.进入爆发状态动画
        private void QBurstEnterAnimation()
        {
            //播放角色爆发动画
            stateMachine.Animator.CrossFadeInFixedTime(stateMachine.GetCombatAnimationName(_currentQBurstStep.attackId), 0f, 0, 0);

            //播放龙动画
            stateMachine.JinxiSpecialSkillLinker.PlayDragonAction(_currentQBurstStep);

            //隐藏装饰剑
            stateMachine.manifestation.HideDecorationSwordFade();

            //播放爆发特效
            stateMachine.effectController?.PlayEffectAction(_currentQBurstStep);
        }
        #endregion

        public override void UpdateState()
        {
            //1.更新爆发阶段
            UpdateQBurstPhase();

            //2.更新爆发状态动画
            QBurstUpdateAnimation();

            //3.状态转换
            CheckStateTransitions();
        }

        #region UpdateState子方法
        //1.更新爆发阶段
        private void UpdateQBurstPhase()
        {
            _stateTime += Time.deltaTime;

            //执行阶段结束后进入恢复阶段
            if (_statePhase == QBurstPhase.Execution && _stateTime >= _executionDuration)
            {
                _statePhase = QBurstPhase.Recovery;
                stateMachine.IsStateLocked = false;
            }
        }

        //2.更新爆发状态动画
        private void QBurstUpdateAnimation()
        {
            //同步移动混合树参数，防止技能结束后动画参数突变
            stateMachine.movementLogic.UpdateFreeMoveAnimation(stateMachine.MoveInput, stateMachine.IsHoldingRun);
        }
        #endregion

        public override void ExitState()
        {
            //1.退出爆发状态动画
            QBurstExitAnimation();

            //2.确保退出时重置垂直速度
            stateMachine.movementLogic.ResetVerticalVelocity();
        }

        #region ExitState子方法
        //1.退出爆发状态动画
        private void QBurstExitAnimation()
        {
            //龙隐藏
            stateMachine.JinxiSpecialSkillLinker.HideDragonInstantly();

            //隐藏特效
            stateMachine.context?.EffectController?.EndEffectAction();
        }
        #endregion

        private void CheckStateTransitions()
        {
            //死亡状态
            if (stateMachine.runtimeData.currentHealth <= 0)
            {
                SwitchState(CharacterState.JinxiDead);
                return;
            }

            //受击状态
            if (stateMachine.TryConsumeHitRequest())
            {
                SwitchState(CharacterState.JinxiHit);
                return;
            }

            //执行阶段不处理退出
            if (_statePhase == QBurstPhase.Execution)
            {
                return;
            }

            //战技状态
            if (stateMachine.CheckAndConsumeESkillRequest() && stateMachine.JinxiSpecialSkillLinker.IsESkillable())
            {
                SwitchState(CharacterState.JinxiESkill);
                return;
            }

            //延奏状态
            if (stateMachine.CheckAndConsumeQteSkillRequest() && stateMachine.JinxiSpecialSkillLinker.IsQteSkillable())
            {
                SwitchState(CharacterState.JinxiQteSkill);
                return;
            }

            //御空冲刺状态
            if (stateMachine.WantsToDash && stateMachine.movementLogic.IsFloatDashAvailable())
            {
                SwitchState(CharacterState.JinxiFloatDash);
                return;
            }

            //冲刺状态
            if (stateMachine.CheckAndConsumeDashRequest() && stateMachine.movementLogic.IsDashAvailable())
            {
                SwitchState(CharacterState.JinxiDash);
                return;
            }
            

            //跳跃状态
            if (stateMachine.CheckAndConsumeJumpRequest() && stateMachine.movementLogic.IsJumpAvailable())
            {
                SwitchState(CharacterState.JinxiJump);
                return;
            }

            //御空攻击状态
            if (stateMachine.CheckAndConsumeAirAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsAirAttackable())
            {
                SwitchState(CharacterState.JinxiAirAttack);
                return;
            }

            //重击状态
            if (stateMachine.CheckAndConsumeHeavyAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsHeavyAttackable())
            {
                SwitchState(CharacterState.JinxiHeavyAttack);
                return;
            }

            //攻击状态
            if (stateMachine.CheckAndConsumeAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsAttackable())
            {
                SwitchState(CharacterState.JinxiAttack);
                return;
            }

            //移动状态
            if (stateMachine.MoveInput.magnitude > stateMachine.movementLogic.moveThreshold
                && stateMachine.movementLogic.CustomCheckGrounded()
                )
            {
                SwitchState(CharacterState.JinxiMove);
                return;
            }

            //坠落状态
            if (_stateTime >= 0.99f * (_animationLength))
            {
                SwitchState(CharacterState.JinxiFall);
                return;
            }
        }
    }
    #endregion

     #region 延奏技能状态
    //延奏技能状态
    public class JinxiQteSkillState : CharacterBaseState
    {
        private enum QteSkillPhase
        {
            Execution, // 延奏技能执行阶段：不可打断
            Recovery   // 延奏技能恢复阶段：可打断
        }

        private QteSkillPhase _statePhase;
        private AttackStep _currentQteStep;

        private float _stateTime;//处于延奏技能状态的时长
        private float _executionDuration;//延奏技能执行阶段时长
        private float _animationLength;//延奏技能动画总时长

        public JinxiQteSkillState(CharacterStateMachine stateMachine, CharacterStateFactory factory) : base(stateMachine, factory) { }

        public override void EnterState()
        {
            //1.初始化延奏技能数据
            InitializeQteSkillData();

            //延奏技能数据为空时直接回待机，防止后续空引用
            if (_currentQteStep == null)
            {
                stateMachine.IsStateLocked = false;
                SwitchState(CharacterState.JinxiIdle);
                return;
            }

            //2.初始化延奏技能状态
            InitializeQteSkillState();

            //3.进入延奏技能状态动画
            QteSkillEnterAnimation();
        }

        #region EnterState子方法
        //1.初始化延奏技能数据
        private void InitializeQteSkillData()
        {
            //1.获取当前延奏技能攻击段
            _currentQteStep = stateMachine.JinxiSpecialSkillLinker.InitializeQteSkillStep();

            //延奏技能数据为空时直接返回，防止后续空引用
            if (_currentQteStep == null)
            {
                _executionDuration = 0f;
                _animationLength = 0f;
                return;
            }

            //2.同步攻击阶段信息
            stateMachine.currentStep = _currentQteStep;

            //3.初始化延奏技能执行阶段时长
            _executionDuration = stateMachine.attackLogic.GetExecutionDuration(_currentQteStep);

            //4.初始化延奏技能动画总时长
            _animationLength = stateMachine.GetCombatAnimationLength(_currentQteStep.attackId);
        }

        //2.初始化延奏技能状态
        private void InitializeQteSkillState()
        {
            //1.清理延奏技能输入请求，避免重复触发
            stateMachine.CleanWantsToQteSkillRequest();

            //2.初始化状态时间
            _stateTime = 0f;

            //3.初始化技能阶段
            _statePhase = QteSkillPhase.Execution;

            //4.锁定状态，执行阶段不能随意切走
            stateMachine.IsStateLocked = true;
        }

        //3.进入延奏技能状态动画
        private void QteSkillEnterAnimation()
        {
            //播放角色延奏技能动画
            stateMachine.Animator.CrossFadeInFixedTime(stateMachine.GetCombatAnimationName(_currentQteStep.attackId), 0f, 0, 0);

            //播放武器表现
            stateMachine.context?.WeaponController?.PlayWeaponAction(_currentQteStep);

            //龙动画
            stateMachine.JinxiSpecialSkillLinker.PlayDragonAction(_currentQteStep);

            //播放延奏技能特效
            stateMachine.effectController?.PlayEffectAction(_currentQteStep);

            //隐藏装饰剑
            stateMachine.manifestation.HideDecorationSwordFade();
        }
        #endregion

        public override void UpdateState()
        {
            //1.更新延奏技能阶段
            UpdateQteSkillPhase();

            //2.更新延奏技能状态动画
            QteSkillUpdateAnimation();

            //3.状态转换
            CheckStateTransitions();
        }

        #region UpdateState子方法
        //1.更新延奏技能阶段
        private void UpdateQteSkillPhase()
        {
            _stateTime += Time.deltaTime;

            if (_statePhase == QteSkillPhase.Execution && _stateTime >= _executionDuration)
            {
                _statePhase = QteSkillPhase.Recovery;
                stateMachine.IsStateLocked = false;
            }
        }

        //2.更新延奏技能状态动画
        private void QteSkillUpdateAnimation()
        {
            //同步移动混合树参数，防止技能结束后移动参数突变
            stateMachine.movementLogic.UpdateFreeMoveAnimation(stateMachine.MoveInput, stateMachine.IsHoldingRun);
        }
        #endregion

        public override void ExitState()
        {
            //1.退出延奏技能状态动画
            QteSkillExitAnimation();

            //2.重置垂直速度
            stateMachine.movementLogic.ResetVerticalVelocity();
        }

        #region ExitState子方法
        //1.退出延奏技能状态动画
        private void QteSkillExitAnimation()
        {
            //结束武器表现
            stateMachine.context?.WeaponController?.EndWeaponAction();

            //结束延奏技能特效
            stateMachine.context?.EffectController?.EndEffectAction();

            //显示装饰剑
            stateMachine.manifestation.ShowDecorationSwordFade();
        }
        #endregion

        private void CheckStateTransitions()
        {
            //死亡状态
            if (stateMachine.runtimeData.currentHealth <= 0)
            {
                SwitchState(CharacterState.JinxiDead);
                return;
            }
            //爆发状态
            if (stateMachine.JinxiSpecialSkillLinker.IsQBurstable() && stateMachine.CheckAndConsumeQBurstRequest())
            {
                stateMachine.IsStateLocked = false;
                SwitchState(CharacterState.JinxiQBurst);
                return;
            }

            //受击状态
            if (stateMachine.TryConsumeHitRequest())
            {
                SwitchState(CharacterState.JinxiHit);
                return;
            }

            //战技状态
            if (stateMachine.CheckAndConsumeESkillRequest() && stateMachine.JinxiSpecialSkillLinker.IsESkillable())
            {
                SwitchState(CharacterState.JinxiESkill);
                return;
            }
            //延奏状态
            if (stateMachine.CheckAndConsumeQteSkillRequest() && stateMachine.JinxiSpecialSkillLinker.IsQteSkillable())
            {
                SwitchState(CharacterState.JinxiQteSkill);
                return;
            }
            //御空冲刺状态
            if (stateMachine.WantsToDash && stateMachine.movementLogic.IsFloatDashAvailable() && _stateTime >= 2f)
            {
                stateMachine.IsStateLocked = false;
                SwitchState(CharacterState.JinxiFloatDash);
                return;
            }
            //冲刺状态
            if (stateMachine.CheckAndConsumeDashRequest() && stateMachine.movementLogic.IsDashAvailable() && _stateTime >= 2f)
            {
                stateMachine.IsStateLocked = false;
                SwitchState(CharacterState.JinxiDash);
                return;
            }

            //跳跃状态
            if (stateMachine.CheckAndConsumeJumpRequest() && stateMachine.movementLogic.IsJumpAvailable())
            {
                SwitchState(CharacterState.JinxiJump);
                return;
            }
            //御空攻击状态
            if (stateMachine.CheckAndConsumeAirAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsAirAttackable())
            {
                SwitchState(CharacterState.JinxiAirAttack);
                return;
            }
            //重击状态
            if (stateMachine.CheckAndConsumeHeavyAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsHeavyAttackable())
            {
                SwitchState(CharacterState.JinxiHeavyAttack);
                return;
            }
            //攻击状态
            if (stateMachine.CheckAndConsumeAttackRequest() && stateMachine.JinxiSpecialSkillLinker.IsAttackable())
            {
                SwitchState(CharacterState.JinxiAttack);
                return;
            }
            //移动状态
            if (stateMachine.MoveInput.magnitude > stateMachine.movementLogic.moveThreshold && stateMachine.movementLogic.CustomCheckGrounded())
            {
                SwitchState(CharacterState.JinxiMove);
                return;
            }
            // 待机状态
            if (stateMachine.MoveInput.magnitude < stateMachine.movementLogic.moveThreshold && _stateTime >= _animationLength)
            {
                SwitchState(CharacterState.JinxiIdle);
                return;
            }
        }
    }
    #endregion

     #region 受击状态
    //受击状态
    public class JinxiHitState : CharacterBaseState
    {
        public JinxiHitState(CharacterStateMachine stateMachine, CharacterStateFactory factory) : base(stateMachine, factory) { }

        public override void EnterState()
        {
            // 进入受击：播放受击动画/扣血/强制位移/锁定状态
            stateMachine.IsStateLocked = true;
        }

        public override void UpdateState()
        {
            // 受击帧逻辑：判断动画是否结束
            CheckStateTransitions();
        }


        public override void ExitState()
        {
            // 退出受击：重置受击参数/解锁状态
            stateMachine.IsStateLocked = false;
        }

        private void CheckStateTransitions()
        {
            // 受击动画结束 → 闲置/死亡
            AnimatorStateInfo stateInfo = stateMachine.Animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsTag("JinxiHit") && stateInfo.normalizedTime >= 1f)
            {
                if (stateMachine.runtimeData.currentHealth <= 0)
                {
                    SwitchState(CharacterState.JinxiDead);
                }
                else
                {
                    SwitchState(CharacterState.JinxiIdle);
                }
            }
        }
    }
    #endregion

     #region 死亡状态
    //死亡状态
    public class JinxiDeadState : CharacterBaseState
    {
        public JinxiDeadState(CharacterStateMachine stateMachine, CharacterStateFactory factory) : base(stateMachine, factory) { }

        public override void EnterState()
        {
            //// 进入死亡：播放死亡动画/禁用输入/禁用物理/销毁组件等
            //stateMachine.ResetInputState(); // 重置输入
            //stateMachine.characterController.enabled = false;
            //stateMachine.IsStateLocked = true;
        }

        public override void UpdateState()
        {
            // 死亡状态：无帧逻辑，或处理死亡后特效/销毁等
        }


        public override void ExitState()
        {
            // 死亡状态不可退出，留空
        }

        // 死亡状态无状态切换判断
    }
    #endregion

    #endregion

}

