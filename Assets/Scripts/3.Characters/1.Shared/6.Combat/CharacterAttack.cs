using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// 类魂游戏核心命名空间
namespace WutheringWaves
{
    #region 攻击段
    //攻击段枚举
    public enum AttackStepListType
    {
        Attack,     // 对应 attackSteps
        FallAttack,// 对应 fallAttackSteps
        SkillAttack,
        SkillAirAttack
    }
    [System.Serializable]
    //攻击段
    public class AttackStep
    {
        [Header("攻击动画")]
        public AttackAnimation attackAnimation = new AttackAnimation();
        [Header("攻击数据")]
        public AttackData attackData = new AttackData();
        [Header("御剑轨迹（相对角色本地坐标）")]
        public SwordAnimation swordAnimation = new SwordAnimation();
        public override string ToString()
        {
            return $"攻击步骤：{attackAnimation.attackAnimationName}";
        }
    }
    [System.Serializable]
    //攻击动画
    public class AttackAnimation
    {
        [Header("攻击动画 Trigger 名称")]
        [Header("攻击动画名")]
        public string attackAnimationName;  // 攻击动画名（对应Animator的Trigger）
        [Header("攻击执行所需时长")]
        public float executionAttackCostTime;//攻击执行所需时长
        public float GetAnimationLength(Animator animator)
        {
            float Animationlength = animator.runtimeAnimatorController.animationClips.First(clip => clip.name == attackAnimationName).length;
            return Animationlength;
        }
    }
    [System.Serializable]
    //攻击数据
    public class AttackData
    {
        [Header("攻击数据")]
        [Header("伤害倍率")]
        public float damageMultiplier;// 伤害倍率
        [Header("体力消耗")]
        public float staminaCost;// 该段攻击耐力消耗

        [Header("攻击判定配置")]
        [Header("伤害球形判定半径")]
        public float attackRadius;// 球形判定半径
        [Header("伤害扇形判定角度")]
        public float attackAngle; // 扇形判定角度
        [Header("伤害判定偏移量")]
        public Vector3 attackOffset = new Vector3(0, 1f, 0.5f); // 判定偏移量

    }
    [System.Serializable]
    //御剑动画数据
    public class SwordAnimation
    {
        [Header("御剑动画总时长")]
        public float swordAnimationCostTime = 1f;
        [Header("御剑动画延迟")]
        public float DelayTime = 1f;
        [Header("是否有御剑动画")]
        public bool hasSwordAnimation = false;
        [Header("御剑对象（优先使用此处御剑对象，如为空使用通用御剑对象）")]
        public GameObject floatingSword;

        [Header("初始剑柄位置（相对于角色的本地位置/旋转）")]
        public Vector3 initSwordHandlePos = Vector3.zero;
        [Header("初始剑朝向（相对角色本地欧拉角）")]
        public Vector3 initSwordRotationEuler = Vector3.zero;
        public Quaternion InitSwordRotation => Quaternion.Euler(initSwordRotationEuler);

        [Header("轨迹列表（逐段飞行）")]
        public List<SwordTrajectory> swordTrajectory = new List<SwordTrajectory>();

    }
    [System.Serializable]
    //单段御剑轨迹
    public class SwordTrajectory
    {
        [Header("本段轨迹剑柄最终位置（相对角色本地坐标）")]
        public Vector3 targeSwordHandlelPos = Vector3.zero;
        [Header("本段轨迹剑最终朝向（相对角色本地欧拉角）")]
        public Vector3 targetSwordRotationEuler = Vector3.zero;
        public Quaternion TargetSwordRotation => Quaternion.Euler(targetSwordRotationEuler);
        [Header("时间权重/权重越大，耗时越长")]
        public float timeWeight = 1f;
    }

    [System.Serializable]
    // 今汐专用御空御剑的单段表现配置
    public class JinxiSpecialAirSwordStageConfig
    {
        [Header("飞剑预制体")]
        public GameObject swordPrefab;
        [Header("生成延迟")]
        public float startDelay = 0f;
        [Header("飞行总时长")]
        public float flightDuration = 0.25f;
        [Header("延迟销毁时长")]
        public float destroyDelay = 0.1f;
        [Header("缩放倍率")]
        public Vector3 scaleMultiplier = Vector3.one;
        [Header("模型朝向补偿")]
        public Vector3 rotationOffsetEuler = Vector3.zero;
    }

    [System.Serializable]
    // 今汐专用御空御剑的整体参数配置
    public class JinxiSpecialAirSwordConfig
    {
        [Header("是否启用今汐专用御空御剑")]
        public bool enable = false;
        [Header("整体局部偏移（相对角色Transform）")]
        public Vector3 anchorOffset = Vector3.zero;
        [Header("a层高度（脚）")]
        public float layerAHeight = 0f;
        [Header("b层高度（胯）")]
        public float layerBHeight = 1f;
        [Header("c层高度（头）")]
        public float layerCHeight = 2f;
        [Header("近排距离（7/8/9）")]
        public float nearRowDistance = 1.2f;
        [Header("中排距离（4/5/6）")]
        public float middleRowDistance = 2f;
        [Header("远排距离（1/2/3）")]
        public float farRowDistance = 2.8f;
        [Header("左右横向偏移")]
        public float horizontalOffset = 0.8f;
        [Header("第一段起点额外抬高")]
        public float firstStageExtraHeight = 0.6f;
        [Header("第一段配置")]
        public JinxiSpecialAirSwordStageConfig firstStage = new JinxiSpecialAirSwordStageConfig();
        [Header("第二段配置")]
        public JinxiSpecialAirSwordStageConfig secondStage = new JinxiSpecialAirSwordStageConfig();
        [Header("第三段配置")]
        public JinxiSpecialAirSwordStageConfig thirdStage = new JinxiSpecialAirSwordStageConfig();
    }
    #endregion

    public class CharacterAttack : MonoBehaviour
    {
        // 核心依赖组件
        private CharacterContext context;            // 角色共享上下文
        private Animator animator;                  // 角色动画控制器
        private CharacterDataSO characterData;      // 角色静态数据（攻击力等基础属性）
        private CombatConfigSO combatConfig;        // 角色战斗配置（攻击段等）

        #region 攻击配置（Inspector面板可编辑参数）
        [Header("===连击窗口持续时间（秒）===")]
        public float comboWindowDuration = 1.5f;
        [Header("攻击可命中的敌人层级")]
        public LayerMask enemyLayer;

        [Header("=== 御剑配置===")]
        [Header("通用御剑对象")]
        public GameObject floatingSword;
        [Header("长剑配置")]
        [Tooltip("剑的实际长度（用于绘制长条剑、计算朝向）")]
        public float swordLength = 1f;
        [Header("御剑偏移")]
        public Vector3 swordPositionOffset;//御剑位置偏移（相对角色本地坐标）
        public Vector3 swordRotationOffset; // 御剑角度偏移（相对角色本地欧拉角
        // 当前运行时生成出来的通用御剑实例
        private GameObject sword;
        [Header("=== 今汐专用御空御剑 ===")]
        public JinxiSpecialAirSwordConfig jinxiSpecialAirSword = new JinxiSpecialAirSwordConfig();

