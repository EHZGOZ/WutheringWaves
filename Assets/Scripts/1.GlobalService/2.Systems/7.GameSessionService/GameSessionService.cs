using UnityEngine;
using UnityEngine.SceneManagement;

namespace WutheringWaves
{
    // 游戏会话状态：只描述当前是否已经真正进入玩法
    public enum GameSessionState
    {
        OutGame, // 游戏外：主菜单、登录界面、未开始游戏
        InGame   // 游戏中：账号已登录，并且已经开始游戏
    }
    // 游戏会话服务：负责当前账号开始游戏、进入游戏、运行时数据同步和退出自动保存
    public class GameSessionService : MonoBehaviour
    {
        public static GameSessionService Instance { get; private set; }

        [Header(" 是否输出详细日志")]
        [SerializeField] private bool verboseLog = true;

        [Header(" 玩家控制器")]
        [SerializeField] private PlayerController playerController;

        [Header(" 玩家运行时数据")]
        [SerializeField] private PlayerRuntimeData playerRuntimeData;

        public bool IsInitialized { get; private set; }
        public bool IsInGame { get; private set; }

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
        public void Initialize()
        {
            // 1.已经初始化过时直接返回，避免重复初始化会话状态
            if (IsInitialized)
            {
                return;
            }

            // 2.默认还没有进入游戏
            IsInGame = false;

            // 3.标记初始化完成
            IsInitialized = true;

            if (verboseLog)
            {
                Debug.Log("[游戏会话服务] 初始化完成。");
            }
        }
        #endregion

        #region 当前账号开始游戏
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
            // 1.解析玩家引用，用于读取默认出生点
            ResolvePlayer();

            // 2.玩家节点缺失时使用兜底出生点
            Vector3 defaultPlayerPosition = playerController != null
                ? playerController.transform.position
                : new Vector3(0f, 0f, -5f);

            Vector3 defaultPlayerEulerAngles = playerController != null
                ? playerController.transform.eulerAngles
                : Vector3.zero;

            // 3.创建默认存档数据：默认队伍、背包等由SaveData内部统一生成
            SaveData defaultSaveData = SaveData.CreateDefault(
                SceneManager.GetActiveScene().name,
                defaultPlayerPosition,
                defaultPlayerEulerAngles
            );

            // 4.交给存档服务保存当前账号默认存档
            return SaveService.Instance.CreateSave(username, defaultSaveData);
        }
        #endregion

        #region 进入游戏
        // 使用存档数据进入游戏：同步运行时数据、初始化玩家、绑定背包和UI
        private void EnterGameWithSaveData(SaveData saveData)
        {
            // 1.空值检查
            if (saveData == null)
            {
                Debug.LogError("[游戏会话服务] 进入游戏失败：存档数据为空。", this);
                return;
            }

            // 2.解析玩家控制器和运行时数据
            ResolvePlayer();

            // 3.玩家运行时数据为空时无法注入存档
            if (playerRuntimeData == null)
            {
                Debug.LogError("[游戏会话服务] 进入游戏失败：PlayerRuntimeData为空。", this);
                return;
            }

            // 4.玩家控制器为空时无法生成和绑定角色
            if (playerController == null)
            {
                Debug.LogError("[游戏会话服务] 进入游戏失败：PlayerController为空。", this);
                return;
            }

            // 5.绑定当前存档中的背包数据
            InventoryService.Instance?.Bind(saveData.inventory);

            // 6.把存档数据同步到玩家运行时数据
            playerRuntimeData.SyncRuntimeDataFromSaveData(saveData);

            // 7.初始化玩家控制器，生成队伍角色并绑定当前角色
            playerController.Initialize();

            // 8.绑定输入服务，并启用玩家输入
            InputService.Instance?.BindPlayer(playerController);
            InputService.Instance?.EnablePlayerInput();

            // 9.显示玩法UI
            UIRoot.Instance?.ShowGameplayUI();

            // 10.标记当前已经进入游戏
            IsInGame = true;

            // 11.通知外部系统：当前已经进入游戏中
            GameEvents.RaiseGameSessionStateChanged(GameSessionState.InGame);

            if (verboseLog)
            {
                Debug.Log("[游戏会话服务] 当前账号已进入游戏。");
            }
        }
        #endregion

        #region 退出自动保存
        // 退出游戏时自动保存：退出程序、登出账号、返回登录前都可以调用
        public bool SaveCurrentGameOnExit()
        {
            // 1.没有进入游戏时，不需要保存
            if (!IsInGame)
            {
                return false;
            }

            // 2.基础数据不完整时，不执行自动保存
            if (!CanSaveCurrentGame())
            {
                return false;
            }

            // 3.先从当前场景收集最新玩家运行时数据
            playerRuntimeData.SyncRuntimeDataFromScene();

            // 4.把运行时数据写回当前账号存档
            playerRuntimeData.SyncSaveDataFromRuntimeData(SaveService.Instance.CurrentData);

            // 5.交给存档服务保存当前账号存档
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

        // 结束当前游戏会话：保存后清理会话状态
        public void EndCurrentGameSession()
        {
            // 1.退出当前会话前先尝试保存
            SaveCurrentGameOnExit();

            // 2.清理玩家输入绑定，避免回到菜单后旧玩家对象继续响应输入
            InputService.Instance?.ClearPlayer();

            // 3.清理背包绑定，避免下一个账号误用旧背包引用
            InventoryService.Instance?.Clear();

            // 4.标记当前已经不在游戏中
            IsInGame = false;

            // 5.通知外部系统：当前已经回到游戏外
            GameEvents.RaiseGameSessionStateChanged(GameSessionState.OutGame);
        }
        #endregion

        #region 数据注入
        // 解析玩家控制器和玩家运行时数据，并保持两边引用一致
        private void ResolvePlayer()
        {
            // 1.优先使用PlayerController单例
            if (playerController == null)
            {
                playerController = PlayerController.Instance;
            }

            // 2.兜底从场景中查找玩家控制器
            if (playerController == null)
            {
                playerController = FindObjectOfType<PlayerController>();
            }

            // 3.优先从PlayerController身上获取PlayerRuntimeData
            if (playerRuntimeData == null && playerController != null)
            {
                playerRuntimeData = playerController.PlayerRuntimeData;
            }

            // 4.兜底从场景中查找玩家运行时数据
            if (playerRuntimeData == null)
            {
                playerRuntimeData = FindObjectOfType<PlayerRuntimeData>();
            }

            // 5.互相注入，保证玩家控制器和运行时数据引用一致
            if (playerRuntimeData != null && playerController != null)
            {
                playerRuntimeData.Bind(playerController);
                playerController.Bind(playerRuntimeData);
            }
        }
        #endregion
    }
}