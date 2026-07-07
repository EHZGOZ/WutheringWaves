using System.Collections.Generic;
using UnityEngine;

namespace WutheringWaves
{
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [Header(" 玩家数据")]
        [SerializeField] private PlayerRuntimeData playerRuntimeData; // 玩家数据
        [Header(" 输入引用 ")]
        [SerializeField] private PlayerInputReader playerInputReader;//玩家输入
        [Header("玩家体力")]
        [SerializeField] private PlayerStamina playerStamina;//玩家共享体力
        [Header("相机引用 ")]
        [SerializeField] private PlayerCamera playerCamera; // 全局相机控制器（所有角色共用）
        
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
        
        // 外部脚本只读访问队伍角色列表
        public CharacterContext[] TeamCharacters => teamCharacters;
        // 外部脚本只读访问当前角色共享上下文
        public CharacterContext CurrentCharacterContext => _currentCharacterContext;

        #endregion

        #region 生命周期

        private void Awake()
        {
            // 单例模式核心：如果已存在玩家控制器，销毁当前重复对象
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // 玩家控制器作为玩家根节点，切换场景时不销毁
            DontDestroyOnLoad(gameObject);
        }
        private void Update()
        {
            // 统一驱动队伍中所有角色的中档运行时状态：即使角色对象被禁用，冷却也要继续推进
            UpdateTeamCharacterRuntimeData();
        }


        private void LateUpdate()
        {
            // 晚更新驱动相机：保证角色位置先更新，相机再跟随，避免抖动
            UpdateCurrentPlayerCamera();

        }

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

        private void OnDestroy()
        {
            // 控制器销毁时解绑输入事件，避免残留委托引用
            UnsubscribeInputEvents();

            // 如果销毁的是当前单例，清空单例引用
            if (Instance == this)
            {
                Instance = null;
            }
        }


        #endregion

        #region 初始化组件
        public void Bind(PlayerRuntimeData playerRuntimeData)
        {
            this.playerRuntimeData = playerRuntimeData;
        }

        // 初始化玩家对象（新建 读档可用）
        public void Initialize()
        {
            //1. 获取输入 体力 相机 运行时数据 并订阅等
            ResolveDependencies();
            //2. 清理当前队伍角色
            ClearCurrentTeamCharacters();
            //3.根据玩家运行时数据生成队伍角色
            SpawnCharacter();
            //4.为队伍中的所有角色注入运行时数据
            InjectCharacterRuntimeData();
            //5.绑定受控角色：同步缓存角色所有核心组件
            BindCurrentCharacter();
        }
        #endregion

        # region 获取输入 体力 相机 运行时数据等
        //获取输入 体力 相机 运行时数据 并订阅等
        private void ResolveDependencies()
        {
            //获取玩家输入入并初始化
            ResolvePlayerInputReader();   
            //获取玩家共享体力入并初始化
            ResolvePlayerStamina();
            //获取玩家相机
            ResolveplayerCamerar();
            //获取玩家运行数据
            ResolvePlayerRuntimeData();
            //订阅输入事件
            SubscribeInputEvents();
        }

        #region 获取玩家输入并初始化
        //获取玩家输入入并初始化
        private void ResolvePlayerInputReader()
        {
            if (playerInputReader == null)
            {
                playerInputReader = GetComponent<PlayerInputReader>();
            }
            playerInputReader.Initialize();
        }
        #endregion

        #region 获取玩家共享体力并初始化
        //获取玩家共享体力入并初始化
        private void ResolvePlayerStamina()
        {
            if (playerStamina == null)
            {
                playerStamina = GetComponent<PlayerStamina>();
            }
            playerStamina.Initialize();
        }
        #endregion

        #region 获取玩家相机并初始化
        //获取玩家相机
        private void ResolveplayerCamerar()
        {
            if (playerCamera == null)
            {
                playerCamera = GetComponent<PlayerCamera>();
            }
            playerCamera.Initialize();
        }

        #endregion

        #region 获取玩家运行数据
        //获取玩家运行数据
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

        #endregion