        [Header("=== 旧配置兼容（迁移完成后可删除） ===")]
        [Tooltip("旧轻攻击攻击段列表：仅在 CombatConfigSO 未配置时兜底使用")]
        [SerializeField] private List<AttackStep> legacyAttackSteps = new List<AttackStep>();
        [Tooltip("旧下落攻击段列表：仅在 CombatConfigSO 未配置时兜底使用")]
        [SerializeField] private List<AttackStep> legacyFallAttackSteps = new List<AttackStep>();
        [Tooltip("旧御空攻击段列表（空中）：仅在 CombatConfigSO 未配置时兜底使用")]
        [SerializeField] private List<AttackStep> legacySkillAirAttackSteps = new List<AttackStep>();
        [Tooltip("旧御空攻击段列表（近地）：仅在 CombatConfigSO 未配置时兜底使用")]
        [SerializeField] private List<AttackStep> legacySkillAttackSteps = new List<AttackStep>();
        [Tooltip("旧技能攻击段列表：仅在 CombatConfigSO 未配置时兜底使用")]
        [SerializeField] private List<AttackStep> legacyESkillAttackSteps = new List<AttackStep>();
        [Tooltip("旧爆发攻击段列表：仅在 CombatConfigSO 未配置时兜底使用")]
        [SerializeField] private List<AttackStep> legacyQBurstAttackSteps = new List<AttackStep>();
        #endregion

        #region 攻击段配置访问器
        // 轻攻击攻击段：优先读取 CombatConfigSO，未配置时回退到旧 Inspector 数据
        public List<AttackStep> attackSteps => ResolveAttackSteps(combatConfig != null ? combatConfig.attackSteps : null, legacyAttackSteps);
        // 下落攻击段：优先读取 CombatConfigSO，未配置时回退到旧 Inspector 数据
        public List<AttackStep> fallAttackSteps => ResolveAttackSteps(combatConfig != null ? combatConfig.fallAttackSteps : null, legacyFallAttackSteps);
        // 御空攻击段（空中）：优先读取 CombatConfigSO，未配置时回退到旧 Inspector 数据
        public List<AttackStep> SkillAirAttackSteps => ResolveAttackSteps(combatConfig != null ? combatConfig.skillAirAttackSteps : null, legacySkillAirAttackSteps);
        // 御空攻击段（近地）：优先读取 CombatConfigSO，未配置时回退到旧 Inspector 数据
        public List<AttackStep> SkillAttackSteps => ResolveAttackSteps(combatConfig != null ? combatConfig.skillAttackSteps : null, legacySkillAttackSteps);
        // 技能攻击段：优先读取 CombatConfigSO，未配置时回退到旧 Inspector 数据
        public List<AttackStep> ESkillAttackSteps => ResolveAttackSteps(combatConfig != null ? combatConfig.eSkillAttackSteps : null, legacyESkillAttackSteps);
        // 爆发攻击段：优先读取 CombatConfigSO，未配置时回退到旧 Inspector 数据
        public List<AttackStep> QBurstAttackSteps => ResolveAttackSteps(combatConfig != null ? combatConfig.qBurstAttackSteps : null, legacyQBurstAttackSteps);
        #endregion

        #region 初始化
        // 组件初始化：由CharacterFacade调用，绑定共享依赖
        public void Initialize(CharacterContext context)
        {
            //1.初始化Context
            this.context = context;
            //2.获取子物体的动画控制器
            this.animator = context != null ? context.Animator : null;
            //3.获取角色属性数据
            this.characterData = context != null ? context.CharacterDataSO : null;
            //4.获取角色战斗配置
            this.combatConfig = characterData != null ? characterData.combatConfig : null;
        }

        #endregion

        #region 配置解析
        // 攻击段配置解析：优先使用 CombatConfigSO，未配置时回退到旧 Inspector 列表
        private List<AttackStep> ResolveAttackSteps(List<AttackStep> combatConfigSteps, List<AttackStep> legacySteps)
        {
            if (combatConfigSteps != null && combatConfigSteps.Count > 0)
            {
                return combatConfigSteps;
            }

            return legacySteps;
        }
        #endregion

        private void Update()
        {
            // ========== 普通连击窗口更新 ==========
            UpdateNormalAttackComboWindow();

            // ========== 御空连击窗口更新 ==========
            UpdateAirAttackComboWindow();

            // ========== 技能CD与窗口更新 ==========
            UpdateSkillWindow();

            // 龙跟随角色时，按当前攻击步骤使用对应的本地偏移
            UpdateDragonPosition();
        }

        #region Gizmos调试（编辑器可视化）
        [Header("=== Gizmos预览配置 ===")]
        [Tooltip("选择要预览的攻击列表")]
        public AttackStepListType selectedListType; //列表选择器
        [Tooltip("选择要预览的攻击段索引")]
        public int selectedAttackStepIndex = 0;     // 索引选择器
        [Header("是否启用攻击范围Gizmos）")]
        public bool useGizmosAttackRange = true;
        [Header("是否启用御剑轨迹Gizmos）")]
        public bool useGizmosSwordTrajectory = true;
        [Header("编辑器模型刷新周期(秒)")]
        public float editorSwordRefreshInterval = 2f;

        // 使用List存储多个剑的实例（初始点 + 所有轨迹点）
        private List<GameObject> _editorSwordInstances = new List<GameObject>();
        private float _editorTimer;              // 编辑器计时器
        private void OnDrawGizmos()
        {
            //获取当前step
            AttackStep targetAttackStep = GetAttackStepForGizmos();
            //对应动作攻击范围Gizmos
            GizmosAttackRange(targetAttackStep);
            //对应动作御剑轨迹Gizmos (绘制线条)
            GizmosSwordTrajectory(targetAttackStep);
            //生成剑模型
            EditorSwordRefreshLogic(targetAttackStep);

        }
        //获取当前step
        private AttackStep GetAttackStepForGizmos()
        {
            List<AttackStep> targetList = null;

            // 根据枚举选择对应的 List
            switch (selectedListType)
            {
                case AttackStepListType.Attack:
                    targetList = attackSteps;
                    break;
                case AttackStepListType.FallAttack:
                    targetList = fallAttackSteps;
                    break;
                case AttackStepListType.SkillAttack:
                    targetList = SkillAttackSteps;
                    break;
                case AttackStepListType.SkillAirAttack:
                    targetList = SkillAirAttackSteps;
                    break;
                    // 未来新增列表只需在这里添加 case
            }

            // 安全校验
            if (targetList == null || targetList.Count == 0) return null;
            if (selectedAttackStepIndex < 0 || selectedAttackStepIndex >= targetList.Count) return null;

            return targetList[selectedAttackStepIndex];
        }
        // 对应动作攻击范围Gizmos
        private void GizmosAttackRange(AttackStep targetAttackStep)
        {
            if (!useGizmosAttackRange || targetAttackStep == null) return;

            AttackData attackData = targetAttackStep.attackData;
            Vector3 attackCenter = transform.position + transform.TransformDirection(attackData.attackOffset);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackCenter, attackData.attackRadius);

            Gizmos.color = Color.yellow;
            Vector3 forwardDir = transform.forward;
            float halfAngleRad = attackData.attackAngle * 0.5f * Mathf.Deg2Rad;
            int segments = 12;

