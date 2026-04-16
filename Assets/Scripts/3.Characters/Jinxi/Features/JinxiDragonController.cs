using System.Collections;
using System.Linq;
using UnityEngine;

namespace WutheringWaves
{
    [DisallowMultipleComponent]
    public class JinxiDragonController : MonoBehaviour
    {
        [Header("=== 龙表现配置 ===")]
        [Header("龙对象")]
        [SerializeField] private GameObject dragon;

        [Header("龙动画控制器")]
        [SerializeField] private Animator dragonAnimator;

        [Header("龙位置偏移（相对角色本地坐标）")]
        [SerializeField] private Vector3 defaultDragonLocalOffset = Vector3.zero;
        [SerializeField] private Vector3 eSkillStep3DragonLocalOffset = Vector3.zero;
        [SerializeField] private Vector3 skillAttackSteps3DragonLocalOffset = new Vector3(0f, 0f, 3f);
        [SerializeField] private Vector3 skillAttackSteps4DragonLocalOffset = new Vector3(0f, 0f, 3f);

        private Animator characterAnimator;
        private AnimationConfigSO animationConfig;
        private Coroutine dragonHideCoroutine;
        private Coroutine dragonShowCoroutine;
        private Vector3 currentDragonLocalOffset = Vector3.zero;

        public void Initialize(CharacterContext context)
        {
            characterAnimator = context != null ? context.Animator : null;
            animationConfig = context != null && context.CharacterDataSO != null
                ? context.CharacterDataSO.animationConfigSO
                : null;
            currentDragonLocalOffset = defaultDragonLocalOffset;
        }

        private void Update()
        {
            UpdateDragonPosition();
        }

        public void PlayDragonAction(AttackStep step)
        {
            if (step == null)
            {
                return;
            }

            PlayDragonAction(step.attackId, step);
        }

        public void PlayDragonAction(AttackId attackId)
        {
            PlayDragonAction(attackId, null);
        }

        public void HideDragonInstantly()
        {
            if (dragonShowCoroutine != null)
            {
                StopCoroutine(dragonShowCoroutine);
                dragonShowCoroutine = null;
            }

            if (dragonHideCoroutine != null)
            {
                StopCoroutine(dragonHideCoroutine);
                dragonHideCoroutine = null;
            }

            if (dragon == null)
            {
                return;
            }

            currentDragonLocalOffset = defaultDragonLocalOffset;
            dragon.SetActive(false);
        }

        public void ShowDragonInstantly()
        {
            if (dragon == null)
            {
                return;
            }

            dragon.SetActive(true);
        }

        private void PlayDragonAction(AttackId attackId, AttackStep step)
        {
            if (!CanPlayDragonAction(attackId))
            {
                return;
            }

            ApplyDragonOffset(attackId);

            if (dragonShowCoroutine != null)
            {
                StopCoroutine(dragonShowCoroutine);
                dragonShowCoroutine = null;
            }

            switch (attackId)
            {
                case AttackId.Attack03:
                    dragonShowCoroutine = StartCoroutine(DelayShowDragon(0.2f, 0f, attackId, step));
                    return;
                case AttackId.QBurst:
                    dragonShowCoroutine = StartCoroutine(DelayShowDragon(0f, 0f, attackId, step));
                    return;
                case AttackId.ESkill03:
                    dragonShowCoroutine = StartCoroutine(DelayShowDragon(0.7f, 0f, attackId, step));
                    return;
                case AttackId.ESkill04:
                    dragonShowCoroutine = StartCoroutine(DelayShowDragon(0f, 1f, attackId, step));
                    return;
                case AttackId.FloatAttackGround03:
                case AttackId.FloatAttackAir03:
                    dragonShowCoroutine = StartCoroutine(DelayShowDragon(0.2f, 0f, attackId, step));
                    return;
                case AttackId.FloatAttackGround04:
                case AttackId.FloatAttackAir04:
                    dragonShowCoroutine = StartCoroutine(DelayShowDragon(0f, 0.3f, attackId, step));
                    return;
            }
        }

        private void ApplyDragonOffset(AttackId attackId)
        {
            currentDragonLocalOffset = defaultDragonLocalOffset;

            if (attackId == AttackId.ESkill03)
            {
                currentDragonLocalOffset = eSkillStep3DragonLocalOffset;
                return;
            }

            if (attackId == AttackId.FloatAttackGround03 || attackId == AttackId.FloatAttackAir03)
            {
                currentDragonLocalOffset = skillAttackSteps3DragonLocalOffset;
                return;
            }

            if (attackId == AttackId.FloatAttackGround04 || attackId == AttackId.FloatAttackAir04)
            {
                currentDragonLocalOffset = skillAttackSteps4DragonLocalOffset;
            }
        }