        #region 清理当前队伍角色
        // 清理当前已经生成的队伍角色：（ 新档 读档 或者更换队伍使用）
        public void ClearCurrentTeamCharacters()
        {
            // 清除当前绑定
            ClearCurrentBind();
            // 销毁当前队伍角色数组里的角色与队伍缓存
            DestroyTeamCharacterArrayObjects();
        }
        // 清除当前绑定
        private void ClearCurrentBind()
        {
            // 1.先清空当前受控角色缓存，避免输入、相机、UI继续指向旧角色
            _currentCharacterContext = null;

            // 2.解绑当前输入缓冲，避免输入继续写入旧角色
            playerInputReader?.BindInputBuffer(null);

            // 3.解绑当前相机观察点，避免相机继续跟随旧角色
            playerCamera?.BindCameraPivot(null);
        }
        // 销毁队伍角色数组中的角色对象
        private void DestroyTeamCharacterArrayObjects()
        {
            if (teamCharacters == null)
            {
                return;
            }

            for (int i = 0; i < teamCharacters.Length; i++)
            {
                CharacterContext context = teamCharacters[i];
                if (context == null)
                {
                    continue;
                }

                Destroy(context.gameObject);
                teamCharacters[i] = null;
            }
            // 清空队伍缓存
            teamCharacters = null;
        }

        #endregion

        #region 生成玩家所操控的角色
        // 生成玩家所操控的角色
        private void SpawnCharacter()
        {
            // 1.空值校验
            if (!CanSpawnCharacters())
            {
                return;
            }

            // 2.清空旧队伍缓存，保证本次生成结果完全来自运行时数据
            teamCharacters = null;

            // 3.根据运行时数据中的队伍槽位逐个生成角色
            List<CharacterContext> spawnedCharacters = new List<CharacterContext>();
            for (int i = 0; i < playerRuntimeData.teamSlots.Count; i++)
            {
                TeamCharacterSlotData slotData = playerRuntimeData.teamSlots[i];
                if (slotData == null)
                {
                    continue;
                }

                CharacterContext context = SpawnSingleCharacter(slotData);
                if (context == null)
                {
                    continue;
                }

                spawnedCharacters.Add(context);
            }

            // 4.写回队伍角色列表，后续切人时按相同索引访问
            teamCharacters = spawnedCharacters.ToArray();
        }

        // 判断当前是否可以生成队伍角色
        private bool CanSpawnCharacters()
        {
            // 1.玩家运行时数据为空时，无法读取队伍槽位
            if (playerRuntimeData == null)
            {
                Debug.LogError("[PlayerController] 无法生成队伍角色：PlayerRuntimeData为空。", this);
                return false;
            }

            // 2.队伍槽位列表为空时，无法知道应该生成哪些角色
            if (playerRuntimeData.teamSlots == null)
            {
                Debug.LogError("[PlayerController] 无法生成队伍角色：teamSlots为空。", this);
                return false;
            }

            // 3.队伍槽位数量为0时，说明当前存档没有任何角色
            if (playerRuntimeData.teamSlots.Count == 0)
            {
                Debug.LogError("[PlayerController] 无法生成队伍角色：teamSlots数量为0。", this);
                return false;
            }

            // 4.角色生成服务为空时，无法通过角色名称生成对应预制体
            if (CharacterSpawnService.Instance == null)
            {
                Debug.LogError("[PlayerController] 无法生成队伍角色：CharacterSpawnService.Instance为空。", this);
                return false;
            }

            // 5.角色生成服务尚未初始化时，角色预制体映射可能还没有构建完成
            if (!CharacterSpawnService.Instance.IsInitialized)
            {
                Debug.LogError("[PlayerController] 无法生成队伍角色：CharacterSpawnService尚未初始化。", this);
                return false;
            }

            return true;
        }

        // 根据单个队伍槽位生成角色
        private CharacterContext SpawnSingleCharacter(TeamCharacterSlotData slotData)
        {
            // 1.空值校验
            if (slotData == null)
            {
                return null;
            }

            // 2.根据槽位数据生成角色对象，实际生成逻辑交给角色生成服务
            GameObject character = CharacterSpawnService.Instance.SpawnCharacter(
                slotData.characterName,
                playerRuntimeData.playerPosition,
                Quaternion.Euler(playerRuntimeData.playerEulerAngles),
                transform
            );

            if (character == null)
            {
                return null;
            }

            // 3.获取角色上下文
            CharacterContext context = character.GetComponent<CharacterContext>();
            if (context == null)
            {
                Debug.LogError($"[PlayerController] 角色 {slotData.characterName} 缺少CharacterContext组件。", character);
                Destroy(character);
                return null;
            }

            // 4.生成后先不隐藏，等绑定当前角色时由SetOnlyCurrentCharacterActive统一处理显隐
            return context;

        }
        #endregion