            for (int i = 0; i <= segments; i++)
            {
                float currentAngleRad = Mathf.Lerp(-halfAngleRad, halfAngleRad, (float)i / segments);
                Vector3 radialDir = Quaternion.AngleAxis(currentAngleRad * Mathf.Rad2Deg, Vector3.up) * forwardDir;
                Vector3 arcPoint = attackCenter + radialDir * attackData.attackRadius;
                Gizmos.DrawLine(attackCenter, arcPoint);
            }
        }
        // 对应动作御剑轨迹Gizmos (仅绘制线条，模型由上面的逻辑生成)
        private void GizmosSwordTrajectory(AttackStep targetAttackStep)
        {
            if (!useGizmosSwordTrajectory || targetAttackStep == null) return;

            SwordAnimation swordAnim = targetAttackStep.swordAnimation;
            if (swordAnim == null) return;

            // 简单的线条绘制逻辑（可选），帮助看清路径
            Gizmos.color = Color.cyan;

            // 1. 转换初始点为世界坐标
            Vector3 prevPoint = transform.TransformPoint(swordAnim.initSwordHandlePos + swordPositionOffset);

            if (swordAnim.swordTrajectory == null || swordAnim.swordTrajectory.Count == 0) return;

            // 2. 绘制连线
            for (int i = 0; i < swordAnim.swordTrajectory.Count; i++)
            {
                var traj = swordAnim.swordTrajectory[i];
                Vector3 currentPoint = transform.TransformPoint(traj.targeSwordHandlelPos + swordPositionOffset);

                Gizmos.DrawLine(prevPoint, currentPoint);
                prevPoint = currentPoint;
            }
        }
        // 编辑模式下的剑刷新逻辑：在所有关键点生成剑模型
        private void EditorSwordRefreshLogic(AttackStep targetAttackStep)
        {
            // 1. 基础安全检查
            // 【修改】这里的 floatingSword 检查保留，作为最后的兜底
            if (floatingSword == null || !useGizmosSwordTrajectory || targetAttackStep == null)
            {
                DestroyImmediateIfNeeded();
                return;
            }

            SwordAnimation swordAnim = targetAttackStep.swordAnimation;
            if (swordAnim == null || !swordAnim.hasSwordAnimation)
            {
                DestroyImmediateIfNeeded();
                return;
            }

            // 2. 编辑器时间累积
            _editorTimer += Time.deltaTime;

            // 3. 如果列表为空（第一次）或者时间到了，进行刷新
            if (_editorSwordInstances.Count == 0 || _editorTimer >= editorSwordRefreshInterval)
            {
                // 3.1 销毁旧的所有实例
                DestroyImmediateIfNeeded();

                // 核心逻辑：优先用当前攻击段的剑，没有则用通用的
                GameObject currentPrefab = swordAnim.floatingSword != null ? swordAnim.floatingSword : floatingSword;

                // 3.2 生成初始位置的剑（传入四元数）
                SpawnEditorSword(swordAnim.initSwordHandlePos, swordAnim.InitSwordRotation, currentPrefab);

                // 3.3 生成轨迹列表中每一个位置的剑（传入四元数）
                if (swordAnim.swordTrajectory != null)
                {
                    foreach (var traj in swordAnim.swordTrajectory)
                    {
                        SpawnEditorSword(traj.targeSwordHandlelPos, traj.TargetSwordRotation, currentPrefab);
                    }
                }

                // 3.4 重置计时器
                _editorTimer = 0f;
            }
        }
        // 辅助方法：在指定位置生成一把编辑器用剑
        private void SpawnEditorSword(Vector3 localPos, Quaternion localRot, GameObject swordPrefab)
        {
            if (swordPrefab == null) return;

            Vector3 finalLocalPos = localPos + swordPositionOffset;
            //四元数乘法叠加旋转偏移（注意顺序：偏移 * 基础旋转）
            Quaternion finalLocalRot = Quaternion.Euler(swordRotationOffset) * localRot;

#if UNITY_EDITOR
            GameObject sword = (GameObject)PrefabUtility.InstantiatePrefab(swordPrefab, transform);
#else
    GameObject sword = Instantiate(swordPrefab, transform);
#endif

            if (sword != null)
            {
                sword.transform.localPosition = finalLocalPos;
                sword.transform.localRotation = finalLocalRot; // 直接赋值四元数
                sword.hideFlags = HideFlags.HideAndDontSave;
                _editorSwordInstances.Add(sword);
            }
        }
        // 辅助方法：安全销毁编辑器实例列表
        private void DestroyImmediateIfNeeded()
        {
            if (_editorSwordInstances != null && _editorSwordInstances.Count > 0)
            {
                foreach (var sword in _editorSwordInstances)
                {
                    if (sword != null)
                    {
                        DestroyImmediate(sword);
                    }
                }
                _editorSwordInstances.Clear();
            }
        }

        #endregion

        #region 范围检测
        // 攻击命中检测：球形范围+扇形角度筛选敌人
        private void CheckAttackHit()
        {
            // 安全判断：防止段数越界
            if (currentComboCount >= attackSteps.Count) return;

            // 获取当前段数的攻击数据（索引=连击段数）
            AttackStep currentStep = attackSteps[currentComboCount];
            // 【修改】通过 attackData 子对象访问字段
            AttackData attackData = currentStep.attackData;

            // 计算攻击判定位置
            Vector3 attackCenter = transform.position + transform.TransformDirection(attackData.attackOffset);
            // 球形检测
            Collider[] hitEnemies = Physics.OverlapSphere(attackCenter, attackData.attackRadius, enemyLayer);
            // 计算伤害
            float actualDamage = characterData.baseAttack * attackData.damageMultiplier;

            // 命中判定
            foreach (var enemy in hitEnemies)
            {
                Vector3 dir = (enemy.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, dir);
                if (angle <= attackData.attackAngle / 2f)
                {
                    Debug.Log($"命中{enemy.name} | 段数:{currentComboCount} | 伤害:{actualDamage}");
                }
            }
        }
        #endregion

        #region 普通攻击方法
        // 攻击运行时状态（内部逻辑变量）
        private AttackStep currentStep;
        private int currentComboCount = 0;   // 当前连击段数（从0开始）
        private float _comboWindowTimer = 0f;  // 连击窗口计时器
        private bool _isComboWindowOpen = false;  // 连击窗口是否已开启
        // 普通攻击判断
        public bool IsAttackable()
        {
            //条件1：是否为可打断状态（非受击/非死亡，基础状态判断）
            bool isCanInterrupt = context != null && context.StateMachine != null && context.StateMachine.IsInterruptible();
            //条件2：在地面上
            bool isGrounded = context != null && context.MovementLogic != null && context.MovementLogic.CustomCheckGrounded();
            //条件3：处于非御空状态
            bool isFloating = _isFloating;
            // 满足所有条件 → 可以攻击
            return isCanInterrupt && isGrounded&&!isFloating;
        }
        //普通攻击数据判断
        public AttackStep InitializeSteps(List<AttackStep> Step)
        {
            // 1. 安全校验：如果没有配置攻击段，返回 null
            if (Step == null || Step.Count == 0)
            {
                Debug.LogError("Steps list is empty!");
                return null;
            }

            // 2. 判断逻辑：
            // 如果窗口开启 -> 打下一步 (currentComboCount + 1)如果窗口关闭 -> 打第一段 (currentComboCount = 0)
            if (_isComboWindowOpen)
            {
                // 确保段数不越界 (如果超过配置的最大段数，回到第一段或者保持最后一段，这里按回到第一段处理)
                currentComboCount++;
                if (currentComboCount >= attackSteps.Count)
                {
                    currentComboCount = 0; // 或者改为 currentComboCount = attackSteps.Count - 1; 看你需求
                }
            }
            else
            {
                // 窗口关闭，重置为第一段
                currentComboCount = 0;
            }
            _isComboWindowOpen = false;
            // 3. 拿到当前要打的攻击段数据
            AttackStep currentAttackStep = Step[currentComboCount];
            currentStep = Step[currentComboCount];
            // 4. 返回数据供状态机播放动画
            return currentAttackStep;
        }
        public void StartComboWindow()
        {
            _isComboWindowOpen = true;
            _comboWindowTimer = comboWindowDuration;
            // ==========地面普攻第四段触发Skill2窗口 ==========
            if (!_isFloating && currentComboCount == 3)
            {
                OpenSkill2Window();
            }
        }
        // 重置连击 
        public void ResetCombo()
        {
            currentComboCount = 0;
            _isComboWindowOpen = false;
            _comboWindowTimer = 0f;
        }
         // 普通连击窗口更新 
        private void UpdateNormalAttackComboWindow()
        {
          
            if (_isComboWindowOpen)
            {
                _comboWindowTimer -= Time.deltaTime;
                if (_comboWindowTimer <= 0)
                {
                    _isComboWindowOpen = false;
                    ResetCombo();
                }
            }
        }
        #endregion

