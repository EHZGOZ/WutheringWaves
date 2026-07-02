using UnityEngine;

namespace WutheringWaves
{
    // 全局服务根节点：只负责让整个全局服务树在场景切换时不被销毁
    public class GlobalServiceRoot : MonoBehaviour
    {
        public static GlobalServiceRoot Instance { get; private set; }

        #region 生命周期
        private void Awake()
        {
            // 1.保持全局服务根节点唯一，避免切换场景后出现多个全局服务树
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // 2.缓存全局服务根节点单例
            Instance = this;

            // 3.只在根节点执行DontDestroyOnLoad，子系统不再各自负责常驻
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            // 1.如果销毁的是当前全局服务根节点，则清空单例引用
            if (Instance == this)
            {
                Instance = null;
            }
        }
        #endregion
    }
}