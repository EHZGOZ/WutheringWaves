using UnityEngine;

namespace WutheringWaves
{
    // 角色上下文：聚合新架构下角色运行所需的共享依赖，供FSM/Controller/State统一读取
    public class CharacterContext : MonoBehaviour
    {
        [Header("=== 核心引用（优先自动获取，可手动补全） ===")]
        [Tooltip("新架构角色入口外观")]
        [SerializeField] private CharacterFacade facade;
        [Tooltip("输入读取器：负责采集原始输入")]
        [SerializeField] private CharacterInputReader inputReader;
        [Tooltip("输入缓冲器：负责缓存动作请求")]
        [SerializeField] private InputBuffer inputBuffer;
        public CharacterFacade Facade => facade;
        public CharacterInputReader InputReader => inputReader;
        public InputBuffer InputBuffer => inputBuffer;

        public bool IsInitialized { get; private set; }

        #region 初始化
        // 初始化上下文依赖（由CharacterFacade统一调用）
        public void Initialize(CharacterFacade ownerFacade = null)
        {
            if (ownerFacade != null)
            {
                facade = ownerFacade;
            }

            ResolveReferences();
            IsInitialized = inputReader != null && inputBuffer != null;
        }

        // 自动补齐当前角色根节点上的核心输入依赖
        private void ResolveReferences()
        {
            if (facade == null)
            {
                facade = GetComponent<CharacterFacade>();
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
        #endregion
    }
}