        #region 下落攻击方法
        //下落攻击判断
        public bool IsFallAttackable()
        {
            //条件1：是否为可打断状态（非受击/非死亡，基础状态判断）
            bool isCanInterrupt = context != null && context.StateMachine != null && context.StateMachine.IsInterruptible();
            //条件2：是否在地面
            bool isInTheAir = context != null && context.MovementLogic != null && !context.MovementLogic.CustomCheckGrounded();
            //条件3：是否为御空状态
            bool IsFloating = _isFloating;

            // 满足所有条件 → 可以攻击
            return isCanInterrupt && isInTheAir &&!IsFloating;
        }
        #endregion

        #region 御空攻击方法
        // 御空连击运行时状态
        private int _currentAirComboCount = 0; // 御空攻击当前连击段数
        private float _airComboWindowTimer = 0f; // 御空连击窗口计时器
        private bool _isAirComboWindowOpen = false; // 御空连击窗口是否开启
        //御空攻击判断
        public bool IsAirAttackable()
        {
            //条件1：是否为可打断状态（非受击/非死亡闪避爆发状态判断）
            bool isCanInterrupt = context != null && context.StateMachine != null && context.StateMachine.IsInterruptible();
            //条件2：处于御空状态
            bool isFloating = _isFloating;
            return isCanInterrupt && isFloating;
        }
        // 初始化御空攻击段
        public AttackStep InitializeAirSteps(List<AttackStep> stepList)
        {
            // 1. 安全校验：如果没有配置攻击段，返回 null
            if (stepList == null || stepList.Count == 0)
            {
                Debug.LogError("御空攻击段配置为空！");
                return null;
            }
            // 2. 判断逻辑：
            // 如果窗口开启 -> 打下一步 (currentComboCount + 1)如果窗口关闭 -> 打第一段 (currentComboCount = 0)
            if (_isAirComboWindowOpen)
            {
                _currentAirComboCount++;
                if (_currentAirComboCount >= stepList.Count)
                {
                    _currentAirComboCount = 0;
                }
                    
            }
            else
            {
                _currentAirComboCount = 0;
            }
            _isAirComboWindowOpen = false;
            // 3. 拿到当前要打的攻击段数据
            currentStep = stepList[_currentAirComboCount];
            // 4. 返回数据供状态机播放动画
            return currentStep;
        }

        // 开启御空连击窗口
        public void StartAirComboWindow()
        {
            _isAirComboWindowOpen = true;
            _airComboWindowTimer = comboWindowDuration;
            // 御空普攻第四段触发Skill4窗口 
            if (_currentAirComboCount == 3)
            {
                OpenSkill4Window();
            }
        }

        // 重置御空连击
        public void ResetAirCombo()
        {
            _currentAirComboCount = 0;
            _isAirComboWindowOpen = false;
            _airComboWindowTimer = 0f;
        }
        // 御空连击窗口更新
        private void UpdateAirAttackComboWindow()
        {
            if (_isAirComboWindowOpen)
            {
                _airComboWindowTimer -= Time.deltaTime;
                if (_airComboWindowTimer <= 0)
                {
                    _isAirComboWindowOpen = false;
                    ResetAirCombo();
                }
            }
        }
        #endregion

        #region 战技攻击方法（流光夕影）（神霓飞芒）（逐天取月）（乘岁凌霄）
        public bool CanUseSkill1()
        {
            //条件1：是否在CD
            bool isCD = _skill1CDTimer <= 0f;
            //条件2：在地面上
            bool isGrounded = context != null && context.MovementLogic != null && context.MovementLogic.CustomCheckGrounded();
            return isCD&& isGrounded;
        }
        
        public bool CanUseSkill2 => _isSkill2WindowOpen && _skill2WindowTimer > 0f;
        public bool CanUseSkill3 => _isSkill3WindowOpen && _skill3WindowTimer > 0f;
        public bool CanUseSkill4 => _isSkill4WindowOpen && _skill4WindowTimer > 0f;
        //战技攻击判断
        public bool IsESkillable()
        {
            //条件1：是否为可打断状态（非受击/非死亡，基础状态判断）
            bool isCanInterrupt = context != null && context.StateMachine != null && context.StateMachine.IsInterruptible();
            //条件2： Skill1  Skill2  Skill3  Skill4是否可用
            bool CanEskill = CanUseSkill1 ()|| CanUseSkill2 || CanUseSkill3 || CanUseSkill4;
            return isCanInterrupt&& CanEskill;
        }

        //战技攻击判断
        public AttackStep InitializeESkillSteps()
        {
            // 1. 安全校验：如果没有配置攻击段，返回 null
            if (ESkillAttackSteps == null || ESkillAttackSteps.Count == 0)
            {
                Debug.LogError("Steps list is empty!");
                return null;
            }
            // 2. 拿到当前要打的攻击段数据
            AttackStep currentAttackStep;
            if (_isSkill4WindowOpen)
            {
                 currentAttackStep = ESkillAttackSteps[3];
            }
            else if(_isSkill3WindowOpen)
            {
                currentAttackStep = ESkillAttackSteps[2];
            }
            else if(_isSkill2WindowOpen)
            {
                currentAttackStep = ESkillAttackSteps[1];
            }
        else
            {
                currentAttackStep = ESkillAttackSteps[0];
            }

            // 3. 返回数据供状态机播放动画
            currentStep = currentAttackStep;
            return currentAttackStep;
        }
        // Skill1使用后回调
        public void OnSkill1Used()
        {
            _skill1CDTimer = skill1CD;
            _isSkill2WindowOpen = false; // 用了普通E关掉Skill2窗口
            _skill2WindowTimer = 0f;
            NotifySkillUIChanged();
        }
        // Skill2使用后回调
        public void OnSkill2Used()
        {
            _isSkill2WindowOpen = false;
            _skill2WindowTimer = 0f;
            OpenSkill3Window(); // 开启Skill3窗口
            SetFloating(true); // 进入御空状态
            NotifySkillUIChanged();
        }
        // Skill3使用后回调
        public void OnSkill3Used()
        {
            _isSkill3WindowOpen = false;
            _skill3WindowTimer = 0f;
            NotifySkillUIChanged();
        }
        // Skill4使用后回调
        public void OnSkill4Used()
        {
            _isSkill4WindowOpen = false;
            _isSkill3WindowOpen = false;
            _skill4WindowTimer = 0f;
            _skill3WindowTimer = 0f;
            NotifySkillUIChanged();
        }
        #endregion

        #region 爆发攻击方法（移岁诛邪）
        //爆发攻击判断
        public bool IsQBurstable()
        {
            //条件1：是否为可打断状态（非受击/非死亡，基础状态判断）
            bool isCanInterrupt = context != null && context.StateMachine != null && context.StateMachine.IsInterruptible();
            //条件2：是否存在爆发攻击段配置
            bool hasQBurstStep = HasQBurstConfigured;
            //条件3：是否不在CD
            bool isCD = _qBurstCDTimer <= 0f;
            return isCanInterrupt && hasQBurstStep && isCD;
        }
        // 爆发使用后回调
        public void OnQBurstUsed()
        {
            _qBurstCDTimer = qBurstCD;
            NotifySkillUIChanged();
        }

        #endregion

        #region 延奏攻击方法（蟠龙清辉）
        //爆发攻击判断
        public bool IsRBurstable()
        {
            return false;
        }

        #endregion

        #region 技能CD与窗口运行时状态
        [Header("=== 技能CD与窗口配置 ===")]
        public float skill1CD = 5f;       // Skill1 最大CD（流光夕影）
        public float skill2WindowCD = 5f; // Skill2 窗口持续时间（神霓飞芒）
        public float skill3WindowCD = 5f; // Skill3 窗口持续时间（逐天取月）
        public float skill4WindowCD = 5f; // Skill4 窗口持续时间（惊龙破空）
        public float FloatingDuration = 10f; // 御空状态持续时间（乘岁凌霄）
        public float qBurstCD = 15f; // 爆发CD（移岁诛邪）

