using System.Collections;
using System.Linq;
using UnityEngine;

namespace WutheringWaves
{
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
        [SerializeField] private Vector3 QteSkillDragonLocalOffset = new Vector3(0f, -2f, 0f);

        private Animator characterAnimator; // 角色动画控制器，用于兜底查询动画长度
        private AnimationConfigSO animationConfig; // 今汐动画配置
        private Coroutine dragonShowCoroutine; // 龙延迟显示协程
        private Coroutine dragonHideCoroutine; // 龙自动隐藏协程
        private Vector3 currentDragonLocalOffset = Vector3.zero; // 当前龙位置偏移

        #region 初始化
        public void Initialize(CharacterContext context)
        {
            characterAnimator = context != null ? context.Animator : null;
            animationConfig = context != null && context.CharacterDataSO != null
                ? context.CharacterDataSO.animationConfigSO
                : null;

            currentDragonLocalOffset = defaultDragonLocalOffset;
            HideDragonInstantly();
        }
        #endregion

        #region 生命周期
        private void Update()
        {
            UpdateDragonPosition();
        }

        private void OnDisable()
        {
            StopDragonCoroutines();
        }
        #endregion

        #region 对外接口
        // 播放龙表现：由攻击段入口调用
        public void PlayDragonAction(AttackStep step)
        {
            if (step == null)
            {
                return;
            }

            PlayDragonAction(step.attackId, step);
        }

        // 播放龙表现：由攻击ID入口调用
        public void PlayDragonAction(AttackId attackId)
        {
            PlayDragonAction(attackId, null);
        }

        // 立即隐藏龙表现
        public void HideDragonInstantly()
        {
            StopDragonCoroutines();

            if (dragon == null)
            {
                return;
            }

            currentDragonLocalOffset = defaultDragonLocalOffset;
            dragon.SetActive(false);
        }

        // 立即显示龙表现
        public void ShowDragonInstantly()
        {
            if (dragon == null)
            {
                return;
            }

            dragon.SetActive(true);
            UpdateDragonPosition();
        }
        #endregion

        #region 龙表现主流程
        // 播放龙表现核心逻辑
        private void PlayDragonAction(AttackId attackId, AttackStep step)
        {
            // 非龙表现攻击段不处理，避免普通攻击误触发龙
            if (!CanPlayDragonAction(attackId))
            {
                return;
            }

            // 当前对象未激活时不启动协程，避免 inactive 物体开启协程报错
            if (!isActiveAndEnabled)
            {
                return;
            }

            StopDragonCoroutines();
            ApplyDragonOffset(attackId);

            switch (attackId)
            {
                case AttackId.Attack03:
                    dragonShowCoroutine = StartCoroutine(DelayShowDragon(0.2f, 0f, attackId, step));
                    return;

                case AttackId.QBurst:
                    dragonShowCoroutine = StartCoroutine(DelayShowDragon(0f, 0f, attackId, step));
                    return;

                case AttackId.QteSkill:
                    dragonShowCoroutine = StartCoroutine(DelayShowDragon(0f, 1.5f, attackId, step));
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

        // 判断当前攻击段是否需要播放龙表现
        private bool CanPlayDragonAction(AttackId attackId)
        {
            switch (attackId)
            {
                case AttackId.Attack03:
                case AttackId.QBurst:
                case AttackId.QteSkill:
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
        #endregion

        #region 龙位置
        // 根据攻击段应用龙位置偏移
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
            if (attackId == AttackId.QteSkill )
            {
                currentDragonLocalOffset = QteSkillDragonLocalOffset;
            }
        }

        // 每帧同步龙位置到角色身边
        private void UpdateDragonPosition()
        {
            if (dragon == null)
            {
                return;
            }

            dragon.transform.position = transform.TransformPoint(currentDragonLocalOffset);
        }
        #endregion

        #region 协程控制
        // 延迟显示龙，并播放对应动画
        private IEnumerator DelayShowDragon(float delay, float earlyHideOffset, AttackId attackId, AttackStep step)
        {
            yield return new WaitForSeconds(delay);

            ShowDragonInstantly();
            PlayDragonAnimation(attackId);
            StartDragonAutoHide(attackId, step, earlyHideOffset);

            dragonShowCoroutine = null;
        }

        // 开始龙自动隐藏计时
        private void StartDragonAutoHide(AttackId attackId, AttackStep step, float earlyHideOffset)
        {
            if (dragonHideCoroutine != null)
            {
                StopCoroutine(dragonHideCoroutine);
                dragonHideCoroutine = null;
            }

            dragonHideCoroutine = StartCoroutine(DelayHideDragon(attackId, step, earlyHideOffset));
        }

        // 延迟隐藏龙
        private IEnumerator DelayHideDragon(AttackId attackId, AttackStep step, float earlyHideOffset)
        {
            yield return null;

            float hideTime = GetDragonAnimationLength(attackId, step) - earlyHideOffset;
            yield return new WaitForSeconds(Mathf.Max(0f, hideTime));

            HideDragonInstantly();
        }

        // 停止龙表现相关协程
        private void StopDragonCoroutines()
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
        }
        #endregion

        #region 动画控制
        // 播放龙动画
        private void PlayDragonAnimation(AttackId attackId)
        {
            if (dragonAnimator == null)
            {
                return;
            }

            string dragonTriggerName = GetDragonAnimationName(attackId);
            if (string.IsNullOrWhiteSpace(dragonTriggerName))
            {
                return;
            }

            dragonAnimator.SetTrigger(dragonTriggerName);
        }

        // 获取龙表现动画时长
        private float GetDragonAnimationLength(AttackId attackId, AttackStep step)
        {
            Animator targetAnimator = dragonAnimator != null ? dragonAnimator : characterAnimator;
            if (targetAnimator == null)
            {
                return GetAttackStepDuration(step);
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
                return GetAttackStepDuration(step);
            }

            string dragonClipName = GetDragonAnimationName(attackId);
            if (string.IsNullOrWhiteSpace(dragonClipName))
            {
                return GetAttackStepDuration(step);
            }

            AnimationClip clip = targetAnimator.runtimeAnimatorController.animationClips
                .FirstOrDefault(currentClip => currentClip != null && currentClip.name == dragonClipName);

            return clip != null ? clip.length : GetAttackStepDuration(step);
        }

        // 获取攻击段配置时长，用于动画长度兜底
        private float GetAttackStepDuration(AttackStep step)
        {
            return step != null && step.timingConfig != null
                ? step.timingConfig.executionAttackCostTime
                : 0f;
        }

        // 根据攻击ID获取龙动画名
        private string GetDragonAnimationName(AttackId attackId)
        {
            if (animationConfig == null)
            {
                return string.Empty;
            }

            AnimationClip clip = animationConfig.GetCombatClip(attackId);
            return clip != null ? clip.name : string.Empty;
        }
        #endregion
    }
}
