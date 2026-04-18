using System.Collections.Generic;
using UnityEngine;

namespace WutheringWaves
{
    public class PlayerController : MonoBehaviour
    {

        [Header(" 玩家数据")]
        [SerializeField] private PlayerRuntimeData playerRuntimeData; // 玩家数据

        [Header("相机引用 ")]
        [SerializeField] private PlayerCamera playerCamera; // 全局相机控制器（所有角色共用）
        [Header("相机观察点")]
        [SerializeField] private Transform cameraTarget; // 全局相机绑定的观察点/旋转锚点（所有角色共用）
        [Header(" 输入引用 ")]
        [SerializeField] private PlayerInputReader playerInputReader;
        [Header("玩家体力")]
        [SerializeField] private PlayerStamina playerStamina;
        [Header("队伍角色列表")]
        [SerializeField] private CharacterFacade[] teamCharacters; // 队伍内可切换角色，1/2/3按顺序对应数组索引
        [Header("默认受控角色槽位")]
        [SerializeField] private int defaultCharacterIndex = 0; // 默认操控角色索引
        [Header("当前受控角色的门面脚本(不可修改)")]
        [SerializeField] private CharacterFacade _currentCharacterFacade; // 当前受控角色的门面脚本
        [Header("当前受控角色的共享上下文(不可修改)")]
        [SerializeField] private CharacterContext _currentCharacterContext; // 当前受控角色的共享上下文
        private int _currentCharacterIndex = -1; // 当前受控角色索引


        #region 对外只读属性
        // 外部脚本只读访问当前角色相机
        public PlayerCamera CurrentPlayerCamera => playerCamera;
        // 外部脚本只读访问相机观察点
        public Transform CurrentCameraTarget => cameraTarget;
        // 外部脚本只读访问当前输入读取器
        public PlayerInputReader CurrentPlayerInputReader => playerInputReader;
        // 外部脚本只读访问玩家共享体力组件
        public PlayerStamina PlayerStamina => playerStamina;
        // 外部脚本只读访问队伍角色列表
        public CharacterFacade[] TeamCharacters => teamCharacters;
        // 外部脚本只读访问当前受控角色索引
        public int CurrentCharacterIndex => _currentCharacterIndex;
        // 外部脚本只读访问当前受控角色门面
        public CharacterFacade CurrentCharacterFacade => _currentCharacterFacade;
        // 外部脚本只读访问当前角色共享上下文
        public CharacterContext CurrentCharacterContext => _currentCharacterContext;

        #endregion


        #region 生命周期

        private void OnDestroy()
        {
            // 控制器销毁时解绑输入事件，避免残留委托引用
            UnsubscribeInputEvents();
        }

        private void LateUpdate()
        {
            // 晚更新驱动相机：保证角色位置先更新，相机再跟随，避免抖动
            UpdateCurrentPlayerCamera();

        }
        private void OnDisable()
        {

        }
        private void OnApplicationQuit()
        {

        }
        #endregion


        #region 初始化组件
        public void Injected(PlayerRuntimeData playerRuntimeData)
        {
            this.playerRuntimeData = playerRuntimeData;
        }

        // 玩家控制器初始化总入口：绑定默认角色，完成相机配置
        public void Initialize()
        {
            //1.订阅输入事件
            SubscribeInputEvents();
            //2.获取玩家输入
            ResolvePlayerInputReader();
            //3.获取玩家共享体力
            ResolvePlayerStamina();

            //1.获取玩家相机
            ResolveplayerCamerar();
            //5.解析队伍角色列表
            ResolveTeamCharacters();
            

            //8.初始化队伍激活状态
            SetupInitialTeamState();
            //9.绑定角色组件
            BindCurrentCharacter(teamCharacters[_currentCharacterIndex]);

        }
     
        #endregion

        #region 事件订阅
        //1.订阅输入层切人事件：由玩家输入统一驱动角色切换
        private void SubscribeInputEvents()
        {
            if (playerInputReader != null)
            {
                playerInputReader.OnSwitchCharacterRequested -= HandleSwitchCharacterRequest;
                playerInputReader.OnSwitchCharacterRequested += HandleSwitchCharacterRequest;
            }
        }
        // 解绑输入层切人事件：防止对象销毁后残留订阅
        private void UnsubscribeInputEvents()
        {
            if (playerInputReader != null)
            {
                playerInputReader.OnSwitchCharacterRequested -= HandleSwitchCharacterRequest;
            }
        }
        #endregion

        #region 获取玩家输入
        //获取玩家输入
        private void ResolvePlayerInputReader()
        {
            if (playerInputReader == null)
            {
                playerInputReader = GetComponent<PlayerInputReader>();
            }
        }
        #endregion

