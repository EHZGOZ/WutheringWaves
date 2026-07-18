using UnityEngine;

namespace WutheringWaves
{
    // 玩家总控制器：负责玩家组件初始化、流程协调和外部统一访问
    public class PlayerController : MonoBehaviour
    {
        [Header("=== 核心组件 ===")]

        [Header("玩家运行数据")]
        [SerializeField] private PlayerRuntimeData playerRuntimeData;

        [Header("玩家队伍控制器")]
        [SerializeField] private PlayerTeamController playerTeamController;

        [Header("玩家角色切换器")]
        [SerializeField] private PlayerCharacterSwitcher playerCharacterSwitcher;

        [Header("玩家输入读取器")]
        [SerializeField] private PlayerInputReader playerInputReader;

        [Header("玩家相机")]
        [SerializeField] private PlayerCamera playerCamera;

        [Header("玩家共享体力")]
        [SerializeField] private PlayerStamina playerStamina;

        #region 外部访问
        public PlayerRuntimeData PlayerRuntimeData => playerRuntimeData;
        public PlayerTeamController PlayerTeamController => playerTeamController;
        public PlayerCharacterSwitcher PlayerCharacterSwitcher => playerCharacterSwitcher;
        public PlayerInputReader CurrentPlayerInputReader => playerInputReader;
        public PlayerCamera CurrentPlayerCamera => playerCamera;
        public PlayerStamina PlayerStamina => playerStamina;

        // 兼容现有PlayerRuntimeData的数组访问方式
        // 返回的是数组快照，外部修改不会影响PlayerTeamController中的真实队伍
        public CharacterContext[] TeamCharacters
        {
            get
            {
                // 1.队伍控制器为空时返回空数组
                if (playerTeamController == null
                    || playerTeamController.TeamCharacterCount == 0)
                {
                    return new CharacterContext[0];
                }

                // 2.复制当前队伍角色引用
                CharacterContext[] teamCharacters =
                    new CharacterContext[playerTeamController.TeamCharacterCount];

                for (int i = 0; i < teamCharacters.Length; i++)
                {
                    teamCharacters[i] = playerTeamController.GetCharacter(i);
                }

                return teamCharacters;
            }
        }

        // 当前受控角色由PlayerCharacterSwitcher唯一保存
        public CharacterContext CurrentCharacterContext =>
            playerCharacterSwitcher != null
                ? playerCharacterSwitcher.CurrentCharacterContext
                : null;
        #endregion

        #region 生命周期
        private void Awake()
        {
            // 玩家根节点切换场景时不销毁
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            // 统一推进所有队伍角色的运行时数据
            playerTeamController?.UpdateTeamCharacterRuntimeData(Time.deltaTime);
        }

        private void LateUpdate()
        {
            // 角色完成移动后再更新相机，减少跟随抖动
            UpdateCurrentPlayerCamera();
        }
        #endregion

        #region 初始化
        // 初始化玩家对象：新建存档或者读取存档后调用
        public void Initialize()
        {
            // 1.解析、校验并初始化全部玩家组件
            if (!ResolveDependencies())
            {
                Debug.LogError("[PlayerController] 玩家初始化失败：核心组件不完整。", this);
                return;
            }

            // 2.生成新队伍前清除当前角色绑定
            playerCharacterSwitcher.ClearCurrentCharacterBinding();

            // 3.由队伍控制器生成并初始化完整队伍
            if (!playerTeamController.SpawnTeam())
            {
                Debug.LogError("[PlayerController] 玩家初始化失败：队伍生成失败。", this);
                return;
            }

            // 4.由角色切换器绑定新建或读档后的初始角色
            if (!playerCharacterSwitcher.BindInitialCharacter())
            {
                Debug.LogError("[PlayerController] 玩家初始化失败：初始角色绑定失败。", this);

                // 绑定失败时清理本次生成的队伍，避免留下残缺对象
                playerTeamController.ClearTeamCharacters();
                return;
            }

            // 5.把玩家控制器绑定到UIRoot，供菜单和输入流程访问
            UIRoot.Instance?.Bind(this);
        }

        // 解析玩家核心依赖
        private bool ResolveDependencies()
        {
            // 1.获取玩家根节点上的全部核心组件
            ResolveComponent();

            // 2.校验组件是否完整
            if (!ValidateComponent())
            {
                return false;
            }

            // 3.按照依赖顺序初始化组件
            return InitializeComponent();
        }

