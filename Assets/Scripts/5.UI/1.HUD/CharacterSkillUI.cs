using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace WutheringWaves
{
    /// <summary>
    /// 魂类游戏 角色技能UI控制器
    /// 功能：负责管理角色技能、Q爆发的全部UI显示逻辑
    /// 包含：技能图标切换、冷却遮罩、冷却数字、按键提示、可用/禁用状态颜色、锁定/解锁状态
    /// 依赖：CharacterFacade（角色统一入口）、CharacterAttack（攻击/技能逻辑）
    /// </summary>
    [DisallowMultipleComponent]
    public class CharacterSkillUI : MonoBehaviour
    {
        #region 角色绑定相关
        [Header("=== 角色绑定设置 ===")]
        [Tooltip("手动绑定的角色门面脚本，优先级最高，手动指定后不会自动查找")]
        [SerializeField] private CharacterFacade targetFacade;

        [Tooltip("兼容旧链路：手动绑定的角色核心脚本，未迁完时可继续作为兜底引用")]
        [FormerlySerializedAs("targetCore")]
        [SerializeField] private CharacterCore legacyTargetCore;

        [Tooltip("是否从当前物体上的UIRoot自动获取角色门面")]
        [FormerlySerializedAs("autoBindFromUIManager")]
        [SerializeField] private bool autoBindFromUIRoot = true;

        [Tooltip("兜底方案：无任何绑定时，自动在全场景查找CharacterFacade")]
        [SerializeField] private bool autoFindCharacterFacade = true;

        [Tooltip("兼容旧链路：未找到CharacterFacade时，是否自动在全场景查找CharacterCore")]
        [SerializeField] private bool autoFindCharacterCore = true;

        private UIRoot _uiRoot; // UI总管引用
        private CharacterAttack _attackLogic; // 角色攻击/技能逻辑引用
        private CharacterStamina _staminaLogic; // 角色体力逻辑引用
        private bool _hasSubscribedAttackEvent; // 标记是否已订阅全局技能UI事件，防止重复订阅
        private bool _hasSubscribedStaminaEvent; // 标记是否已订阅全局体力事件，防止重复订阅
        #endregion

        #region UI元素引用
        [Header("=======================================")]
        [Header("=== 头像UI设置 ===")]
        [SerializeField] private Image avatarIcon; // 角色头像图片

        [Header("=======================================")]
        [Header("=== 通用技能UI - 基础按钮 ===")]
        [SerializeField] private Image skillButtonFrame; // 技能按钮边框
        [SerializeField] private TextMeshProUGUI skillKeyText; // 技能按键文字（如E）

        [Header("=== 通用技能UI - 冷却显示 ===")]
        [SerializeField] private Image skillCooldownMask; // 技能冷却环形遮罩
        [SerializeField] private TextMeshProUGUI skillCooldownText; // 技能冷却数字

        [Header("=== 通用技能UI - 技能图标 ===")]
        [SerializeField] private Image skill1Icon; // 技能1图标
        [SerializeField] private Image skill2Icon; // 技能2图标
        [SerializeField] private Image skill3Icon; // 技能3图标
        [SerializeField] private Image skill4Icon; // 技能4图标

        [Header("=======================================")]
        [Header("=== Q爆发技能UI - 基础按钮 ===")]
        [SerializeField] private Image qBurstButtonFrame; // Q爆发按钮边框
        [SerializeField] private TextMeshProUGUI qBurstKeyText; // Q爆发按键文字（如Q）

        [Header("=== Q爆发技能UI - 冷却显示 ===")]
        [SerializeField] private Image qBurstCooldownMask; // Q爆发冷却环形遮罩
        [SerializeField] private TextMeshProUGUI qBurstCooldownText; // Q爆发冷却数字

        [Header("=== Q爆发技能UI - 状态图标 ===")]
        [SerializeField] private Image qBurstAvailableIcon; // Q爆发可用图标
        [SerializeField] private Image qBurstLockedIcon; // Q爆发锁定/不可用图标

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
        private void Awake()
        {
            _uiRoot = GetComponentInParent<UIRoot>();
            InitializeStaticUI();
        }

        public void InjectDependencies(UIRoot root, CharacterFacade facade)
        {
            if (root != null)
            {
                _uiRoot = root;
            }

            if (facade != null)
            {
                targetFacade = facade;
                legacyTargetCore = facade.LegacyCore;
            }

            TryBindCharacterUI();
            RefreshUI();
        }

        // 兼容旧链路：允许外部仍按旧接口注入CharacterCore
        public void InjectDependencies(UIRoot root, CharacterCore core)
        {
            legacyTargetCore = core;
            InjectDependencies(root, core != null ? (core.Facade != null ? core.Facade : core.GetComponent<CharacterFacade>()) : null);
        }

        public void SetTargetFacade(CharacterFacade facade)
        {
            targetFacade = facade;
            legacyTargetCore = facade != null ? facade.LegacyCore : legacyTargetCore;
            TryBindCharacterUI();
            RefreshUI();
        }

        // 兼容旧链路：保留旧版接口
        public void SetTargetCore(CharacterCore core)
        {
            legacyTargetCore = core;
            SetTargetFacade(core != null ? (core.Facade != null ? core.Facade : core.GetComponent<CharacterFacade>()) : null);
        }

        private void OnEnable()
        {
            TryBindCharacterUI();
            RefreshUI();
        }

        private void Start()
        {
            TryBindCharacterUI();
            RefreshUI();
        }

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

        private void OnDisable()
        {
            UnsubscribeAttackEvent();
            UnsubscribeStaminaEvent();
        }

        private void OnDestroy()
        {
            UnsubscribeAttackEvent();
            UnsubscribeStaminaEvent();
        }
        #endregion

        #region 初始化逻辑
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

        private bool TryBindCharacterUI()
        {
            CharacterFacade nextFacade = ResolveTargetFacade();
            CharacterContext context = nextFacade != null ? nextFacade.Context : null;
            CharacterAttack nextAttack = context != null ? context.AttackLogic : null;
            CharacterStamina nextStamina = context != null ? context.StaminaLogic : null;

            // 兼容旧链路：门面未准备好时，回退到旧核心解析
            CharacterCore nextCore = ResolveLegacyTargetCore(nextFacade);
            if (nextAttack == null && nextCore != null)
            {
                nextAttack = nextCore.attackLogic;
            }

            if (nextStamina == null && nextCore != null)
            {
                nextStamina = nextCore.GetCharacterStamina();
            }

            // 无可用角色逻辑，绑定失败
            if (nextAttack == null)
            {
                return false;
            }

            // 保存当前绑定入口
            targetFacade = nextFacade;
            legacyTargetCore = nextCore;

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

        private CharacterFacade ResolveTargetFacade()
        {
            if (targetFacade != null)
            {
                return targetFacade;
            }

            // 方案1：从UIRoot获取角色门面
            if (autoBindFromUIRoot && _uiRoot != null && _uiRoot.Facade != null)
            {
                return _uiRoot.Facade;
            }

            // 方案2：从旧核心反查角色门面
            if (legacyTargetCore != null)
            {
                return legacyTargetCore.Facade != null
                    ? legacyTargetCore.Facade
                    : legacyTargetCore.GetComponent<CharacterFacade>();
            }

            // 方案3：全场景自动查找角色门面
            if (autoFindCharacterFacade)
            {
                CharacterFacade facade = FindObjectOfType<CharacterFacade>();
                if (facade != null)
                {
                    return facade;
                }
            }

            return null;
        }

        private CharacterCore ResolveLegacyTargetCore(CharacterFacade facade)
        {
            if (facade != null && facade.LegacyCore != null)
            {
                return facade.LegacyCore;
            }

            if (legacyTargetCore != null)
            {
                return legacyTargetCore;
            }

            if (autoBindFromUIRoot && _uiRoot != null && _uiRoot.Core != null)
            {
                return _uiRoot.Core;
            }

            if (autoFindCharacterCore)
            {
                return FindObjectOfType<CharacterCore>();
            }

            return null;
        }

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
            CharacterFacade facade = targetFacade;
            if (staminaRoot == null || facade == null)
            {
                return;
            }

            Transform staminaAnchor = facade.transform.Find("StaminaAnchor");
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

            CharacterContext context = targetFacade != null ? targetFacade.Context : null;
            if (context != null && context.PlayerCamera != null && context.PlayerCamera.cameraPivot != null)
            {
                return context.PlayerCamera.cameraPivot.GetComponent<Camera>();
            }

            if (legacyTargetCore != null && legacyTargetCore.playerCamera != null && legacyTargetCore.playerCamera.cameraPivot != null)
            {
                return legacyTargetCore.playerCamera.cameraPivot.GetComponent<Camera>();
            }

            return Camera.main;
        }

        private void RefreshUnboundUI()
        {
            SetActiveSkillIcon(1);
            ApplyCooldownMask(skillCooldownMask, 0f, 1f);
            SetCooldownText(skillCooldownText, 0f);
            SetGraphicColor(skillButtonFrame, disableColor);
            SetGraphicColor(skillKeyText, disableColor);
            SetSkillIconVisual(GetCurrentSkillIcon(1), disableColor, 0f, 1f);
            SetImageActive(qBurstAvailableIcon, false);
            SetImageActive(qBurstLockedIcon, true);
            ApplyCooldownMask(qBurstCooldownMask, 0f, 1f);
            SetCooldownText(qBurstCooldownText, 0f);
            SetGraphicColor(qBurstButtonFrame, disableColor);
            SetGraphicColor(qBurstKeyText, disableColor);
            SetSkillIconVisual(qBurstLockedIcon, disableColor, 0f, 1f);
        }

        private void RefreshSkillUI()
        {
            int currentSkillIndex = _attackLogic.CurrentESkillUIIndex;
            Image currentSkillIcon = GetCurrentSkillIcon(currentSkillIndex);
            bool isPrimarySkillCooldown = currentSkillIndex == 1 && _attackLogic.Skill1CDTimer > 0f;
            float remainingTime = isPrimarySkillCooldown ? _attackLogic.Skill1CDTimer : 0f;
            float totalTime = isPrimarySkillCooldown ? _attackLogic.skill1CD : 0f;
            Color targetColor = isPrimarySkillCooldown ? disableColor : normalColor;

            SetActiveSkillIcon(currentSkillIndex);
            ApplyCooldownMask(skillCooldownMask, remainingTime, totalTime);
            SetCooldownText(skillCooldownText, remainingTime);
            SetGraphicColor(skillButtonFrame, targetColor);
            SetGraphicColor(skillKeyText, targetColor);
            SetSkillIconVisual(currentSkillIcon, targetColor, remainingTime, totalTime);
        }

        private void RefreshQBurstUI()
        {
            bool isQBurstAvailable = _attackLogic.IsQBurstable();
            bool hasQBurstConfigured = _attackLogic.HasQBurstConfigured;
            bool showAvailableIcon = isQBurstAvailable && hasQBurstConfigured;
            float remainingTime = _attackLogic.QBurstCDTimer;
            Color targetColor = showAvailableIcon ? normalColor : disableColor;

            SetImageActive(qBurstAvailableIcon, showAvailableIcon);
            SetImageActive(qBurstLockedIcon, !showAvailableIcon);
            ApplyCooldownMask(qBurstCooldownMask, remainingTime, _attackLogic.qBurstCD);
            SetCooldownText(qBurstCooldownText, remainingTime);
            SetGraphicColor(qBurstButtonFrame, targetColor);
            SetGraphicColor(qBurstKeyText, targetColor);

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

        private void SetActiveSkillIcon(int skillIndex)
        {
            SetImageActive(skill1Icon, skillIndex == 1);
            SetImageActive(skill2Icon, skillIndex == 2);
            SetImageActive(skill3Icon, skillIndex == 3);
            SetImageActive(skill4Icon, skillIndex == 4);
        }

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

        private void ApplyCooldownMask(Image targetMask, float remainingTime, float totalTime)
        {
            if (targetMask == null) return;

            bool showMask = remainingTime > 0f && totalTime > 0f;
            targetMask.gameObject.SetActive(showMask);

            if (!showMask)
            {
                targetMask.fillAmount = 0f;
                return;
            }

            targetMask.fillAmount = Mathf.Clamp01(remainingTime / totalTime);
        }

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

            targetText.text = FormatCooldownTime(remainingTime);
        }

        private string FormatCooldownTime(float timeValue)
        {
            return timeValue.ToString("0.0");
        }

        private void SetKeyText(TextMeshProUGUI targetText, string keyName)
        {
            if (targetText == null) return;
            targetText.text = keyName;
        }

        private void SetGraphicColor(Graphic targetGraphic, Color targetColor)
        {
            if (targetGraphic == null) return;
            targetGraphic.color = targetColor;
        }

        private void SetImageActive(Image targetImage, bool active)
        {
            if (targetImage == null) return;
            if (targetImage.gameObject.activeSelf != active)
            {
                targetImage.gameObject.SetActive(active);
            }
        }

        private void SetSkillIconVisual(Graphic targetGraphic, Color targetColor, float remainingTime, float totalTime)
        {
            if (targetGraphic == null) return;

            Color finalColor = targetColor;
            finalColor.a = CalculateCooldownAlpha(remainingTime, totalTime);
            targetGraphic.color = finalColor;
        }

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
