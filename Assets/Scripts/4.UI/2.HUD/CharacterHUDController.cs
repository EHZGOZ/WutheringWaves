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

        #region 技能UI引用
        [Header("=== 队伍头像UI ===")]
        [SerializeField] private TeamAvatarSlotUI[] teamAvatarSlots = new TeamAvatarSlotUI[3]; // 三人队伍头像槽位

        [Header("=== 技能图标 ===")]
        [SerializeField] private Image eSkillIcon; // 当前E技能图标
        [SerializeField] private Image qBurstIcon; // 当前Q技能图标

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
        [SerializeField] private Camera uiFollowCamera; // 体力槽跟随使用的相机
        [SerializeField] private Vector3 staminaWorldOffset = Vector3.zero; // 体力槽世界偏移
        [SerializeField] private Vector2 staminaScreenOffset = new Vector2(40f, 0f); // 体力槽屏幕偏移
        [SerializeField] [Min(0f)] private float staminaFollowSmooth = 18f; // 体力槽跟随平滑
        [SerializeField] [Range(0f, 1f)] private float staminaHiddenAlpha = 0f; // 体力槽隐藏透明度
        [SerializeField] [Min(0f)] private float staminaFadeSpeed = 8f; // 体力槽淡入淡出速度
        #endregion

        private UIRoot uiRoot; // UI根节点引用
        private CharacterAttack attackLogic; // 当前角色攻击逻辑
        private JinxiSpecialSkillLinker jinxiSpecialSkillLinker; // 今汐专属技能逻辑
        private PlayerStamina playerStamina; // 玩家共享体力逻辑
        private bool hasSubscribedAttackEvent; // 是否已经订阅技能刷新事件
        private bool hasSubscribedStaminaEvent; // 是否已经订阅体力刷新事件
        private bool hasSubscribedHealthEvent; // 是否已经订阅生命值刷新事件

        #region 生命周期
        private void OnDestroy()
        {
            UnsubscribeHealthEvent();
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

            // 2.缓存当前角色上下文
            context = injectedContext;

            // 3.刷新HUD静态显示
            RefreshHUD();

            // 4.主动刷新当前角色生命值，避免刚绑定时UI还是旧角色血量
            context.ForceRefreshHealth();
        }

        #endregion

        #region 初始化
        // 初始化角色HUD：只处理一次性依赖和事件订阅
        public void Initialize(UIRoot uiRoot)
        {
            this.uiRoot = uiRoot;

            // HUD初始化时订阅生命值变化事件
            SubscribeHealthEvent();
        }

        #endregion

        #region 事件订阅
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

        #region 生命值UI刷新
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

        #region 刷新HUD图标
        // 刷新HUD显示：用于切人、队伍变化或运行时数据变化后调用
        public void RefreshHUD()
        {
            RefreshHUDStaticIcons();
        }
        // 刷新HUD静态图标：队伍头像、E技能图标、Q爆发图标
        private void RefreshHUDStaticIcons()
        {
            RefreshTeamAvatarIcons();
            RefreshESkillIcon();
            RefreshQBurstIcon();
        }

        // 刷新队伍头像：根据PlayerRuntimeData中的队伍槽位显示三人头像
        private void RefreshTeamAvatarIcons()
        {
            // 1.头像槽位为空时直接返回
            if (teamAvatarSlots == null || teamAvatarSlots.Length == 0)
            {
                return;
            }

            // 2.获取玩家运行时数据
            PlayerRuntimeData playerRuntimeData = context != null && context.PlayerController != null
                ? context.PlayerController.PlayerRuntimeData
                : null;

            // 3.没有队伍数据时，隐藏全部头像槽位
            if (playerRuntimeData == null || playerRuntimeData.teamSlots == null)
            {
                SetAllAvatarSlotsVisible(false);
                return;
            }

            // 4.逐个刷新头像槽位
            for (int i = 0; i < teamAvatarSlots.Length; i++)
            {
                TeamAvatarSlotUI slotUI = teamAvatarSlots[i];
                if (slotUI == null)
                {
                    continue;
                }

                // 5.队伍没有这个槽位，隐藏
                if (i >= playerRuntimeData.teamSlots.Count)
                {
                    SetAvatarSlotVisible(slotUI, false);
                    continue;
                }

                TeamCharacterSlotData slotData = playerRuntimeData.teamSlots[i];
                if (slotData == null)
                {
                    SetAvatarSlotVisible(slotUI, false);
                    continue;
                }

                // 6.根据角色名解析角色配置
                CharacterDataSO characterDataSO = ResolveCharacterDataSO(slotData.characterName);
                if (characterDataSO == null || characterDataSO.avatarIcon == null)
                {
                    SetAvatarSlotVisible(slotUI, false);
                    continue;
                }

                // 7.显示头像槽位并设置头像
                SetAvatarSlotVisible(slotUI, true);

                if (slotUI.avatarIcon != null)
                {
                    slotUI.avatarIcon.sprite = characterDataSO.avatarIcon;
                    slotUI.avatarIcon.color = normalColor;
                }

                // 8.显示当前受控角色底框
                if (slotUI.selectedFrame != null)
                {
                    slotUI.selectedFrame.SetActive(i == playerRuntimeData.currentCharacterIndex);
                }
            }
        }

        // 刷新E技能默认图标：切换角色或绑定角色时调用
        private void RefreshESkillIcon()
        {
            // 1.空值检查
            if (context == null || context.CharacterDataSO == null || eSkillIcon == null)
            {
                return;
            }

            // 2.读取角色E技能图标列表
            Sprite[] icons = context.CharacterDataSO.eSkillIcons;
            if (icons == null || icons.Length == 0 || icons[0] == null)
            {
                return;
            }

            // 3.默认先显示第0张E技能图标
            eSkillIcon.sprite = icons[0];
            eSkillIcon.color = normalColor;
        }

        // 刷新Q爆发默认图标：切换角色或绑定角色时调用
        private void RefreshQBurstIcon()
        {
            // 1.空值检查
            if (context == null || context.CharacterDataSO == null || qBurstIcon == null)
            {
                return;
            }

            // 2.优先读取Q技能可释放图标列表
            Sprite[] readyIcons = context.CharacterDataSO.qBurstReadyIcons;
            if (readyIcons != null && readyIcons.Length > 0 && readyIcons[0] != null)
            {
                qBurstIcon.sprite = readyIcons[0];
                qBurstIcon.color = normalColor;
                return;
            }

            // 3.没有可释放图标时，兜底显示未充能图标
            Sprite lockedIcon = context.CharacterDataSO.qBurstLockedIcon;
            if (lockedIcon != null)
            {
                qBurstIcon.sprite = lockedIcon;
                qBurstIcon.color = disableColor;
            }
        }
        #endregion

        #region 根据角色数据更换角色技能图片
        // 根据角色名称解析角色配置
        private CharacterDataSO ResolveCharacterDataSO(CharacterName characterName)
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
            return prefabContext != null ? prefabContext.CharacterDataSO : null;
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
        // 隐藏或显示所有头像槽位
        private void SetAllAvatarSlotsVisible(bool visible)
        {
            if (teamAvatarSlots == null)
            {
                return;
            }

            for (int i = 0; i < teamAvatarSlots.Length; i++)
            {
                TeamAvatarSlotUI slotUI = teamAvatarSlots[i];
                if (slotUI == null)
                {
                    continue;
                }

                SetAvatarSlotVisible(slotUI, visible);
            }
        }

        // 设置单个头像槽位显隐
        private void SetAvatarSlotVisible(TeamAvatarSlotUI slotUI, bool visible)
        {
            if (slotUI == null)
            {
                return;
            }

            if (slotUI.root != null)
            {
                slotUI.root.SetActive(visible);
            }

            if (!visible && slotUI.selectedFrame != null)
            {
                slotUI.selectedFrame.SetActive(false);
            }
        }


        #endregion
    }
}
