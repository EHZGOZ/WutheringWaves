using UnityEngine;
using UnityEngine.SceneManagement;

namespace WutheringWaves
{
    // 游戏会话状态：描述当前处于游戏外、普通游戏中、战斗中
    public enum GameSessionState
    {
        OutGame, // 游戏外：主菜单、登录界面、未开始游戏
        InGame, // 游戏中：账号已登录，并且已经进入普通玩法
        InGameBattle // 游戏战斗中：已经进入玩法，并且触发战斗流程
    }
    // 游戏会话服务：负责当前账号开始游戏、进入游戏、运行时数据同步和退出自动保存
    public class GameSessionService : MonoBehaviour
    {
        public static GameSessionService Instance { get; private set; }

        [Header(" 玩家控制器")]
        [SerializeField] private PlayerController playerController;

        [Header(" 玩家运行时数据")]
        [SerializeField] private PlayerRuntimeData playerRuntimeData;

        [Header(" 是否输出详细日志")]
        [SerializeField] private bool verboseLog = true;

        #region 外部访问
        public bool IsInitialized { get; private set; }
        public GameSessionState CurrentState { get; private set; } = GameSessionState.OutGame; // 当前游戏会话状态
        public bool IsInGame => CurrentState == GameSessionState.InGame || CurrentState == GameSessionState.InGameBattle; // 是否处于游戏内
        public bool IsInBattle => CurrentState == GameSessionState.InGameBattle; // 是否处于战斗中
        #endregion

        #region 生命周期
        private void Awake()
        {
            // 1.保持单例，避免多个GameSessionService同时管理当前游戏会话
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // 2.缓存单例引用
            Instance = this;
        }

        private void OnDestroy()
        {
            // 1.如果销毁的是当前单例，清空单例引用
            if (Instance == this)
            {
                Instance = null;
            }
        }
        #endregion

        #region 初始化
        // 初始化游戏会话服务
        public void Initialize(GameBootstrap bootstrap)
        {
            // 1.已经初始化过时直接返回，避免重复初始化会话状态
            if (IsInitialized)
            {
                return;
            }

            // 2.默认还没有进入游戏
  
            CurrentState = GameSessionState.OutGame;

            // 3.标记初始化完成
            IsInitialized = true;

            //注入组件
            if (playerController == null)
            {
                playerController = bootstrap.PlayerController;
            }
            if (playerRuntimeData == null)
            {
                playerRuntimeData = bootstrap.PlayerRuntimeData;
            }

            if (verboseLog)
            {
                Debug.Log("[游戏会话服务] 初始化完成。");
            }
        }
        #endregion

        #region 进入游戏流程与退出游戏流程

        #region 新号创建存档或者老号读取数据
        // 当前账号开始游戏：有存档则读取，没有存档则新建，然后进入游戏
        public void StartGameWithCurrentAccount()
        {
            // 1.确认基础服务可用
            if (!CanStartGame())
            {
                return;
            }

            // 2.获取当前登录账号名
            string username = AccountManager.Instance.CurrentAccount.username;

            // 3.优先读取当前账号已有存档
            SaveData saveData = SaveService.Instance.LoadSave(username);

            // 4.没有存档时，为当前账号创建默认存档
            if (saveData == null)
            {
                saveData = CreateDefaultSaveForCurrentAccount(username);
            }

            // 5.仍然没有拿到存档时，停止进入游戏
            if (saveData == null)
            {
                Debug.LogError($"[游戏会话服务] 开始游戏失败：账号 '{username}' 存档数据为空。", this);
                return;
            }

            // 6.使用当前账号存档进入游戏
            EnterGameWithSaveData(saveData);
        }

        // 判断当前是否可以开始游戏
        private bool CanStartGame()
        {
            // 1.账号服务为空时，无法确认当前登录账号
            if (AccountManager.Instance == null)
            {
                Debug.LogError("[游戏会话服务] 开始游戏失败：AccountManager为空。", this);
                return false;
            }

            // 2.当前没有登录账号时，不允许进入游戏
            if (!AccountManager.Instance.IsLoggedIn || AccountManager.Instance.CurrentAccount == null)
            {
                Debug.LogWarning("[游戏会话服务] 当前没有登录账号，无法开始游戏。", this);
                return false;
            }

            // 3.存档服务为空时，无法读取或创建账号存档
            if (SaveService.Instance == null)
            {
                Debug.LogError("[游戏会话服务] 开始游戏失败：SaveService为空。", this);
                return false;
            }

            return true;
        }

        // 为当前账号创建默认存档
        private SaveData CreateDefaultSaveForCurrentAccount(string username)
        {

            //玩家节点缺失时使用兜底出生点
            Vector3 defaultPlayerPosition = playerController != null ? playerController.transform.position : new Vector3(0f, 0f, -5f);
            Vector3 defaultPlayerEulerAngles = playerController != null ? playerController.transform.eulerAngles : Vector3.zero;

            //创建默认存档数据：默认队伍、背包等由SaveData内部统一生成
            SaveData defaultSaveData = SaveData.CreateDefault(
                SceneManager.GetActiveScene().name,
                defaultPlayerPosition,
                defaultPlayerEulerAngles
            );

            //交给存档服务保存当前账号默认存档
            return SaveService.Instance.CreateSave(username, defaultSaveData);
        }
        #endregion

