using UnityEngine;

namespace WutheringWaves
{
    // 新根运动驱动：当前先负责承接 Context 依赖，后续再逐步迁移旧根运动逻辑
    public class RootMotionDriver : MonoBehaviour
    {
        [Header("=== 核心引用（优先自动获取，可手动补全） ===")]
        [Tooltip("角色共享上下文")]
        [SerializeField] private CharacterContext context;

        public CharacterContext Context => context;
        public CharacterRootMotion LegacyRootMotion => context != null ? context.RootMotion : null;
        public bool IsInitialized { get; private set; }

        // 初始化根运动驱动：先建立与 Context 的桥接关系
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
