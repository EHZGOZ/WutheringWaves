using UnityEngine;

namespace WutheringWaves
{
    // 新移动域控制器：当前先负责承接 Context 依赖，后续再逐步迁移旧移动逻辑
    public class CharacterMovementController : MonoBehaviour
    {
        [Header("=== 核心引用（优先自动获取，可手动补全） ===")]
        [Tooltip("角色共享上下文")]
        [SerializeField] private CharacterContext context;

        public CharacterContext Context => context;
        public CharacterMovement LegacyMovement => context != null ? context.MovementLogic : null;
        public PlayerInputReader PlayerInputReader => context != null ? context.PlayerInputReader : null;
        public InputBuffer InputBuffer => context != null ? context.InputBuffer : null;
        public bool IsInitialized { get; private set; }

        // 初始化移动域控制器：先建立与 Context 的桥接关系
        public void Initialize(CharacterContext injectedContext = null)
        {
            if (injectedContext != null)
            {
                context = injectedContext;
            }

            if (context == null)
            {
                context = GetComponent<CharacterContext>();
            }

            IsInitialized = context != null;
        }
    }
}
