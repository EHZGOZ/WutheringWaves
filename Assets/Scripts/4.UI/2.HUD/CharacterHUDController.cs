using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WutheringWaves
{
    [System.Serializable]
    public class TeamAvatarSlotUI
    {
        [Header("头像根节点")]
        public GameObject root; // 整个头像槽位，没角色时隐藏

        [Header("头像图片")]
        public Image avatarIcon; // 角色头像图片

        [Header("当前角色底框")]
        public GameObject selectedFrame; // 当前受控角色底框
    }

    [DisallowMultipleComponent]
    // 角色HUD控制器：负责玩家HUD、技能UI、Q爆发UI和共享体力UI的显示与刷新
    public class CharacterHUDController : MonoBehaviour
    {
        [Header("=== HUD根节点 ===")]
        [SerializeField] private GameObject hudPanel; // HUD根节点
        [Header("角色上下文")]
        [SerializeField] private CharacterContext context; // 当前绑定的角色上下文
        [Header("玩家共享体力逻辑")]
        [SerializeField] private PlayerStamina playerStamina; // 玩家共享体力逻辑
        [Header("角色UI图标")]
        [SerializeField] private CharacterUIConfigSO uiConfig;//角色UI图标
        [Header("玩家相机逻辑")]
        [SerializeField] private PlayerCamera playerCamera; // 玩家相机逻辑


        #region 技能UI引用
        [Header("=== 技能图标 ===")]
        [Header("队伍头像图标配置缓存")]
        private UIIconLayoutData[] teamAvatarIconLayouts = new UIIconLayoutData[3];
        [Header("E技能图标")]
        public UIIconLayoutData[] eSkillIcons;
        [Header("Q技能未充能图标")]
        public UIIconLayoutData qBurstLockedIcon;
        [Header("Q技能可释放图标")]
        public UIIconLayoutData[] qBurstReadyIcons;
        [Header("共鸣条装饰UI")]
        public UIIconLayoutData resonanceDecor;
        [Header("延奏值装饰UI")]
        public UIIconLayoutData concertoDecor;

        [Header("=== 冷却显示 ===")]
        [SerializeField] private Image eSkillCooldownMask; // E技能冷却遮罩
        [SerializeField] private Image qBurstCooldownMask; // Q爆发冷却遮罩
        [SerializeField] private TextMeshProUGUI eSkillCooldownText; // E技能冷却数字
        [SerializeField] private TextMeshProUGUI qBurstCooldownText; // Q爆发冷却数字

        [Header("技能冷却/禁用状态的UI颜色")]
        [SerializeField] private Color disableColor = new Color(0.45f, 0.45f, 0.45f, 1f); // 禁用颜色
        [SerializeField] private Color normalColor = Color.white; // 正常颜色

        [Header("E技能键盘按键提示")]
        [SerializeField] private Image eKeyImage; // E技能键盘按键提示
        [Header("Q爆发键盘按键提示")]
        [SerializeField] private Image qKeyImage; // Q爆发键盘按键提示
        #endregion

        #region 生命值槽UI引用
        [Header("=== 生命值槽UI ===")]
        [SerializeField] private Image healthSlotImage; // 生命值槽背景
        [SerializeField] private Image healthFillImage; // 生命值填充
        [SerializeField] private TextMeshProUGUI healthText; // 生命值文本
        #endregion

        #region 体力槽UI引用
        [Header("=== 体力槽UI ===")]
        [SerializeField] private RectTransform staminaRoot; // 体力槽根节点
        [SerializeField] private Image staminaRingBg; // 体力槽背景
        [SerializeField] private Image staminaRingFill; // 体力槽填充
        [SerializeField] private CanvasGroup staminaCanvasGroup; // 体力槽透明度控制

        [Header("=== 体力槽显示配置 ===")]
        [SerializeField] private Vector3 staminaWorldOffset = Vector3.zero; // 体力槽世界偏移
        [SerializeField] private Vector2 staminaScreenOffset = new Vector2(40f, 0f); // 体力槽屏幕偏移
        [SerializeField] [Min(0f)] private float staminaFollowSmooth = 18f; // 体力槽跟随平滑
        [SerializeField] [Range(0f, 1f)] private float staminaHiddenAlpha = 0f; // 体力槽隐藏透明度
        [SerializeField] [Min(0f)] private float staminaFadeSpeed = 8f; // 体力槽淡入淡出速度
        [Header("=== 体力槽缩放配置 ===")]
        [SerializeField] private Vector2 staminaBaseSize = new Vector2(100f, 100f); // 体力槽基础尺寸
        [SerializeField][Min(0f)] private float staminaMinScale = 0.8f; // 镜头最远时体力槽缩放
        [SerializeField][Min(0f)] private float staminaMaxScale = 1.2f; // 镜头最近时体力槽缩放
        [SerializeField][Min(0f)] private float staminaScaleSmooth = 18f; // 体力槽缩放平滑速度

        #endregion

        private UIRoot uiRoot; // UI根节点引用
        private JinxiSpecialSkillLinker jinxiSpecialSkillLinker; // 今汐专属技能逻辑

        private bool hasSubscribedAttackEvent; // 是否已经订阅技能刷新事件
        private bool hasSubscribedStaminaEvent; // 是否已经订阅体力刷新事件
        private bool hasSubscribedHealthEvent; // 是否已经订阅生命值刷新事件
        private float targetStaminaAlpha; // 体力条目标透明度

        #region 生命周期
        private void LateUpdate()
        {
            UpdateStaminaScreenPosition();
            UpdateStaminaScaleByCameraZoom();
            UpdateStaminaFade();
        }

        private void OnDestroy()
        {
            UnsubscribeHealthEvent();
            UnsubscribeStaminaEvent();
        }

        #endregion

        #region 绑定角色
        // 绑定当前角色上下文：切人、新建、读档后由UIRoot调用
        public void Bind(CharacterContext injectedContext)
        {
            // 1.空值检查
            if (injectedContext == null)
            {
                return;
            }

            // 2.绑定数据集
            BindData(injectedContext);

            // 3.解析并缓存当前角色的技能UI配置
            ResolveSkillUIConfig();

            // 4.刷新UI
            RefreshUI();


            // 5,LOG
            //Debug.Log($"[CharacterHUDController] 当前角色UI配置: {(uiConfig != null ? uiConfig.name : "null")}", this);

        }
        

        
        #endregion

        #region 初始化
        // 初始化角色HUD：只处理一次性依赖和事件订阅
        public void Initialize(UIRoot uiRoot)
        {
            this.uiRoot = uiRoot;

            // HUD初始化时订阅生命值变化事件
            SubscribeHealthEvent();

            // HUD初始化时订阅体力变化事件
            SubscribeStaminaEvent();
        }

        #endregion

        #region 事件订阅

        #region 生命值变化事件
        // 订阅生命值变化事件
        private void SubscribeHealthEvent()
        {
            if (hasSubscribedHealthEvent)
            {
                return;
            }

            GameEvents.OnHealthChanged += HandleHealthChanged;
            hasSubscribedHealthEvent = true;
        }

        // 解绑生命值变化事件
        private void UnsubscribeHealthEvent()
        {
            if (!hasSubscribedHealthEvent)
            {
                return;
            }

            GameEvents.OnHealthChanged -= HandleHealthChanged;
            hasSubscribedHealthEvent = false;
        }
        #endregion

        #region 体力变化事件
        // 订阅体力变化事件
        private void SubscribeStaminaEvent()
        {
            // 1.已经订阅过时直接返回，避免重复绑定事件
            if (hasSubscribedStaminaEvent)
            {
                return;
            }

            // 2.订阅体力数值变化事件
            GameEvents.OnStaminaChanged += HandleStaminaChanged;

            // 3.订阅体力条显隐事件
            GameEvents.OnStaminaVisibilityChanged += HandleStaminaVisibilityChanged;

            // 4.标记体力事件已订阅
            hasSubscribedStaminaEvent = true;
        }


        // 解绑体力变化事件
        private void UnsubscribeStaminaEvent()
        {
            // 1.没有订阅过时直接返回
            if (!hasSubscribedStaminaEvent)
            {
                return;
            }

            // 2.解绑体力数值变化事件
            GameEvents.OnStaminaChanged -= HandleStaminaChanged;

            // 3.解绑体力条显隐事件
            GameEvents.OnStaminaVisibilityChanged -= HandleStaminaVisibilityChanged;

            // 4.标记体力事件已解绑
            hasSubscribedStaminaEvent = false;
        }

        #endregion

        #region 技能UI变化事件

        #endregion

        #endregion

        #region 绑定数据集
        // 绑定数据集
        public void BindData(CharacterContext injectedContext)
        {
            // 1.绑定当前角色上下文
            context = injectedContext;

            // 2.绑定玩家共享体力逻辑
            playerStamina = context != null ? context.PlayerStamina : null;

            // 3.绑定玩家相机逻辑
            playerCamera = context != null && context.PlayerController != null
                ? context.PlayerController.CurrentPlayerCamera
                : null;

            // 4.绑定角色UI图标配置
            uiConfig = context != null && context.CharacterDataSO != null
                ? context.CharacterDataSO.characterUIConfigSO
                : null;
        }

        #endregion

        #region 解析技能UI配置
        // 清空技能UI配置缓存
        private void ClearSkillUIConfigCache()
        {
            // 1.清空队伍头像配置缓存
            if (teamAvatarIconLayouts == null || teamAvatarIconLayouts.Length != 3)
            {
                teamAvatarIconLayouts = new UIIconLayoutData[3];
            }

            for (int i = 0; i < teamAvatarIconLayouts.Length; i++)
            {
                teamAvatarIconLayouts[i] = null;
            }

            // 2.清空E技能图标配置缓存
            eSkillIcons = null;

            // 3.清空Q技能图标配置缓存
            qBurstLockedIcon = null;
            qBurstReadyIcons = null;

            // 4.清空角色装饰UI配置缓存
            resonanceDecor = null;
            concertoDecor = null;
        }

        // 解析技能UI配置
        private void ResolveSkillUIConfig()
        {
            // 1.先清空上一名角色残留的技能UI配置缓存
            ClearSkillUIConfigCache();

            // 2.解析队伍头像图标配置
            ResolveTeamAvatarIcons();

            // 3.解析当前角色E技能图标配置
            ResolveESkillIcons();

            // 4.解析当前角色Q技能图标配置
            ResolveQBurstIcons();

            // 5.解析当前角色共鸣条装饰UI配置
            ResolveResonanceDecor();

            // 6.解析当前角色延奏值装饰UI配置
            ResolveConcertoDecor();
        }

        // 解析队伍头像图标配置
        private void ResolveTeamAvatarIcons()
        {
            // 1.空值检查：没有当前角色或玩家控制器时无法读取队伍数据
            if (context == null || context.PlayerController == null || context.PlayerController.PlayerRuntimeData == null)
            {
                return;
            }

            PlayerRuntimeData playerRuntimeData = context.PlayerController.PlayerRuntimeData;
            if (playerRuntimeData.teamSlots == null)
            {
                return;
            }

            // 2.逐个槽位解析角色头像配置
            for (int i = 0; i < teamAvatarIconLayouts.Length; i++)
            {
                if (i >= playerRuntimeData.teamSlots.Count)
                {
                    continue;
                }

                TeamCharacterSlotData slotData = playerRuntimeData.teamSlots[i];
                if (slotData == null)
                {
                    continue;
                }

                CharacterUIConfigSO slotUIConfig = ResolveCharacterUIConfig(slotData.characterName);
                teamAvatarIconLayouts[i] = slotUIConfig != null ? slotUIConfig.avatarIcon : null;
            }
        }
        // 根据角色名称解析角色UI配置
        private CharacterUIConfigSO ResolveCharacterUIConfig(CharacterName characterName)
        {
            if (GameBootstrap.Instance == null)
            {
                return null;
            }

            if (!GameBootstrap.Instance.TryGetCharacterPrefab(characterName, out GameObject prefab) || prefab == null)
            {
                return null;
            }

            CharacterContext prefabContext = prefab.GetComponent<CharacterContext>();
            if (prefabContext == null || prefabContext.CharacterDataSO == null)
            {
                return null;
            }

            return prefabContext.CharacterDataSO.characterUIConfigSO;
        }

        // 解析E技能图标配置
        private void ResolveESkillIcons()
        {
            eSkillIcons = uiConfig.eSkillIcons;
        }

        // 解析Q技能图标配置
        private void ResolveQBurstIcons()
        {
            qBurstLockedIcon = uiConfig.qBurstLockedIcon;
            qBurstReadyIcons = uiConfig.qBurstReadyIcons;
        }

        // 解析共鸣条装饰UI配置
        private void ResolveResonanceDecor()
        {
            resonanceDecor = uiConfig.resonanceDecor;
        }

        // 解析延奏值装饰UI配置
        private void ResolveConcertoDecor()
        {
            concertoDecor = uiConfig.concertoDecor;
        }
        #endregion

        #region 刷新UI
        //刷新UI
        public void RefreshUI()
        {
            // 强制刷新当前角色生命值，避免切人后生命值UI停留在上一名角色
            context.ForceRefreshHealth();

        }

        #region 刷新生命值UI
        // 处理生命值变化事件
        private void HandleHealthChanged(CharacterContext source, float current, float max, float normalized)
        {
            //1.只刷新当前HUD绑定的角色
            if (source == null || source != context)
            {
                return;
            }

            //2.刷新生命值UI
            RefreshHealthUI(current, max, normalized);
        }

        // 刷新生命值UI
        private void RefreshHealthUI(float current, float max, float normalized)
        {
            if (healthFillImage != null)
            {
                healthFillImage.fillAmount = Mathf.Clamp01(normalized);
            }

            if (healthText != null)
            {
                healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            }
        }
        #endregion

        #region 刷新技能UI



        #endregion

        #region 刷新体力UI
        // 处理体力变化事件
        private void HandleStaminaChanged(PlayerStamina source, float current, float max, float normalized)
        {
            // 1.只刷新当前HUD绑定的玩家体力
            if (source == null || source != playerStamina)
            {
                return;
            }

            // 2.刷新体力槽填充
            RefreshStaminaUI(current, max, normalized);
        }
        // 处理体力条显隐事件
        private void HandleStaminaVisibilityChanged(PlayerStamina source, bool visible)
        {
            // 1.只处理当前HUD绑定的玩家体力
            if (source == null || source != playerStamina)
            {
                return;
            }

            // 2.刷新体力条显隐
            RefreshStaminaVisibility(visible);
        }
        // 刷新体力条显隐
        private void RefreshStaminaVisibility(bool visible)
        {
            // 1.确保体力条根节点处于激活状态，显隐交给透明度控制
            if (staminaRoot != null && !staminaRoot.gameObject.activeSelf)
            {
                staminaRoot.gameObject.SetActive(true);
            }

            // 2.优先使用CanvasGroup控制透明度，避免关闭物体后位置和缩放逻辑被打断
            if (staminaCanvasGroup != null)
            {
                targetStaminaAlpha = visible ? 1f : staminaHiddenAlpha;
                return;
            }


            // 3.没有CanvasGroup时兜底控制根节点显隐
            if (staminaRoot != null)
            {
                staminaRoot.gameObject.SetActive(visible);
            }
        }


        // 刷新体力UI
        private void RefreshStaminaUI(float current, float max, float normalized)
        {
            // 1.刷新体力环填充比例
            if (staminaRingFill != null)
            {
                staminaRingFill.fillAmount = Mathf.Clamp01(normalized);
            }
        }

        #endregion

        #endregion

        #region 更新体力条屏幕位置
        // 更新体力条屏幕位置
        private void UpdateStaminaScreenPosition()
        {
            // 1.空值检查：缺少当前角色或体力条根节点时不更新
            if (context == null || staminaRoot == null)
            {
                return;
            }

            // 2.获取主相机，世界坐标转屏幕坐标需要使用真实渲染相机
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }

            // 3.优先使用角色相机观察点作为体力条跟随点，避免镜头缩放时根节点投影漂移太明显
            Transform followTarget = context.CameraTarget != null ? context.CameraTarget : context.transform;
            Vector3 worldPosition = followTarget.position + staminaWorldOffset;


            // 4.把世界坐标转换成屏幕坐标
            Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);


            // 6.叠加屏幕偏移，得到目标屏幕位置
            Vector2 targetScreenPosition = new Vector2(screenPosition.x, screenPosition.y) + staminaScreenOffset;

            // 7.根据平滑配置更新体力条位置
            if (staminaFollowSmooth <= 0f)
            {
                staminaRoot.position = targetScreenPosition;
            }
            else
            {
                staminaRoot.position = Vector3.Lerp(
                    staminaRoot.position,
                    targetScreenPosition,
                    staminaFollowSmooth * Time.unscaledDeltaTime
                );
            }
        }
        #endregion

        #region 更新体力条大小
        // 根据相机缩放距离更新体力条大小
        private void UpdateStaminaScaleByCameraZoom()
        {
            // 1.空值检查：缺少体力条根节点或玩家相机时不更新
            if (staminaRoot == null || playerCamera == null)
            {
                return;
            }

            // 2.根据相机当前缩放距离计算0到1的归一化值
            float zoomNormalized = Mathf.InverseLerp(
                playerCamera.MinZoomDistance,
                playerCamera.MaxZoomDistance,
                playerCamera.CurrentZoomDistance
            );

            // 3.镜头越近体力条越大，镜头越远体力条越小
            float targetScale = Mathf.Lerp(staminaMinScale, staminaMaxScale, zoomNormalized);


            // 4.根据缩放比例计算目标缩放
            Vector3 targetLocalScale = Vector3.one * targetScale;

            // 5.根据平滑配置更新体力条整体缩放
            if (staminaScaleSmooth <= 0f)
            {
                staminaRoot.localScale = targetLocalScale;
            }
            else
            {
                staminaRoot.localScale = Vector3.Lerp(
                    staminaRoot.localScale,
                    targetLocalScale,
                    staminaScaleSmooth * Time.unscaledDeltaTime
                );
            }

        }
        #endregion

        #region 更新体力条透明度
        // 更新体力条透明度
        private void UpdateStaminaFade()
        {
            // 1.空值检查：没有CanvasGroup时不处理淡入淡出
            if (staminaCanvasGroup == null)
            {
                return;
            }

            // 2.速度为0时直接设置目标透明度
            if (staminaFadeSpeed <= 0f)
            {
                staminaCanvasGroup.alpha = targetStaminaAlpha;
                return;
            }

            // 3.逐帧向目标透明度移动，实现淡入淡出
            staminaCanvasGroup.alpha = Mathf.MoveTowards(
                staminaCanvasGroup.alpha,
                targetStaminaAlpha,
                staminaFadeSpeed * Time.unscaledDeltaTime
            );
        }
        #endregion

        #region 工具方法
        // 设置HUD显隐
        public void SetVisible(bool visible)
        {
            if (hudPanel != null)
            {
                hudPanel.SetActive(visible);
            }
        }
        #endregion
    }
}
