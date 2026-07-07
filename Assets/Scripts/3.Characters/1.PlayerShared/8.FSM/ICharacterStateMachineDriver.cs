namespace WutheringWaves
{
    /// <summary>
    /// 角色状态机驱动接口。
    /// 这里只保留状态注册职责，角色专属玩法逻辑继续留在各自角色脚本中。
    /// </summary>
    public interface ICharacterStateMachineDriver
    {
        /// <summary>
        /// 向状态工厂注册当前角色拥有的状态。
        /// </summary>
        void RegisterSpecialStates(CharacterStateFactory factory, CharacterStateMachine machine);
    }
}