        #region 注入角色运行时数据
        // 为队伍中的所有角色注入运行时数据
        private void InjectCharacterRuntimeData()
        {
            // 1.注入前校验
            if (!CanInjectCharacterRuntimeData())
            {
                return;
            }

            // 2.按队伍索引给每个角色注入对应槽位的运行时数据
            for (int i = 0; i < teamCharacters.Length; i++)
            {
                CharacterContext context = teamCharacters[i];
                if (context == null)
                {
                    Debug.LogError($"[PlayerController] 注入角色运行时数据失败：teamCharacters[{i}]为空。", this);
                    continue;
                }

                CharacterRuntimeData runtimeData = ResolveCharacterRuntimeData(i);
                if (runtimeData == null)
                {
                    Debug.LogError($"[PlayerController] 注入角色运行时数据失败：槽位 {i} 的 CharacterRuntimeData为空。", context);
                    continue;
                }

                context.Initialize(runtimeData);
            }
        }

        // 判断当前是否可以注入角色运行时数据
        private bool CanInjectCharacterRuntimeData()
        {
            // 1.队伍角色数组为空时，无法注入
            if (teamCharacters == null)
            {
                Debug.LogError("[PlayerController] 注入角色运行时数据失败：teamCharacters为空。", this);
                return false;
            }

            // 2.队伍角色数量为0时，无法注入
            if (teamCharacters.Length == 0)
            {
                Debug.LogError("[PlayerController] 注入角色运行时数据失败：teamCharacters数量为0。", this);
                return false;
            }

            // 3.玩家运行时数据为空时，无法读取队伍槽位数据
            if (playerRuntimeData == null)
            {
                Debug.LogError("[PlayerController] 注入角色运行时数据失败：PlayerRuntimeData为空。", this);
                return false;
            }

            // 4.队伍槽位列表为空时，无法读取角色运行时数据
            if (playerRuntimeData.teamSlots == null)
            {
                Debug.LogError("[PlayerController] 注入角色运行时数据失败：teamSlots为空。", this);
                return false;
            }

            // 5.角色数量和槽位数量不一致时，说明生成流程或存档数据有问题
            if (teamCharacters.Length != playerRuntimeData.teamSlots.Count)
            {
                Debug.LogError($"[PlayerController] 注入角色运行时数据失败：角色数量和槽位数量不一致。teamCharacters = {teamCharacters.Length}, teamSlots = {playerRuntimeData.teamSlots.Count}", this);
                return false;
            }

            return true;
        }

        // 根据队伍槽位索引解析角色运行时数据
        private CharacterRuntimeData ResolveCharacterRuntimeData(int characterIndex)
        {
            // 1.索引非法时，无法读取角色运行时数据
            if (characterIndex < 0 || characterIndex >= playerRuntimeData.teamSlots.Count)
            {
                Debug.LogError($"[PlayerController] 解析角色运行时数据失败：角色索引非法。characterIndex = {characterIndex}", this);
                return null;
            }

            // 2.获取当前槽位数据
            TeamCharacterSlotData slotData = playerRuntimeData.teamSlots[characterIndex];
            if (slotData == null)
            {
                Debug.LogError($"[PlayerController] 解析角色运行时数据失败：槽位数据为空。characterIndex = {characterIndex}", this);
                return null;
            }

            // 3.返回当前槽位里的角色运行时数据
            return slotData.runtimeData;
        }
        #endregion

        #region 绑定当前受控角色
        // 角色绑定模式：用于区分读档落位和运行中切人落位
        private enum CharacterBindMode
        {
            FromSavedTransform, // 从存档记录的位置旋转绑定，适合新建/读档后第一次绑定
            FromCurrentCharacter // 从当前受控角色的位置旋转绑定，适合运行中切人
        }
        // 外部切人入口：运行中切换角色时调用，使用当前受控角色的位置旋转
        public void BindCurrentCharacter(CharacterContext context)
        {
            BindCurrentCharacter(context, CharacterBindMode.FromCurrentCharacter);
        }
        private void BindCurrentCharacter()
        {
            // 1.空值校验
            if (teamCharacters == null || teamCharacters.Length == 0)
            {
                Debug.LogError("[PlayerController] 绑定默认当前角色失败：teamCharacters为空或数量为0。", this);
                return;
            }

            // 2.解析默认受控角色索引
            int targetIndex = ResolveDefaultCurrentCharacterIndex();

            // 3.按槽位绑定当前受控角色：新建/读档第一次绑定时使用存档记录的位置旋转
            BindCurrentCharacter(teamCharacters[targetIndex], CharacterBindMode.FromSavedTransform);
        }
        // 绑定受控角色
        private void BindCurrentCharacter(CharacterContext context, CharacterBindMode bindMode)
        {
            // 1.校验当前角色上下文是否可以绑定
            if (!CanBindCurrentCharacter(context))
            {
                return;
            }

            // 2.同步玩家运行时当前受控角色索引
            if (!SyncCurrentCharacterIndex(context))
            {
                return;
            }

            // 3.先把目标角色移动到正确位置，避免目标角色在旧位置被激活
            ApplyPlayerTransformToCharacter(context, bindMode);

            // 4.再只激活当前受控角色，关闭其他队伍角色
            SetOnlyCurrentCharacterActive(context);

            // 5.设置当前受控角色上下文，并同步输入、相机、UI
            SetCurrentCharacterContext(context);
        }

