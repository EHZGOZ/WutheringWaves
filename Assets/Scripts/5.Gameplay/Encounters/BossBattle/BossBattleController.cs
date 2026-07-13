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
        [SerializeField] private CharacterContext currentTarget; // 当前Boss战目标，仅用于运行时观察

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
            // 1.配置关闭时，不自动开启Boss战
            if (!startBattleOnEnter)
            {
                return;
            }

            // 2.只处理Player层碰撞体，忽略Boss、特效和其他场景物体
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer < 0 || other.gameObject.layer != playerLayer)
            {
                return;
            }

            // 3.尝试解析当前进入触发器的玩家角色上下文
            CharacterContext characterContext = ResolveCharacterContext(other);

            // 4.Player层物体仍然没有CharacterContext时，才属于真实配置问题
            if (characterContext == null)
            {
                Debug.LogWarning($"Boss触发器没有从玩家物体 {other.name} 获取到 CharacterContext。", other);
                return;
            }

            // 5.启动Boss战，并把玩家设置为Boss追击目标
            StartBossBattle(characterContext);
        }

        private void OnTriggerExit(Collider other)
        {
            // 1.正式Boss战一般不建议玩家离开后清空目标，这里只作为测试开关
            if (!clearTargetOnExit)
            {
                return;
            }

            // 2.只处理Player层碰撞体，避免其他物体离开时参与Boss战判断
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer < 0 || other.gameObject.layer != playerLayer)
            {
                return;
            }

            // 3.尝试获取离开触发器的玩家上下文
            CharacterContext characterContext = ResolveCharacterContext(other);

            // 4.离开的不是当前目标时不处理
            if (characterContext == null || characterContext != currentTarget)
            {
                return;
            }

            // 5.结束Boss战测试流程
            EndBossBattle();
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
            // 1.碰撞体为空时不能解析
            if (other == null)
            {
                return null;
            }

            // 2.优先从碰撞体自身或父物体查找CharacterContext
            CharacterContext characterContext = other.GetComponentInParent<CharacterContext>();
            if (characterContext != null)
            {
                return characterContext;
            }

            // 3.如果碰撞体属于PlayerController，则获取当前受控角色
            PlayerController playerController = other.GetComponentInParent<PlayerController>();
            if (playerController != null)
            {
                return playerController.CurrentCharacterContext;
            }

            // 4.碰撞体不属于玩家时返回空，不能使用玩家单例强行兜底
            return null;
        }
        #endregion

        #region Boss战流程

        #region 开始Boss战
        public void StartBossBattle(CharacterContext characterContext)
        {
            // 1.Boss战已经开始或已经完成时不重复处理
            if (isBattleStarted || isBattleCompleted)
            {
                return;
            }

            // 2.玩家目标为空时不启动Boss战
            if (characterContext == null)
            {
                Debug.LogWarning($"Boss战控制器 {name} 没有获取到有效的玩家目标。", this);
                return;
            }

            // 3.生成Boss，生成失败时终止启动流程
            if (!SpawnBoss())
            {
                return;
            }

            // 4.Boss移动组件为空时不能锁定玩家
            if (bossContext.MovementLogic == null)
            {
                Debug.LogError($"Boss {bossContext.name} 缺少 EnemyMovement 组件。", bossContext);
                return;
            }

            // 5.订阅当前Boss死亡事件
            if (!SubscribeBossEvents())
            {
                return;
            }

            // 6.记录当前Boss战目标
            currentTarget = characterContext;

            // 7.把当前玩家指定为Boss追击目标
            bossContext.MovementLogic.SetTarget(currentTarget);

            // 8.全部启动流程成功后，再标记Boss战已经开始
            isBattleStarted = true;

            // 9.通知游戏会话服务进入战斗状态
            GameSessionService.Instance?.EnterBattleSession();

            // 10.输出日志，确认Boss生成和目标绑定成功
            Debug.Log($"Boss战开始：{bossContext.name} 锁定目标 {currentTarget.name}");
        }
        #endregion

        #region 结束Boss战
        // 结束Boss战：当前用于玩家离开场地后的中断和重置
        public void EndBossBattle()
        {
            // 1.Boss战未开始且没有Boss实例时，不重复处理
            if (!isBattleStarted && bossInstance == null)
            {
                return;
            }

            // 2.先标记Boss战已经结束，防止结束过程中重复触发
            isBattleStarted = false;

            // 3.中断Boss战时取消死亡事件订阅
            UnsubscribeBossEvents();

            // 4.根据配置决定销毁Boss还是保留实例并复位
            if (destroyBossOnExit)
            {
                DespawnBoss();
            }
            else
            {
                ResetBoss();
            }

            // 5.清空当前玩家目标
            currentTarget = null;

            // 6.通知游戏会话服务退出战斗状态
            GameSessionService.Instance?.ExitBattleSession();

            // 7.输出日志，确认Boss战中断流程已经完成
            Debug.Log(destroyBossOnExit
                ? "Boss战结束：Boss实例已销毁"
                : "Boss战结束：Boss实例已重置");
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
        // 销毁Boss：结束并清理当前Boss实例
        private void DespawnBoss()
        {
            // 1.销毁前先取消Boss事件订阅
            UnsubscribeBossEvents();

            // 2.销毁前清空Boss目标，恢复相关碰撞设置
            if (bossContext != null && bossContext.MovementLogic != null)
            {
                bossContext.MovementLogic.ClearTarget();
            }

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
                Debug.LogError($"Boss实例 {bossInstance.name} 缺少 EnemyContext，无法执行重置。", bossInstance);
                DespawnBoss();
                return;
            }

            // 3.死亡Boss当前还没有完整复活协议，因此死亡后统一销毁
            if (bossContext.IsDead)
            {
                DespawnBoss();
                return;
            }

            // 4.清空当前追击目标
            if (bossContext.MovementLogic != null)
            {
                bossContext.MovementLogic.ClearTarget();
            }

            // 5.恢复Boss出生位置和旋转
            bossInstance.transform.SetPositionAndRotation(
                bossSpawnPoint.position,
                bossSpawnPoint.rotation
            );

            // 6.重新初始化属性、移动和状态机
            // 当前只允许对仍然存活的Boss执行，避免死亡状态锁阻止状态重置
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

        // 完成Boss战：只结束战斗流程，不立即销毁死亡Boss
        private void CompleteBossBattle()
        {
            // 1.标记Boss战已经结束并完成
            isBattleStarted = false;
            isBattleCompleted = true;

            // 2.清空Boss追击目标，让死亡状态不再保留玩家碰撞关系
            if (bossContext != null && bossContext.MovementLogic != null)
            {
                bossContext.MovementLogic.ClearTarget();
            }

            // 3.清空当前玩家目标
            currentTarget = null;

            // 4.取消死亡事件订阅，避免重复响应
            UnsubscribeBossEvents();

            // 5.退出游戏战斗状态，切回普通游戏背景音乐
            GameSessionService.Instance?.ExitBattleSession();

            // 6.不在这里销毁Boss，EnemyAttributes后续还会通知状态机进入Dead
            Debug.Log($"Boss战胜利：{bossContext.name} 已被击败");
        }
        #endregion



    }
}