        // 运行时计时器：分别记录普通战技CD、派生技能窗口、御空状态、爆发CD
        private float _skill1CDTimer; // Skill1普通战技CD计时器
        private float _skill2WindowTimer; // Skill2窗口计时器
        private float _skill3WindowTimer; // Skill3窗口计时器
        private float _skill4WindowTimer; // Skill4窗口计时器
        private float _FloatingTimer; // 御空状态计时器
        private float _qBurstCDTimer; // 爆发CD计时器

        private bool _isSkill2WindowOpen; // Skill2窗口是否可用
        private bool _isSkill3WindowOpen; // Skill3窗口是否可用
        private bool _isSkill4WindowOpen; // Skill4窗口是否可用

        private bool _isFloating; // 是否处于御空状态

        // 对外暴露给 UI / 其他系统读取的只读状态
        public bool IsFloating => _isFloating;
        public float Skill1CDTimer => Mathf.Max(0f, _skill1CDTimer);
        public float Skill2WindowTimer => _isSkill2WindowOpen ? Mathf.Max(0f, _skill2WindowTimer) : 0f;
        public float Skill3WindowTimer => _isSkill3WindowOpen ? Mathf.Max(0f, _skill3WindowTimer) : 0f;
        public float Skill4WindowTimer => _isSkill4WindowOpen ? Mathf.Max(0f, _skill4WindowTimer) : 0f;
        public float QBurstCDTimer => Mathf.Max(0f, _qBurstCDTimer);
        public bool IsSkill2WindowOpen => _isSkill2WindowOpen && _skill2WindowTimer > 0f;
        public bool IsSkill3WindowOpen => _isSkill3WindowOpen && _skill3WindowTimer > 0f;
        public bool IsSkill4WindowOpen => _isSkill4WindowOpen && _skill4WindowTimer > 0f;
        public bool HasQBurstConfigured => QBurstAttackSteps != null && QBurstAttackSteps.Count > 0;
        // 当前 E 技能 UI 应展示的阶段编号：4 优先级最高，依次回退到 1
        public int CurrentESkillUIIndex
        {
            get
            {
                if (IsSkill4WindowOpen) return 4;
                if (IsSkill3WindowOpen) return 3;
                if (IsSkill2WindowOpen) return 2;
                return 1;
            }
        }
        // 当前 UI 所显示阶段对应的总时长，用于进度条满值
        public float CurrentESkillDisplayDuration
        {
            get
            {
                switch (CurrentESkillUIIndex)
                {
                    case 4:
                        return skill4WindowCD;
                    case 3:
                        return skill3WindowCD;
                    case 2:
                        return skill2WindowCD;
                    default:
                        return skill1CD;
                }
            }
        }
        // 当前 UI 所显示阶段对应的剩余时间，用于实时刷新冷却/窗口进度
        public float CurrentESkillDisplayTimer
        {
            get
            {
                switch (CurrentESkillUIIndex)
                {
                    case 4:
                        return Skill4WindowTimer;
                    case 3:
                        return Skill3WindowTimer;
                    case 2:
                        return Skill2WindowTimer;
                    default:
                        return Skill1CDTimer;
                }
            }
        }

        // 设置御空状态
        public void SetFloating(bool value)
        {
            _isFloating = value;
            _FloatingTimer = value ? FloatingDuration : 0f;
            GameEvents.RaiseFloatingChanged(this, _isFloating);
            NotifySkillUIChanged();
        }

        // 技能CD与窗口更新
        private void UpdateSkillWindow()
        {
            bool isSkillUIChanged = false;
            // Skill1 CD倒计时
            if (_skill1CDTimer > 0)
            {
                _skill1CDTimer = Mathf.Max(0f, _skill1CDTimer - Time.deltaTime);
                isSkillUIChanged = true;
            }
            // Skill2窗口倒计时
            if (_isSkill2WindowOpen)
            {
                _skill2WindowTimer -= Time.deltaTime;
                isSkillUIChanged = true;
                if (_skill2WindowTimer <= 0)
                {
                    _skill2WindowTimer = 0f;
                    _isSkill2WindowOpen = false;
                }
            }
            // Skill3窗口倒计时
            if (_isSkill3WindowOpen)
            {
                _skill3WindowTimer -= Time.deltaTime;
                isSkillUIChanged = true;
                if (_skill3WindowTimer <= 0)
                {
                    _skill3WindowTimer = 0f;
                    _isSkill3WindowOpen = false;
                }
            }
            // Skill4窗口倒计时
            if (_isSkill4WindowOpen)
            {
                _skill4WindowTimer -= Time.deltaTime;
                isSkillUIChanged = true;
                if (_skill4WindowTimer <= 0)
                {
                    _skill4WindowTimer = 0f;
                    _isSkill4WindowOpen = false;
                }
            }
            // 爆发CD倒计时
            if (_qBurstCDTimer > 0f)
            {
                _qBurstCDTimer = Mathf.Max(0f, _qBurstCDTimer - Time.deltaTime);
                isSkillUIChanged = true;
            }
            if (_isFloating)
            {
                _FloatingTimer -= Time.deltaTime;
                if (_FloatingTimer <= 0)
                    SetFloating(false);
            }

            if (isSkillUIChanged)
                NotifySkillUIChanged();
        }

        #region 技能窗口与御空控制
        // 开启Skill2窗口（普攻第四段调用）
        public void OpenSkill2Window()
        {
            _isSkill2WindowOpen = true;
            _skill2WindowTimer = skill2WindowCD;
            NotifySkillUIChanged();
        }
        // 开启Skill3窗口（Skill2使用后调用）
        public void OpenSkill3Window()
        {
            _isSkill3WindowOpen = true;
            _skill3WindowTimer = skill3WindowCD;
            NotifySkillUIChanged();
        }
        // 开启Skill4窗口（御空普攻第四段调用）
        public void OpenSkill4Window()
        {
            _isSkill4WindowOpen = true;
            _skill4WindowTimer = skill4WindowCD;
            NotifySkillUIChanged();
        }
        #endregion

        private void NotifySkillUIChanged()
        {
            GameEvents.RaiseSkillUIStateChanged(this);
        }

        #endregion

        #region 御剑动画
        // 协程与实例缓存：用于中断旧御剑表现，避免重复生成飞剑对象
        private Coroutine _flyingSwordCoroutine;
        private Coroutine _jinxiSpecialSwordCoroutine;
        private readonly List<GameObject> _jinxiSpecialSwords = new List<GameObject>();


        // 启动御剑动画：优先尝试今汐专用表现，否则走通用轨迹御剑逻辑
        public void StartFlyingSword(AttackStep Step)
        {
            if (Step == null) return;

            if (TryPlayJinxiSpecialAirSword(Step))
            {
                return;
            }

            SwordAnimation swordAnim = Step.swordAnimation;
            if (swordAnim == null || !swordAnim.hasSwordAnimation || floatingSword == null) return;

            // 安全措施：停止旧协程 + 销毁旧剑
            if (_flyingSwordCoroutine != null) StopCoroutine(_flyingSwordCoroutine);
            if (sword != null) Destroy(sword);

            _flyingSwordCoroutine = StartCoroutine(FlySwordCoroutine(swordAnim));
        }