        #region 获取玩家共享体力
        //获取玩家共享体力
        private void ResolvePlayerStamina()
        {
            if (playerStamina == null)
            {
                playerStamina = GetComponent<PlayerStamina>();
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
            _currentCharacterIndex = ResolveCharacterIndex(facade);
            facade.SetPlayerControlled(true);

            // 角色绑定完成后，同步玩家层共享体力配置。
            playerStamina?.Initialize(_currentCharacterContext);

            // 切换角色时重新绑定当前角色的输入缓冲，保证玩家输入只写入当前受控角色
            playerInputReader?.BindInputBuffer(facade.Context != null ? facade.Context.InputBuffer : null);
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

            // 当前状态不可打断时，不允许切人，避免战斗状态被错误截断
            if (!CanSwitchCharacter())
            {
                return;
            }

            CharacterFacade previousFacade = _currentCharacterFacade;

            // 切人前让新角色先同步到旧角色位置，保证切人落点一致
            SyncNextCharacterTransform(nextFacade);

            // 旧角色切出时先做输入清理，再关闭对象
            previousFacade?.OnSwitchOut();

            // 禁用当前正在操控的角色
            if (previousFacade != null)
            {
                previousFacade.SetPlayerControlled(false);
                previousFacade.gameObject.SetActive(false);
            }

            // 激活新角色并绑定
            nextFacade.gameObject.SetActive(true);
            BindCurrentCharacter(nextFacade);
            nextFacade.OnSwitchIn();

            // 广播切人事件，供UI/特效/小地图等外围系统同步刷新
            GameEvents.RaiseCharacterSwitched(previousFacade, nextFacade);
        }

        // 按槽位切人：外部输入层统一通过 0 基索引调用
        public void SwitchCharacterByIndex(int index)
        {
            if (teamCharacters == null || teamCharacters.Length == 0)
            {
                return;
            }

            if (index < 0 || index >= teamCharacters.Length)
            {
                return;
            }

            CharacterFacade nextFacade = teamCharacters[index];
            if (nextFacade == null)
            {
                return;
            }

            SwitchCharacter(nextFacade);
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
            if (_currentCharacterFacade == null)
            {
                cameraTarget = null;
                return;
            }

            // 每次切人都重新绑定当前角色观察点，避免沿用旧角色相机锚点
            cameraTarget = _currentCharacterFacade.CameraTarget;
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





        // 处理输入层发出的切人请求：输入槽位为 1/2/3，这里统一换算成数组索引
        private void HandleSwitchCharacterRequest(int slot)
        {
            SwitchCharacterByIndex(slot - 1);
        }

        // 判断当前角色是否允许切人：当前状态可打断时才放行
        private bool CanSwitchCharacter()
        {
            if (_currentCharacterContext == null || _currentCharacterContext.StateMachine == null)
            {
                return true;
            }

            return _currentCharacterContext.StateMachine.IsInterruptible();
        }

        // 切人时同步新角色的位置和朝向：保证切换后角色无缝接手当前站位
        private void SyncNextCharacterTransform(CharacterFacade nextFacade)
        {
            if (_currentCharacterFacade == null || nextFacade == null)
            {
                return;
            }

            nextFacade.transform.position = _currentCharacterFacade.transform.position;
            nextFacade.transform.rotation = _currentCharacterFacade.transform.rotation;
        }

        // 解析角色在队伍中的索引：便于后续UI/逻辑按槽位读取当前角色
        private int ResolveCharacterIndex(CharacterFacade facade)
        {
            if (teamCharacters == null || facade == null)
            {
                return -1;
            }

            for (int i = 0; i < teamCharacters.Length; i++)
            {
                if (teamCharacters[i] == facade)
                {
                    return i;
                }
            }

            return -1;
        }

        #endregion

        //2.获取玩家相机
        private void ResolveplayerCamerar()
        {
            if (playerCamera == null)
            {
                playerCamera = GetComponent<PlayerCamera>();
            }
        }

        

        //4.1 解析队伍角色列表：优先使用编辑器配置，缺失时自动从子节点收集
        private void ResolveTeamCharacters()
        {
            if (teamCharacters != null && teamCharacters.Length > 0)
            {
                return;
            }

            teamCharacters = GetComponentsInChildren<CharacterFacade>(true);
        }

        



        // 初始化队伍激活状态：默认角色激活，其他角色隐藏并标记为非受控
        private void SetupInitialTeamState()
        {
            //if (teamCharacters == null || teamCharacters.Length == 0)
            //{
            //    return;
            //}

            //for (int i = 0; i < teamCharacters.Length; i++)
            //{
            //    CharacterFacade facade = teamCharacters[i];
            //    if (facade == null)
            //    {
            //        continue;
            //    }

            //    bool isDefaultCharacter = facade == defaultCharacterFacade;
            //    facade.gameObject.SetActive(isDefaultCharacter);
            //    facade.SetPlayerControlled(false);
            //}
        }



    }
}
