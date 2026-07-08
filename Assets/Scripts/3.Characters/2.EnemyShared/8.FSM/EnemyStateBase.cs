namespace WutheringWaves
{
    #region 敌人状态抽象基类
    // 所有敌人状态的抽象基类：统一状态生命周期
    public abstract class EnemyStateBase
    {
        #region 核心依赖
        protected EnemyStateMachine stateMachine; // 敌人状态机
        protected EnemyStateFactory factory; // 敌人状态工厂
        #endregion

        #region 构造函数
        protected EnemyStateBase(EnemyStateMachine stateMachine, EnemyStateFactory factory)
        {
            this.stateMachine = stateMachine;
            this.factory = factory;
        }
        #endregion

        #region 状态生命周期
        public abstract void EnterState();
        public abstract void UpdateState();
        public abstract void ExitState();
        #endregion

        #region 状态切换
        protected void SwitchState(EnemyState targetState)
        {
            stateMachine.SwitchState(factory.GetState(targetState));
        }
        #endregion
    }
    #endregion
}