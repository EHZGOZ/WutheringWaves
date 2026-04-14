using UnityEngine;

namespace WutheringWaves
{
    [DisallowMultipleComponent]
    // 角色HUD控制器：负责管理血条、体力条和技能UI的可见性与依赖注入
    public class CharacterHUDController : MonoBehaviour
    {
        [Header("HUD Root")]
        [SerializeField] private GameObject hudPanel; // HUD根节点

        [Header("HUD Widgets")]
        [SerializeField] private GameObject playerHealthbar; // 玩家血条
        [SerializeField] private GameObject playerStaminabar; // 玩家体力条
        [SerializeField] private GameObject healthPotionUI; // 药水UI
        [SerializeField] private GameObject enemyHealthSlider; // 敌人血条
        [SerializeField] private GameObject enemyHealthText; // 敌人血量文本
        [SerializeField] private GameObject enemyStunBar; // 敌人韧性条
        [SerializeField] private CharacterSkillUI[] skillUIs; // 技能UI集合

        private CharacterFacade facade; // 当前绑定的角色门面
        private UIRoot uiRoot; // UI根节点引用
        private bool initialized; // 是否已完成初始化

        public void ConfigureIfMissing(
            GameObject panel,
            GameObject playerHp,
            GameObject playerStamina,
            GameObject healthPotion,
            GameObject enemyHpSlider,
            GameObject enemyHpText,
            GameObject enemyStun)
        {
            // 兼容旧场景拖线方式：仅在字段为空时回填引用
            if (hudPanel == null) hudPanel = panel;
            if (playerHealthbar == null) playerHealthbar = playerHp;
            if (playerStaminabar == null) playerStaminabar = playerStamina;
            if (healthPotionUI == null) healthPotionUI = healthPotion;
            if (enemyHealthSlider == null) enemyHealthSlider = enemyHpSlider;
            if (enemyHealthText == null) enemyHealthText = enemyHpText;
            if (enemyStunBar == null) enemyStunBar = enemyStun;
        }

        public void Initialize(CharacterFacade injectedFacade, UIRoot injectedRoot)
        {
            // 初始化时缓存依赖，并把依赖继续下发到各个技能UI
            facade = injectedFacade;
            uiRoot = injectedRoot;
            CacheSkillUIs();
            InjectDependencies();
            initialized = true;
        }

        public void SetVisible(bool visible)
        {
            // HUD显隐统一通过根节点控制
            if (hudPanel != null)
            {
                hudPanel.SetActive(visible);
            }
        }

        public void SetCharacterFacade(CharacterFacade injectedFacade)
        {
            // 角色对象变化时，及时把新依赖同步给子UI
            facade = injectedFacade;
            InjectDependencies();
        }

        // 兼容旧链路：允许外部仍按旧接口传入CharacterCore
        public void SetCharacterCore(CharacterCore injectedCore)
        {
            if (injectedCore == null)
            {
                return;
            }

            SetCharacterFacade(injectedCore.Facade != null ? injectedCore.Facade : injectedCore.GetComponent<CharacterFacade>());
        }

        private void Awake()
        {
            CacheSkillUIs();
        }

        private void OnEnable()
        {
            if (initialized)
            {
                InjectDependencies();
            }
        }

        private void CacheSkillUIs()
        {
            // 未显式拖拽时自动从子节点收集技能UI组件
            if (skillUIs == null || skillUIs.Length == 0)
            {
                skillUIs = GetComponentsInChildren<CharacterSkillUI>(true);
            }
        }

        private void InjectDependencies()
        {
            // 技能UI缺失时直接返回，避免空数组遍历
            if (skillUIs == null || skillUIs.Length == 0)
            {
                return;
            }

            for (int i = 0; i < skillUIs.Length; i++)
            {
                CharacterSkillUI skillUI = skillUIs[i];
                if (skillUI != null)
                {
                    skillUI.InjectDependencies(uiRoot, facade);
                }
            }
        }
    }
}
