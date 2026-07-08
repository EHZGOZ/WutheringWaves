using UnityEngine;

namespace WutheringWaves
{
    // 角色动画事件转发器：挂在 Animator 所在物体上，用于把动画事件转发给角色根节点的 CharacterAttack
    public class CharacterAnimationEventReceiver : MonoBehaviour
    {
        private CharacterAttack attackLogic; // 角色攻击逻辑，通常挂在父级角色根节点

        private void Awake()
        {
            // 1.从当前 Animator 物体往父级查找 CharacterAttack
            attackLogic = GetComponentInParent<CharacterAttack>();
        }

        // 攻击命中检测事件：在攻击动画的命中帧调用
        public void CheckAttackHit()
        {
            // 1.空值保护，避免角色预制体缺少 CharacterAttack 时动画事件报错
            if (attackLogic == null)
            {
                return;
            }

            // 2.转发给角色攻击逻辑，由 CharacterAttack 负责范围检测和伤害投递
            attackLogic.CheckAttackHit();
        }
    }
}