        #region 绑定当前受控角色相关方法
        // 解析默认受控角色索引
        private int ResolveDefaultCurrentCharacterIndex()
        {
            // 1.优先使用玩家运行时数据中的当前角色索引
            int targetIndex = playerRuntimeData != null
                ? playerRuntimeData.currentCharacterIndex
                : defaultCharacterIndex;

            // 2.限制索引范围，避免存档中的索引越界
            return Mathf.Clamp(targetIndex, 0, teamCharacters.Length - 1);
        }

        // 判断当前角色上下文是否可以绑定
        private bool CanBindCurrentCharacter(CharacterContext context)
        {
            // 1.当前角色上下文为空时，无法绑定
            if (context == null)
            {
                Debug.LogError("[PlayerController] 绑定当前角色失败：CharacterContext为空。", this);
                return false;
            }

            // 2.队伍角色数组为空时，无法确认当前角色是否属于队伍
            if (teamCharacters == null)
            {
                Debug.LogError("[PlayerController] 绑定当前角色失败：teamCharacters为空。", this);
                return false;
            }

            // 3.队伍角色数量为0时，无法绑定任何角色
            if (teamCharacters.Length == 0)
            {
                Debug.LogError("[PlayerController] 绑定当前角色失败：teamCharacters数量为0。", this);
                return false;
            }

            // 4.玩家运行时数据为空时，无法同步当前受控角色索引和位置
            if (playerRuntimeData == null)
            {
                Debug.LogError("[PlayerController] 绑定当前角色失败：PlayerRuntimeData为空。", this);
                return false;
            }

            return true;
        }

        // 根据当前角色上下文同步当前受控角色索引
        private bool SyncCurrentCharacterIndex(CharacterContext context)
        {
            // 1.当前角色上下文为空时，无法同步索引
            if (context == null)
            {
                Debug.LogError("[PlayerController] 同步当前角色索引失败：CharacterContext为空。", this);
                return false;
            }

            // 2.队伍角色数组为空时，无法同步索引
            if (teamCharacters == null)
            {
                Debug.LogError("[PlayerController] 同步当前角色索引失败：teamCharacters为空。", this);
                return false;
            }

            // 3.队伍角色数量为0时，无法同步索引
            if (teamCharacters.Length == 0)
            {
                Debug.LogError("[PlayerController] 同步当前角色索引失败：teamCharacters数量为0。", this);
                return false;
            }

            // 4.玩家运行时数据为空时，无法写入当前角色索引
            if (playerRuntimeData == null)
            {
                Debug.LogError("[PlayerController] 同步当前角色索引失败：PlayerRuntimeData为空。", this);
                return false;
            }

            // 5.遍历队伍角色数组，查找目标角色索引
            for (int i = 0; i < teamCharacters.Length; i++)
            {
                if (teamCharacters[i] == context)
                {
                    playerRuntimeData.currentCharacterIndex = i;
                    return true;
                }
            }

            // 6.没有找到目标角色时，说明传入的角色不属于当前队伍
            Debug.LogError($"[PlayerController] 同步当前角色索引失败：角色 {context.name} 不属于当前队伍。", context);
            return false;
        }

        // 只激活当前受控角色，关闭其他队伍角色
        private void SetOnlyCurrentCharacterActive(CharacterContext context)
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

        // 根据绑定模式同步玩家位置旋转到当前角色
        private void ApplyPlayerTransformToCharacter(CharacterContext context, CharacterBindMode bindMode)
        {
            // 1.新建/读档第一次绑定时，从存档记录的位置旋转中读取
            if (bindMode == CharacterBindMode.FromSavedTransform)
            {
                context.transform.position = playerRuntimeData.playerPosition;
                context.transform.rotation = Quaternion.Euler(playerRuntimeData.playerEulerAngles);
                return;
            }

            // 2.运行中切人时，从当前受控角色读取实时位置旋转
            if (bindMode == CharacterBindMode.FromCurrentCharacter)
            {
                if (_currentCharacterContext != null)
                {
                    context.transform.position = _currentCharacterContext.transform.position;
                    context.transform.rotation = _currentCharacterContext.transform.rotation;

                    // 同步一份到PlayerRuntimeData，保证后续存档拿到的是最新位置
                    playerRuntimeData.playerPosition = context.transform.position;
                    playerRuntimeData.playerEulerAngles = context.transform.eulerAngles;
                    return;
                }

                // 3.如果当前没有受控角色，兜底使用存档记录的位置旋转
                context.transform.position = playerRuntimeData.playerPosition;
                context.transform.rotation = Quaternion.Euler(playerRuntimeData.playerEulerAngles);
            }
        }