        // 获取玩家根节点上的核心组件
        private void ResolveComponent()
        {
            if (playerRuntimeData == null)
            {
                playerRuntimeData = GetComponent<PlayerRuntimeData>();
            }

            if (playerTeamController == null)
            {
                playerTeamController = GetComponent<PlayerTeamController>();
            }

            if (playerCharacterSwitcher == null)
            {
                playerCharacterSwitcher = GetComponent<PlayerCharacterSwitcher>();
            }

            if (playerInputReader == null)
            {
                playerInputReader = GetComponent<PlayerInputReader>();
            }

            if (playerCamera == null)
            {
                playerCamera = GetComponent<PlayerCamera>();
            }

            if (playerStamina == null)
            {
                playerStamina = GetComponent<PlayerStamina>();
            }
        }

        // 校验玩家控制器需要的全部核心组件
        private bool ValidateComponent()
        {
            bool isValid = true;

            // 1.校验玩家运行时数据
            if (playerRuntimeData == null)
            {
                Debug.LogError("[PlayerController] 缺少PlayerRuntimeData组件。", this);
                isValid = false;
            }

            // 2.校验玩家队伍控制器
            if (playerTeamController == null)
            {
                Debug.LogError("[PlayerController] 缺少PlayerTeamController组件。", this);
                isValid = false;
            }

            // 3.校验玩家角色切换器
            if (playerCharacterSwitcher == null)
            {
                Debug.LogError("[PlayerController] 缺少PlayerCharacterSwitcher组件。", this);
                isValid = false;
            }

            // 4.校验玩家输入读取器
            if (playerInputReader == null)
            {
                Debug.LogError("[PlayerController] 缺少PlayerInputReader组件。", this);
                isValid = false;
            }

            // 5.校验玩家相机控制器
            if (playerCamera == null)
            {
                Debug.LogError("[PlayerController] 缺少PlayerCamera组件。", this);
                isValid = false;
            }

            // 6.校验玩家共享体力
            if (playerStamina == null)
            {
                Debug.LogError("[PlayerController] 缺少PlayerStamina组件。", this);
                isValid = false;
            }

            return isValid;
        }

        // 按照依赖顺序初始化玩家组件
        private bool InitializeComponent()
        {
            // 1.运行数据仍需要PlayerController作为场景数据收集入口
            playerRuntimeData.Initialize(this);

            // 2.初始化输入、相机和共享体力
            playerInputReader.Initialize();
            playerCamera.Initialize();
            playerStamina.Initialize();

            // 3.初始化队伍控制器
            playerTeamController.Initialize(
                playerRuntimeData,
                transform
            );

            if (!playerTeamController.IsInitialized)
            {
                Debug.LogError("[PlayerController] PlayerTeamController初始化失败。", this);
                return false;
            }

            // 4.最后初始化角色切换器
            // 此时它依赖的队伍、运行数据、输入和相机都已经准备完成
            playerCharacterSwitcher.Initialize(
                playerTeamController,
                playerRuntimeData,
                playerInputReader,
                playerCamera
            );

            if (!playerCharacterSwitcher.IsInitialized)
            {
                Debug.LogError("[PlayerController] PlayerCharacterSwitcher初始化失败。", this);
                return false;
            }

            return true;
        }
        #endregion

        #region 清理玩家队伍
        // 清理当前角色绑定和全部队伍角色
        public void ClearCurrentTeamCharacters()
        {
            // 1.先解除输入、相机和当前角色绑定
            playerCharacterSwitcher?.ClearCurrentCharacterBinding();

            // 2.再销毁队伍角色，避免其他模块继续引用已销毁对象
            playerTeamController?.ClearTeamCharacters();
        }
        #endregion

        #region 相机更新
        // 统一驱动玩家相机更新
        public void UpdateCurrentPlayerCamera()
        {
            // 1.核心组件不完整时不更新相机
            if (playerCamera == null || playerInputReader == null)
            {
                return;
            }

            // 2.更新相机视角
            playerCamera.UpdateCameraLook(playerInputReader.LookInput);

            // 3.更新相机缩放
            playerCamera.UpdateCameraZoom(playerInputReader.ZoomInput);
        }
        #endregion
    }
}