        private bool CanPlayDragonAction(AttackId attackId)
        {
            switch (attackId)
            {
                case AttackId.Attack03:
                case AttackId.QBurst:
                case AttackId.ESkill03:
                case AttackId.ESkill04:
                case AttackId.FloatAttackGround03:
                case AttackId.FloatAttackAir03:
                case AttackId.FloatAttackGround04:
                case AttackId.FloatAttackAir04:
                    return true;
                default:
                    return false;
            }
        }

        private void UpdateDragonPosition()
        {
            if (dragon == null)
            {
                return;
            }

            dragon.transform.position = transform.TransformPoint(currentDragonLocalOffset);
        }

        private IEnumerator DelayShowDragon(float delay, float earlyHideOffset, AttackId attackId, AttackStep step)
        {
            yield return new WaitForSeconds(delay);

            ShowDragonInstantly();

            if (dragonAnimator != null)
            {
                string dragonTriggerName = GetDragonTriggerName(attackId);
                if (!string.IsNullOrWhiteSpace(dragonTriggerName))
                {
                    dragonAnimator.SetTrigger(dragonTriggerName);
                }
            }

            StartDragonAutoHide(attackId, step, earlyHideOffset);
            dragonShowCoroutine = null;
        }

        private void StartDragonAutoHide(AttackId attackId, AttackStep step, float earlyHideOffset = 0f)
        {
            if (dragonHideCoroutine != null)
            {
                StopCoroutine(dragonHideCoroutine);
            }

            dragonHideCoroutine = StartCoroutine(DelayHideDragon(attackId, step, earlyHideOffset));
        }

        private float GetDragonAnimationLength(AttackId attackId, AttackStep step)
        {
            Animator targetAnimator = dragonAnimator != null ? dragonAnimator : characterAnimator;
            if (targetAnimator == null)
            {
                return step != null && step.timingConfig != null ? step.timingConfig.executionAttackCostTime : 0f;
            }

            AnimatorClipInfo[] currentClips = targetAnimator.GetCurrentAnimatorClipInfo(0);
            if (currentClips != null && currentClips.Length > 0 && currentClips[0].clip != null)
            {
                return currentClips[0].clip.length;
            }

            AnimatorClipInfo[] nextClips = targetAnimator.GetNextAnimatorClipInfo(0);
            if (nextClips != null && nextClips.Length > 0 && nextClips[0].clip != null)
            {
                return nextClips[0].clip.length;
            }

            if (targetAnimator.runtimeAnimatorController == null)
            {
                return step != null && step.timingConfig != null ? step.timingConfig.executionAttackCostTime : 0f;
            }

            string dragonClipName = GetDragonClipName(attackId);
            if (string.IsNullOrWhiteSpace(dragonClipName))
            {
                return step != null && step.timingConfig != null ? step.timingConfig.executionAttackCostTime : 0f;
            }

            AnimationClip clip = targetAnimator.runtimeAnimatorController.animationClips
                .FirstOrDefault(currentClip => currentClip != null && currentClip.name == dragonClipName);

            if (clip != null)
            {
                return clip.length;
            }

            return step != null && step.timingConfig != null ? step.timingConfig.executionAttackCostTime : 0f;
        }

        private IEnumerator DelayHideDragon(AttackId attackId, AttackStep step, float earlyHideOffset = 0f)
        {
            yield return null;

            float time = GetDragonAnimationLength(attackId, step) - earlyHideOffset;
            yield return new WaitForSeconds(Mathf.Max(0f, time));

            HideDragonInstantly();
        }

        private string GetDragonTriggerName(AttackId attackId)
        {
            if (animationConfig == null)
            {
                return string.Empty;
            }

            AnimationClip clip = animationConfig.GetCombatClip(attackId);
            return clip != null ? clip.name : string.Empty;
        }

        private string GetDragonClipName(AttackId attackId)
        {
            if (animationConfig == null)
            {
                return string.Empty;
            }

            AnimationClip clip = animationConfig.GetCombatClip(attackId);
            return clip != null ? clip.name : string.Empty;
        }
    }
}