        // 设置当前受控角色上下文，并同步输入、相机、UI
        private void SetCurrentCharacterContext(CharacterContext context)
        {
            // 1.同步当前受控角色缓存
            _currentCharacterContext = context;

            // 2.绑定输入到当前受控角色
            playerInputReader?.BindInputBuffer(context.InputBuffer);

            // 3.切换角色后强制回到默认状态
            context.StateMachine?.ForceResetToDefaultState();

            // 4.绑定相机到当前受控角色
            if (context.CameraTarget == null)
            {
                Debug.LogWarning("[PlayerController] 当前角色缺少相机观察点。", context);
            }
            else
            {
                playerCamera?.BindCameraPivot(context.CameraTarget);
            }

            // 5.绑定UI到当前受控角色
            if (UIRoot.Instance != null)
            {
                UIRoot.Instance.Bind(this);
            }
        }

        #endregion

        #endregion

        #region 切换角色
        [Header("切人最小间隔时间")]
        [SerializeField] private float switchCharacterInterval = 0.3f;

        private float lastSwitchCharacterTime = -999f; // 上一次成功切人的时间
        // 处理输入层发来的切人请求：targetSlot为1/2/3
        private void HandleSwitchCharacterRequest(int targetSlot)
        {
            //1.把输入槽位转换成数组索引
            int targetIndex = targetSlot - 1;

            //2.检查是否可以切换到目标角色
            if (!CanSwitchToCharacter(targetIndex))
            {
                return;
            }

            //3.记录切换前角色，方便事件通知
            CharacterContext previousContext = _currentCharacterContext;
            CharacterContext targetContext = teamCharacters[targetIndex];

            //4.绑定目标角色
            BindCurrentCharacter(targetContext);

            //5.记录本次成功切人的时间
            lastSwitchCharacterTime = Time.time;

            //6.通知其他系统当前角色发生变化
            GameEvents.RaiseCharacterSwitched(previousContext, _currentCharacterContext);

        }

        // 判断是否可以切换到指定角色
        private bool CanSwitchToCharacter(int targetIndex)
        {
            //1.队伍为空时不能切换
            if (teamCharacters == null || teamCharacters.Length == 0)
            {
                return false;
            }

            //2.索引非法时不能切换
            if (targetIndex < 0 || targetIndex >= teamCharacters.Length)
            {
                return false;
            }

            CharacterContext targetContext = teamCharacters[targetIndex];

            //3.目标角色为空时不能切换
            if (targetContext == null)
            {
                return false;
            }

            //4.目标角色已经是当前角色时不重复切换
            if (targetContext == _currentCharacterContext)
            {
                return false;
            }

            //5.切人间隔未结束时不能切换
            if (Time.time - lastSwitchCharacterTime < switchCharacterInterval)
            {
                return false;
            }

            //6.目标角色死亡时先不允许切换
            if (targetContext.CharacterRuntimeData != null && targetContext.CharacterRuntimeData.IsDead)
            {
                return false;
            }

            //7.当前角色处于不可打断状态时先不允许切换
            if (_currentCharacterContext != null
                && _currentCharacterContext.StateMachine != null
                && !_currentCharacterContext.StateMachine.IsInterruptible())
            {
                return false;
            }


            return true;
        }
        #endregion

        #region 角色数据更新
        // 统一推进队伍中所有角色的中档运行时数据：即使角色对象被禁用，冷却也要继续倒计时
        private void UpdateTeamCharacterRuntimeData()
        {
            //1.队伍为空时不处理
            if (teamCharacters == null || teamCharacters.Length == 0)
            {
                return;
            }

            //2.逐个推进角色运行时数据
            for (int i = 0; i < teamCharacters.Length; i++)
            {
                CharacterContext context = teamCharacters[i];
                if (context == null || context.CharacterRuntimeData == null)
                {
                    continue;
                }

                context.CharacterRuntimeData.UpdateRuntime(Time.deltaTime);
            }
        }
        #endregion




    }
}
