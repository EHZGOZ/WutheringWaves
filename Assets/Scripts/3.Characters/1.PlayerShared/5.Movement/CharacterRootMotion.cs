using UnityEngine;

namespace WutheringWaves
{
    public class CharacterRootMotion : MonoBehaviour
    {
        private CharacterContext context;
        private Animator animator;
        private CharacterController characterController; // Unity内置角色物理控制器

        [Header("=== Root Motion 调试 ===")]
        [SerializeField] private bool enableRootMotionDebug = false; // 是否开启根运动调试日志
        [SerializeField] private bool onlyLogWhenMoved = true; // 是否只在有位移时输出日志


        // 根运动初始化：改为直接绑定共享上下文，不再依赖CharacterCore
        public void Initialize(CharacterContext context)
        {
            this.context = context;
            if (animator == null) animator = context != null ? context.Animator : GetComponent<Animator>();
            if (characterController == null) characterController = context != null ? context.CharacterController : GetComponentInParent<CharacterController>();
        }
        // 递归查找子物体
        private Transform FindChildByName(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            foreach (Transform child in parent)
            {
                if (child.name == childName)
                {
                    return child;
                }

                Transform result = FindChildByName(child, childName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }


        private void OnAnimatorMove()
        {
            CharacterStateMachine stateMachine = context != null ? context.StateMachine : null;
            if (stateMachine != null
                && (stateMachine.CurrentStateType == CharacterState.JinxiMove
                || stateMachine.CurrentStateType == CharacterState.JinxiIdle
                || stateMachine.CurrentStateType == CharacterState.KatixiyaMove
                || stateMachine.CurrentStateType == CharacterState.KatixiyaIdle
                ))
            {
                return;
            }

            // 安全校验：空物体直接跳过
            if (animator == null || characterController == null)
            {
                return;
            }

            // 1. 获取动画输出的根运动数据（位移 + 旋转）
            Vector3 rootMotionDeltaMove = animator.deltaPosition;   // 位移增量
            Quaternion rootMotionDeltaRot = animator.deltaRotation; // 旋转增量




            // 2.输出根运动调试信息
            LogRootMotionDebug(stateMachine, rootMotionDeltaMove, rootMotionDeltaRot);

            // 3. 应用【旋转】（根运动驱动角色转向）
            transform.rotation *= rootMotionDeltaRot;

            // 4. 应用【位移】（CharacterController 接管，带碰撞检测）
            characterController.Move(rootMotionDeltaMove);
        }

        // 输出根运动调试信息：用于确认当前动画是否真的提供了位移和旋转增量
        private void LogRootMotionDebug(CharacterStateMachine stateMachine, Vector3 rootMotionDeltaMove, Quaternion rootMotionDeltaRot)
        {
            if (!enableRootMotionDebug)
            {
                return;
            }

            bool hasMove = rootMotionDeltaMove.sqrMagnitude > 0.000001f;
            bool hasRotation = Quaternion.Angle(Quaternion.identity, rootMotionDeltaRot) > 0.01f;

            if (onlyLogWhenMoved && !hasMove && !hasRotation)
            {
                return;
            }

            string stateName = stateMachine != null ? stateMachine.CurrentStateType.ToString() : "None";

            Debug.Log(
                $"[CharacterRootMotion] 角色={name} 状态={stateName} deltaMove={rootMotionDeltaMove} deltaRotation={rootMotionDeltaRot.eulerAngles}",
                this
            );
        }
    }
}
