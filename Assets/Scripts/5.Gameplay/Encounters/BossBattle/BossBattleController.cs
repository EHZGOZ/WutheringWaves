using UnityEngine;

namespace WutheringWaves
{
    // Boss战控制器：负责玩家进入Boss场地后，启动Boss战并给Boss指定目标
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public class BossBattleController : MonoBehaviour
    {
        #region 核心引用
        [Header("=== Boss战核心引用 ===")]
        [SerializeField] private BoxCollider battleTriggerCollider; // Boss战场地触发器，运行时自动获取

        [Header("=== Boss生成配置 ===")]
        [SerializeField] private GameObject bossPrefab; // Boss预制体：需要在Inspector中手动拖入
        [SerializeField] private Transform bossSpawnPoint; // Boss出生点：需要在Inspector中手动拖入
        #endregion

        #region 运行时数据
        [Header("=== Boss战运行时数据 ===")]
        [SerializeField] private GameObject bossInstance; // 当前生成出来的Boss实例，仅用于运行时观察
        [SerializeField] private EnemyContext bossContext; // 当前Boss上下文，仅用于运行时观察
        [SerializeField] private CharacterContext currentParticipant; // 当前进入Boss战的玩家参与者，仅用于退出判断

        private bool isBattleStarted; // Boss战是否已经开始
        private bool isBattleCompleted; // Boss战是否已经胜利完成
        #endregion

        #region Boss战配置
        [Header("=== Boss战配置 ===")]
        [SerializeField] private bool startBattleOnEnter = true; // 玩家进入触发器后是否自动开始Boss战
        [SerializeField] private bool clearTargetOnExit = false; // 玩家离开触发器后是否清空Boss目标，正式Boss战一般关闭
        [SerializeField] private bool destroyBossOnExit = true; // 离开Boss场地时是否销毁Boss；关闭时只重置位置
        #endregion


        #region 生命周期
        private void Awake()
        {
            // 1.初始化并验证Boss战控制器必需组件
            if (!InitializeRequiredComponents())
            {
                // 2.配置不完整时禁用脚本，避免进入Boss战后出现不完整流程
                enabled = false;
                return;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // 1.触发器入口只转发碰撞体，具体启动判断由StartBossBattle统一处理
            StartBossBattle(other);
        }

        private void OnTriggerExit(Collider other)
        {
            // 结束Boss战测试流程
            EndBossBattle(other);
        }

        private void OnDisable()
        {
            // 控制器禁用或场景卸载时解除Boss事件
            UnsubscribeBossEvents();
        }

        #endregion

        #region 初始化
        // 初始化并验证Boss战控制器必需组件
        private bool InitializeRequiredComponents()
        {
            // 1.获取同一物体上的Boss战场地碰撞体
            if (!TryGetComponent(out battleTriggerCollider))
            {
                Debug.LogError($"Boss战控制器 {name} 缺少 BoxCollider 组件。", this);
                return false;
            }

            // 2.场地碰撞体必须开启Trigger
            if (!battleTriggerCollider.isTrigger)
            {
                Debug.LogError($"Boss战控制器 {name} 的 BoxCollider 没有开启 Is Trigger。", battleTriggerCollider);
                return false;
            }

            // 3.Boss预制体必须由Inspector配置
            if (bossPrefab == null)
            {
                Debug.LogError($"Boss战控制器 {name} 缺少 Boss Prefab。", this);
                return false;
            }

            // 4.Boss出生点必须由Inspector配置
            if (bossSpawnPoint == null)
            {
                Debug.LogError($"Boss战控制器 {name} 缺少 Boss Spawn Point。", this);
                return false;
            }

            // 5.全部必需配置验证通过
            return true;
        }
        #endregion

        #region 解析获取玩家
        // 解析进入或离开Boss触发器的玩家角色上下文
        private CharacterContext ResolveCharacterContext(Collider other)
        {
            //碰撞体为空时不能解析
            if (other == null)
            {
                return null;
            }

            //只处理Player层碰撞体，忽略Boss、特效和其他场景物体
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer < 0 || other.gameObject.layer != playerLayer)
            {
                return null;
            }

            //优先从碰撞体自身或父物体查找CharacterContext
            CharacterContext characterContext = other.GetComponentInParent<CharacterContext>();
            if (characterContext != null)
            {
                return characterContext;
            }

            //如果碰撞体属于PlayerController，则获取当前受控角色
            PlayerController playerController = other.GetComponentInParent<PlayerController>();
            if (playerController != null)
            {
                return playerController.CurrentCharacterContext;
            }

            //碰撞体不属于玩家时返回空，不能使用玩家单例强行兜底
            return null;
        }
        #endregion

        #region Boss战流程

        #region 开始Boss战
        // 开始Boss战：由触发器入口调用，并统一验证进入者和战斗状态
        private void StartBossBattle(Collider other)
        {
            // 1.未开启自动开始时，不响应触发器进入事件
            if (!startBattleOnEnter)
            {
                return;
            }

            // 2.Boss战已经开始或已经完成时不重复处理
            if (isBattleStarted || isBattleCompleted)
            {
                return;
            }

            // 3.解析进入场地的玩家参与者
            CharacterContext characterContext =
                ResolveCharacterContext(other);

            // 4.非玩家碰撞体进入时静默忽略
            if (characterContext == null)
            {
                return;
            }

            // 5.生成Boss，生成失败时终止启动流程
            if (!SpawnBoss())
            {
                return;
            }

            // 6.订阅当前Boss死亡事件
            if (!SubscribeBossEvents())
            {
                return;
            }

            // 7.记录当前Boss战参与者
            // 这里只用于场地退出判断，不再作为EnemyMovement追击目标
            currentParticipant = characterContext;

            // 8.全部启动流程成功后，再标记Boss战已经开始
            isBattleStarted = true;

            // 9.通知游戏会话服务进入战斗状态
            GameSessionService.Instance?.EnterBattleSession();

            // 10.输出日志，确认Boss战启动成功
            Debug.Log(
                $"Boss战开始：{bossContext.name}"
                + $"，场地参与者：{currentParticipant.name}"
            );
        }
        #endregion

        #region 失败结束Boss战
        // 结束Boss战：当前用于玩家离开场地后的中断和重置
        public void EndBossBattle(Collider other)
        {
            // 正式Boss战一般不建议玩家离开后结束战斗
            // 当前开关只用于测试Boss战中断和重置流程
            if (!clearTargetOnExit)
            {
                return;
            }

            // Boss战未开始且没有Boss实例时，不重复处理
            if (!isBattleStarted && bossInstance == null)
            {
                return;
            }

            // 解析离开场地的玩家参与者
            CharacterContext characterContext =
                ResolveCharacterContext(other);

            // 离开的不是当前战斗参与者时不处理
            if (characterContext == null
                || characterContext != currentParticipant)
            {
                return;
            }
            

            // 先标记Boss战已经结束，防止结束过程中重复触发
            isBattleStarted = false;

            // 中断Boss战时取消死亡事件订阅
            UnsubscribeBossEvents();

            // 根据配置决定销毁Boss还是保留实例并复位
            if (destroyBossOnExit)
            {
                DespawnBoss();
            }
            else
            {
                ResetBoss();
            }

            // 清空当前Boss战参与者
            currentParticipant = null;

            //通知游戏会话服务退出战斗状态
            GameSessionService.Instance?.ExitBattleSession();

            // 输出日志，确认Boss战中断流程已经完成
            Debug.Log(
                destroyBossOnExit
                    ? "Boss战结束：Boss实例已销毁"
                    : "Boss战结束：Boss实例已重置"
            );
        }
        #endregion

        #region 成功结束Boss战
        // 完成Boss战：只结束战斗流程，不立即销毁死亡Boss
        private void CompleteBossBattle()
        {
            // 1.标记Boss战已经结束并完成
            isBattleStarted = false;
            isBattleCompleted = true;

            // 2.清空Boss目标并重置导航运行状态
            ClearBossTargetAndNavigation();

            // 3.清空当前Boss战参与者
            currentParticipant = null;

            // 4.取消死亡事件订阅，避免重复响应
            UnsubscribeBossEvents();

            // 5.退出游戏战斗状态，切回普通游戏背景音乐
            GameSessionService.Instance?.ExitBattleSession();

            // 6.不在这里销毁Boss
            // EnemyAttributes后续还会通知状态机进入Dead
            Debug.Log($"Boss战胜利：{bossContext.name} 已被击败");
        }
        #endregion

        #endregion

        #region Boss生成
        // 生成Boss：成功生成或已存在有效Boss时返回true
        private bool SpawnBoss()
        {
            // 1.Boss实例已经存在时，尝试恢复Boss上下文引用
            if (bossInstance != null)
            {
                if (bossContext == null)
                {
                    bossContext = bossInstance.GetComponent<EnemyContext>();
                }

                // 2.现有Boss结构有效时直接复用，避免重复生成
                if (bossContext != null)
                {
                    return true;
                }

                // 3.现有实例结构无效时先销毁，再尝试重新生成
                Debug.LogError($"Boss实例 {bossInstance.name} 缺少 EnemyContext，将重新生成。", bossInstance);
                Destroy(bossInstance);
                bossInstance = null;
            }

            // 4.Boss预制体为空时不能生成
            if (bossPrefab == null)
            {
                Debug.LogError($"Boss战控制器 {name} 缺少 Boss Prefab。", this);
                return false;
            }

            // 5.Boss出生点为空时不能生成
            if (bossSpawnPoint == null)
            {
                Debug.LogError($"Boss战控制器 {name} 缺少 Boss Spawn Point。", this);
                return false;
            }

            // 6.根据出生点的位置和旋转生成Boss
            bossInstance = Instantiate(
                bossPrefab,
                bossSpawnPoint.position,
                bossSpawnPoint.rotation
            );

            // 7.EnemyContext应当位于Boss预制体根物体
            if (!bossInstance.TryGetComponent(out bossContext))
            {
                Debug.LogError($"生成的Boss {bossInstance.name} 根物体缺少 EnemyContext 组件。", bossInstance);

                // 8.销毁结构错误的Boss，避免场景留下无效实例
                Destroy(bossInstance);
                bossInstance = null;
                bossContext = null;
                return false;
            }

            // 9.Boss生成成功
            return true;
        }
        // 清理当前Boss的目标和导航运行状态
        private void ClearBossTargetAndNavigation()
        {
            // 1.当前没有有效Boss上下文时不执行清理
            if (bossContext == null)
            {
                return;
            }

            // 2.由EnemyTargeting清空当前玩家目标
            bossContext.Targeting.ClearTarget();

            // 3.由EnemyMovement停止移动并清除旧导航路径
            bossContext.MovementLogic.ResetNavigation();
        }
        // 销毁Boss：结束并清理当前Boss实例
        private void DespawnBoss()
        {
            // 1.销毁前先取消Boss事件订阅
            UnsubscribeBossEvents();

            // 2.销毁前清空Boss目标并重置导航
            ClearBossTargetAndNavigation();

            // 3.销毁当前Boss实例
            if (bossInstance != null)
            {
                Destroy(bossInstance);
            }

            // 4.立即清空运行时引用
            bossInstance = null;
            bossContext = null;
        }

        // 重置Boss：保留当前实例，恢复出生位置和初始运行状态
        private void ResetBoss()
        {
            // 1.Boss实例不存在时，只清理可能残留的上下文引用
            if (bossInstance == null)
            {
                bossContext = null;
                return;
            }

            // 2.Boss上下文丢失时无法安全重置，改为销毁无效实例
            if (bossContext == null)
            {
                Debug.LogError(
                    $"Boss实例 {bossInstance.name} 缺少 EnemyContext，无法执行重置。",
                    bossInstance
                );

                DespawnBoss();
                return;
            }

            // 3.死亡Boss当前还没有完整复活协议，因此死亡后统一销毁
            if (bossContext.IsDead)
            {
                DespawnBoss();
                return;
            }

            // 4.清空上一场战斗的目标和导航运行状态
            ClearBossTargetAndNavigation();

            // 5.恢复Boss出生位置和旋转
            bossInstance.transform.SetPositionAndRotation(
                bossSpawnPoint.position,
                bossSpawnPoint.rotation
            );

            // 6.重新初始化属性、目标、移动、根运动和状态机
            // 当前只允许对仍然存活的Boss执行
            bossContext.Initialize();
        }
        #endregion

        #region Boss事件
        // 订阅当前Boss属性事件
        private bool SubscribeBossEvents()
        {
            // 1.Boss上下文为空时无法订阅
            if (bossContext == null)
            {
                Debug.LogError($"Boss战控制器 {name} 没有可订阅的 BossContext。", this);
                return false;
            }

            // 2.Boss属性组件为空时无法监听死亡
            if (bossContext.EnemyAttributes == null)
            {
                Debug.LogError($"Boss {bossContext.name} 缺少 EnemyAttributes 组件。", bossContext);
                return false;
            }

            // 3.先取消旧订阅再重新订阅，避免重复进入时重复响应
            bossContext.EnemyAttributes.OnDead -= HandleBossDead;
            bossContext.EnemyAttributes.OnDead += HandleBossDead;

            return true;
        }

        // 取消当前Boss属性事件订阅
        private void UnsubscribeBossEvents()
        {
            // 1.Boss上下文或属性组件为空时不处理
            if (bossContext == null || bossContext.EnemyAttributes == null)
            {
                return;
            }

            // 2.取消死亡事件订阅，避免旧Boss继续回调控制器
            bossContext.EnemyAttributes.OnDead -= HandleBossDead;
        }

        // 处理当前Boss死亡事件
        private void HandleBossDead(EnemyAttributes deadBossAttributes)
        {
            // 1.当前没有进行Boss战时忽略死亡回调
            if (!isBattleStarted)
            {
                return;
            }

            // 2.死亡来源不是当前Boss时不处理
            if (bossContext == null || deadBossAttributes != bossContext.EnemyAttributes)
            {
                return;
            }

            // 3.进入Boss战胜利完成流程
            CompleteBossBattle();
        }
        #endregion



    }
}