using System.Collections;
using System.Linq;
using UnityEngine;

namespace WutheringWaves
{
    //武器控制器：负责根据攻击步骤播放独立的武器表现
    public class WeaponController : MonoBehaviour
    {
        private CharacterContext context; // 角色共享上下文
        private WeaponConfigSO weaponConfig; // 武器配置

        private Coroutine weaponCoroutine; // 当前武器表现协程
        private GameObject currentWeapon; // 当前实例化的武器对象

        //初始化：由 CharacterFacade 统一调用，绑定角色上下文与配置
        public void Initialize(CharacterContext context)
        {
            this.context = context;
            weaponConfig = context != null && context.CharacterDataSO != null ? context.CharacterDataSO.weaponConfigSO : null;
        }

        //根据攻击步骤播放对应武器表现
        public void PlayWeaponAction(AttackStep attackStep)
        {
            // 攻击步骤无效，或当前角色没有配置武器系统时，直接清空旧表现
            if (attackStep == null || attackStep.attackId == AttackId.None || weaponConfig == null)
            {
                EndWeaponAction();
                return;
            }

            // 通过稳定的 AttackId 查找武器动作，避免再和 AttackStep 对象引用强耦合
            WeaponActionConfig weaponActionConfig = weaponConfig.GetWeaponActionConfig(attackStep.attackId);
            if (weaponActionConfig == null || weaponActionConfig.swordAnimation == null || !weaponActionConfig.swordAnimation.hasSwordAnimation)
            {
                EndWeaponAction();
                return;
            }

            // 新动作开始前，先终止旧协程，避免同一时刻叠加多个武器表现
            if (weaponCoroutine != null)
            {
                StopCoroutine(weaponCoroutine);
                weaponCoroutine = null;
            }

            // 若场上还残留旧武器实例，先销毁再重新生成
            if (currentWeapon != null)
            {
                Destroy(currentWeapon);
                currentWeapon = null;
            }

            weaponCoroutine = StartCoroutine(PlayWeaponActionCoroutine(weaponActionConfig.swordAnimation));
        }

        //结束当前武器表现
        public void EndWeaponAction()
        {
            if (weaponCoroutine != null)
            {
                StopCoroutine(weaponCoroutine);
                weaponCoroutine = null;
            }

            if (currentWeapon != null)
            {
                Destroy(currentWeapon);
                currentWeapon = null;
            }
        }


        //播放单次武器表现：按配置生成武器并沿轨迹推进
        private IEnumerator PlayWeaponActionCoroutine(SwordAnimation swordAnimation)
        {
            // 如果配置了延迟，则等待到动作节点再生成武器
            if (swordAnimation.DelayTime > 0f)
            {
                yield return new WaitForSeconds(swordAnimation.DelayTime);
            }

            // 动作可覆盖默认武器预制体；若未覆盖则回退到通用配置
            GameObject prefabToUse = swordAnimation.floatingSword != null ? swordAnimation.floatingSword : weaponConfig.floatingSword;
            if (prefabToUse == null)
            {
                weaponCoroutine = null;
                yield break;
            }

            currentWeapon = Instantiate(prefabToUse);
            if (currentWeapon == null)
            {
                weaponCoroutine = null;
                yield break;
            }

            Transform weaponTransform = currentWeapon.transform;
            weaponTransform.localScale = prefabToUse.transform.localScale * 1.2f;

            // 先统计总权重，再按权重把总时长切分给每一段轨迹
            float totalWeight = swordAnimation.swordTrajectory.Sum(trajectory => trajectory.timeWeight);
            totalWeight = totalWeight <= 0f ? 1f : totalWeight;

            Vector3 initLocalPosition = swordAnimation.initSwordHandlePos + weaponConfig.swordPositionOffset;
            Quaternion initLocalRotation = Quaternion.Euler(weaponConfig.swordRotationOffset) * swordAnimation.InitSwordRotation;

            Vector3 currentHandlePosition = transform.TransformPoint(initLocalPosition);
            Quaternion currentWeaponRotation = transform.rotation * initLocalRotation;

            weaponTransform.position = currentHandlePosition;
            weaponTransform.rotation = currentWeaponRotation;

            foreach (SwordTrajectory trajectory in swordAnimation.swordTrajectory)
            {
                // 武器若在过程中被提前销毁，则立即结束协程
                if (currentWeapon == null)
                {
                    weaponCoroutine = null;
                    yield break;
                }

                Vector3 targetHandlePosition = transform.TransformPoint(trajectory.targeSwordHandlelPos + weaponConfig.swordPositionOffset);
                Quaternion targetLocalRotation = Quaternion.Euler(weaponConfig.swordRotationOffset) * trajectory.TargetSwordRotation;
                Quaternion targetWeaponRotation = transform.rotation * targetLocalRotation;

                Vector3 startHandlePosition = currentHandlePosition;
                Quaternion startWeaponRotation = currentWeaponRotation;
                float duration = swordAnimation.swordAnimationCostTime * (trajectory.timeWeight / totalWeight);
                float elapsed = 0f;

                // 逐帧插值到目标轨迹点，形成连贯的武器运动
                while (elapsed < duration)
                {
                    if (currentWeapon == null)
                    {
                        weaponCoroutine = null;
                        yield break;
                    }

                    elapsed += Time.deltaTime;
                    float progress = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);

                    currentHandlePosition = Vector3.Lerp(startHandlePosition, targetHandlePosition, progress);
                    currentWeaponRotation = Quaternion.SlerpUnclamped(startWeaponRotation, targetWeaponRotation, progress);

                    weaponTransform.position = currentHandlePosition;
                    weaponTransform.rotation = currentWeaponRotation;

                    yield return null;
                }

                currentHandlePosition = targetHandlePosition;
                currentWeaponRotation = targetWeaponRotation;
            }

            // 动作完成后清理实例，避免场景中残留一次性武器对象
            if (currentWeapon != null)
            {
                Destroy(currentWeapon);
                currentWeapon = null;
            }

            weaponCoroutine = null;
        }
    }
}