        // 通用御剑飞行协程：按配置轨迹逐段插值移动与旋转
        private IEnumerator FlySwordCoroutine(SwordAnimation swordAnim)
        {
            // 先等待攻击段配置的延迟，再创建御剑实体
            yield return new WaitForSeconds(swordAnim.DelayTime);

            GameObject prefabToUse = swordAnim.floatingSword != null ? swordAnim.floatingSword : floatingSword;
            sword = Instantiate(prefabToUse);
            if (sword == null) yield break;

            Transform swordTrans = sword.transform;
            // 运行时适当放大模型，让飞剑表现更明显
            swordTrans.localScale = prefabToUse.transform.localScale * 1.2f;

            // 将每一段轨迹的 timeWeight 归一化到总动画时长里
            float totalWeight = swordAnim.swordTrajectory.Sum(t => t.timeWeight);
            totalWeight = totalWeight <= 0 ? 1 : totalWeight;

            // 【修改】初始旋转：全程四元数叠加，不转欧拉角
            Vector3 initPosWithOffset = swordAnim.initSwordHandlePos + swordPositionOffset;
            Quaternion initRotWithOffset = Quaternion.Euler(swordRotationOffset) * swordAnim.InitSwordRotation;
            Vector3 currentHandlePos = transform.TransformPoint(initPosWithOffset);
            Quaternion currentSwordRot = transform.rotation * initRotWithOffset;

            swordTrans.position = currentHandlePos;
            swordTrans.rotation = currentSwordRot;

            foreach (var traj in swordAnim.swordTrajectory)
            {
                if (sword == null) yield break;

                // 【修改】目标旋转：全程四元数计算
                Vector3 targetHandlePos = transform.TransformPoint(traj.targeSwordHandlelPos + swordPositionOffset);
                Quaternion targetRotWithOffset = Quaternion.Euler(swordRotationOffset) * traj.TargetSwordRotation;
                Quaternion targetSwordRot = transform.rotation * targetRotWithOffset;

                Quaternion startSwordRot = currentSwordRot;
                Vector3 startHandlePos = currentHandlePos;
                float duration = swordAnim.swordAnimationCostTime * (traj.timeWeight / totalWeight);
                float time = 0;

                while (time < duration)
                {
                    if (sword == null) yield break;

                    time += Time.deltaTime;
                    float t = Mathf.Clamp01(time / duration);
                    currentHandlePos = Vector3.Lerp(startHandlePos, targetHandlePos, t);
                    // 【修改】用 SlerpUnclamped 实现更顺滑的球面插值
                    currentSwordRot = Quaternion.SlerpUnclamped(startSwordRot, targetSwordRot, t);

                    swordTrans.position = currentHandlePos;
                    swordTrans.rotation = currentSwordRot; // 直接赋值四元数

                    yield return null;
                }

                currentHandlePos = targetHandlePos;
                currentSwordRot = targetSwordRot;
            }

            if (sword != null) Destroy(sword);
            sword = null;
            _flyingSwordCoroutine = null;
        }
        // 外部结束御剑时的统一清理入口
        public void EndFlyingSword()
        {
            // 停止运行中的协程
            if (_flyingSwordCoroutine != null)
            {
                StopCoroutine(_flyingSwordCoroutine);
                _flyingSwordCoroutine = null;
            }
            // 销毁剑对象
            if (sword != null)
            {
                Destroy(sword);
                sword = null;
            }

            StopJinxiSpecialAirSword();
        }

        // 尝试播放今汐专用御空御剑；只有匹配到阶段配置时才会接管默认逻辑
        private bool TryPlayJinxiSpecialAirSword(AttackStep step)
        {
            if (jinxiSpecialAirSword == null || !jinxiSpecialAirSword.enable || step == null)
            {
                return false;
            }

            JinxiSpecialAirSwordStageConfig stageConfig = GetJinxiSpecialAirSwordStageConfig(step);
            if (stageConfig == null || stageConfig.swordPrefab == null)
            {
                return false;
            }

            StopJinxiSpecialAirSword();

            if (_flyingSwordCoroutine != null)
            {
                StopCoroutine(_flyingSwordCoroutine);
                _flyingSwordCoroutine = null;
            }

            if (sword != null)
            {
                Destroy(sword);
                sword = null;
            }

            _jinxiSpecialSwordCoroutine = StartCoroutine(PlayJinxiSpecialAirSwordCoroutine(step, stageConfig));
            return true;
        }

        // 根据当前攻击段映射到今汐专用御剑的第几段配置
        private JinxiSpecialAirSwordStageConfig GetJinxiSpecialAirSwordStageConfig(AttackStep step)
        {
            if (step == null) return null;

            if ((SkillAttackSteps != null && SkillAttackSteps.Count > 0 && step == SkillAttackSteps[0]) ||
                (SkillAirAttackSteps != null && SkillAirAttackSteps.Count > 0 && step == SkillAirAttackSteps[0]))
            {
                return jinxiSpecialAirSword.firstStage;
            }

            if ((SkillAttackSteps != null && SkillAttackSteps.Count > 1 && step == SkillAttackSteps[1]) ||
                (SkillAirAttackSteps != null && SkillAirAttackSteps.Count > 1 && step == SkillAirAttackSteps[1]))
            {
                return jinxiSpecialAirSword.secondStage;
            }

            if ((SkillAttackSteps != null && SkillAttackSteps.Count > 2 && step == SkillAttackSteps[2]) ||
                (SkillAirAttackSteps != null && SkillAirAttackSteps.Count > 2 && step == SkillAirAttackSteps[2]))
            {
                return jinxiSpecialAirSword.thirdStage;
            }

            return null;
        }

        // 今汐专用御剑协程：按预设路径批量生成飞剑并同步推进
        private IEnumerator PlayJinxiSpecialAirSwordCoroutine(AttackStep step, JinxiSpecialAirSwordStageConfig stageConfig)
        {
            if (stageConfig.startDelay > 0f)
            {
                yield return new WaitForSeconds(stageConfig.startDelay);
            }

            // 固定起始参考点，保证整段攻击期间的飞剑路径稳定
            Vector3 referencePosition = transform.position;
            Quaternion referenceRotation = transform.rotation;
            List<SpecialSwordMotion> motions = BuildJinxiSpecialSwordMotions(step, stageConfig, referencePosition, referenceRotation);

            if (motions.Count == 0)
            {
                _jinxiSpecialSwordCoroutine = null;
                yield break;
            }

            Quaternion rotationOffset = Quaternion.Euler(stageConfig.rotationOffsetEuler);

            // 先批量生成所有飞剑实例，后续统一做插值推进
            foreach (SpecialSwordMotion motion in motions)
            {
                GameObject spawnedSword = Instantiate(stageConfig.swordPrefab);
                if (spawnedSword == null) continue;

                spawnedSword.transform.position = motion.startPosition;
                spawnedSword.transform.rotation = Quaternion.LookRotation(motion.endPosition - motion.startPosition, Vector3.up) * rotationOffset;
                spawnedSword.transform.localScale = Vector3.Scale(stageConfig.swordPrefab.transform.localScale, stageConfig.scaleMultiplier);
                _jinxiSpecialSwords.Add(spawnedSword);
            }

            float duration = Mathf.Max(0.01f, stageConfig.flightDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                for (int i = 0; i < motions.Count && i < _jinxiSpecialSwords.Count; i++)
                {
                    GameObject spawnedSword = _jinxiSpecialSwords[i];
                    if (spawnedSword == null) continue;

                    SpecialSwordMotion motion = motions[i];
                    spawnedSword.transform.position = Vector3.Lerp(motion.startPosition, motion.endPosition, t);
                    Vector3 direction = motion.endPosition - motion.startPosition;
                    if (direction.sqrMagnitude > 0.0001f)
                    {
                        spawnedSword.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up) * rotationOffset;
                    }
                }

                yield return null;
            }

            if (stageConfig.destroyDelay > 0f)
            {
                yield return new WaitForSeconds(stageConfig.destroyDelay);
            }

