using System.Collections.Generic;

namespace WutheringWaves
{
    #region 敌人状态工厂
    // 敌人状态工厂：负责注册和获取敌人状态实例
    public class EnemyStateFactory
    {
        private readonly Dictionary<EnemyState, EnemyStateBase> registeredStates = new Dictionary<EnemyState, EnemyStateBase>();

        #region 注册与查询
        public void RegisterState(EnemyState stateType, EnemyStateBase state)
        {
            // 1.状态为空时不注册
            if (state == null)
            {
                return;
            }

            // 2.重复注册时以后注册的状态覆盖旧状态
            registeredStates[stateType] = state;
        }

        public EnemyStateBase GetState(EnemyState stateType)
        {
            return registeredStates.TryGetValue(stateType, out EnemyStateBase state) ? state : null;
        }

        public bool HasState(EnemyState stateType)
        {
            return registeredStates.ContainsKey(stateType);
        }
        #endregion
    }
    #endregion
}