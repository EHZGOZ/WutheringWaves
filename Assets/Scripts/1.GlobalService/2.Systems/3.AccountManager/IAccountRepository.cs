namespace WutheringWaves
{
    // 账号数据仓储接口：定义存取账号列表的契约
    // 不管是 JSON、SQLite 还是未来的远端 HTTP，都实现这个接口
    public interface IAccountRepository
    {
        // 读取账号索引列表，永远不返回 null
        AccountIndexData Load();

        // 保存账号索引列表，失败返回 false
        bool Save(AccountIndexData index);

        // 存储中是否已有数据
        bool Exists();
    }
}