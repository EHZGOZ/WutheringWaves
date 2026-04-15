using UnityEngine;

namespace WutheringWaves
{
    public class PlayerController : MonoBehaviour
    {
        #region 核心引用
        [Header("=== 相机引用 ===")]
        [SerializeField] private PlayerCamera playerCamera; // 全局相机控制器（所有角色共用）
        [Header("=== 相机观察点 ===")]
        [SerializeField] private Transform cameraTarget; // 全局相机绑定的观察点/旋转锚点（所有角色共用）
        [Header("=== 输入引用 ===")]
        [SerializeField] private PlayerInputReader playerInputReader;
        [Header("=== 玩家体力 ===")]
        [SerializeField] private PlayerStamina playerStamina;
        [Header("=== 角色引用 ===")]
        [SerializeField] private CharacterFacade defaultCharacterFacade;// 编辑器手动指定的默认角色门面（优先级最高）
        //运行时缓存
        private CharacterFacade _currentCharacterFacade; // 当前受控角色的门面脚本
        private CharacterContext _currentCharacterContext; // 当前受控角色的共享上下文
        #endregion

        #region 对外只读属性
        // 外部脚本只读访问当前角色相机
        public PlayerCamera CurrentPlayerCamera => playerCamera;
        // 外部脚本只读访问相机观察点
        public Transform CurrentCameraTarget => cameraTarget;
        // 外部脚本只读访问当前输入读取器
        public PlayerInputReader CurrentPlayerInputReader => playerInputReader;
        // 外部脚本只读访问当前受控角色门面
        public CharacterFacade CurrentCharacterFacade => _currentCharacterFacade;
        // 外部脚本只读访问当前角色共享上下文
        public CharacterContext CurrentCharacterContext => _currentCharacterContext;
        // 外部脚本只读访问玩家共享体力组件
        public PlayerStamina PlayerStamina => playerStamina;
        
        // 控制器是否完成初始化
        public bool IsInitialized { get; private set; }
        #endregion

        private void Awake()
        {
            //1.自动初始化
                Initialize();
        }

        private void LateUpdate()
        {
            // 晚更新驱动相机：保证角色位置先更新，相机再跟随，避免抖动
            UpdateCurrentPlayerCamera();

        }


        #region 初始化
        // 玩家控制器初始化总入口：绑定默认角色，完成相机配置
        public void Initialize()
        {
            //1.防止重复初始化
            if (IsInitialized)
            {
                return;
            }
            //2.获取玩家相机
            ResolveplayerCamerar();
            //3.获取玩家输入
            ResolvePlayerInputReader();
            //4.初始化共享运行时数据
            ResolvePlayerStamina();
            //5.确定默认受控角色
            ResolveDefaultCharacter();
            //6.绑定角色组件
             BindCurrentCharacter(defaultCharacterFacade);
            //7.标记初始化完成
            IsInitialized = true;
        }
        //2.获取玩家相机
        private void ResolveplayerCamerar()
        {
            if (playerCamera == null)
            {
                playerCamera = GetComponent<PlayerCamera>();
            }
        }

        //3.获取玩家输入
        private void ResolvePlayerInputReader()
        {
            if (playerInputReader == null)
            {
                playerInputReader = GetComponent<PlayerInputReader>();
            }
        }

        //4.解析玩家共享体力组件
        private void ResolvePlayerStamina()
        {
            if (playerStamina == null)
            {
                playerStamina = GetComponent<PlayerStamina>();
            }
        }

        //5.确定默认受控角色
        private void ResolveDefaultCharacter()
        {
            // 编辑器已指定，直接返回
            if (defaultCharacterFacade != null)
            {
                return;
            }

            // 获取所有子节点的角色门面（包含未激活对象）
            CharacterFacade[] facades = GetComponentsInChildren<CharacterFacade>(true);
            for (int i = 0; i < facades.Length; i++)
            {
                CharacterFacade facade = facades[i];
                // 找到第一个激活状态的角色
                if (facade != null && facade.gameObject.activeInHierarchy)
                {
                    defaultCharacterFacade = facade;
                    return;
                }
            }

            // 兜底：没有激活角色时，取第一个角色门面
            if (facades.Length > 0)
            {
                defaultCharacterFacade = facades[0];
            }
        }
        #endregion

        #region 角色管理
        //5.绑定受控角色：同步缓存角色所有核心组件
        public void BindCurrentCharacter(CharacterFacade facade)
        {
            //1.空值校验
            if (facade == null)
             return;
 

            //2.初始化角色门面
            facade.Initialize();

            // 3.同步缓存所有核心组件
            _currentCharacterFacade = facade;
            _currentCharacterContext = facade.Context;

            // 角色绑定完成后，同步玩家层共享体力配置。
            playerStamina?.Initialize(_currentCharacterContext);

            // 切换角色时重新绑定当前角色的输入缓冲，保证玩家输入只写入当前受控角色
            playerInputReader?.BindInputBuffer(facade.Context != null ? facade.Context.InputBuffer : facade.Context.InputBuffer);
            playerInputReader?.Initialize();

            // 配置当前角色的相机参数
            SetupCurrentPlayerCamera();

            // 角色绑定完成后，主动把最新依赖同步给 UI 层
            if (UIRoot.Instance != null)
            {
                UIRoot.Instance.InjectPlayerController(this);
            }
        }
        // 切换受控角色：禁用旧角色，激活新角色并绑定
        public void SwitchCharacter(CharacterFacade nextFacade)
        {
            // 空值/重复切换校验
            if (nextFacade == null || nextFacade == _currentCharacterFacade)
            {
                return;
            }

            // 禁用当前正在操控的角色
            if (_currentCharacterFacade != null)
            {
                _currentCharacterFacade.gameObject.SetActive(false);
            }

            // 激活新角色并绑定
            nextFacade.gameObject.SetActive(true);
            BindCurrentCharacter(nextFacade);
        }
        #endregion

        #region 相机管理
        // 配置角色相机：解析当前角色观察点，并交给相机控制器初始化
        private void SetupCurrentPlayerCamera()
        {
            //1.空值校验
            if (playerCamera == null)
            {
                return;
            }

            //2.自动查找当前角色观察点
            ResolveCameraTarget();

            //3.将当前角色观察点绑定到全局相机控制器
            if (cameraTarget != null)
            {
                playerCamera.BindCameraPivot(cameraTarget);
            }

            //4.初始化相机组件
            playerCamera.Initialize();
        }

        // 自动解析相机观察点
        private void ResolveCameraTarget()
        {
            // 编辑器已指定，直接返回
            if (cameraTarget != null)
            {
                return;
            }
            //先不管相机观察点，我后面有别的想法

        }

        // 统一驱动相机更新：控制器只负责转发输入，相机细节由PlayerCamera内部处理
        public void UpdateCurrentPlayerCamera()
        {
            // 组件空值校验
            if (playerCamera == null || playerInputReader == null)
            {
                return;
            }
            // 更新相机视角（鼠标/摇杆输入）
            playerCamera.UpdateCameraLook(playerInputReader.LookInput);
            // 更新相机缩放（滚轮输入）
            playerCamera.UpdateCameraZoom(playerInputReader.ZoomInput);
        }

        #endregion

    }
}