        #region 进入游戏自动读取数据
        // 使用存档数据进入游戏：同步运行时数据、初始化玩家、绑定背包和UI
        private void EnterGameWithSaveData(SaveData saveData)
        {
            //空值检查
            if (saveData == null)
            {
                Debug.LogError("[游戏会话服务] 进入游戏失败：存档数据为空。", this);
                return;
            }
            if (playerRuntimeData == null)
            {
                Debug.LogError("[游戏会话服务] 进入游戏失败：PlayerRuntimeData为空。", this);
                return;
            }
            if (playerController == null)
            {
                Debug.LogError("[游戏会话服务] 进入游戏失败：PlayerController为空。", this);
                return;
            }

            //绑定当前存档中的背包数据
            InventoryService.Instance?.Bind(saveData);

            // 6.把存档数据同步到玩家运行时数据
            playerRuntimeData.SyncRuntimeDataFromSaveData(saveData);

            // 7.初始化玩家控制器，生成队伍角色并绑定当前角色
            playerController.Initialize();

            // 8.绑定输入服务，并启用玩家输入
            InputService.Instance?.BindPlayer(playerController);
            InputService.Instance?.EnablePlayerInput();

            // 9.显示玩法UI
            UIRoot.Instance?.ShowGameplayUI();

            // 10.切换当前游戏会话状态为普通游戏中
            ChangeGameSessionState(GameSessionState.InGame);

            if (verboseLog)
            {
                Debug.Log("[游戏会话服务] 当前账号已进入游戏。");
            }
        }
        #endregion

        #region 退出游戏自动保存

        // 结束当前游戏会话：保存后清理角色和会话状态
        public void EndCurrentGameSession()
        {
            // 1.清理任何运行对象前，先尝试保存当前游戏数据
            SaveCurrentGameOnExit();

            // 2.停止玩家输入，避免清理过程中继续触发角色操作
            InputService.Instance?.ClearPlayer();

            // 3.解除当前角色绑定，并销毁全部运行时队伍角色
            playerController?.ClearCurrentTeamCharacters();

            // 4.清理背包绑定，避免下一个账号误用旧背包引用
            InventoryService.Instance?.Clear();

            // 5.切换当前游戏会话状态为游戏外
            ChangeGameSessionState(GameSessionState.OutGame);
        }

        // 退出游戏时自动保存：退出程序、登出账号、返回登录前都可以调用
        public bool SaveCurrentGameOnExit()
        {
            //没有进入游戏时，不需要保存
            if (!IsInGame)
            {
                return false;
            }

            //基础数据不完整时，不执行自动保存
            if (!CanSaveCurrentGame())
            {
                return false;
            }

            //把运行时数据写回当前账号存档
            playerRuntimeData.SyncSaveDataFromRuntimeData(SaveService.Instance.CurrentData);

            //交给存档服务保存当前账号存档
            bool ok = SaveService.Instance.SaveCurrentSave();

            if (verboseLog)
            {
                Debug.Log(ok ? "[游戏会话服务] 退出自动保存成功。" : "[游戏会话服务] 退出自动保存失败。");
            }

            return ok;
        }

        // 判断当前是否可以保存游戏
        private bool CanSaveCurrentGame()
        {
            // 1.存档服务为空时无法保存
            if (SaveService.Instance == null)
            {
                Debug.LogError("[游戏会话服务] 自动保存失败：SaveService为空。", this);
                return false;
            }

            // 2.当前没有账号存档数据时无法保存
            if (SaveService.Instance.CurrentData == null || string.IsNullOrWhiteSpace(SaveService.Instance.CurrentUsername))
            {
                Debug.LogWarning("[游戏会话服务] 自动保存失败：当前没有正在使用的账号存档。", this);
                return false;
            }

            // 3.玩家运行时数据为空时无法收集场景数据
            if (playerRuntimeData == null)
            {
                Debug.LogError("[游戏会话服务] 自动保存失败：PlayerRuntimeData为空。", this);
                return false;
            }

            return true;
        }
        #endregion

        #endregion

        #region 游戏会话状态切换
        // 切换游戏会话状态，并通知外部系统
        private void ChangeGameSessionState(GameSessionState targetState)
        {
            // 1.状态相同时不重复派发事件
            if (CurrentState == targetState)
            {
                return;
            }

            // 2.记录当前游戏会话状态
            CurrentState = targetState;

            // 3.通知外部系统当前游戏会话状态发生变化
            GameEvents.RaiseGameSessionStateChanged(CurrentState);
        }

        // 进入战斗状态：由Boss战、怪物战等玩法系统调用
        public void EnterBattleSession()
        {
            // 1.只有普通游戏中才能进入战斗状态
            if (CurrentState != GameSessionState.InGame)
            {
                return;
            }

            // 2.切换为战斗中
            ChangeGameSessionState(GameSessionState.InGameBattle);
        }

        // 退出战斗状态：战斗结束后回到普通游戏中
        public void ExitBattleSession()
        {
            // 1.只有战斗中才能退出战斗状态
            if (CurrentState != GameSessionState.InGameBattle)
            {
                return;
            }

            // 2.切回普通游戏中
            ChangeGameSessionState(GameSessionState.InGame);
        }
        #endregion
    }
}