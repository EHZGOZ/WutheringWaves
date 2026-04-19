using System.Collections.Generic;
using UnityEngine;

namespace WutheringWaves
{
    public class PlayerController : MonoBehaviour
    {

        [Header(" 玩家数据")]
        [SerializeField] private PlayerRuntimeData playerRuntimeData; // 玩家数据
        [Header(" 输入引用 ")]
        [SerializeField] private PlayerInputReader playerInputReader;//玩家输入
        [Header("玩家体力")]
        [SerializeField] private PlayerStamina playerStamina;//玩家共享体力
        [Header("相机引用 ")]
        [SerializeField] private PlayerCamera playerCamera; // 全局相机控制器（所有角色共用）
        [Header("相机观察点")]
        [SerializeField] private Transform cameraTarget; // 全局相机绑定的观察点/旋转锚点（所有角色共用）
        
        [Header("队伍角色列表")]
        [SerializeField] private CharacterContext[] teamCharacters; // 队伍内可切换角色，1/2/3按顺序对应数组索引
        [Header("默认受控角色槽位")]
        [SerializeField] private int defaultCharacterIndex = 0; // 默认操控角色索引

        [Header("当前受控角色的共享上下文(不可修改)")]
        [SerializeField] private CharacterContext _currentCharacterContext; // 当前受控角色的共享上下文


        #region 对外只读属性
        // 外部脚本只读访问当前角色相机
        public PlayerRuntimeData PlayerRuntimeData => playerRuntimeData;
        // 外部脚本只读访问当前输入读取器
        public PlayerInputReader CurrentPlayerInputReader => playerInputReader;
        // 外部脚本只读访问玩家共享体力组件
        public PlayerStamina PlayerStamina => playerStamina;
        // 外部脚本只读访问当前角色相机
        public PlayerCamera CurrentPlayerCamera => playerCamera;
        // 外部脚本只读访问相机观察点
        public Transform CurrentCameraTarget => cameraTarget;
        
        // 外部脚本只读访问队伍角色列表
        public CharacterContext[] TeamCharacters => teamCharacters;
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
            //1.获取玩家输入
            ResolvePlayerInputReader();
            //2.订阅输入事件
            SubscribeInputEvents();
            //3.获取玩家共享体力
            ResolvePlayerStamina();
            //4.获取玩家相机
            ResolveplayerCamerar();
            //5.同步运行数据
            ResolvePlayerRuntimeData();  
            //6.根据玩家运行时数据生成队伍角色
            SpawnCharacter();
            //7.绑定受控角色：同步缓存角色所有核心组件
            BindCurrentCharacter();
            //8.解析当前角色观察点
            SetupCurrentPlayerCamera();


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
            playerInputReader.Initialize();
        }
        #endregion

        #region 事件订阅
        //1.订阅输入层切人事件：由玩家输入统一驱动角色切换
        private void SubscribeInputEvents()
        {
            //if (playerInputReader != null)
            //{
            //    playerInputReader.OnSwitchCharacterRequested -= HandleSwitchCharacterRequest;
            //    playerInputReader.OnSwitchCharacterRequested += HandleSwitchCharacterRequest;
            //}
        }
        // 解绑输入层切人事件：防止对象销毁后残留订阅
        private void UnsubscribeInputEvents()
        {
            //if (playerInputReader != null)
            //{
            //    playerInputReader.OnSwitchCharacterRequested -= HandleSwitchCharacterRequest;
            //}
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
            playerStamina.Initialize();
        }
        #endregion

        #region 获取玩家相机
        //获取玩家相机
        private void ResolveplayerCamerar()
        {
            if (playerCamera == null)
            {
                playerCamera = GetComponent<PlayerCamera>();
            }
            
        }

        #endregion

        #region 同步运行数据
        //同步运行数据
        private void ResolvePlayerRuntimeData()
        {
            if (playerRuntimeData == null)
            {
                playerRuntimeData = GetComponent<PlayerRuntimeData>();
            }

            if (playerRuntimeData == null || SaveService.Instance == null)
            {
                return;
            }

            playerRuntimeData.SyncRuntimeDataFromSaveData(SaveService.Instance.Load());
        }
        #endregion

