using UnityEngine;

namespace WutheringWaves
{
    // 新架构角色入口：负责初始化上下文和输入底层，不承载旧链路的具体业务逻辑
    public class CharacterFacade : MonoBehaviour
    {
        [Header("=== 核心组件（优先自动获取，可手动补全） ===")]
        [Tooltip("角色上下文")]
        [SerializeField] private CharacterContext context;
        [Tooltip("输入读取器：负责采集原始输入")]
        [SerializeField] private CharacterInputReader inputReader;
        [Tooltip("输入缓冲器：负责缓存动作请求")]
        [SerializeField] private InputBuffer inputBuffer;
        [Header("=== 启动选项 ===")]
        [SerializeField] private bool initializeOnAwake = true;
        [SerializeField] private bool disablePlayerInputOnStart = false;

        public CharacterContext Context => context;
        public CharacterInputReader InputReader => inputReader;
        public InputBuffer InputBuffer => inputBuffer;

        public bool IsInitialized { get; private set; }

        #region 初始化
        private void Awake()
        {
            if (initializeOnAwake)
            {
                Initialize();
            }
        }

        // 初始化新架构角色入口
        public void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            AutoGetComponents();
            InitializeInputStack();
            InitializeContext();

            if (disablePlayerInputOnStart && inputReader != null)
            {
                inputReader.DisablePlayerInput();
            }

            IsInitialized = true;
        }

        // 自动获取当前角色根节点上的核心组件
        private void AutoGetComponents()
        {
            if (context == null)
            {
                context = GetComponent<CharacterContext>();
            }

            if (inputReader == null)
            {
                inputReader = GetComponent<CharacterInputReader>();
            }

            if (inputBuffer == null)
            {
                inputBuffer = GetComponent<InputBuffer>();
            }

        }

        // 初始化输入底层（Reader + Buffer）
        private void InitializeInputStack()
        {
            inputReader?.Initialize();
            inputBuffer?.Initialize();
        }

        // 初始化上下文，供后续FSM/Controller直接读取新输入依赖
        private void InitializeContext()
        {
            context?.Initialize(this);
        }
        #endregion
    }
}
