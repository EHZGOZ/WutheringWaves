using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WutheringWaves
{
    [System.Serializable]

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


        #region 头像UI引用
        [Header("=== 队伍头像UI ===")]
        [SerializeField] private Image[] teamAvatarIconImages = new Image[3]; // 队伍头像真实图片组件
        [Header("头像选中颜色")]
        [SerializeField] private Color selectedAvatarColor = Color.white; // 当前角色头像颜色
        [Header("头像未选中颜色")]
        [SerializeField] private Color unselectedAvatarColor = new Color(0.7f, 0.7f, 0.7f, 1f); // 非当前角色头像颜色

        private Vector2[] teamAvatarBaseAnchoredPositions = new Vector2[3]; // 队伍头像Scene原始位置缓存
        #endregion

        #region 共鸣与延奏UI引用
        [Header("=== 共鸣与延奏UI ===")]
        [SerializeField] private Image resonanceDecorImage; // 共鸣条装饰真实图片组件
        [SerializeField] private Image concertoDecorImage; // 延奏值装饰真实图片组件

        [Header("共鸣条装饰UI配置缓存")]
        private UIIconLayoutData resonanceDecor; // 当前角色共鸣条装饰配置缓存
        [Header("延奏值装饰UI配置缓存")]
        private UIIconLayoutData concertoDecor; // 当前角色延奏值装饰配置缓存

        private Vector2 resonanceDecorBaseAnchoredPosition; // 共鸣条装饰Scene原始位置缓存
        private Vector2 concertoDecorBaseAnchoredPosition; // 延奏值装饰Scene原始位置缓存

        #endregion

        #region 技能UI引用
        [Header("=== 技能图标UI ===")]
        [SerializeField] private Image eSkillIconImage; // E技能真实图片组件
        [SerializeField] private Image qBurstIconImage; // Q技能真实图片组件
        private Vector2 eSkillIconBaseAnchoredPosition; // E技能图标Scene原始位置缓存
        private Vector2 qBurstIconBaseAnchoredPosition; // Q技能图标Scene原始位置缓存


        [Header("E技能图标配置缓存")]
        private UIIconLayoutData[] eSkillIcons; // 当前角色全部E技能候选图标配置

        [Header("Q技能未充能图标配置缓存")]
        private UIIconLayoutData qBurstLockedIcon; // 当前角色Q技能未充能图标配置

        [Header("Q技能可释放图标配置缓存")]
        private UIIconLayoutData[] qBurstReadyIcons; // 当前角色全部Q技能可释放候选图标配置

        [Header("冷却显示")]
        [SerializeField] private TextMeshProUGUI eSkillCooldownText; // E技能冷却数字
        [SerializeField] private TextMeshProUGUI qBurstCooldownText; // Q爆发冷却数字
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
            // 更新耐力条UI在屏幕上的跟随位置
            UpdateStaminaScreenPosition();
            // 根据相机的缩放比例，动态调整耐力条UI的大小
            UpdateStaminaScaleByCameraZoom();
            // 更新耐力条UI的淡入淡出效果
            UpdateStaminaFade();
        }

        private void OnDestroy()
        {
            // 取消订阅生命值相关的事件
            UnsubscribeHealthEvent();
            // 取消订阅耐力值相关的事件
            UnsubscribeStaminaEvent();
            // 取消订阅技能UI变化事件
            UnsubscribeSkillUIEvent();

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

            // 2.先清空上一名角色残留的技能UI配置缓存
            ClearUICache();

            // 3.绑定数据集
            BindData(injectedContext);

            // 4.解析并缓存当前角色的技能UI配置
            ResolveSkillUIConfig();

            //刷新当前角色生命值
            RefreshHealth();

        }



        #endregion

        #region 清空上一名角色残留的技能UI配置缓存
        // 清空UI配置缓存和真实UI显示
        private void ClearUICache()
        {
            // 1.清空头像UI配置和真实头像显示
            ClearTeamAvatarUIConfigCache();

            // 2.清空共鸣与延奏UI配置和真实装饰显示
            ClearResonanceAndConcertoUIConfigCache();

            // 3.清空技能UI配置和真实技能显示
            ClearSkillIconUIConfigCache();

            // 4.清空生命值槽UI显示
            ClearHealthSlotUIConfigCache();

            // 5.清空体力槽UI显示
            ClearStaminaSlotUIConfigCache();
        }
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
        // 解析技能UI配置
        private void ResolveSkillUIConfig()
        {
            // 1.解析队伍头像图标配置
            ResolveTeamAvatarIcons();

            // 2.解析当前角色共鸣条装饰UI配置
            ResolveResonanceDecor();

            // 3.解析当前角色延奏值装饰UI配置
            ResolveConcertoDecor();

            // 4.解析当前角色E技能图标配置
            ResolveESkillIcons();

            // 5.解析当前角色Q技能图标配置
            ResolveQBurstIcons();
        }

        // 1.解析并应用队伍头像图标配置
        private void ResolveTeamAvatarIcons()
        {
            // 1.空值检查：没有当前角色或玩家控制器时无法读取队伍数据
            if (context == null || context.PlayerController == null || context.PlayerController.PlayerRuntimeData == null)
            {
                return;
            }

            // 2.空值检查：没有头像图片数组时无法刷新头像
            if (teamAvatarIconImages == null || teamAvatarIconImages.Length == 0)
            {
                return;
            }

            PlayerRuntimeData playerRuntimeData = context.PlayerController.PlayerRuntimeData;
            if (playerRuntimeData.teamSlots == null)
            {
                return;
            }

            // 3.获取当前受控角色索引，用于刷新头像颜色
            int currentAvatarIndex = ResolveCurrentTeamAvatarIndex();

            // 4.逐个槽位解析角色头像配置，并直接应用到真实头像图片
            for (int i = 0; i < teamAvatarIconImages.Length; i++)
            {
                Image avatarIconImage = teamAvatarIconImages[i];
                if (avatarIconImage == null)
                {
                    continue;
                }

                if (i >= playerRuntimeData.teamSlots.Count)
                {
                    ApplyAvatarIconLayout(i, avatarIconImage, null);
                    continue;
                }

                TeamCharacterSlotData slotData = playerRuntimeData.teamSlots[i];
                if (slotData == null)
                {
                    ApplyAvatarIconLayout(i, avatarIconImage, null);
                    continue;
                }

                CharacterUIConfigSO slotUIConfig = ResolveCharacterUIConfig(slotData.characterName);
                UIIconLayoutData avatarLayout = slotUIConfig != null ? slotUIConfig.avatarIcon : null;

                // 5.把队伍头像配置直接应用到真实头像图片
                ApplyAvatarIconLayout(i, avatarIconImage, avatarLayout);

                // 6.根据当前受控角色索引刷新头像颜色
                RefreshTeamAvatarState(avatarIconImage, i == currentAvatarIndex);
            }
        }

        // 2.解析共鸣条装饰UI配置
        private void ResolveResonanceDecor()
        {
            // 1.读取当前角色共鸣条装饰配置缓存
            resonanceDecor = uiConfig != null ? uiConfig.resonanceDecor : null;

            // 2.把共鸣条装饰配置应用到真实图片
            ApplyDecorIconLayout(resonanceDecorImage, resonanceDecor, resonanceDecorBaseAnchoredPosition);
        }

        // 3.解析延奏值装饰UI配置
        private void ResolveConcertoDecor()
        {
            // 1.读取当前角色延奏值装饰配置缓存
            concertoDecor = uiConfig != null ? uiConfig.concertoDecor : null;

            // 2.把延奏值装饰配置应用到真实图片
            ApplyDecorIconLayout(concertoDecorImage, concertoDecor, concertoDecorBaseAnchoredPosition);
        }

        // 解析E技能图标配置
        private void ResolveESkillIcons()
        {
            // 1.读取当前角色E技能图标配置缓存
            eSkillIcons = uiConfig != null ? uiConfig.eSkillIcons : null;

            // 2.默认显示第一个E技能图标
            UIIconLayoutData defaultESkillIcon = eSkillIcons != null && eSkillIcons.Length > 0
                ? eSkillIcons[0]
                : null;

            // 3.把E技能默认图标应用到真实图片
            ApplySkillIconLayout(eSkillIconImage, defaultESkillIcon, eSkillIconBaseAnchoredPosition);
        }

        // 解析Q技能图标配置
        private void ResolveQBurstIcons()
        {
            // 1.读取当前角色Q技能图标配置缓存
            qBurstLockedIcon = uiConfig != null ? uiConfig.qBurstLockedIcon : null;
            qBurstReadyIcons = uiConfig != null ? uiConfig.qBurstReadyIcons : null;

            // 2.默认显示Q技能已充能图标
            UIIconLayoutData defaultQBurstIcon = qBurstReadyIcons != null && qBurstReadyIcons.Length > 0
                ? qBurstReadyIcons[0]
                : qBurstLockedIcon;

            // 3.把Q技能默认图标应用到真实图片
            ApplySkillIconLayout(qBurstIconImage, defaultQBurstIcon, qBurstIconBaseAnchoredPosition);
        }

        #endregion

        #region 初始化
        // 初始化角色HUD：只处理一次性依赖和事件订阅
        public void Initialize(UIRoot uiRoot)
        {
            this.uiRoot = uiRoot;

            //记录初始头像位置
            CacheTeamAvatarBaseAnchoredPositions();
            //记录共鸣条和延奏位置
            CacheResonanceAndConcertoBaseAnchoredPositions();
            //记录技能图标位置
            CacheSkillIconBaseAnchoredPositions();

            // HUD初始化时订阅生命值变化事件
            SubscribeHealthEvent();

            // HUD初始化时订阅体力变化事件
            SubscribeStaminaEvent();

            // HUD初始化时订阅技能UI变化事件
            SubscribeSkillUIEvent();

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
        // 订阅技能UI变化事件
        private void SubscribeSkillUIEvent()
        {
            // 1.已经订阅过时直接返回，避免重复绑定事件
            if (hasSubscribedAttackEvent)
            {
                return;
            }

            // 2.订阅技能图标UI刷新事件
            GameEvents.OnSkillIconUIChanged += HandleSkillIconUIChanged;

            // 3.订阅技能图标UI运行时刷新事件
            GameEvents.OnSkillIconUIRuntimeChanged += HandleSkillIconUIRuntimeChanged;

            // 4.标记技能UI事件已订阅
            hasSubscribedAttackEvent = true;

        }

        // 解绑技能UI变化事件
        private void UnsubscribeSkillUIEvent()
        {
            // 1.没有订阅过时直接返回
            if (!hasSubscribedAttackEvent)
            {
                return;
            }

            // 2.解绑技能图标UI刷新事件
            GameEvents.OnSkillIconUIChanged -= HandleSkillIconUIChanged;

            // 3.解绑技能图标UI运行时刷新事件
            GameEvents.OnSkillIconUIRuntimeChanged -= HandleSkillIconUIRuntimeChanged;

            // 4.标记技能UI事件已解绑
            hasSubscribedAttackEvent = false;


        }
        #endregion

        #endregion

        #region 头像
        // 清空头像UI显示
        private void ClearTeamAvatarUIConfigCache()
        {
            // 1.空值检查：没有头像图片时无需清理
            if (teamAvatarIconImages == null)
            {
                return;
            }

            // 2.逐个清空真实头像图片显示
            for (int i = 0; i < teamAvatarIconImages.Length; i++)
            {
                ApplyAvatarIconLayout(i, teamAvatarIconImages[i], null);
            }
        }
        // 根据角色名称解析角色UI配置
        private CharacterUIConfigSO ResolveCharacterUIConfig(CharacterName characterName)
        {
            // 1.角色生成服务为空时，无法通过角色名称查找角色预制体
            if (CharacterSpawnService.Instance == null)
            {
                return null;
            }

            // 2.从角色生成服务中查询角色预制体
            if (!CharacterSpawnService.Instance.TryGetCharacterPrefab(characterName, out GameObject prefab) || prefab == null)
            {
                return null;
            }

            // 3.从角色预制体上获取角色上下文
            CharacterContext prefabContext = prefab.GetComponent<CharacterContext>();
            if (prefabContext == null || prefabContext.CharacterDataSO == null)
            {
                return null;
            }

            // 4.返回角色UI配置，用于刷新头像等HUD显示
            return prefabContext.CharacterDataSO.characterUIConfigSO;
        }

        // 解析当前受控角色头像槽位索引
        private int ResolveCurrentTeamAvatarIndex()
        {
            // 1.空值检查：没有玩家运行时数据时无法解析当前角色槽位
            if (context == null || context.PlayerController == null || context.PlayerController.PlayerRuntimeData == null)
            {
                return -1;
            }

            // 2.直接读取玩家运行时数据中的当前受控角色索引
            return context.PlayerController.PlayerRuntimeData.currentCharacterIndex;
        }
        // 刷新头像状态
        private void RefreshTeamAvatarState(Image avatarIconImage, bool selected)
        {
            // 1.空值检查：没有头像图片时无法刷新状态
            if (avatarIconImage == null)
            {
                return;
            }

            // 2.根据是否为当前受控角色，应用不同颜色
            avatarIconImage.color = selected ? selectedAvatarColor : unselectedAvatarColor;
        }

        // 缓存队伍头像原始锚点位置
        private void CacheTeamAvatarBaseAnchoredPositions()
        {
            // 1.空值检查：没有头像图片时无法缓存
            if (teamAvatarIconImages == null)
            {
                return;
            }

            // 2.保证缓存数组长度和头像图片数组一致
            if (teamAvatarBaseAnchoredPositions == null || teamAvatarBaseAnchoredPositions.Length != teamAvatarIconImages.Length)
            {
                teamAvatarBaseAnchoredPositions = new Vector2[teamAvatarIconImages.Length];
            }

            // 3.逐个记录Scene中摆好的头像原始位置
            for (int i = 0; i < teamAvatarIconImages.Length; i++)
            {
                Image avatarIconImage = teamAvatarIconImages[i];
                if (avatarIconImage == null || avatarIconImage.rectTransform == null)
                {
                    continue;
                }

                teamAvatarBaseAnchoredPositions[i] = avatarIconImage.rectTransform.anchoredPosition;
            }
        }
        // 应用头像图标配置
        private void ApplyAvatarIconLayout(int avatarIndex, Image targetImage, UIIconLayoutData layoutData)
        {
            // 1.目标图片为空时无法刷新
            if (targetImage == null)
            {
                return;
            }

            // 2.配置为空或图标为空时，清空当前头像，避免残留上一名角色的图标
            if (layoutData == null || layoutData.sprite == null)
            {
                targetImage.sprite = null;
                targetImage.enabled = false;
                return;
            }

            // 3.应用头像图标和显示配置
            targetImage.enabled = true;
            targetImage.sprite = layoutData.sprite;
            targetImage.preserveAspect = layoutData.preserveAspect;
            targetImage.color = normalColor;

            // 4.应用尺寸，并把配置位置当作基于Scene原始位置的偏移
            RectTransform rectTransform = targetImage.rectTransform;
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = layoutData.size;

                Vector2 basePosition = avatarIndex >= 0
                    && teamAvatarBaseAnchoredPositions != null
                    && avatarIndex < teamAvatarBaseAnchoredPositions.Length
                    ? teamAvatarBaseAnchoredPositions[avatarIndex]
                    : rectTransform.anchoredPosition;

                rectTransform.anchoredPosition = basePosition + layoutData.anchoredPosition;
            }
        }

        #endregion

        #region 共鸣与延奏
        // 清空共鸣与延奏UI配置和真实装饰显示
        private void ClearResonanceAndConcertoUIConfigCache()
        {
            // 1.清空共鸣条装饰配置缓存
            resonanceDecor = null;

            // 2.清空延奏值装饰配置缓存
            concertoDecor = null;

            // 3.清空真实共鸣条装饰显示
            ApplyDecorIconLayout(resonanceDecorImage, null, resonanceDecorBaseAnchoredPosition);

            // 4.清空真实延奏值装饰显示
            ApplyDecorIconLayout(concertoDecorImage, null, concertoDecorBaseAnchoredPosition);
        }

        // 缓存共鸣与延奏装饰原始锚点位置
        private void CacheResonanceAndConcertoBaseAnchoredPositions()
        {
            // 1.记录共鸣条装饰Scene原始位置
            if (resonanceDecorImage != null && resonanceDecorImage.rectTransform != null)
            {
                resonanceDecorBaseAnchoredPosition = resonanceDecorImage.rectTransform.anchoredPosition;
            }

            // 2.记录延奏值装饰Scene原始位置
            if (concertoDecorImage != null && concertoDecorImage.rectTransform != null)
            {
                concertoDecorBaseAnchoredPosition = concertoDecorImage.rectTransform.anchoredPosition;
            }
        }
        // 应用装饰图标配置
        private void ApplyDecorIconLayout(Image targetImage, UIIconLayoutData layoutData, Vector2 baseAnchoredPosition)
        {
            // 1.目标图片为空时无法刷新
            if (targetImage == null)
            {
                return;
            }

            // 2.配置为空或图标为空时，清空当前装饰图标
            if (layoutData == null || layoutData.sprite == null)
            {
                targetImage.sprite = null;
                targetImage.enabled = false;
                return;
            }

            // 3.应用图标和显示配置
            targetImage.enabled = true;
            targetImage.sprite = layoutData.sprite;
            targetImage.preserveAspect = layoutData.preserveAspect;
            targetImage.color = normalColor;

            // 4.应用尺寸，并把配置位置当作基于Scene原始位置的偏移
            RectTransform rectTransform = targetImage.rectTransform;
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = layoutData.size;
                rectTransform.anchoredPosition = baseAnchoredPosition + layoutData.anchoredPosition;
            }
        }
        #endregion

        #region 技能
        // 清空技能UI配置和真实技能显示
        private void ClearSkillIconUIConfigCache()
        {
            // 1.清空E技能图标配置缓存
            eSkillIcons = null;

            // 2.清空Q技能图标配置缓存
            qBurstLockedIcon = null;
            qBurstReadyIcons = null;

            // 3.清空真实E技能图标显示
            ApplySkillIconLayout(eSkillIconImage, null, eSkillIconBaseAnchoredPosition);

            // 4.清空真实Q技能图标显示
            ApplySkillIconLayout(qBurstIconImage, null, qBurstIconBaseAnchoredPosition);


            // 5.清空E技能冷却文本
            if (eSkillCooldownText != null)
            {
                eSkillCooldownText.text = string.Empty;
                eSkillCooldownText.enabled = false;
            }

            // 6.清空Q技能冷却文本
            if (qBurstCooldownText != null)
            {
                qBurstCooldownText.text = string.Empty;
                qBurstCooldownText.enabled = false;
            }

            // 7.恢复按键提示颜色
            if (eKeyImage != null)
            {
                eKeyImage.color = normalColor;
            }

            if (qKeyImage != null)
            {
                qKeyImage.color = normalColor;
            }
        }
        // 缓存技能图标原始锚点位置
        private void CacheSkillIconBaseAnchoredPositions()
        {
            // 1.记录E技能图标Scene原始位置
            if (eSkillIconImage != null && eSkillIconImage.rectTransform != null)
            {
                eSkillIconBaseAnchoredPosition = eSkillIconImage.rectTransform.anchoredPosition;
            }

            // 2.记录Q技能图标Scene原始位置
            if (qBurstIconImage != null && qBurstIconImage.rectTransform != null)
            {
                qBurstIconBaseAnchoredPosition = qBurstIconImage.rectTransform.anchoredPosition;
            }
        }

        // 应用技能图标配置
        private void ApplySkillIconLayout(Image targetImage, UIIconLayoutData layoutData, Vector2 baseAnchoredPosition)
        {
            // 1.目标图片为空时无法刷新
            if (targetImage == null)
            {
                return;
            }

            // 2.配置为空或图标为空时，清空当前技能图标，避免残留上一名角色的图标
            if (layoutData == null || layoutData.sprite == null)
            {
                targetImage.sprite = null;
                targetImage.enabled = false;
                return;
            }

            // 3.应用技能图标和显示配置
            targetImage.enabled = true;
            targetImage.sprite = layoutData.sprite;
            targetImage.preserveAspect = layoutData.preserveAspect;
            targetImage.color = normalColor;

            // 4.应用尺寸，并把配置位置当作基于Scene原始位置的偏移
            RectTransform rectTransform = targetImage.rectTransform;
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = layoutData.size;
                rectTransform.anchoredPosition = baseAnchoredPosition + layoutData.anchoredPosition;
            }
        }


        // 处理技能图标UI变化事件
        private void HandleSkillIconUIChanged(CharacterContext source, SkillUIType skillType, int iconIndex, float cooldownRemaining)
        {
            // 1.只刷新当前HUD绑定的角色
            if (source == null || source != context)
            {
                return;
            }

            // 2.根据技能类型刷新对应技能槽
            switch (skillType)
            {
                case SkillUIType.ESkill:
                    RefreshESkillUI(iconIndex, cooldownRemaining);
                    break;

                case SkillUIType.QBurst:
                    RefreshQBurstUI(iconIndex, cooldownRemaining);
                    break;
            }
        }

        // 刷新E技能UI
        private void RefreshESkillUI(int iconIndex, float cooldownRemaining)
        {
            // 1.根据索引读取E技能图标配置
            UIIconLayoutData eSkillLayout = eSkillIcons != null
                && iconIndex >= 0
                && iconIndex < eSkillIcons.Length
                ? eSkillIcons[iconIndex]
                : null;

            // 2.应用E技能图标配置
            ApplySkillIconLayout(eSkillIconImage, eSkillLayout, eSkillIconBaseAnchoredPosition);

            // 3.刷新E技能冷却文本
            RefreshSkillCooldownText(eSkillCooldownText, cooldownRemaining);

            // 4.刷新E技能图标颜色
            if (eSkillIconImage != null && eSkillIconImage.enabled)
            {
                eSkillIconImage.color = cooldownRemaining > 0f ? disableColor : normalColor;
            }
        }


        // 刷新Q爆发UI
        private void RefreshQBurstUI(int iconIndex, float cooldownRemaining)
        {
            // 1.根据索引读取Q技能图标配置：-1表示未充能图标
            UIIconLayoutData qBurstLayout = iconIndex < 0
                ? qBurstLockedIcon
                : qBurstReadyIcons != null && iconIndex < qBurstReadyIcons.Length
                    ? qBurstReadyIcons[iconIndex]
                    : null;

            // 2.应用Q技能图标配置
            ApplySkillIconLayout(qBurstIconImage, qBurstLayout, qBurstIconBaseAnchoredPosition);

            // 3.刷新Q技能冷却文本
            RefreshSkillCooldownText(qBurstCooldownText, cooldownRemaining);

            // 4.刷新Q技能图标颜色
            if (qBurstIconImage != null && qBurstIconImage.enabled)
            {
                qBurstIconImage.color = cooldownRemaining > 0f ? disableColor : normalColor;
            }
        }


        // 刷新技能冷却文本
        private void RefreshSkillCooldownText(TextMeshProUGUI cooldownText, float cooldownRemaining)
        {
            // 1.空值检查：没有文本组件时无法刷新
            if (cooldownText == null)
            {
                return;
            }

            // 2.没有冷却时隐藏文本
            if (cooldownRemaining <= 0f)
            {
                cooldownText.text = string.Empty;
                cooldownText.enabled = false;
                return;
            }

            // 3.有冷却时显示保留一位小数的剩余时间
            cooldownText.enabled = true;
            cooldownText.text = cooldownRemaining.ToString("F1");
        }

        // 处理技能图标UI运行时变化事件
        private void HandleSkillIconUIRuntimeChanged(CharacterRuntimeData source, SkillUIType skillType, int iconIndex, float cooldownRemaining)
        {
            // 1.只刷新当前HUD绑定角色的运行时数据
            if (source == null || context == null || source != context.RuntimeData)
            {
                return;
            }

            // 2.根据技能类型刷新对应技能槽
            switch (skillType)
            {
                case SkillUIType.ESkill:
                    RefreshESkillUI(iconIndex, cooldownRemaining);
                    break;

                case SkillUIType.QBurst:
                    RefreshQBurstUI(iconIndex, cooldownRemaining);
                    break;
            }
        }

        #region 刷新技能UI


        #endregion

        #endregion

        #region 生命
        // 清空生命值槽UI显示
        private void ClearHealthSlotUIConfigCache()
        {
            // 1.重置生命值填充
            if (healthFillImage != null)
            {
                healthFillImage.fillAmount = 0f;
            }

            // 2.清空生命值文本
            if (healthText != null)
            {
                healthText.text = string.Empty;
            }
        }

        public void RefreshHealth()
        {
            context.ForceRefreshHealth();
        }
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
            if (healthSlotImage != null)
            {
                healthSlotImage.enabled = true;
            }

            if (healthFillImage != null)
            {
                healthFillImage.enabled = true;
                healthFillImage.fillAmount = Mathf.Clamp01(normalized);
            }

            if (healthText != null)
            {
                healthText.enabled = true;
                healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            }

        }

        #endregion

        #region 体力
        // 清空体力槽UI显示
        private void ClearStaminaSlotUIConfigCache()
        {
            // 不用清空体力槽
        }
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
