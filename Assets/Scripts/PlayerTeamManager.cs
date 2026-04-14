namespace WutheringWaves
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    // 玩家队伍管理器：当前主要作为跨场景保留的单例占位
    public class PlayerTemaManager : MonoBehaviour
    {
        public static PlayerTemaManager Instance; // 全局访问入口

        #region 生命周期函数
        private void Awake()
        {
            // 这里保留当前原始逻辑，只补充注释说明行为。
            if (Instance != null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);//切换场景不销毁
            }
            else
            {
                Destroy(gameObject);
            }
        }
        private void OnEnable()
        {
            
        }
        void Start()
        {

        }


        void Update()
        {

        }
        private void OnDisable()
        {

        }
        #endregion

    }

}

