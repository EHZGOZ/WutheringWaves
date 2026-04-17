using UnityEngine;

namespace WutheringWaves
{
    public class CharacterRootMotion : MonoBehaviour
    {
        private CharacterContext context;
        private Animator animator;
        private CharacterController characterController; // Unity内置角色物理控制器


        // 根运动初始化：改为直接绑定共享上下文，不再依赖CharacterCore
        public void Initialize(CharacterContext context)
        {
            this.context = context;
            if (animator == null) animator = context != null ? context.Animator : GetComponent<Animator>();
            if (characterController == null) characterController = context != null ? context.CharacterController : GetComponentInParent<CharacterController>();
        }


        private void OnAnimatorMove()
        {
            CharacterStateMachine stateMachine = context != null ? context.StateMachine : null;
            if (stateMachine != null
                && (stateMachine.CurrentStateType == CharacterState.JinxiMove
                || stateMachine.CurrentStateType == CharacterState.JinxiIdle))
            {
                return;
            }

            // 安全校验：空物体直接跳过
            if (animator == null || characterController == null)
                return;

            // 1. 获取动画输出的根运动数据（位移 + 旋转）
            Vector3 rootMotionDeltaMove = animator.deltaPosition;   // 位移增量
            Quaternion rootMotionDeltaRot = animator.deltaRotation; // 旋转增量

            // 2. 应用【旋转】（根运动驱动角色转向）
            transform.rotation *= rootMotionDeltaRot;

            // 3. 应用【位移】（CharacterController 接管，带碰撞检测）
            characterController.Move(rootMotionDeltaMove);
        }
    }
}

