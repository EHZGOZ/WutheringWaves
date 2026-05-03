using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WutheringWaves
{
    // 今汐专用御剑控制器：负责 SkillAttackSteps / SkillAirAttackSteps 的专用飞剑表现
    public class JinxiSpecialSwordController : MonoBehaviour
    {
        [System.Serializable]
        // 单段专用御剑配置
        private class SpecialSwordStageConfig
        {
            [Header("飞剑预制体")]
            public GameObject swordPrefab;

            [Header("开始延迟")]
            public float startDelay = 0f;

            [Header("飞行时长")]
            public float flightDuration = 0.3f;

            [Header("到达后延迟销毁")]
            public float destroyDelay = 0f;

            [Header("缩放倍率")]
            public float scaleMultiplier = 1f;

            [Header("旋转修正")]
            public Vector3 rotationOffsetEuler = Vector3.zero;

            [Header("本段空间额外偏移")]
            public Vector3 stageOffset = Vector3.zero;
        }

        [Header("=== 今汐专用御剑总开关 ===")]
        [SerializeField] private bool hasSpecialSwordAnimation = true;

        [Header("=== 空间点位配置 ===")]
        [Header("空间锚点偏移（相对角色本地坐标）")]
        [SerializeField] private Vector3 anchorOffset = Vector3.zero;

        [Header("高度层")]
        [SerializeField] private float layerAHeight = 0f; // a层：脚部高度
        [SerializeField] private float layerBHeight = 1f; // b层：胯部高度
        [SerializeField] private float layerCHeight = 2f; // c层：头部高度

        [Header("前后排距离")]
        [SerializeField] private float nearRowDistance = 1f; // 7/8/9 近排
        [SerializeField] private float middleRowDistance = 2f; // 4/5/6 中排
        [SerializeField] private float farRowDistance = 3f; // 1/2/3 远排

        [Header("左右点位距离")]
        [SerializeField] private float horizontalOffset = 1f;

        [Header("第一段起点额外抬高")]
        [SerializeField] private float firstStageExtraHeight = 0f;

        [Header("=== 第一段御剑 c6 -> b5 ===")]
        [SerializeField] private SpecialSwordStageConfig firstStage = new SpecialSwordStageConfig();

        [Header("=== 第二段御剑 四剑交叉 ===")]
        [SerializeField] private SpecialSwordStageConfig secondStage = new SpecialSwordStageConfig();

        [Header("=== 第三段御剑 c6 -> b5 ===")]
        [SerializeField] private SpecialSwordStageConfig thirdStage = new SpecialSwordStageConfig();

        private CharacterContext context; // 角色共享上下文
        private readonly List<GameObject> currentSwords = new List<GameObject>(); // 当前生成的飞剑对象
        private readonly List<Coroutine> swordCoroutines = new List<Coroutine>(); // 当前飞剑表现协程

        #region 生命周期
        private void OnDisable()
        {
            EndSpecialSwordAction();
        }
        #endregion

        #region 初始化
        // 初始化：由今汐专属模块统一调用
        public void Initialize(CharacterContext context)
        {
            this.context = context;
        }
        #endregion

        #region 对外接口
        // 根据攻击段播放专用御剑表现
        public void PlaySpecialSwordAction(AttackStep step)
        {
            if (step == null)
            {
                return;
            }

            PlaySpecialSwordAction(step.attackId);
        }

        // 根据攻击ID播放专用御剑表现
        public void PlaySpecialSwordAction(AttackId attackId)
        {
            if (!hasSpecialSwordAnimation)
            {
                return;
            }

            switch (attackId)
            {
                case AttackId.FloatAttackGround01:
                case AttackId.FloatAttackAir01:
                    PlayFirstStage();
                    break;

                case AttackId.FloatAttackGround02:
                case AttackId.FloatAttackAir02:
                    PlaySecondStage();
                    break;

                case AttackId.FloatAttackGround03:
                case AttackId.FloatAttackAir03:
                    PlayThirdStage();
                    break;
            }
        }

        // 结束当前专用御剑表现
        public void EndSpecialSwordAction()
        {
            for (int i = 0; i < swordCoroutines.Count; i++)
            {
                Coroutine coroutine = swordCoroutines[i];
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
            }
            swordCoroutines.Clear();

            for (int i = currentSwords.Count - 1; i >= 0; i--)
            {
                GameObject sword = currentSwords[i];
                if (sword != null)
                {
                    Destroy(sword);
                }
            }
            currentSwords.Clear();
        }
        #endregion

        #region 分段表现
        // 第一段：1把剑 c6 -> b5，起点支持额外抬高
        private void PlayFirstStage()
        {
            Vector3 startPosition = GetSwordPoint('c', 6, firstStageExtraHeight, firstStage.stageOffset);
            Vector3 targetPosition = GetSwordPoint('b', 5, 0f, firstStage.stageOffset);

            StartSpecialSwordMotion(firstStage, startPosition, targetPosition);
        }

        // 第二段：4把剑同时生成、同时飞行
        private void PlaySecondStage()
        {
            StartSpecialSwordMotion(secondStage, GetSwordPoint('b', 1, 0f, secondStage.stageOffset), GetSwordPoint('b', 9, 0f, secondStage.stageOffset));
            StartSpecialSwordMotion(secondStage, GetSwordPoint('b', 9, 0f, secondStage.stageOffset), GetSwordPoint('b', 1, 0f, secondStage.stageOffset));
            StartSpecialSwordMotion(secondStage, GetSwordPoint('b', 3, 0f, secondStage.stageOffset), GetSwordPoint('b', 7, 0f, secondStage.stageOffset));
            StartSpecialSwordMotion(secondStage, GetSwordPoint('b', 7, 0f, secondStage.stageOffset), GetSwordPoint('b', 3, 0f, secondStage.stageOffset));
        }

        // 第三段：1把剑 c6 -> b5，配置和第一段独立
        private void PlayThirdStage()
        {
            Vector3 startPosition = GetSwordPoint('c', 6, 0f, thirdStage.stageOffset);
            Vector3 targetPosition = GetSwordPoint('b', 5, 0f, thirdStage.stageOffset);

            StartSpecialSwordMotion(thirdStage, startPosition, targetPosition);
        }

        // 启动单把飞剑表现
        private void StartSpecialSwordMotion(SpecialSwordStageConfig stageConfig, Vector3 startPosition, Vector3 targetPosition)
        {
            if (stageConfig == null)
            {
                return;
            }

            Coroutine coroutine = StartCoroutine(PlaySpecialSwordMotionCoroutine(stageConfig, startPosition, targetPosition));
            swordCoroutines.Add(coroutine);
        }
        #endregion

        #region 飞剑协程
        // 播放单把飞剑飞行动画
        private IEnumerator PlaySpecialSwordMotionCoroutine(SpecialSwordStageConfig stageConfig, Vector3 startPosition, Vector3 targetPosition)
        {
            if (stageConfig.startDelay > 0f)
            {
                yield return new WaitForSeconds(stageConfig.startDelay);
            }

            if (!isActiveAndEnabled)
            {
                yield break;
            }

            if (stageConfig.swordPrefab == null)
            {
                yield break;
            }

            GameObject currentSword = Instantiate(stageConfig.swordPrefab);
            if (currentSword == null)
            {
                yield break;
            }

            currentSwords.Add(currentSword);

            Transform swordTransform = currentSword.transform;
            swordTransform.localScale = stageConfig.swordPrefab.transform.localScale * stageConfig.scaleMultiplier;

            Vector3 flyDirection = targetPosition - startPosition;
            Quaternion swordRotation = flyDirection.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(flyDirection.normalized, Vector3.up) * Quaternion.Euler(stageConfig.rotationOffsetEuler)
                : transform.rotation * Quaternion.Euler(stageConfig.rotationOffsetEuler);

            swordTransform.position = startPosition;
            swordTransform.rotation = swordRotation;

            float elapsed = 0f;
            float duration = Mathf.Max(0f, stageConfig.flightDuration);

            while (elapsed < duration)
            {
                if (currentSword == null)
                {
                    currentSwords.Remove(currentSword);
                    yield break;
                }

                elapsed += Time.deltaTime;
                float progress = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);

                swordTransform.position = Vector3.Lerp(startPosition, targetPosition, progress);
                swordTransform.rotation = swordRotation;

                yield return null;
            }

            if (currentSword != null)
            {
                swordTransform.position = targetPosition;
                swordTransform.rotation = swordRotation;
            }

            if (stageConfig.destroyDelay > 0f)
            {
                yield return new WaitForSeconds(stageConfig.destroyDelay);
            }

            if (currentSword != null)
            {
                currentSwords.Remove(currentSword);
                Destroy(currentSword);
            }
        }
        #endregion

        #region 空间点位
        // 根据层级和九宫格编号计算飞剑世界坐标
        private Vector3 GetSwordPoint(char layer, int pointIndex, float extraHeight, Vector3 stageOffset)
        {
            float height = ResolveLayerHeight(layer) + extraHeight;
            float forwardDistance = ResolveRowDistance(pointIndex);
            float rightOffset = ResolveHorizontalOffset(pointIndex);

            // 最终点位 = 全局空间偏移 + 当前段额外偏移 + 九宫格点位
            Vector3 localPosition = anchorOffset + stageOffset + new Vector3(rightOffset, height, forwardDistance);

            // 生成时锁定世界坐标，飞行过程中不跟随角色转向变化
            return transform.TransformPoint(localPosition);
        }

        // 解析高度层
        private float ResolveLayerHeight(char layer)
        {
            switch (layer)
            {
                case 'a':
                    return layerAHeight;
                case 'b':
                    return layerBHeight;
                case 'c':
                    return layerCHeight;
                default:
                    return layerBHeight;
            }
        }

        // 解析前后排距离：角色正对 8 -> 5 -> 2
        private float ResolveRowDistance(int pointIndex)
        {
            switch (pointIndex)
            {
                case 7:
                case 8:
                case 9:
                    return nearRowDistance;
                case 4:
                case 5:
                case 6:
                    return middleRowDistance;
                case 1:
                case 2:
                case 3:
                    return farRowDistance;
                default:
                    return middleRowDistance;
            }
        }

        // 解析左右偏移：7/4/1 左侧，9/6/3 右侧
        private float ResolveHorizontalOffset(int pointIndex)
        {
            switch (pointIndex)
            {
                case 1:
                case 4:
                case 7:
                    return -horizontalOffset;
                case 3:
                case 6:
                case 9:
                    return horizontalOffset;
                default:
                    return 0f;
            }
        }
        #endregion
    }
}
