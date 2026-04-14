using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace WutheringWaves
{
    /// <summary>
    /// 魂类游戏 角色技能UI控制器
    /// 功能：负责管理角色技能、Q爆发的全部UI显示逻辑
    /// 包含：技能图标切换、冷却遮罩、冷却数字、按键提示、可用/禁用状态颜色、锁定/解锁状态
    /// 依赖：CharacterCore（角色核心）、CharacterAttack（攻击/技能逻辑）
    /// </summary>
    [DisallowMultipleComponent] // 禁止同一物体挂载多个该组件
    public class CharacterSkillUI : MonoBehaviour
    {
        #region 角色绑定相关
        [Header("=== 角色绑定设置 ===")]
        [Tooltip("手动绑定的角色核心脚本，优先级最高，手动指定后不会自动查找")]
        [SerializeField] private CharacterCore targetCore;

        [Tooltip("是否从当前物体上的UIRoot自动获取角色核心")]
        [FormerlySerializedAs("autoBindFromUIManager")]
        [SerializeField] private bool autoBindFromUIRoot = true;

        [Tooltip("兜底方案：无任何绑定时，自动在全场景查找CharacterCore")]
        [SerializeField] private bool autoFindCharacterCore = true;

        // 内部私有引用（不显示在Inspector面板）
        private UIRoot _uiRoot;               // UI总管引用
        private CharacterAttack _attackLogic;  // 角色攻击/技能逻辑引用
        private CharacterStamina _staminaLogic; // 角色体力逻辑引用
        private bool _hasSubscribedAttackEvent;// 标记是否已订阅全局技能UI事件，防止重复订阅
        private bool _hasSubscribedStaminaEvent;// 标记是否已订阅全局体力事件，防止重复订阅
        #endregion

        #region UI元素引用
        [Header("=======================================")]
        [Header("=== 头像UI设置 ===")]
        [SerializeField] private Image avatarIcon; // 角色头像图片

        [Header("=======================================")]
        [Header("=== 通用技能UI - 基础按钮 ===")]
        [SerializeField] private Image skillButtonFrame;    // 技能按钮边框
        [SerializeField] private TextMeshProUGUI skillKeyText; // 技能按键文字（如E）

        [Header("=== 通用技能UI - 冷却显示 ===")]
        [SerializeField] private Image skillCooldownMask;     // 技能冷却环形遮罩
        [SerializeField] private TextMeshProUGUI skillCooldownText; // 技能冷却数字

        [Header("=== 通用技能UI - 技能图标 ===")]
        [SerializeField] private Image skill1Icon; // 技能1图标
        [SerializeField] private Image skill2Icon; // 技能2图标
        [SerializeField] private Image skill3Icon; // 技能3图标
        [SerializeField] private Image skill4Icon; // 技能4图标

        [Header("=======================================")]
        [Header("=== Q爆发技能UI - 基础按钮 ===")]
        [SerializeField] private Image qBurstButtonFrame;    // Q爆发按钮边框
        [SerializeField] private TextMeshProUGUI qBurstKeyText; // Q爆发按键文字（如Q）

        [Header("=== Q爆发技能UI - 冷却显示 ===")]
        [SerializeField] private Image qBurstCooldownMask;     // Q爆发冷却环形遮罩
        [SerializeField] private TextMeshProUGUI qBurstCooldownText; // Q爆发冷却数字

        [Header("=== Q爆发技能UI - 状态图标 ===")]
        [SerializeField] private Image qBurstAvailableIcon; // Q爆发可用图标
        [SerializeField] private Image qBurstLockedIcon;    // Q爆发锁定/不可用图标

        [Header("=======================================")]
        [Header("=== 体力槽UI ===")]
        [SerializeField] private RectTransform staminaRoot;
        [SerializeField] private Image staminaRingBg;
        [SerializeField] private Image staminaRingFill;
        [SerializeField] private Image staminaRingGlow;
        [SerializeField] private CanvasGroup staminaCanvasGroup;
        #endregion

        #region 显示设置
        [Header("=======================================")]
        [Header("=== UI显示配置 ===")]
        [Tooltip("技能触发按键的显示名称")]
        [SerializeField] private string skillKeyName = "E";
        [Tooltip("Q爆发触发按键的显示名称")]
        [SerializeField] private string qBurstKeyName = "Q";
        [Tooltip("技能可用状态的UI颜色")]
        [SerializeField] private Color normalColor = Color.white;
        [Tooltip("技能冷却/禁用状态的UI颜色")]
        [SerializeField] private Color disableColor = new Color(0.45f, 0.45f, 0.45f, 1f);
        [Tooltip("冷却开始时图标的最低透明度")]
        [SerializeField] [Range(0f, 1f)] private float cooldownMinAlpha = 0.3f;
        [Header("=== 体力槽显示配置 ===")]
        [SerializeField] private Camera uiFollowCamera;
        [SerializeField] private Vector3 staminaWorldOffset = Vector3.zero;
        [SerializeField] private Vector2 staminaScreenOffset = new Vector2(40f, 0f);
        [SerializeField] [Min(0f)] private float staminaFollowSmooth = 18f;
        [SerializeField] [Range(0f, 1f)] private float staminaHiddenAlpha = 0f;
        [SerializeField] [Min(0f)] private float staminaFadeSpeed = 8f;
        [SerializeField] [Range(0f, 1f)] private float staminaGlowMinAlpha = 0.15f;
        [SerializeField] [Range(0f, 1f)] private float staminaGlowMaxAlpha = 0.85f;
        #endregion

        #region Unity生命周期函数
        /// <summary>
        /// 唤醒：组件创建时执行
        /// 作用：获取UIRoot，初始化静态不变的UI元素
        /// </summary>
        private void Awake()
        {
            _uiRoot = GetComponentInParent<UIRoot>();
            InitializeStaticUI();
        }

        public void InjectDependencies(UIRoot root, CharacterCore core)
        {
            if (root != null)
            {
                _uiRoot = root;
            }

            if (core != null)
            {
                targetCore = core;
            }

            TryBindCharacterUI();
            RefreshUI();
        }

        public void SetTargetCore(CharacterCore core)
        {
            targetCore = core;
            TryBindCharacterUI();
            RefreshUI();
        }

        /// <summary>
        /// 启用：物体激活/组件启用时执行
        /// 作用：尝试绑定技能逻辑，刷新一次UI
        /// </summary>
        private void OnEnable()
        {
            TryBindCharacterUI();
            RefreshUI();
        }

        /// <summary>
        /// 开始：游戏启动第一帧执行
        /// 作用：最终确认绑定，刷新UI
        /// </summary>
        private void Start()
        {
            TryBindCharacterUI();
            RefreshUI();
        }

        /// <summary>
        /// 每帧更新
        /// 作用：动态检测技能逻辑是否丢失，丢失则重新绑定
        /// </summary>
        private void Update()
        {
            // 攻击逻辑为空时，重新尝试绑定，绑定成功后立即刷新UI
            if ((_attackLogic == null || _staminaLogic == null) && TryBindCharacterUI())
            {
                RefreshUI();
            }

            UpdateStaminaFollow();
            UpdateStaminaFade();
        }

        /// <summary>
        /// 禁用：物体失活/组件禁用时执行
        /// 作用：取消事件订阅，避免报错
        /// </summary>
        private void OnDisable()
        {
            UnsubscribeAttackEvent();
            UnsubscribeStaminaEvent();
        }

        /// <summary>
        /// 销毁：组件销毁时执行
        /// 作用：彻底取消订阅，防止内存泄漏
        /// </summary>
        private void OnDestroy()
        {
            UnsubscribeAttackEvent();
            UnsubscribeStaminaEvent();
        }
        #endregion

        #region 初始化逻辑
        /// <summary>
        /// 初始化静态UI（游戏运行中不会改变的UI）
        /// 包括：按键文字、冷却遮罩格式、头像颜色
        /// </summary>
        private void InitializeStaticUI()
        {
            // 设置按键显示文字
            SetKeyText(skillKeyText, skillKeyName);
            SetKeyText(qBurstKeyText, qBurstKeyName);

            // 如果你没有配置遮罩图片，这两个字段可以留空
            InitializeMask(skillCooldownMask);
            InitializeMask(qBurstCooldownMask);

            // 设置头像为正常颜色
            if (avatarIcon != null)
            {
                avatarIcon.color = normalColor;
            }

            InitializeStaminaUI();
        }

        /// <summary>
        /// 按优先级尝试绑定角色攻击逻辑
        /// 优先级：手动绑定targetCore → UIRoot获取 → 全场景自动查找
        /// </summary>
        /// <returns>绑定成功返回true，失败返回false</returns>
        private bool TryBindCharacterUI()
        {
            CharacterCore nextCore = targetCore;

            // 方案1：从UIRoot获取角色核心
            if (nextCore == null && autoBindFromUIRoot && _uiRoot != null)
            {
                nextCore = _uiRoot.Core;
            }

            // 方案2：全场景自动查找角色核心
            if (nextCore == null && autoFindCharacterCore)
            {
                nextCore = FindObjectOfType<CharacterCore>();
            }

            // 无可用角色核心/攻击逻辑，绑定失败
            if (nextCore == null || nextCore.attackLogic == null)
            {
                return false;
            }

            // 保存当前绑定的角色核心
            targetCore = nextCore;

            CharacterAttack nextAttack = nextCore.attackLogic;
            CharacterStamina nextStamina = nextCore.GetCharacterStamina();

            // 角色逻辑未变化，只补充订阅
            if (_attackLogic == nextAttack && _staminaLogic == nextStamina)
            {
                SubscribeAttackEvent();
                SubscribeStaminaEvent();
                return true;
            }

            // 角色逻辑发生变化：先取消旧事件，再赋值新逻辑，最后订阅新事件
            UnsubscribeAttackEvent();
            UnsubscribeStaminaEvent();
            _attackLogic = nextAttack;
            _staminaLogic = nextStamina;
            SubscribeAttackEvent();
            SubscribeStaminaEvent();
            return true;
        }

        /// <summary>
        /// 订阅攻击逻辑的UI更新事件
        /// 作用：技能状态变化时自动刷新UI，无需每帧主动调用
        /// </summary>
        private void SubscribeAttackEvent()
        {
            // 空引用/已订阅则直接返回
            if (_attackLogic == null || _hasSubscribedAttackEvent) return;

            GameEvents.OnSkillUIStateChanged += HandleSkillUIStateChanged;
            _hasSubscribedAttackEvent = true;
        }

        private void SubscribeStaminaEvent()
        {
            if (_staminaLogic == null || _hasSubscribedStaminaEvent) return;

            GameEvents.OnStaminaChanged += HandleStaminaChangedFromEventBus;
            GameEvents.OnStaminaVisibilityChanged += HandleStaminaVisibilityChangedFromEventBus;
            _hasSubscribedStaminaEvent = true;
            _staminaLogic.ForceRefreshEvents();
        }

        /// <summary>
        /// 取消订阅攻击逻辑事件
        /// 作用：防止物体销毁后仍触发事件导致空引用报错
        /// </summary>
        private void UnsubscribeAttackEvent()
        {
            if (!_hasSubscribedAttackEvent) return;

            GameEvents.OnSkillUIStateChanged -= HandleSkillUIStateChanged;
            _hasSubscribedAttackEvent = false;
        }

        private void UnsubscribeStaminaEvent()
        {
            if (!_hasSubscribedStaminaEvent) return;

            GameEvents.OnStaminaChanged -= HandleStaminaChangedFromEventBus;
            GameEvents.OnStaminaVisibilityChanged -= HandleStaminaVisibilityChangedFromEventBus;
            _hasSubscribedStaminaEvent = false;
        }
        #endregion

        #region UI刷新逻辑
        /// <summary>
        /// UI总刷新入口
        /// 作用：统一调度所有技能UI的更新
        /// </summary>
        private void RefreshUI()
        {
            // 强制刷新按键文本（防止配置修改后不生效）
            SetKeyText(skillKeyText, skillKeyName);
            SetKeyText(qBurstKeyText, qBurstKeyName);

            // 未绑定攻击逻辑：显示全禁用UI
            if (_attackLogic == null)
            {
                RefreshUnboundUI();
                return;
            }

            // 已绑定：分别刷新普通技能 + Q爆发技能UI
            RefreshSkillUI();
            RefreshQBurstUI();
        }

        private void InitializeStaminaUI()
        {
            InitializeStaminaRing(staminaRingFill);
            InitializeStaminaRing(staminaRingGlow);

            if (staminaRingBg != null)
            {
                Color bgColor = staminaRingBg.color;
                bgColor.a = Mathf.Clamp01(bgColor.a);
                staminaRingBg.color = bgColor;
            }

            if (staminaCanvasGroup != null)
            {
                staminaCanvasGroup.alpha = staminaHiddenAlpha;
            }

            ApplyStaminaFill(1f);
        }

        private void InitializeStaminaRing(Image targetRing)
        {
            if (targetRing == null) return;

            targetRing.type = Image.Type.Filled;
            targetRing.fillMethod = Image.FillMethod.Radial360;
            targetRing.fillOrigin = (int)Image.Origin360.Top;
            targetRing.fillClockwise = false;
            targetRing.fillAmount = 1f;
        }

        private void HandleStaminaChanged(float current, float max, float normalized)
        {
            ApplyStaminaFill(normalized);
        }

        private void HandleSkillUIStateChanged(CharacterAttack source)
        {
            if (source == _attackLogic)
            {
                RefreshUI();
            }
        }

        private void HandleStaminaChangedFromEventBus(CharacterStamina source, float current, float max, float normalized)
        {
            if (source == _staminaLogic)
            {
                HandleStaminaChanged(current, max, normalized);
            }
        }

        private void HandleStaminaVisibilityChangedFromEventBus(CharacterStamina source, bool visible)
        {
            if (source == _staminaLogic)
            {
                HandleStaminaVisibilityChanged(visible);
            }
        }

        private void HandleStaminaVisibilityChanged(bool visible)
        {
            if (staminaRoot != null && visible && !staminaRoot.gameObject.activeSelf)
            {
                staminaRoot.gameObject.SetActive(true);
            }
        }

        private void ApplyStaminaFill(float normalized)
        {
            float clamped = Mathf.Clamp01(normalized);

            if (staminaRingFill != null)
            {
                staminaRingFill.fillAmount = clamped;
            }

            if (staminaRingGlow != null)
            {
                staminaRingGlow.fillAmount = clamped;
                Color glowColor = staminaRingGlow.color;
                glowColor.a = Mathf.Lerp(staminaGlowMinAlpha, staminaGlowMaxAlpha, clamped);
                staminaRingGlow.color = glowColor;
            }
        }

        private void UpdateStaminaFollow()
        {
            if (staminaRoot == null || targetCore == null)
            {
                return;
            }

            Transform staminaAnchor = targetCore.transform.Find("StaminaAnchor");
            Camera followCamera = ResolveFollowCamera();
            if (staminaAnchor == null || followCamera == null)
            {
                return;
            }

            Vector3 screenPosition = followCamera.WorldToScreenPoint(staminaAnchor.position + staminaWorldOffset);
            if (screenPosition.z <= 0f)
            {
                if (staminaCanvasGroup != null)
                {
                    staminaCanvasGroup.alpha = staminaHiddenAlpha;
                }
                return;
            }

            Vector3 targetPosition = screenPosition + (Vector3)staminaScreenOffset;
            float followLerp = 1f - Mathf.Exp(-staminaFollowSmooth * Time.deltaTime);
            staminaRoot.position = Vector3.Lerp(staminaRoot.position, targetPosition, followLerp);
        }

        private void UpdateStaminaFade()
        {
            if (staminaCanvasGroup == null)
            {
                return;
            }

            float targetAlpha = (_staminaLogic != null && _staminaLogic.IsVisible) ? 1f : staminaHiddenAlpha;
            staminaCanvasGroup.alpha = Mathf.MoveTowards(
                staminaCanvasGroup.alpha,
                targetAlpha,
                staminaFadeSpeed * Time.deltaTime);
        }

        private Camera ResolveFollowCamera()
        {
            if (uiFollowCamera != null)
            {
                return uiFollowCamera;
            }

            if (targetCore != null && targetCore.cameraLogic != null && targetCore.cameraLogic._mainCamera != null)
            {
                return targetCore.cameraLogic._mainCamera.GetComponent<Camera>();
            }

            return Camera.main;
        }

        /// <summary>
        /// 未绑定角色时的UI显示（全部灰色禁用状态）
        /// </summary>
        private void RefreshUnboundUI()
        {
            // 默认显示技能1图标
            SetActiveSkillIcon(1);

            // 普通技能全禁用
            ApplyCooldownMask(skillCooldownMask, 0f, 1f);
            SetCooldownText(skillCooldownText, 0f);
            SetGraphicColor(skillButtonFrame, disableColor);
            SetGraphicColor(skillKeyText, disableColor);
            SetSkillIconVisual(GetCurrentSkillIcon(1), disableColor, 0f, 1f);

            // Q爆发显示锁定状态
            SetImageActive(qBurstAvailableIcon, false);
            SetImageActive(qBurstLockedIcon, true);
            ApplyCooldownMask(qBurstCooldownMask, 0f, 1f);
            SetCooldownText(qBurstCooldownText, 0f);
            SetGraphicColor(qBurstButtonFrame, disableColor);
            SetGraphicColor(qBurstKeyText, disableColor);
            SetSkillIconVisual(qBurstLockedIcon, disableColor, 0f, 1f);
        }

        /// <summary>
        /// 刷新普通E技能UI
        /// 包括：当前技能图标、冷却遮罩、冷却数字、可用颜色状态
        /// </summary>
        private void RefreshSkillUI()
        {
            // 获取当前技能数据
            int currentSkillIndex = _attackLogic.CurrentESkillUIIndex;
            Image currentSkillIcon = GetCurrentSkillIcon(currentSkillIndex);
            bool isPrimarySkillCooldown = currentSkillIndex == 1 && _attackLogic.Skill1CDTimer > 0f;
            float remainingTime = isPrimarySkillCooldown ? _attackLogic.Skill1CDTimer : 0f;
            float totalTime = isPrimarySkillCooldown ? _attackLogic.skill1CD : 0f;
            Color targetColor = isPrimarySkillCooldown ? disableColor : normalColor;

            // 执行UI更新
            SetActiveSkillIcon(currentSkillIndex);
            ApplyCooldownMask(skillCooldownMask, remainingTime, totalTime);
            SetCooldownText(skillCooldownText, remainingTime);
            SetGraphicColor(skillButtonFrame, targetColor);
            SetGraphicColor(skillKeyText, targetColor);
            SetSkillIconVisual(currentSkillIcon, targetColor, remainingTime, totalTime);
        }

        /// <summary>
        /// 刷新Q爆发技能UI
        /// 包括：锁定/可用图标切换、冷却、颜色、按键状态
        /// </summary>
        private void RefreshQBurstUI()
        {
            // 获取Q爆发状态数据
            bool isQBurstAvailable = _attackLogic.IsQBurstable();
            bool hasQBurstConfigured = _attackLogic.HasQBurstConfigured;
            bool showAvailableIcon = isQBurstAvailable && hasQBurstConfigured;
            float remainingTime = _attackLogic.QBurstCDTimer;
            Color targetColor = showAvailableIcon ? normalColor : disableColor;

            // 更新图标显隐（可用/锁定二选一）
            SetImageActive(qBurstAvailableIcon, showAvailableIcon);
            SetImageActive(qBurstLockedIcon, !showAvailableIcon);

            // 更新冷却与颜色
            ApplyCooldownMask(qBurstCooldownMask, remainingTime, _attackLogic.qBurstCD);
            SetCooldownText(qBurstCooldownText, remainingTime);
            SetGraphicColor(qBurstButtonFrame, targetColor);
            SetGraphicColor(qBurstKeyText, targetColor);

            // 单独设置状态图标颜色
            if (showAvailableIcon)
            {
                SetSkillIconVisual(qBurstAvailableIcon, normalColor, remainingTime, _attackLogic.qBurstCD);
            }
            else
            {
                SetSkillIconVisual(qBurstLockedIcon, disableColor, remainingTime, _attackLogic.qBurstCD);
            }
        }
        #endregion

        #region 工具方法
        /// <summary>
        /// 初始化冷却遮罩为360°环形填充样式
        /// 如果没有传遮罩图片，这里会直接跳过
        /// </summary>
        private void InitializeMask(Image targetMask)
        {
            if (targetMask == null) return;

            targetMask.type = Image.Type.Filled;
            targetMask.fillMethod = Image.FillMethod.Radial360;
            targetMask.fillOrigin = (int)Image.Origin360.Top;
            targetMask.fillClockwise = false;
            targetMask.fillAmount = 0f;
            targetMask.gameObject.SetActive(false);
        }

        /// <summary>
        /// 技能图标切换：只激活对应序号图标，隐藏其他图标
        /// </summary>
        private void SetActiveSkillIcon(int skillIndex)
        {
            SetImageActive(skill1Icon, skillIndex == 1);
            SetImageActive(skill2Icon, skillIndex == 2);
            SetImageActive(skill3Icon, skillIndex == 3);
            SetImageActive(skill4Icon, skillIndex == 4);
        }

        /// <summary>
        /// 根据技能序号返回对应图标Image
        /// </summary>
        private Image GetCurrentSkillIcon(int skillIndex)
        {
            switch (skillIndex)
            {
                case 4: return skill4Icon;
                case 3: return skill3Icon;
                case 2: return skill2Icon;
                default: return skill1Icon;
            }
        }

        /// <summary>
        /// 设置冷却遮罩填充进度
        /// 没有配置遮罩图片时会自动跳过，仅使用图标透明度表现CD
        /// </summary>
        private void ApplyCooldownMask(Image targetMask, float remainingTime, float totalTime)
        {
            if (targetMask == null) return;

            // 判断是否需要显示冷却遮罩
            bool showMask = remainingTime > 0f && totalTime > 0f;
            targetMask.gameObject.SetActive(showMask);

            if (!showMask)
            {
                targetMask.fillAmount = 0f;
                return;
            }

            // 设置填充比例（0~1）
            targetMask.fillAmount = Mathf.Clamp01(remainingTime / totalTime);
        }

        /// <summary>
        /// 设置冷却数字文本
        /// 剩余时间>0显示数字，否则清空隐藏
        /// </summary>
        private void SetCooldownText(TextMeshProUGUI targetText, float remainingTime)
        {
            if (targetText == null) return;

            bool showText = remainingTime > 0f;
            targetText.gameObject.SetActive(showText);

            if (!showText)
            {
                targetText.text = string.Empty;
                return;
            }

            // 格式化时间后显示
            targetText.text = FormatCooldownTime(remainingTime);
        }

        /// <summary>
        /// 冷却时间格式化工具
        /// 规则：统一显示到小数点后一位
        /// </summary>
        private string FormatCooldownTime(float timeValue)
        {
            return timeValue.ToString("0.0");
        }

        /// <summary>
        /// 设置UI按键文字（E/Q）
        /// </summary>
        private void SetKeyText(TextMeshProUGUI targetText, string keyName)
        {
            if (targetText == null) return;
            targetText.text = keyName;
        }

        /// <summary>
        /// 统一设置UI图形颜色（Image/Text都继承自Graphic）
        /// </summary>
        private void SetGraphicColor(Graphic targetGraphic, Color targetColor)
        {
            if (targetGraphic == null) return;
            targetGraphic.color = targetColor;
        }

        /// <summary>
        /// 统一设置图片激活状态
        /// </summary>
        private void SetImageActive(Image targetImage, bool active)
        {
            if (targetImage == null) return;
            if (targetImage.gameObject.activeSelf != active)
            {
                targetImage.gameObject.SetActive(active);
            }
        }

        /// <summary>
        /// 根据剩余冷却时间修改图标透明度
        /// CD越长越透明，CD结束恢复为正常不透明
        /// </summary>
        private void SetSkillIconVisual(Graphic targetGraphic, Color targetColor, float remainingTime, float totalTime)
        {
            if (targetGraphic == null) return;

            Color finalColor = targetColor;
            finalColor.a = CalculateCooldownAlpha(remainingTime, totalTime);
            targetGraphic.color = finalColor;
        }

        /// <summary>
        /// 计算冷却中的透明度
        /// </summary>
        private float CalculateCooldownAlpha(float remainingTime, float totalTime)
        {
            if (remainingTime <= 0f || totalTime <= 0f)
            {
                return 1f;
            }

            float progress = 1f - Mathf.Clamp01(remainingTime / totalTime);
            return Mathf.Lerp(cooldownMinAlpha, 1f, progress);
        }
        #endregion
    }
}