        #region 生成玩家所操控的角色
        private void SpawnCharacter()
        {
            //1.空值校验
            if (playerRuntimeData == null || playerRuntimeData.teamSlots == null|| playerRuntimeData.teamSlots.Count == 0|| GameBootstrap.Instance == null)
            {
                Debug.LogError("[PlayerController] 无法生成队伍角色。", this);
                return;
            }

            // 已存在队伍角色时不重复生成，避免重复初始化时刷出多套角色
            CharacterContext[] existingCharacters = GetComponentsInChildren<CharacterContext>(true);
            if (existingCharacters != null && existingCharacters.Length > 0)
            {
                teamCharacters = existingCharacters;
                return;
            }

            //2.清理旧队伍缓存，后续重新收集生成出来的角色
            teamCharacters = null;

            //3.根据运行时数据中的队伍槽位逐个生成角色
            List<CharacterContext> spawnedCharacters = new List<CharacterContext>();
            for (int i = 0; i < playerRuntimeData.teamSlots.Count; i++)
            {
                TeamCharacterSlotData slotData = playerRuntimeData.teamSlots[i];
                if (slotData == null)
                {
                    continue;
                }

                GameObject character = GameBootstrap.Instance.SpawnCharacter(
                    slotData.characterName,
                    playerRuntimeData.playerPosition,
                    Quaternion.Euler(playerRuntimeData.playerEulerAngles),
                    transform
                );

                if (character == null)
                {
                    continue;
                }

                //4.生成后收集角色上下文，保证teamCharacters顺序和teamSlots槽位顺序一致
                CharacterContext context = character.GetComponent<CharacterContext>();
                if (context == null)
                {
                    Debug.LogError($"[PlayerController] 角色 {slotData.characterName} 缺少CharacterContext组件。", character);
                    continue;
                }

                context.gameObject.SetActive(false);
                spawnedCharacters.Add(context);
            }

            //5.写回队伍角色列表，后续切人时按相同索引访问
            teamCharacters = spawnedCharacters.ToArray();
        }
        #endregion

        #region 绑定受控角色：同步缓存角色所有核心组件
        private void BindCurrentCharacter()
        {
            //1.空值校验
            if (teamCharacters == null || teamCharacters.Length == 0)
            {
                return;
            }

            //2.根据玩家运行时数据确定当前受控角色槽位
            int targetIndex = playerRuntimeData != null
                ? Mathf.Clamp(playerRuntimeData.currentCharacterIndex, 0, teamCharacters.Length - 1)
                : Mathf.Clamp(defaultCharacterIndex, 0, teamCharacters.Length - 1);

            //3.按槽位绑定当前受控角色
            BindCurrentCharacter(teamCharacters[targetIndex]);
        }
        //绑定受控角色：同步缓存角色所有核心组件
        public void BindCurrentCharacter(CharacterContext context)
        {
            //1.空值校验
            if (context == null)
                return;

            //2.解析当前角色在队伍中的槽位索引
            int characterIndex = -1;
            if (teamCharacters != null)
            {
                for (int i = 0; i < teamCharacters.Length; i++)
                {
                    if (teamCharacters[i] == context)
                    {
                        characterIndex = i;
                        break;
                    }
                }
            }

            //3.同步玩家运行时当前受控槽位
            if (playerRuntimeData != null && characterIndex >= 0)
            {
                playerRuntimeData.currentCharacterIndex = characterIndex;
            }

            //4.获取当前槽位对应的角色运行时数据
            CharacterRuntimeData runtimeData = null;
            if (playerRuntimeData != null && playerRuntimeData.teamSlots != null && characterIndex >= 0 && characterIndex < playerRuntimeData.teamSlots.Count)
            {
                TeamCharacterSlotData slotData = playerRuntimeData.teamSlots[characterIndex];
                if (slotData != null)
                {
                    runtimeData = slotData.runtimeData;
                }
            }

            //5.只激活当前受控角色，关闭其他队伍角色
            if (teamCharacters != null)
            {
                for (int i = 0; i < teamCharacters.Length; i++)
                {
                    CharacterContext teamCharacter = teamCharacters[i];
                    if (teamCharacter == null)
                    {
                        continue;
                    }

                    teamCharacter.gameObject.SetActive(teamCharacter == context);
                }
            }

            //6.同步当前角色位置，保证读档/切人后角色落点一致
            if (playerRuntimeData != null)
            {
                context.transform.position = playerRuntimeData.playerPosition;
                context.transform.rotation = Quaternion.Euler(playerRuntimeData.playerEulerAngles);
            }

            //7.初始化角色上下文
            context.Initialize(runtimeData);

            //8.同步当前受控角色缓存
            _currentCharacterContext = context;

            //10.切换角色时重新绑定当前角色的输入缓冲，保证玩家输入只写入当前受控角色
            playerInputReader?.BindInputBuffer(context.InputBuffer);
   

            //11.配置当前角色的相机参数
            SetupCurrentPlayerCamera();

            //12.角色绑定完成后，主动把最新依赖同步给 UI 层
            if (UIRoot.Instance != null)
            {
                UIRoot.Instance.InjectPlayerController(this);
            }
        }
        #endregion

        #region 解析当前角色观察点
        // 配置角色相机：解析当前角色观察点，并交给相机控制器初始化
        private void SetupCurrentPlayerCamera()
        {
            //1.空值校验
            if (playerCamera == null)
            {
                return;
            }

            //2.从当前受控角色上下文中解析观察点
            ResolveCameraTarget();

            //3.将当前角色观察点绑定到全局相机控制器
            if (cameraTarget == null)
            {
                return;
            }

            playerCamera.BindCameraPivot(cameraTarget);
            playerCamera.Initialize();
        }

        // 从当前受控角色上下文中解析相机观察点
        private void ResolveCameraTarget()
        {
            //1.当前没有受控角色时，清空观察点，避免沿用旧角色相机锚点
            if (_currentCharacterContext == null)
            {
                cameraTarget = null;
                return;
            }

            //2.每次绑定都从当前角色上下文读取观察点
            cameraTarget = _currentCharacterContext.CameraTarget;
        }


        #endregion

        #region  统一驱动相机更新
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
