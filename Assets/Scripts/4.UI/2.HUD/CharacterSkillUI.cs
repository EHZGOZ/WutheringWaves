using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace WutheringWaves
{
    /// <summary>
    /// 角色技能UI控制器：负责管理技能、Q爆发和体力显示逻辑
    /// 依赖CharacterContext作为当前角色统一入口
    /// </summary>
    [DisallowMultipleComponent]
    public class CharacterSkillUI : MonoBehaviour
    {
        #region 瑙掕壊缁戝畾鐩稿叧
        [Header("=== 瑙掕壊缁戝畾璁剧疆 ===")]
        [Tooltip("鎵嬪姩缁戝畾鐨勮鑹查棬闈㈣剼鏈紝浼樺厛绾ф渶楂橈紝鎵嬪姩鎸囧畾鍚庝笉浼氳嚜鍔ㄦ煡鎵?")]
        [FormerlySerializedAs("targetFacade")]
        [SerializeField] private CharacterContext targetContext;
        [Tooltip("鏄惁浠庡綋鍓嶇墿浣撲笂鐨刄IRoot鑷姩鑾峰彇瑙掕壊闂ㄩ潰")]
        [FormerlySerializedAs("autoBindFromUIManager")]
        [SerializeField] private bool autoBindFromUIRoot = true;
        [Tooltip("兜底方案：无任何绑定时，自动在全场景查找CharacterContext")]
        [FormerlySerializedAs("autoFindCharacterFacade")]
        [SerializeField] private bool autoFindCharacterContext = true;

        private UIRoot _uiRoot; // UIRoot 引用
        private CharacterAttack _attackLogic; // 技能逻辑引用
        private JinxiSpecialSkillLinker _jinxiSpecialSkillLinker; // 今汐专属技能逻辑引用
        private PlayerStamina _playerStamina; // 玩家共享体力逻辑引用
        private bool _hasSubscribedAttackEvent; // 标记是否已经订阅技能 UI 事件，避免重复订阅
        private bool _hasSubscribedStaminaEvent; // 标记是否已经订阅体力事件，避免重复订阅
        #endregion

        #region UI鍏冪礌寮曠敤
        [Header("=======================================")]
        [Header("=== 澶村儚UI璁剧疆 ===")]
        [SerializeField] private Image avatarIcon; // 瑙掕壊澶村儚鍥剧墖

        [Header("=======================================")]
        [Header("=== 閫氱敤鎶€鑳経I - 鍩虹鎸夐挳 ===")]
        [SerializeField] private Image skillButtonFrame; // 技能按钮边框
        [SerializeField] private TextMeshProUGUI skillKeyText; // 技能按键文本
        [Header("=== 閫氱敤鎶€鑳経I - 鍐峰嵈鏄剧ず ===")]
        [SerializeField] private Image skillCooldownMask; // 技能冷却遮罩
        [SerializeField] private TextMeshProUGUI skillCooldownText; // 技能冷却数字
        [Header("=== 閫氱敤鎶€鑳経I - 鎶€鑳藉浘鏍?===")]
        [SerializeField] private Image skill1Icon; // 鎶€鑳?鍥炬爣
        [SerializeField] private Image skill2Icon; // 鎶€鑳?鍥炬爣
        [SerializeField] private Image skill3Icon; // 鎶€鑳?鍥炬爣
        [SerializeField] private Image skill4Icon; // 鎶€鑳?鍥炬爣

        [Header("=======================================")]
        [Header("=== Q鐖嗗彂鎶€鑳経I - 鍩虹鎸夐挳 ===")]
        [SerializeField] private Image qBurstButtonFrame; // Q鐖嗗彂鎸夐挳杈规
        [SerializeField] private TextMeshProUGUI qBurstKeyText; // Q鐖嗗彂鎸夐敭鏂囧瓧锛堝Q锛?
        [Header("=== Q鐖嗗彂鎶€鑳経I - 鍐峰嵈鏄剧ず ===")]
        [SerializeField] private Image qBurstCooldownMask; // Q鐖嗗彂鍐峰嵈鐜舰閬僵
        [SerializeField] private TextMeshProUGUI qBurstCooldownText; // Q鐖嗗彂鍐峰嵈鏁板瓧

        [Header("=== Q鐖嗗彂鎶€鑳経I - 鐘舵€佸浘鏍?===")]
        [SerializeField] private Image qBurstAvailableIcon; // Q鐖嗗彂鍙敤鍥炬爣
        [SerializeField] private Image qBurstLockedIcon; // Q鐖嗗彂閿佸畾/涓嶅彲鐢ㄥ浘鏍?
        [Header("=======================================")]
        [Header("=== 浣撳姏妲経I ===")]
        [SerializeField] private RectTransform staminaRoot;
        [SerializeField] private Image staminaRingBg;
        [SerializeField] private Image staminaRingFill;
        [SerializeField] private Image staminaRingGlow;
        [SerializeField] private CanvasGroup staminaCanvasGroup;
        #endregion

        #region 鏄剧ず璁剧疆
        [Header("=======================================")]
        [Header("=== UI鏄剧ず閰嶇疆 ===")]
        [Tooltip("鎶€鑳借Е鍙戞寜閿殑鏄剧ず鍚嶇О")]
        [SerializeField] private string skillKeyName = "E";
        [Tooltip("Q 爆发按键的显示名称")]
        [SerializeField] private string qBurstKeyName = "Q";
        [Tooltip("鎶€鑳藉彲鐢ㄧ姸鎬佺殑UI棰滆壊")]
        [SerializeField] private Color normalColor = Color.white;
        [Tooltip("鎶€鑳藉喎鍗?绂佺敤鐘舵€佺殑UI棰滆壊")]
        [SerializeField] private Color disableColor = new Color(0.45f, 0.45f, 0.45f, 1f);
        [Tooltip("冷却开始时图标的最低透明度")]
        [SerializeField] [Range(0f, 1f)] private float cooldownMinAlpha = 0.3f;
        [Header("=== 浣撳姏妲芥樉绀洪厤缃?===")]
        [SerializeField] private Camera uiFollowCamera;
        [SerializeField] private Vector3 staminaWorldOffset = Vector3.zero;
        [SerializeField] private Vector2 staminaScreenOffset = new Vector2(40f, 0f);
        [SerializeField] [Min(0f)] private float staminaFollowSmooth = 18f;
        [SerializeField] [Range(0f, 1f)] private float staminaHiddenAlpha = 0f;
        [SerializeField] [Min(0f)] private float staminaFadeSpeed = 8f;
        [SerializeField] [Range(0f, 1f)] private float staminaGlowMinAlpha = 0.15f;
        [SerializeField] [Range(0f, 1f)] private float staminaGlowMaxAlpha = 0.85f;
        #endregion

        #region Unity鐢熷懡鍛ㄦ湡鍑芥暟
        private void Awake()
        {
            _uiRoot = GetComponentInParent<UIRoot>();
            InitializeStaticUI();
        }

        public void InjectDependencies(UIRoot root, CharacterContext context)
        {
            if (root != null)
            {
                _uiRoot = root;
            }

            if (context != null)
            {
                targetContext = context;
            }

            TryBindCharacterUI();
            RefreshUI();
        }

        // 鍏煎鏃ч摼璺細鍏佽澶栭儴浠嶆寜鏃ф帴鍙ｆ敞鍏haracterCore
        public void SetTargetContext(CharacterContext context)
        {
            targetContext = context;
            TryBindCharacterUI();
            RefreshUI();
        }

        // 鍏煎鏃ч摼璺細淇濈暀鏃х増鎺ュ彛
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
            // 鏀诲嚮閫昏緫涓虹┖鏃讹紝閲嶆柊灏濊瘯缁戝畾锛岀粦瀹氭垚鍔熷悗绔嬪嵆鍒锋柊UI
            if ((_attackLogic == null || _playerStamina == null) && TryBindCharacterUI())
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

        #region 鍒濆鍖栭€昏緫
        private void InitializeStaticUI()
        {
            // 璁剧疆鎸夐敭鏄剧ず鏂囧瓧
            SetKeyText(skillKeyText, skillKeyName);
            SetKeyText(qBurstKeyText, qBurstKeyName);

            // 如果没有配置遮罩图片，这两个字段可以留空
            InitializeMask(skillCooldownMask);
            InitializeMask(qBurstCooldownMask);

            // 璁剧疆澶村儚涓烘甯搁鑹?            if (avatarIcon != null)
            {
                avatarIcon.color = normalColor;
            }

            InitializeStaminaUI();
        }

        private bool TryBindCharacterUI()
        {
            CharacterContext nextContext = ResolveTargetContext();
            CharacterContext context = nextContext;
            CharacterAttack nextAttack = context != null ? context.AttackLogic : null;
            JinxiSpecialSkillLinker nextJinxiLinker = context != null && context.StateMachine != null ? context.StateMachine.JinxiSpecialSkillLinker : null;
            PlayerStamina nextStamina = context != null ? context.PlayerStamina : null;

            // 没有可用的门面或上下文时，绑定失败
            if (nextAttack == null)
            {
                return false;
            }

            // 保存当前绑定入口
            targetContext = nextContext;

            // 角色逻辑未变化时，只补充订阅
            if (_attackLogic == nextAttack && _jinxiSpecialSkillLinker == nextJinxiLinker && _playerStamina == nextStamina)
            {
                SubscribeAttackEvent();
                SubscribeStaminaEvent();
                return true;
            }

            // 角色逻辑发生变化：先取消旧事件，再赋值新逻辑，最后订阅新事件
            UnsubscribeAttackEvent();
            UnsubscribeStaminaEvent();
            _attackLogic = nextAttack;
            _jinxiSpecialSkillLinker = nextJinxiLinker;
            _playerStamina = nextStamina;
            SubscribeAttackEvent();
            SubscribeStaminaEvent();
            return true;
        }

        private CharacterContext ResolveTargetContext()
        {
            if (targetContext != null)
            {
                return targetContext;
            }

            // 方案1：从UIRoot获取角色上下文
            if (autoBindFromUIRoot && _uiRoot != null && _uiRoot.Context != null)
            {
                return _uiRoot.Context;
            }

            // 方案2：全场景自动查找角色上下文
            if (autoFindCharacterContext)
            {
                CharacterContext context = FindObjectOfType<CharacterContext>();
                if (context != null)
                {
                    return context;
                }
            }

            return null;
        }

        private void SubscribeAttackEvent()
        {
            // 绌哄紩鐢?宸茶闃呭垯鐩存帴杩斿洖
            if (_attackLogic == null || _hasSubscribedAttackEvent) return;

            GameEvents.OnSkillUIStateChanged += HandleSkillUIStateChanged;
            _hasSubscribedAttackEvent = true;
        }

        private void SubscribeStaminaEvent()
        {
            if (_playerStamina == null || _hasSubscribedStaminaEvent) return;

            GameEvents.OnStaminaChanged += HandleStaminaChangedFromEventBus;
            GameEvents.OnStaminaVisibilityChanged += HandleStaminaVisibilityChangedFromEventBus;
            _hasSubscribedStaminaEvent = true;
            _playerStamina.ForceRefreshEvents();
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

        #region UI鍒锋柊閫昏緫
        private void RefreshUI()
        {
            // 寮哄埗鍒锋柊鎸夐敭鏂囨湰锛堥槻姝㈤厤缃慨鏀瑰悗涓嶇敓鏁堬級
            SetKeyText(skillKeyText, skillKeyName);
            SetKeyText(qBurstKeyText, qBurstKeyName);

            // 鏈粦瀹氭敾鍑婚€昏緫锛氭樉绀哄叏绂佺敤UI
            if (_attackLogic == null)
            {
                RefreshUnboundUI();
                return;
            }

            // 宸茬粦瀹氾細鍒嗗埆鍒锋柊鏅€氭妧鑳?+ Q鐖嗗彂鎶€鑳経I
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

        private void HandleStaminaChangedFromEventBus(PlayerStamina source, float current, float max, float normalized)
        {
            if (source == _playerStamina)
            {
                HandleStaminaChanged(current, max, normalized);
            }
        }

        private void HandleStaminaVisibilityChangedFromEventBus(PlayerStamina source, bool visible)
        {
            if (source == _playerStamina)
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
            CharacterContext context = targetContext;
            if (staminaRoot == null || context == null)
            {
                return;
            }

            Transform staminaAnchor = context.transform.Find("StaminaAnchor");
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

            float targetAlpha = (_playerStamina != null && _playerStamina.IsVisible) ? 1f : staminaHiddenAlpha;
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

            //CharacterContext context = targetContext != null ? targetContext.Context : null;
            //if (context != null && context.PlayerCamera != null && context.PlayerCamera.cameraPivot != null)
            //{
            //    return context.PlayerCamera.cameraPivot.GetComponent<Camera>();
            //}

            
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
            if (_jinxiSpecialSkillLinker == null)
            {
                RefreshUnboundUI();
                return;
            }

            int currentSkillIndex = _jinxiSpecialSkillLinker.CurrentESkillUIIndex;
            Image currentSkillIcon = GetCurrentSkillIcon(currentSkillIndex);
            bool isPrimarySkillCooldown = currentSkillIndex == 1 && _jinxiSpecialSkillLinker.Skill1CDTimer > 0f;
            float remainingTime = isPrimarySkillCooldown ? _jinxiSpecialSkillLinker.Skill1CDTimer : 0f;
            float totalTime = isPrimarySkillCooldown ? _jinxiSpecialSkillLinker.Skill1CD : 0f;
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
            if (_jinxiSpecialSkillLinker == null)
            {
                SetImageActive(qBurstAvailableIcon, false);
                SetImageActive(qBurstLockedIcon, true);
                ApplyCooldownMask(qBurstCooldownMask, 0f, 1f);
                SetCooldownText(qBurstCooldownText, 0f);
                SetGraphicColor(qBurstButtonFrame, disableColor);
                SetGraphicColor(qBurstKeyText, disableColor);
                SetSkillIconVisual(qBurstLockedIcon, disableColor, 0f, 1f);
                return;
            }

            bool isQBurstAvailable = _jinxiSpecialSkillLinker.IsQBurstable();
            bool hasQBurstConfigured = _jinxiSpecialSkillLinker.HasQBurstConfigured;
            bool showAvailableIcon = isQBurstAvailable && hasQBurstConfigured;
            float remainingTime = _jinxiSpecialSkillLinker.QBurstCDTimer;
            Color targetColor = showAvailableIcon ? normalColor : disableColor;

            SetImageActive(qBurstAvailableIcon, showAvailableIcon);
            SetImageActive(qBurstLockedIcon, !showAvailableIcon);
            ApplyCooldownMask(qBurstCooldownMask, remainingTime, _jinxiSpecialSkillLinker.QBurstCD);
            SetCooldownText(qBurstCooldownText, remainingTime);
            SetGraphicColor(qBurstButtonFrame, targetColor);
            SetGraphicColor(qBurstKeyText, targetColor);

            if (showAvailableIcon)
            {
                SetSkillIconVisual(qBurstAvailableIcon, normalColor, remainingTime, _jinxiSpecialSkillLinker.QBurstCD);
            }
            else
            {
                SetSkillIconVisual(qBurstLockedIcon, disableColor, remainingTime, _jinxiSpecialSkillLinker.QBurstCD);
            }
        }
        #endregion

        #region 宸ュ叿鏂规硶
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