            DestroyAllJinxiSpecialSwords();
            _jinxiSpecialSwordCoroutine = null;
        }

        // 生成今汐专用飞剑的起点/终点路径数据
        private List<SpecialSwordMotion> BuildJinxiSpecialSwordMotions(AttackStep step, JinxiSpecialAirSwordStageConfig stageConfig, Vector3 referencePosition, Quaternion referenceRotation)
        {
            List<SpecialSwordMotion> motions = new List<SpecialSwordMotion>();
            if (step == null || stageConfig == null) return motions;

            bool isFirstStage =
                (SkillAttackSteps != null && SkillAttackSteps.Count > 0 && step == SkillAttackSteps[0]) ||
                (SkillAirAttackSteps != null && SkillAirAttackSteps.Count > 0 && step == SkillAirAttackSteps[0]);
            bool isThirdStage =
                (SkillAttackSteps != null && SkillAttackSteps.Count > 2 && step == SkillAttackSteps[2]) ||
                (SkillAirAttackSteps != null && SkillAirAttackSteps.Count > 2 && step == SkillAirAttackSteps[2]);

            if (isFirstStage || isThirdStage)
            {
                Vector3 start = GetJinxiCubeWorldPosition('c', 6, referencePosition, referenceRotation) + Vector3.up * jinxiSpecialAirSword.firstStageExtraHeight;
                Vector3 end = GetJinxiCubeWorldPosition('b', 5, referencePosition, referenceRotation);
                motions.Add(new SpecialSwordMotion(start, end));
                return motions;
            }

            motions.Add(new SpecialSwordMotion(GetJinxiCubeWorldPosition('b', 1, referencePosition, referenceRotation), GetJinxiCubeWorldPosition('b', 9, referencePosition, referenceRotation)));
            motions.Add(new SpecialSwordMotion(GetJinxiCubeWorldPosition('b', 9, referencePosition, referenceRotation), GetJinxiCubeWorldPosition('b', 1, referencePosition, referenceRotation)));
            motions.Add(new SpecialSwordMotion(GetJinxiCubeWorldPosition('b', 3, referencePosition, referenceRotation), GetJinxiCubeWorldPosition('b', 7, referencePosition, referenceRotation)));
            motions.Add(new SpecialSwordMotion(GetJinxiCubeWorldPosition('b', 7, referencePosition, referenceRotation), GetJinxiCubeWorldPosition('b', 3, referencePosition, referenceRotation)));

            return motions;
        }

        // 将“层级 + 九宫格索引”转换为世界坐标，供今汐专用御剑使用
        private Vector3 GetJinxiCubeWorldPosition(char layer, int gridIndex, Vector3 referencePosition, Quaternion referenceRotation)
        {
            Vector3 baseWorldPosition = referencePosition
                + referenceRotation * new Vector3(jinxiSpecialAirSword.anchorOffset.x, 0f, jinxiSpecialAirSword.anchorOffset.z)
                + Vector3.up * jinxiSpecialAirSword.anchorOffset.y;

            float layerHeight = GetJinxiLayerHeight(layer);
            float forwardDistance = GetJinxiRowDistance(gridIndex);
            float horizontalOffset = GetJinxiColumnOffset(gridIndex);

            Vector3 worldPosition = baseWorldPosition;
            worldPosition += Vector3.up * layerHeight;
            worldPosition += referenceRotation * Vector3.forward * forwardDistance;
            worldPosition += referenceRotation * Vector3.right * horizontalOffset;
            return worldPosition;
        }

        // 读取 a / b / c 三层的高度配置
        private float GetJinxiLayerHeight(char layer)
        {
            switch (char.ToLowerInvariant(layer))
            {
                case 'a':
                    return jinxiSpecialAirSword.layerAHeight;
                case 'b':
                    return jinxiSpecialAirSword.layerBHeight;
                case 'c':
                    return jinxiSpecialAirSword.layerCHeight;
                default:
                    return jinxiSpecialAirSword.layerBHeight;
            }
        }

        // 按九宫格索引映射前后排距离
        private float GetJinxiRowDistance(int gridIndex)
        {
            switch (gridIndex)
            {
                case 1:
                case 2:
                case 3:
                    return jinxiSpecialAirSword.farRowDistance;
                case 4:
                case 5:
                case 6:
                    return jinxiSpecialAirSword.middleRowDistance;
                case 7:
                case 8:
                case 9:
                    return jinxiSpecialAirSword.nearRowDistance;
                default:
                    return jinxiSpecialAirSword.middleRowDistance;
            }
        }

        // 按九宫格索引映射左右列偏移
        private float GetJinxiColumnOffset(int gridIndex)
        {
            switch (gridIndex)
            {
                case 1:
                case 4:
                case 7:
                    return -jinxiSpecialAirSword.horizontalOffset;
                case 2:
                case 5:
                case 8:
                    return 0f;
                case 3:
                case 6:
                case 9:
                    return jinxiSpecialAirSword.horizontalOffset;
                default:
                    return 0f;
            }
        }

        // 停止今汐专用御剑协程并回收所有飞剑实例
        private void StopJinxiSpecialAirSword()
        {
            if (_jinxiSpecialSwordCoroutine != null)
            {
                StopCoroutine(_jinxiSpecialSwordCoroutine);
                _jinxiSpecialSwordCoroutine = null;
            }

            DestroyAllJinxiSpecialSwords();
        }

        // 销毁今汐专用御剑创建的所有临时飞剑对象
        private void DestroyAllJinxiSpecialSwords()
        {
            if (_jinxiSpecialSwords.Count == 0) return;

            foreach (GameObject specialSword in _jinxiSpecialSwords)
            {
                if (specialSword != null)
                {
                    Destroy(specialSword);
                }
            }

            _jinxiSpecialSwords.Clear();
        }
        // 今汐专用飞剑单次运动的起点和终点数据
        private readonly struct SpecialSwordMotion
        {
            public readonly Vector3 startPosition;
            public readonly Vector3 endPosition;

            public SpecialSwordMotion(Vector3 startPosition, Vector3 endPosition)
            {
                this.startPosition = startPosition;
                this.endPosition = endPosition;
            }
        }
        #endregion

        #region 御龙动画
        /// <summary>
        /// 龙相关动画控制模块
        /// 功能：根据攻击步骤触发龙的显示/隐藏、播放对应动画、自动根据动画时长隐藏龙
        /// </summary>
        [Header("===角配置===")]
        [Header("龙对象")]
        // 龙的游戏对象（外部拖拽赋值）
        public GameObject dragon;
        [Header("龙位置偏移（相对角色本地坐标）")]
        [SerializeField] private Vector3 defaultDragonLocalOffset = Vector3.zero;
        [SerializeField] private Vector3 eSkillStep3DragonLocalOffset = new Vector3(0f, 0f, 0f);
        [SerializeField] private Vector3 SkillAttackSteps3DragonLocalOffset = new Vector3(0f, 0f, 3f);
        [SerializeField] private Vector3 SkillAttackSteps4DragonLocalOffset = new Vector3(0f, 0f, 3f);

        // 龙的动画控制器（代码中隐含变量，用于播放动画）
        public Animator dragonanimator;
        // 龙隐藏的协程引用（用于复用/停止，避免协程重复执行）
        private Coroutine _dragonHideCoroutine;
        // 当前龙跟随角色时使用的本地偏移
        private Vector3 _currentDragonLocalOffset = Vector3.zero;

        //检查当前攻击步骤，触发对应的龙动画
        public void CheckDragonAnimation(AttackStep step)
        {
            // 普通攻击第3步：延迟0.2秒显示龙
            if (step == attackSteps[2])
            {
                ApplyDragonOffset(step);
                StartCoroutine(DelayShowDragon(0.2f, 0f, step));
                return;
            }
            // Q爆发攻击第1步：无延迟显示龙
            if (step == QBurstAttackSteps[0])
            {
                ApplyDragonOffset(step);
                StartCoroutine(DelayShowDragon(0f, 0f, step));
                return;
            }
            // E技能攻击第3步：无延迟显示龙
            if (step == ESkillAttackSteps[2])
            {
                ApplyDragonOffset(step);
                StartCoroutine(DelayShowDragon(0.7f, 0f, step));
                return;
            }
            // E技能攻击第4步：无延迟显示龙
            if (step == ESkillAttackSteps[3])
            {
                ApplyDragonOffset(step);
                StartCoroutine(DelayShowDragon(0f, 1f, step));
                return;
            }
            // 地面技能/空中技能 第3步：无延迟显示龙
            if (step == SkillAttackSteps[2] || step == SkillAirAttackSteps[2])
            {
                ApplyDragonOffset(step);
                StartCoroutine(DelayShowDragon(0.2f, 0f, step));
                return;
            }
            // 地面技能/空中技能 第4步：无延迟显示龙
            if (step == SkillAttackSteps[3] || step == SkillAirAttackSteps[3])
            {
                ApplyDragonOffset(step);
                StartCoroutine(DelayShowDragon(0f, 0.3f, step));
                return;
            }
        }

        // 根据当前攻击步骤设置龙的本地偏移
        private void ApplyDragonOffset(AttackStep step)
        {
            _currentDragonLocalOffset = defaultDragonLocalOffset;

            if (step == ESkillAttackSteps[2])
            {
                _currentDragonLocalOffset = eSkillStep3DragonLocalOffset;
            }
            if (step == SkillAttackSteps[2] || step == SkillAirAttackSteps[2])
            {
                _currentDragonLocalOffset = SkillAttackSteps3DragonLocalOffset;
            }
            if (step == SkillAttackSteps[3] || step == SkillAirAttackSteps[3])
            {
                _currentDragonLocalOffset = SkillAttackSteps4DragonLocalOffset;
            }
        }

        // 每帧同步龙的位置，特殊招式可稳定保持在角色前方
        private void UpdateDragonPosition()
        {
            if (dragon == null) return;
            dragon.transform.position = transform.TransformPoint(_currentDragonLocalOffset);
        }

       // 延迟显示龙并播放对应攻击动画；time1 为显示延迟，time2 为相对原动画时长的提前隐藏量
        private IEnumerator DelayShowDragon(float time1, float time2, AttackStep step)
        {
            // 等待指定延迟时间
            yield return new WaitForSeconds(time1);
            // 立即显示龙
            ShowdragonInstantly();
            // 播放对应攻击动画（动画控制器不为空时执行）
            if (dragonanimator != null)
            {
                dragonanimator.SetTrigger(step.attackAnimation.attackAnimationName);
            }
            // 启动龙的自动隐藏逻辑（根据动画时长，并可额外提前隐藏）
            StartDragonAutoHide(step, time2);
        }

        // 兼容旧逻辑：未传提前隐藏量时，按完整动画时长隐藏
        private IEnumerator DelayShowDragon(float time, AttackStep step)
        {
            yield return DelayShowDragon(time, 0f, step);
        }

        // 启动龙的自动隐藏协程：停止旧协程后，按龙 Animator 实际播放的动画时长重新计时
        private void StartDragonAutoHide(AttackStep step, float earlyHideOffset = 0f)
        {
            // 如果已有隐藏协程，先停止（避免重复隐藏）
            if (_dragonHideCoroutine != null)
            {
                StopCoroutine(_dragonHideCoroutine);
            }

            // 启动延迟隐藏协程
            _dragonHideCoroutine = StartCoroutine(DelayHideDragon(step, earlyHideOffset));
        }

        // 获取龙的动画时长
        //优先级：龙 Animator 当前/下一状态的实际剪辑 > 控制器中同名剪辑 > 攻击步骤预设时长
        private float GetDragonAnimationLength(AttackStep step)
        {
            // 优先使用龙的动画控制器，否则使用角色主动画控制器
            Animator targetAnimator = dragonanimator != null ? dragonanimator : animator;
            // 无动画控制器时，返回攻击步骤预设时间
            if (targetAnimator == null)
            {
                return step != null ? step.attackAnimation.executionAttackCostTime : 0f;
            }

            // Trigger 和 Clip 名不一定一致，优先读取 Animator 当前已播放的剪辑
            AnimatorClipInfo[] currentClips = targetAnimator.GetCurrentAnimatorClipInfo(0);
            if (currentClips != null && currentClips.Length > 0 && currentClips[0].clip != null)
            {
                return currentClips[0].clip.length;
            }

            // 过渡中时，下一状态的剪辑信息可能已可用
            AnimatorClipInfo[] nextClips = targetAnimator.GetNextAnimatorClipInfo(0);
            if (nextClips != null && nextClips.Length > 0 && nextClips[0].clip != null)
            {
                return nextClips[0].clip.length;
            }

            // 还拿不到实际播放信息时，再退回到控制器里的同名剪辑查找
            if (targetAnimator.runtimeAnimatorController == null)
            {
                return step != null ? step.attackAnimation.executionAttackCostTime : 0f;
            }

            // 查找动画控制器中对应名称的动画剪辑
            AnimationClip clip = targetAnimator.runtimeAnimatorController.animationClips
                .FirstOrDefault(currentClip => step != null && currentClip.name == step.attackAnimation.attackAnimationName);

            // 找到动画剪辑，返回剪辑时长
            if (clip != null)
            {
                return clip.length;
            }

            // 未找到动画剪辑，返回攻击步骤预设时间
            return step != null ? step.attackAnimation.executionAttackCostTime : 0f;
        }

       // 延迟指定时间后隐藏龙；earlyHideOffset 表示在原动画时长基础上提前多久隐藏
        private IEnumerator DelayHideDragon(AttackStep step, float earlyHideOffset = 0f)
        {
            // 等一帧，让 Trigger 真正驱动到龙 Animator 的目标状态
            yield return null;

            float time = GetDragonAnimationLength(step) - earlyHideOffset;

            // 等待时长（保底0秒，避免负数）
            yield return new WaitForSeconds(Mathf.Max(0f, time));
            // 立即隐藏龙
            HidedragonInstantly();
        }

        // 立即隐藏龙（公开方法，可外部调用）
        public void HidedragonInstantly()
        {
            // 停止并清空隐藏协程
            if (_dragonHideCoroutine != null)
            {
                StopCoroutine(_dragonHideCoroutine);
                _dragonHideCoroutine = null;
            }

            // 龙对象为空，直接返回
            if (dragon == null) return;
            _currentDragonLocalOffset = defaultDragonLocalOffset;
            // 失活龙对象（隐藏）
            dragon.SetActive(false);
        }

        // 立即显现龙（公开方法，可外部调用）
        public void ShowdragonInstantly()
        {
            // 龙对象为空，直接返回
            if (dragon == null) return;
            // 激活龙对象（显示）
            dragon.SetActive(true);
        }
        #endregion

        #region 技能特效
        // 根据攻击段映射对应的粒子特效
        public void CheckEffect(AttackStep step)
        {
            if (step == attackSteps[0])
            {
                EffectService.Play("Attack01", this.transform);
                return;
            }
            if (step == attackSteps[3])
            {
                EffectService.Play("Attack04", this.transform);
                return;
            }
            if (step == fallAttackSteps[2])
            {
                EffectService.Play("FallAttackingEnd", this.transform);
                return;
            }
            if (step == ESkillAttackSteps[0])
            {
                EffectService.Play("Eskill1", this.transform);
                return;
            }
            if (step == QBurstAttackSteps[0])
            {
                EffectService.Play("QBurst1", this.transform);
                EffectService.Play("QBurst2", this.transform);
                return;
            }
        }
        #endregion



    }
}

