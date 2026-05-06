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

            [Header("是否跟随角色")]
            public bool isFollowCharacter = false;

            [Header("是否自转")]
            public bool isSelfRotate = false;

            [Header("自转中心节点名（优先使用飞剑Prefab内同名Transform）")]
            public string selfRotatePivotNodeName = "";

            [Header("自转中心点（相对飞剑本地坐标）")]
            public Vector3 selfRotatePivot = Vector3.zero;

            [Header("自转轴（相对飞剑本地坐标）")]
            public Vector3 selfRotateAxis = Vector3.forward;

            [Header("自转速度")]
            public float selfRotateSpeed = 360f;

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

        [Header("=== 重击御剑 b2 -> b6 -> b8 -> b4 -> b2 ===")]
        [SerializeField] private SpecialSwordStageConfig heavyAttackStage = new SpecialSwordStageConfig();


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
                case AttackId.HAttack01:
                    PlayHeavyAttackStage();
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
        // 重击：1把剑沿 b2 -> b6 -> b8 -> b4 -> b2 平滑绕圈
        private void PlayHeavyAttackStage()
        {
            Vector3 pointB2 = GetSwordPoint('b', 2, 0f, heavyAttackStage.stageOffset);
            Vector3 pointB6 = GetSwordPoint('b', 6, 0f, heavyAttackStage.stageOffset);
            Vector3 pointB8 = GetSwordPoint('b', 8, 0f, heavyAttackStage.stageOffset);
            Vector3 pointB4 = GetSwordPoint('b', 4, 0f, heavyAttackStage.stageOffset);

            StartSpecialSwordCircleMotion(heavyAttackStage, pointB2, pointB6, pointB8, pointB4, pointB2);
        }
        // 启动重击圆形飞剑表现
        private void StartSpecialSwordCircleMotion(SpecialSwordStageConfig stageConfig, params Vector3[] pathPoints)
        {
            if (stageConfig == null)
            {
                return;
            }

            Coroutine coroutine = StartCoroutine(PlaySpecialSwordCircleMotionCoroutine(stageConfig, pathPoints));
            swordCoroutines.Add(coroutine);
        }
        // 播放重击圆形飞剑动画
        private IEnumerator PlaySpecialSwordCircleMotionCoroutine(SpecialSwordStageConfig stageConfig, Vector3[] pathPoints)
        {
            if (stageConfig.startDelay > 0f)
            {
                yield return new WaitForSeconds(stageConfig.startDelay);
            }

            if (!isActiveAndEnabled)
            {
                yield break;
            }

            if (stageConfig.swordPrefab == null || pathPoints == null || pathPoints.Length < 5)
            {
                yield break;
            }

            GameObject swordRoot = new GameObject("JinxiHeavyAttackSpecialSwordRoot");
            GameObject rotateRoot = new GameObject("JinxiHeavyAttackSpecialSwordRotateRoot");
            rotateRoot.transform.SetParent(swordRoot.transform, false);

            GameObject currentSword = Instantiate(stageConfig.swordPrefab, rotateRoot.transform);
            if (currentSword == null)
            {
                Destroy(swordRoot);
                yield break;
            }

            currentSwords.Add(swordRoot);

            Transform rootTransform = swordRoot.transform;
            Transform rotateTransform = rotateRoot.transform;
            Transform swordTransform = currentSword.transform;

            // 路径根节点负责移动，旋转根节点负责以指定中心点和轴自转，真正的飞剑只做本地偏移
            swordTransform.localRotation = Quaternion.identity;
            swordTransform.localScale = stageConfig.swordPrefab.transform.localScale * stageConfig.scaleMultiplier;
            swordTransform.localPosition = -Vector3.Scale(GetSelfRotatePivot(stageConfig, swordTransform), swordTransform.localScale);

            // 重击圆形御剑不跟随飞行方向，使用 Inspector 配置的基础旋转
            Quaternion baseRotation = transform.rotation * Quaternion.Euler(stageConfig.rotationOffsetEuler);

            rootTransform.position = pathPoints[0];
            rootTransform.rotation = baseRotation;
            rotateTransform.localRotation = Quaternion.identity;

            float elapsed = 0f;
            float duration = Mathf.Max(0f, stageConfig.flightDuration);

            while (elapsed < duration)
            {
                if (swordRoot == null)
                {
                    currentSwords.Remove(swordRoot);
                    yield break;
                }

                elapsed += Time.deltaTime;
                float progress = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);

                // 是否跟随角色：开启后每帧重新计算九宫格点位，关闭则保持生成瞬间的世界坐标
                if (stageConfig.isFollowCharacter)
                {
                    pathPoints[0] = GetSwordPoint('b', 2, 0f, stageConfig.stageOffset);
                    pathPoints[1] = GetSwordPoint('b', 6, 0f, stageConfig.stageOffset);
                    pathPoints[2] = GetSwordPoint('b', 8, 0f, stageConfig.stageOffset);
                    pathPoints[3] = GetSwordPoint('b', 4, 0f, stageConfig.stageOffset);
                    pathPoints[4] = GetSwordPoint('b', 2, 0f, stageConfig.stageOffset);

                    baseRotation = transform.rotation * Quaternion.Euler(stageConfig.rotationOffsetEuler);
                }

                rootTransform.position = GetClosedCatmullRomPoint(pathPoints, progress);
                rootTransform.rotation = baseRotation;

                // 飞剑自转：以 selfRotatePivot 为中心，沿 selfRotateAxis 指定的本地轴旋转
                if (stageConfig.isSelfRotate)
                {
                    Vector3 rotateAxis = stageConfig.selfRotateAxis.sqrMagnitude > 0.0001f
                        ? stageConfig.selfRotateAxis.normalized
                        : Vector3.forward;

                    rotateTransform.localRotation = Quaternion.AngleAxis(stageConfig.selfRotateSpeed * elapsed, rotateAxis);
                }
                else
                {
                    rotateTransform.localRotation = Quaternion.identity;
                }

                yield return null;
            }

            if (swordRoot != null)
            {
                rootTransform.position = pathPoints[pathPoints.Length - 1];
                rootTransform.rotation = baseRotation;
            }

            if (stageConfig.destroyDelay > 0f)
            {
                yield return new WaitForSeconds(stageConfig.destroyDelay);
            }

            if (swordRoot != null)
            {
                currentSwords.Remove(swordRoot);
                Destroy(swordRoot);
            }
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

        #region 自转中心
        // 获取飞剑自转中心：优先使用飞剑Prefab内指定名称的Transform节点，找不到时使用手填坐标
        private Vector3 GetSelfRotatePivot(SpecialSwordStageConfig stageConfig, Transform swordTransform)
        {
            if (stageConfig == null || swordTransform == null)
            {
                return Vector3.zero;
            }

            if (!string.IsNullOrEmpty(stageConfig.selfRotatePivotNodeName))
            {
                Transform pivotTransform = FindChildByName(swordTransform, stageConfig.selfRotatePivotNodeName);
                if (pivotTransform != null)
                {
                    return swordTransform.InverseTransformPoint(pivotTransform.position);
                }

                Debug.LogWarning("今汐重击御剑没有找到自转中心节点：" + stageConfig.selfRotatePivotNodeName);
            }

            return stageConfig.selfRotatePivot;
        }

        // 递归查找子节点：方便在飞剑Prefab内部放一个 RotatePivot 空物体作为自转中心
        private Transform FindChildByName(Transform parent, string childName)
        {
            if (parent == null || string.IsNullOrEmpty(childName))
            {
                return null;
            }

            if (parent.name == childName)
            {
                return parent;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform result = FindChildByName(parent.GetChild(i), childName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
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
        // 闭合 CatmullRom 曲线：让 b2 -> b6 -> b8 -> b4 -> b2 看起来更接近圆形轨迹
        private Vector3 GetClosedCatmullRomPoint(Vector3[] points, float progress)
        {
            int pointCount = points.Length - 1;
            float totalProgress = Mathf.Clamp01(progress) * pointCount;
            int currentIndex = Mathf.Min(Mathf.FloorToInt(totalProgress), pointCount - 1);
            float segmentProgress = totalProgress - currentIndex;

            Vector3 p0 = points[WrapIndex(currentIndex - 1, pointCount)];
            Vector3 p1 = points[WrapIndex(currentIndex, pointCount)];
            Vector3 p2 = points[WrapIndex(currentIndex + 1, pointCount)];
            Vector3 p3 = points[WrapIndex(currentIndex + 2, pointCount)];

            return CalculateCatmullRom(p0, p1, p2, p3, segmentProgress);
        }

        // 包装索引：闭合曲线使用
        private int WrapIndex(int index, int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            while (index < 0)
            {
                index += count;
            }

            return index % count;
        }

        // CatmullRom 曲线公式
        private Vector3 CalculateCatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            return 0.5f * (
                (2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );
        }

        #endregion
    }
}
