
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    namespace WutheringWaves
{
    #region 具体状态实现类（所有状态继承自抽象基类，实现生命周期方法）

     #region 待机状态
    //待机状态
    public class KatixiyaIdleState : CharacterBaseState
    {
        public KatixiyaIdleState(CharacterStateMachine stateMachine, CharacterStateFactory factory) : base(stateMachine, factory) { }
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

            //3. 重置垂直速度
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
                SwitchState(CharacterState.KatixiyaDead);
                return;
            }
            //爆发状态
            if (stateMachine.KatixiyaSpecialSkillLinker.IsQBurstable() && stateMachine.CheckAndConsumeQBurstRequest())
            {
                stateMachine.IsStateLocked = false;
                SwitchState(CharacterState.KatixiyaQBurst);
                return;
            }
            //受击状态
            if (stateMachine.TryConsumeHitRequest())
            {
                SwitchState(CharacterState.KatixiyaHit);
                return;
            }
            //战技状态
            if (stateMachine.CheckAndConsumeESkillRequest() && stateMachine.KatixiyaSpecialSkillLinker.IsESkillable())
            {
                SwitchState(CharacterState.KatixiyaESkill);
                return;
            }
            //冲刺状态
            if (stateMachine.CheckAndConsumeDashRequest() && stateMachine.movementLogic.IsDashAvailable())
            {
                SwitchState(CharacterState.KatixiyaDash);
                return;
            }
            //下落状态
            if (!stateMachine.movementLogic.CustomCheckGrounded())
            {
                SwitchState(CharacterState.KatixiyaFall);
                return;
            }
            // 跳跃状态
            if (stateMachine.CheckAndConsumeJumpRequest() && stateMachine.movementLogic.IsJumpAvailable())
            {
                SwitchState(CharacterState.KatixiyaJump);
                return;
            }
            //御空攻击状态
            if (stateMachine.CheckAndConsumeAirAttackRequest() && stateMachine.KatixiyaSpecialSkillLinker.IsAirAttackable())
            {
                SwitchState(CharacterState.KatixiyaAirAttack);
                return;
            }
            //攻击状态
            if (stateMachine.CheckAndConsumeAttackRequest() && stateMachine.KatixiyaSpecialSkillLinker.IsAttackable())
            {
                SwitchState(CharacterState.KatixiyaAttack);
                return;
            }
            //移动状态
            if (stateMachine.MoveInput.magnitude > stateMachine.movementLogic.moveThreshold && stateMachine.movementLogic.CustomCheckGrounded())
            {
                SwitchState(CharacterState.KatixiyaMove);
                return;
            }
        }
    }
    #endregion

     #region 移动状态
    //移动状态
    public class KatixiyaMoveState : CharacterBaseState
    {
        private float _stateTimer;//已处于移动状态的时间
        public KatixiyaMoveState(CharacterStateMachine stateMachine, CharacterStateFactory factory) : base(stateMachine, factory) { }

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
                SwitchState(CharacterState.KatixiyaDead);
                return;
            }
            //爆发状态
            if (stateMachine.KatixiyaSpecialSkillLinker.IsQBurstable() && stateMachine.CheckAndConsumeQBurstRequest())
            {
                stateMachine.IsStateLocked = false;
                SwitchState(CharacterState.KatixiyaQBurst);
                return;
            }
            //受击状态
            if (stateMachine.TryConsumeHitRequest())
            {
                SwitchState(CharacterState.KatixiyaHit);
                return;
            }
            //战技状态
            if (stateMachine.CheckAndConsumeESkillRequest() && stateMachine.KatixiyaSpecialSkillLinker.IsESkillable())
            {
                SwitchState(CharacterState.KatixiyaESkill);
                return;
            }
            //冲刺状态
            if (stateMachine.CheckAndConsumeDashRequest() && stateMachine.movementLogic.IsDashAvailable())
            {
                SwitchState(CharacterState.KatixiyaDash);
                return;
            }
            //下落状态
            if (!stateMachine.movementLogic.CustomCheckGrounded())
            {
                SwitchState(CharacterState.KatixiyaFall);
                return;
            }
            // 跳跃状态
            if (stateMachine.CheckAndConsumeJumpRequest() && stateMachine.movementLogic.IsJumpAvailable())
            {
                SwitchState(CharacterState.KatixiyaJump);
                return;
            }
            //御空攻击状态
            if (stateMachine.CheckAndConsumeAirAttackRequest() && stateMachine.KatixiyaSpecialSkillLinker.IsAirAttackable())
            {
                SwitchState(CharacterState.KatixiyaAirAttack);
                return;
            }
            //攻击状态
            if (stateMachine.CheckAndConsumeAttackRequest() && stateMachine.KatixiyaSpecialSkillLinker.IsAttackable())
            {
                SwitchState(CharacterState.KatixiyaAttack);
                return;
            }
            // 收步状态
            if (stateMachine.MoveInput.magnitude < stateMachine.movementLogic.moveThreshold && stateMachine.movementLogic.CustomCheckGrounded())
            {
                SwitchState(CharacterState.KatixiyaStop);
                return;
            }

        }
    }
    #endregion

     #region 收步状态
    // 收步状态
    public class KatixiyaStopState : CharacterBaseState
    {
        private float _stopTimer; // 收步计时器
        private bool _isRunStop;  // 是否是奔跑后的收步

        public KatixiyaStopState(CharacterStateMachine stateMachine, CharacterStateFactory factory)
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
                SwitchState(CharacterState.KatixiyaIdle);
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
                SwitchState(CharacterState.KatixiyaDead);
                return;
            }

            //爆发状态
            if (stateMachine.KatixiyaSpecialSkillLinker.IsQBurstable() && stateMachine.CheckAndConsumeQBurstRequest())
            {
                stateMachine.IsStateLocked = false;
                SwitchState(CharacterState.KatixiyaQBurst);
                return;
            }

            //受击状态
            if (stateMachine.TryConsumeHitRequest())
            {
                SwitchState(CharacterState.KatixiyaHit);
                return;
            }

            //战技状态
            if (stateMachine.CheckAndConsumeESkillRequest() && stateMachine.KatixiyaSpecialSkillLinker.IsESkillable())
            {
                SwitchState(CharacterState.KatixiyaESkill);
                return;
            }

            //冲刺状态
            if (stateMachine.CheckAndConsumeDashRequest() && stateMachine.movementLogic.IsDashAvailable())
            {
                SwitchState(CharacterState.KatixiyaDash);
                return;
            }

            //跳跃状态
            if (stateMachine.CheckAndConsumeJumpRequest() && stateMachine.movementLogic.IsJumpAvailable())
            {
                SwitchState(CharacterState.KatixiyaJump);
                return;
            }

            //坠落状态
            if (!stateMachine.movementLogic.CustomCheckGrounded())
            {
                SwitchState(CharacterState.KatixiyaFall);
                return;
            }

            //御空攻击状态
            if (stateMachine.CheckAndConsumeAirAttackRequest() && stateMachine.KatixiyaSpecialSkillLinker.IsAirAttackable())
            {
                SwitchState(CharacterState.KatixiyaAirAttack);
                return;
            }

            //攻击状态
            if (stateMachine.CheckAndConsumeAttackRequest() && stateMachine.KatixiyaSpecialSkillLinker.IsAttackable())
            {
                SwitchState(CharacterState.KatixiyaAttack);
                return;
            }

            //移动状态
            if (stateMachine.MoveInput.magnitude > stateMachine.movementLogic.moveThreshold && stateMachine.movementLogic.CustomCheckGrounded())
            {
                SwitchState(CharacterState.KatixiyaMove);
                return;
            }
        }
    }
    #endregion

     #region 跳跃状态
    //跳跃状态
    public class KatixiyaJumpState : CharacterBaseState
    {
        private const float JumpLockTime = 0.33f;
        private float _stateTimer;//已处于跳跃状态的时间

        public KatixiyaJumpState(CharacterStateMachine stateMachine, CharacterStateFactory factory) : base(stateMachine, factory) { }

        public override void EnterState()
        {
            //1.进入跳跃状态动画
            JumpingEnterAnimation();
            //2.初始化跳跃状态
            InitializeJumpingState();
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
            // CrossFade 直切动画后无需再清理 Trigger
        }
        #endregion

        //状态转换判断
        private void CheckInterruptions()
        {
            //死亡状态
            if (stateMachine.runtimeData.currentHealth <= 0)
            {
                SwitchState(CharacterState.KatixiyaDead);
                return;
            }
            //爆发状态
            if (stateMachine.KatixiyaSpecialSkillLinker.IsQBurstable() && stateMachine.CheckAndConsumeQBurstRequest())
            {
                stateMachine.IsStateLocked = false;
                SwitchState(CharacterState.KatixiyaQBurst);
                return;
            }
            //受击状态
            if (stateMachine.TryConsumeHitRequest())
            {
                SwitchState(CharacterState.KatixiyaHit);
                return;
            }
            // 御空冲刺状态
            if (stateMachine.CheckAndConsumeDashRequest() && stateMachine.movementLogic.IsFloatDashAvailable())
            {
                SwitchState(CharacterState.KatixiyaFloatDash);
                return;
            }
            //战技状态
            if (stateMachine.CheckAndConsumeESkillRequest() && stateMachine.KatixiyaSpecialSkillLinker.IsESkillable())
            {
                SwitchState(CharacterState.KatixiyaESkill);
                return;
            }
            //御空攻击状态
            if (stateMachine.CheckAndConsumeAirAttackRequest() && stateMachine.KatixiyaSpecialSkillLinker.IsAirAttackable())
            {
                SwitchState(CharacterState.KatixiyaAirAttack);
                return;
            }
            //坠落状态
            if (_stateTimer >= JumpLockTime)
            {
                SwitchState(CharacterState.KatixiyaFall);
                return;
            }
        }
    }
    #endregion

     #region 坠落状态
    //坠落状态
    public class KatixiyaFallState : CharacterBaseState
    {
        //已处于坠落状态的时间
        private float _stateTimer;
        public KatixiyaFallState(CharacterStateMachine stateMachine, CharacterStateFactory factory) : base(stateMachine, factory) { }
        public override void EnterState()
        {
            //1.坠落动画
            FallingEnterAnimation();
            //2.重置下坠时间
            InitializeFallingState();
            //3.重置连招
        }

        #region EnterState子状态
        // 1. 进入下落状态动画
        private void FallingEnterAnimation()
        {
            if (stateMachine.PreviousStateType == CharacterState.KatixiyaAirDash || stateMachine.PreviousStateType == CharacterState.KatixiyaFloatDash || stateMachine.PreviousStateType == CharacterState.KatixiyaJump)
            {
                stateMachine.Animator.CrossFadeInFixedTime(stateMachine.GetLocomotionAnimationName(LocomotionAnimationId.Fall), 0.2f, 0, 0);
            }
            else
            {
                stateMachine.Animator.CrossFadeInFixedTime(stateMachine.GetLocomotionAnimationName(LocomotionAnimationId.Fall), 0, 0, 0);
            }

        }
        //2.初始化下落状态
        private void InitializeFallingState()
        {
            //2.重置下坠时间
            _stateTimer = 0f;
        }
        #endregion

        public override void UpdateState()
        {
            //1.更新下坠时间
            _stateTimer += Time.deltaTime;
            //2.下坠逻辑
            stateMachine.movementLogic.HandleFallingMovement(stateMachine.MoveInput);
            stateMachine.movementLogic.ApplyGroundingForce();

            //3.状态转换判断
            CheckInterruptions();
        }

        #region UpdateState子状态

        #endregion

        public override void ExitState()
        {
            //1.退出下落状态动画
            FallingExitAnimation();
            //2.落地垂直速度重置
            stateMachine.movementLogic.ResetVerticalVelocity();
            //3. 配置落地过渡参数并切换状态

        }

        #region ExitState子状态
        // 1. 退出下落状态动画
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
                SwitchState(CharacterState.KatixiyaDead);
                return;
            }
            //爆发状态
            if (stateMachine.KatixiyaSpecialSkillLinker.IsQBurstable() && stateMachine.CheckAndConsumeQBurstRequest())
            {
                stateMachine.IsStateLocked = false;
                SwitchState(CharacterState.KatixiyaQBurst);
                return;
            }
            //受击状态
            if (stateMachine.TryConsumeHitRequest())
            {
                SwitchState(CharacterState.KatixiyaHit);
                return;
            }
            //战技状态
            if (stateMachine.CheckAndConsumeESkillRequest() && stateMachine.KatixiyaSpecialSkillLinker.IsESkillable())
            {
                SwitchState(CharacterState.KatixiyaESkill);
                return;
            }
            // 御空冲刺状态
            if (stateMachine.CheckAndConsumeDashRequest() && stateMachine.movementLogic.IsFloatDashAvailable())
            {
                SwitchState(CharacterState.KatixiyaFloatDash);
                return;
            }
            // 空中冲刺状态
            if (stateMachine.CheckAndConsumeDashRequest() && stateMachine.movementLogic.IsAirDashAvailable())
            {
                SwitchState(CharacterState.KatixiyaAirDash);
                return;
            }
            //御空攻击状态
            if (stateMachine.CheckAndConsumeAirAttackRequest() && stateMachine.KatixiyaSpecialSkillLinker.IsAirAttackable())
            {
                SwitchState(CharacterState.KatixiyaAirAttack);
                return;
            }
            //下落攻击状态
            if (stateMachine.CheckAndConsumeAttackRequest() && stateMachine.KatixiyaSpecialSkillLinker.IsFallAttackable())
            {
                SwitchState(CharacterState.KatixiyaFallAttack);
                return;
            }
            // 着地状态
            if (stateMachine.movementLogic.CustomCheckGrounded())
            {
                SwitchState(CharacterState.KatixiyaLand);
                return;
            }

        }
    }
    #endregion

     #region 着地状态
    // 着地状态
    public class KatixiyaLandState : CharacterBaseState
    {
        private float _landTimer; // 着地计时器

        public KatixiyaLandState(CharacterStateMachine stateMachine, CharacterStateFactory factory)
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
        {
            stateMachine.Animator.CrossFadeInFixedTime(stateMachine.GetLocomotionAnimationName(LocomotionAnimationId.Land), 0.1f, 0, 0);
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
                SwitchState(CharacterState.KatixiyaMove);
                return;
            }

            SwitchState(CharacterState.KatixiyaIdle);
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
                SwitchState(CharacterState.KatixiyaDead);
                return;
            }

            //爆发状态
            if (stateMachine.KatixiyaSpecialSkillLinker.IsQBurstable() && stateMachine.CheckAndConsumeQBurstRequest())
            {
                stateMachine.IsStateLocked = false;
                SwitchState(CharacterState.KatixiyaQBurst);
                return;
            }

            //受击状态
            if (stateMachine.TryConsumeHitRequest())
            {
                SwitchState(CharacterState.KatixiyaHit);
                return;
            }

            //战技状态
            if (stateMachine.CheckAndConsumeESkillRequest() && stateMachine.KatixiyaSpecialSkillLinker.IsESkillable())
            {
                SwitchState(CharacterState.KatixiyaESkill);
                return;
            }

            //冲刺状态
            if (stateMachine.CheckAndConsumeDashRequest() && stateMachine.movementLogic.IsDashAvailable())
            {
                SwitchState(CharacterState.KatixiyaDash);
                return;
            }

            //跳跃状态
            if (stateMachine.CheckAndConsumeJumpRequest() && stateMachine.movementLogic.IsJumpAvailable())
            {
                SwitchState(CharacterState.KatixiyaJump);
                return;
            }

            //坠落状态
            if (!stateMachine.movementLogic.CustomCheckGrounded())
            {
                SwitchState(CharacterState.KatixiyaFall);
                return;
            }

            //御空攻击状态
            if (stateMachine.CheckAndConsumeAirAttackRequest() && stateMachine.KatixiyaSpecialSkillLinker.IsAirAttackable())
            {
                SwitchState(CharacterState.KatixiyaAirAttack);
                return;
            }

            //攻击状态
            if (stateMachine.CheckAndConsumeAttackRequest() && stateMachine.KatixiyaSpecialSkillLinker.IsAttackable())
            {
                SwitchState(CharacterState.KatixiyaAttack);
                return;
            }

            //移动状态
            if (stateMachine.MoveInput.magnitude > stateMachine.movementLogic.moveThreshold)
            {
                SwitchState(CharacterState.KatixiyaMove);
                return;
            }
        }
    }
    #endregion

     #region 攻击状态
    //攻击状态
    public class KatixiyaAttackState : CharacterBaseState
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
        public KatixiyaAttackState(CharacterStateMachine stateMachine, CharacterStateFactory factory) : base(stateMachine, factory) { }

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
            _attackStep = stateMachine.KatixiyaSpecialSkillLinker.InitializeNormalAttackStep();
            //2.同步攻击阶段信息
            stateMachine.currentStep = _attackStep;
            if(stateMachine.PreviousStateType!=CharacterState.KatixiyaAttack)
            {
                stateMachine.KatixiyaSpecialSkillLinker.ResetNormalCombo();
            }
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
            // 防止攻击数据为空导致后续报错
            if (_attackStep == null)
            {
                Debug.LogError("卡提希娅普通攻击数据为空！");
                return;
            }

            // 播放普通攻击动画
            stateMachine.Animator.CrossFadeInFixedTime(stateMachine.GetCombatAnimationName(_attackStep.attackId), 0f, 0, 0);

            // 播放攻击特效
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
                    stateMachine.KatixiyaSpecialSkillLinker.StartNormalComboWindow();

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
            if (_stateTime * 1.5f > stateMachine.GetCombatAnimationLength(_attackStep.attackId)&& !hasUpataAttackingUpdateAnimation)
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

        }
        #endregion

        private void CheckStateTransitions()
        {
            // 死亡状态
            if (stateMachine.runtimeData.currentHealth <= 0)
            {
                SwitchState(CharacterState.KatixiyaDead);
                return;
            }
            //爆发状态
            if (stateMachine.KatixiyaSpecialSkillLinker.IsQBurstable() && stateMachine.CheckAndConsumeQBurstRequest())
            {
                stateMachine.IsStateLocked = false;
                SwitchState(CharacterState.KatixiyaQBurst);
                return;
            }
            // 受击状态
            if (stateMachine.TryConsumeHitRequest())
            {
                SwitchState(CharacterState.KatixiyaHit);
                return;
            }
            //战技状态
            if (stateMachine.CheckAndConsumeESkillRequest() && stateMachine.KatixiyaSpecialSkillLinker.IsESkillable())
            {
                SwitchState(CharacterState.KatixiyaESkill);
                return;
            }
            //冲刺状态
            if (stateMachine.CheckAndConsumeDashRequest() && stateMachine.movementLogic.IsDashAvailable())
            {
                stateMachine.IsStateLocked = false;
                SwitchState(CharacterState.KatixiyaDash);
                return;
            }
            // 跳跃状态
            if (stateMachine.CheckAndConsumeJumpRequest() && stateMachine.movementLogic.IsJumpAvailable())
            {
                SwitchState(CharacterState.KatixiyaJump);
                return;
            }
            //攻击状态
            if (stateMachine.CheckAndConsumeAttackRequest() && stateMachine.KatixiyaSpecialSkillLinker.IsAttackable())
            {
                SwitchState(CharacterState.KatixiyaAttack);
                return;
            }

            //移动状态
            if (stateMachine.MoveInput.magnitude > stateMachine.movementLogic.moveThreshold && stateMachine.movementLogic.CustomCheckGrounded() && _stateTime >= 1f)
            {
                SwitchState(CharacterState.KatixiyaMove);
                return;
            }
            // 待机状态
            if (stateMachine.MoveInput.magnitude < stateMachine.movementLogic.moveThreshold && _stateTime >= stateMachine.GetCombatAnimationLength(_attackStep.attackId))
            {
                SwitchState(CharacterState.KatixiyaIdle);
                return;
            }
        }
    }
    #endregion

     #region 重击状态
    //重击状态
    public class KatixiyaHeavyAttackState : CharacterBaseState
    {
       

        public KatixiyaHeavyAttackState(CharacterStateMachine stateMachine, CharacterStateFactory factory)
            : base(stateMachine, factory) { }

        public override void EnterState()
        {
            
        }

        public override void UpdateState()
        {

        }



        public override void ExitState()
        {

        }
    }
    #endregion

     #region 下落攻击状态
    // 下落攻击状态
    public class KatixiyaFallAttackState : CharacterBaseState
    {
        private enum FallAttackPhase
        {
            start,  // 启动动作（正常重力）
            loop,   // 持续下落攻击（强重力，快速下砸）
            end,    // 收刀动作（接地后）
            over    // 收刀完成，可切换状态
        }
        public KatixiyaFallAttackState(CharacterStateMachine stateMachine, CharacterStateFactory factory)
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
            if (stateMachine.KatixiyaSpecialSkillLinker.FallAttackSteps.Count >= 3)
            {
                _fallattackStepStart = stateMachine.KatixiyaSpecialSkillLinker.FallAttackSteps[0];
                _fallattackStepLoop = stateMachine.KatixiyaSpecialSkillLinker.FallAttackSteps[1];
                _fallattackStepEnd = stateMachine.KatixiyaSpecialSkillLinker.FallAttackSteps[2];
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
                SwitchState(CharacterState.KatixiyaDead);
                return;
            }
            //爆发状态
            if (stateMachine.KatixiyaSpecialSkillLinker.IsQBurstable() && stateMachine.CheckAndConsumeQBurstRequest())
            {
                stateMachine.IsStateLocked = false;
                SwitchState(CharacterState.KatixiyaQBurst);
                return;
            }
            // 受击状态
            if (stateMachine.TryConsumeHitRequest())
            {
                SwitchState(CharacterState.KatixiyaHit);
                return;
            }
            //战技状态
            if (stateMachine.CheckAndConsumeESkillRequest() && stateMachine.KatixiyaSpecialSkillLinker.IsESkillable())
            {
                SwitchState(CharacterState.KatixiyaESkill);
                return;
            }
            //冲刺状态
            if ((_phase == FallAttackPhase.end || _phase == FallAttackPhase.over) && stateMachine.CheckAndConsumeDashRequest() && stateMachine.movementLogic.IsDashAvailable())
            {
                SwitchState(CharacterState.KatixiyaDash);
                return;
            }
            // 空中冲刺状态
            if (stateMachine.CheckAndConsumeDashRequest() && stateMachine.movementLogic.IsAirDashAvailable() && _phase == FallAttackPhase.loop)
            {
                SwitchState(CharacterState.KatixiyaAirDash);
                return;
            }

            // 跳跃状态
            if ((_phase == FallAttackPhase.end || _phase == FallAttackPhase.over) && stateMachine.CheckAndConsumeJumpRequest() && stateMachine.movementLogic.IsJumpAvailable())
            {
                SwitchState(CharacterState.KatixiyaJump);
                return;
            }
            //攻击状态
            if ((_phase == FallAttackPhase.end || _phase == FallAttackPhase.over) && stateMachine.CheckAndConsumeAttackRequest() && stateMachine.KatixiyaSpecialSkillLinker.IsAttackable())
            {
                SwitchState(CharacterState.KatixiyaAttack);
                return;
            }
            //移动状态
            if ((_phase == FallAttackPhase.end || _phase == FallAttackPhase.over) && stateMachine.MoveInput.magnitude > stateMachine.movementLogic.moveThreshold)
            {
                SwitchState(CharacterState.KatixiyaMove);
                return;
            }
            // 待机状态
            if (stateMachine.MoveInput.magnitude < stateMachine.movementLogic.moveThreshold && _phase == FallAttackPhase.over)
            {
                SwitchState(CharacterState.KatixiyaIdle);
                return;
            }
        }
    }
    #endregion

     #region 御空攻击状态
    // 御空攻击状态
    public class KatixiyaAirAttackState : CharacterBaseState
    {

        public KatixiyaAirAttackState(CharacterStateMachine stateMachine, CharacterStateFactory factory) : base(stateMachine, factory) { }

        public override void EnterState()
        {

        }


        public override void UpdateState()
        {
           
        }



        public override void ExitState()
        {

        }


    }
    #endregion

     #region 冲刺状态
    //冲刺状态
    public class KatixiyaDashState : CharacterBaseState
    {
        private enum DashPhase
        {
            Displacement, // 位移阶段：代码驱动移动，仅再次冲刺可打断
            Stopping       // 急停阶段：纯动画表演，任意动作可打断
        }
        private DashPhase _phase;//冲刺状态的两个阶段
        private float DashLockTime;//冲刺方向锁定时间
        private bool _dashDirection;//冲刺方向
        private float _stateTimer;//已处于冲刺状态的时间
        public KatixiyaDashState(CharacterStateMachine stateMachine, CharacterStateFactory factory) : base(stateMachine, factory) { }
        public override void EnterState()
        {
            //1.初始化阶段 时长 时间 清理冲刺输入缓存 方向 
            InitializeDashData();
            if (!stateMachine.movementLogic.TryConsumeDashStamina())
            {
                stateMachine.IsStateLocked = false;
                SwitchState(stateMachine.MoveInput.magnitude > stateMachine.movementLogic.moveThreshold ? CharacterState.KatixiyaMove : CharacterState.KatixiyaIdle);
                return;
            }
            //2.计算方向（调用CharacterMovement）
            _dashDirection = stateMachine.movementLogic.CalculateDirection(stateMachine.MoveInput);
            //3.计算上次冲刺间隔  重置连冲资格 消耗1次冲刺次数 启动内置 CD 并更新时间戳（调用CharacterMovement）
            stateMachine.movementLogic.CalculateAndUpdateDashCounter();
            //4.检查并施加全局 CD 惩罚（调用CharacterMovement）
            stateMachine.movementLogic.ApplyPenaltyIfNecessary();
            //5.进入冲刺状态动画
            DashingEnterAnimation();
            //6.重置连招
        }

        #region EnterState子方法  
        //1.初始化阶段 时长 时间 清理冲刺输入缓存 方向 
        private void InitializeDashData()
        {
            // 清理冲刺输入缓存
            stateMachine.CleanWantsToDashRequest();
            //初始化阶段
            _phase = DashPhase.Displacement;
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
            {
                stateMachine.Animator.CrossFadeInFixedTime(stateMachine.GetLocomotionAnimationName(LocomotionAnimationId.DashForward), 0f, 0, 0);
            }
                
            else
            {
                stateMachine.Animator.CrossFadeInFixedTime(stateMachine.GetLocomotionAnimationName(LocomotionAnimationId.DashBackward), 0f, 0, 0);
            }
               
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
                _phase = DashPhase.Stopping;
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
                SwitchState(CharacterState.KatixiyaDead);
                return;
            }
            //爆发状态
            if (stateMachine.KatixiyaSpecialSkillLinker.IsQBurstable() && stateMachine.CheckAndConsumeQBurstRequest())
            {
                stateMachine.IsStateLocked = false;
                SwitchState(CharacterState.KatixiyaQBurst);
                return;
            }
            //受击状态
            if (stateMachine.TryConsumeHitRequest())
            {
                SwitchState(CharacterState.KatixiyaHit);
                return;
            }
            //战技状态
            if (stateMachine.CheckAndConsumeESkillRequest() && stateMachine.KatixiyaSpecialSkillLinker.IsESkillable())
            {
                SwitchState(CharacterState.KatixiyaESkill);
                return;
            }
            //冲刺状态
            if ((stateMachine.WantsToDash && stateMachine.movementLogic.IsDashAvailable()))
            {
                SwitchState(CharacterState.KatixiyaDash);
                return;
            }
            // 跳跃状态
            if (stateMachine.CheckAndConsumeJumpRequest() && stateMachine.movementLogic.IsJumpAvailable())
            {
                SwitchState(CharacterState.KatixiyaJump);
                return;
            }
            //御空攻击状态
            if (stateMachine.CheckAndConsumeAirAttackRequest() && stateMachine.KatixiyaSpecialSkillLinker.IsAirAttackable())
            {
                SwitchState(CharacterState.KatixiyaAirAttack);
                return;
            }
            //攻击状态
            if (stateMachine.CheckAndConsumeAttackRequest() && stateMachine.KatixiyaSpecialSkillLinker.IsAttackable())
            {
                SwitchState(CharacterState.KatixiyaAttack);
                return;
            }
            // 移动状态
            if (stateMachine.MoveInput.magnitude > stateMachine.movementLogic.moveThreshold && stateMachine.movementLogic.CustomCheckGrounded())
            {
                SwitchState(CharacterState.KatixiyaMove);
                return;
            }
            // 待机状态
            if (stateMachine.MoveInput.magnitude < stateMachine.movementLogic.moveThreshold && _stateTimer >= 1.33f && stateMachine.movementLogic.CustomCheckGrounded())
            {
                SwitchState(CharacterState.KatixiyaIdle);
                return;
            }
        }
    }
    #endregion

     #region 空中冲刺状态
    //空中冲刺状态
    public class KatixiyaAirDashState : CharacterBaseState
    {
        private float _stateTime; //空中冲刺状态时长
        private float _airdashTimer = 0.5f; // 空中冲刺持续时间计时器
        private bool _airDashDirection;// 冲刺朝向标识：true=向前 false=向后

        public KatixiyaAirDashState(CharacterStateMachine stateMachine, CharacterStateFactory factory) : base(stateMachine, factory) { }

        public override void EnterState()
        {
            //1.初始化空中冲刺数据 计算空中冲刺方向与朝向
            InitializeAirDashingData();
            //2.初始化空中冲刺状态
            InitializeAirDashingState();
            //3.进入空中冲刺状态动画
            AirDashingEnterAnimation();

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
            if (stateMachine.KatixiyaSpecialSkillLinker.IsQBurstable() && stateMachine.CheckAndConsumeQBurstRequest())
            {
                stateMachine.IsStateLocked = false;
                SwitchState(CharacterState.KatixiyaQBurst);
                return;
            }
            //战技状态
            if (stateMachine.CheckAndConsumeESkillRequest() && stateMachine.KatixiyaSpecialSkillLinker.IsESkillable())
            {
                SwitchState(CharacterState.KatixiyaESkill);
                return;
            }
            //御空攻击状态
            if (stateMachine.CheckAndConsumeAirAttackRequest() && stateMachine.KatixiyaSpecialSkillLinker.IsAirAttackable())
            {
                SwitchState(CharacterState.KatixiyaAirAttack);
                return;
            }
            //下落攻击状态
            if (stateMachine.CheckAndConsumeAttackRequest() && stateMachine.KatixiyaSpecialSkillLinker.IsFallAttackable())
            {
                SwitchState(CharacterState.KatixiyaFallAttack);
                return;
            }
            //下落状态
            if (!stateMachine.movementLogic.CustomCheckGrounded())
            {
                SwitchState(CharacterState.KatixiyaFall);
                return;
            }
        }
    }
    #endregion

     #region 御空冲刺状态
    // 御空冲刺状态
    public class KatixiyaFloatDashState : CharacterBaseState
    {

        public KatixiyaFloatDashState(CharacterStateMachine stateMachine, CharacterStateFactory factory) : base(stateMachine, factory) { }
        public override void EnterState()
        {
            
        }


        public override void UpdateState()
        {

        }



        public override void ExitState()
        {

        }

    }
    #endregion

     #region 闪避状态
    // 闪避状态
    public class KatixiyaDodgeState : CharacterBaseState
    {
        public KatixiyaDodgeState(CharacterStateMachine stateMachine, CharacterStateFactory factory) : base(stateMachine, factory) { }

        public override void EnterState()
        {


        }

        public override void UpdateState()
        {

        }



        public override void ExitState()
        {

        }
    }
    #endregion

     #region 御空闪避状态
    // 御空闪避状态
    public class KatixiyaFloatDodgeState : CharacterBaseState
    {
        public KatixiyaFloatDodgeState(CharacterStateMachine stateMachine, CharacterStateFactory factory) : base(stateMachine, factory) { }
        public override void EnterState() { }
        public override void UpdateState() { }

        public override void ExitState() { }
    }
    #endregion

     #region 战技状态
    // 战技状态
    public class KatixiyaESkillState : CharacterBaseState
    {
        private enum ESkillStatePhase
        {
            Execution, // 技能执行阶段：不可打断
            Recovery   // 技能恢复阶段：可以退出技能状态
        }

        private ESkillStatePhase _statePhase;
        private AttackStep _currentSkillStep;
        private float _stateTime;//处于战技状态的时长
        private float _executionDuration;//技能执行阶段时长
        private float _animationLength;//技能动画总时长

        public KatixiyaESkillState(CharacterStateMachine stateMachine, CharacterStateFactory factory) : base(stateMachine, factory) { }

        public override void EnterState()
        {
            //1.初始化战技数据
            InitializeESkillData();

            //战技数据为空时直接回到待机，防止后续空引用
            if (_currentSkillStep == null)
            {
                stateMachine.IsStateLocked = false;
                SwitchState(CharacterState.KatixiyaIdle);
                return;
            }

            //2.初始化战技状态
            InitializeESkillState();

            //3.进入战技状态动画
            ESkillEnterAnimation();
        }

        #region EnterState子方法
        //1.初始化战技数据
        private void InitializeESkillData()
        {
            //获取当前战技攻击段，目前只使用最简单的一段 E 技
            _currentSkillStep = stateMachine.KatixiyaSpecialSkillLinker.InitializeESkillStep();

            //初始化执行阶段时长
            _executionDuration = stateMachine.attackLogic.GetExecutionDuration(_currentSkillStep);

            //初始化动画总时长
            _animationLength = stateMachine.GetCombatAnimationLength(_currentSkillStep.attackId);

            //进入 E 技冷却
            stateMachine.KatixiyaSpecialSkillLinker.OnESkillUsed();
        }

        //2.初始化战技状态
        private void InitializeESkillState()
        {
            //清理战技输入请求，避免重复触发
            stateMachine.CleanWantsToESkilltRequest();

            //初始化状态时间
            _stateTime = 0f;

            //初始化技能阶段
            _statePhase = ESkillStatePhase.Execution;

            //锁定状态，执行阶段不能随意切走
            stateMachine.IsStateLocked = true;
        }

        //3.进入战技状态动画
        private void ESkillEnterAnimation()
        {
            //播放角色战技动画
            stateMachine.Animator.CrossFadeInFixedTime(stateMachine.GetCombatAnimationName(_currentSkillStep.attackId), 0f, 0, 0);

            //播放战技特效
            stateMachine.effectController?.PlayEffectAction(_currentSkillStep);
        }
        #endregion

        public override void UpdateState()
        {
            //1.更新战技阶段
            UpdateESkillPhase();

            //2.更新战技状态动画
            ESkillUpdateAnimation();

            //3.状态转换
            CheckStateTransitions();
        }

        #region UpdateState子方法
        //1.更新战技阶段
        private void UpdateESkillPhase()
        {
            _stateTime += Time.deltaTime;

            if (_statePhase == ESkillStatePhase.Execution && _stateTime >= _executionDuration)
            {
                _statePhase = ESkillStatePhase.Recovery;
                stateMachine.IsStateLocked = false;
            }
        }

        //2.更新战技状态动画
        private void ESkillUpdateAnimation()
        {
            //同步移动混合树参数，防止技能结束后移动参数突变
            stateMachine.movementLogic.UpdateFreeMoveAnimation(stateMachine.MoveInput, stateMachine.IsHoldingRun);
        }
        #endregion

        public override void ExitState()
        {
            //1.退出战技状态动画
            ESkillExitAnimation();

            //2.重置垂直速度
            stateMachine.movementLogic.ResetVerticalVelocity();
        }

        #region ExitState子方法
        //1.退出战技状态动画
        private void ESkillExitAnimation()
        {
            //结束战技特效
            stateMachine.context?.EffectController?.EndEffectAction();
        }
        #endregion

        private void CheckStateTransitions()
        {
            //技能执行阶段不处理退出
            if (_statePhase == ESkillStatePhase.Execution)
            {
                return;
            }

            //死亡状态
            if (stateMachine.runtimeData.currentHealth <= 0)
            {
                SwitchState(CharacterState.KatixiyaDead);
                return;
            }

            //受击状态
            if (stateMachine.TryConsumeHitRequest())
            {
                SwitchState(CharacterState.KatixiyaHit);
                return;
            }

            //坠落状态
            if (!stateMachine.movementLogic.CustomCheckGrounded())
            {
                SwitchState(CharacterState.KatixiyaFall);
                return;
            }

            //攻击状态
            if (stateMachine.CheckAndConsumeAttackRequest() && stateMachine.KatixiyaSpecialSkillLinker.IsAttackable())
            {
                SwitchState(CharacterState.KatixiyaAttack);
                return;
            }

            //移动状态
            if (stateMachine.MoveInput.magnitude > stateMachine.movementLogic.moveThreshold && _stateTime >= _animationLength)
            {
                SwitchState(CharacterState.KatixiyaMove);
                return;
            }

            //待机状态
            if (_stateTime >= _animationLength)
            {
                SwitchState(CharacterState.KatixiyaIdle);
                return;
            }
        }
    }
    #endregion

     #region 爆发状态
    // 爆发状态
    public class KatixiyaQBurstState : CharacterBaseState
    {
     
        public KatixiyaQBurstState(CharacterStateMachine stateMachine, CharacterStateFactory factory) : base(stateMachine, factory) { }
        public override void EnterState()
        {

        }



        public override void UpdateState()
        {

        }


        public override void ExitState()
        {

        }
    }
    #endregion

     #region 受击状态
    //受击状态
    public class KatixiyaHitState : CharacterBaseState
    {
        public KatixiyaHitState(CharacterStateMachine stateMachine, CharacterStateFactory factory) : base(stateMachine, factory) { }

        public override void EnterState()
        {

        }

        public override void UpdateState()
        {

        }


        public override void ExitState()
        {

        }
    }
    #endregion

     #region 死亡状态
    //死亡状态
    public class KatixiyaDeadState : CharacterBaseState
    {
        public KatixiyaDeadState(CharacterStateMachine stateMachine, CharacterStateFactory factory) : base(stateMachine, factory) { }

        public override void EnterState()
        {

        }

        public override void UpdateState()
        {

        }


        public override void ExitState()
        {

        }
    }
    #endregion

    #endregion

}


