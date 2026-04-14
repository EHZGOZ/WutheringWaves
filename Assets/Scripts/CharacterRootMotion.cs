using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;
namespace WutheringWaves
{
    public class CharacterRootMotion : MonoBehaviour
    {
        private CharacterCore core;
        private Animator animator;
        private CharacterController characterController; // Unity内置角色物理控制器
        void Start()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (characterController == null) characterController = GetComponentInParent<CharacterController>();
        }
        public void Initialize(CharacterCore core)
        {
            this.core = core;
        }

        void Update()
        {

        }
        private void OnAnimatorMove()
        {
            //if (core.stateMachine.CurrentStateType == CharacterState.Attacking ||
            //    core.stateMachine.CurrentStateType == CharacterState.FallAttacking ||
            //    core.stateMachine.CurrentStateType == CharacterState.Dashing ||
            //    core.stateMachine.CurrentStateType == CharacterState.Jumping ||
            //    core.stateMachine.CurrentStateType == CharacterState.AirDashing
            if (core.stateMachine.CurrentStateType == CharacterState.Moving||
                core.stateMachine.CurrentStateType == CharacterState.Idle
                //core.stateMachine.CurrentStateType == CharacterState.Transition
                )
            {
                return;
            }




            // 安全校验：空物体直接跳过
            if (animator == null || characterController == null)
                return;

            // 1. 获取动画输出的 根运动数据（位移 + 旋转）
            Vector3 rootMotionDeltaMove = animator.deltaPosition;   // 位移增量
            Quaternion rootMotionDeltaRot = animator.deltaRotation; // 旋转增量

            // 2. 应用【旋转】（根运动驱动角色转向）
            transform.rotation *= rootMotionDeltaRot;

            // 3. 应用【位移】（CharacterController 接管，带碰撞检测）
            characterController.Move(rootMotionDeltaMove);

            //// 调试日志（可删除）
            //Debug.Log($"根运动 → 位移：{rootMotionDeltaMove} | 旋转：{rootMotionDeltaRot.eulerAngles}");
        }
    }

}

