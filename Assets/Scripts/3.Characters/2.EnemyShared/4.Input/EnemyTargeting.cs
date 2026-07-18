using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WutheringWaves
{
    // 敌人目标组件：统一负责获取、保存、切换和清理当前玩家目标
    [DisallowMultipleComponent]
    public class EnemyTargeting : MonoBehaviour
    {

        [Header("=== 核心依赖（由EnemyContext自动注入，无需手动赋值）===")]
        [SerializeField] private EnemyContext context; // 敌人共享上下文      
        [Header("=== 敌人目标运行时数据 ===")]
        [SerializeField] private CharacterContext targetContext; // 当前玩家目标，仅用于运行时观察

        #region 对外只读属性
        public CharacterContext TargetContext => targetContext; // 当前目标的角色上下文

        public Transform TargetTransform =>
            targetContext != null ? targetContext.transform : null; // 当前目标的根节点

        public bool HasTarget => targetContext != null; // 当前是否存在有效目标
        #endregion

        #region 生命周期
        private void Awake()
        {
            
        }
        private void OnEnable()
        {
            //订阅事件
            SubscribeEvents();

            //组件启用时尝试获取当前受控角色
            TryAcquireCurrentPlayerTarget();
        }

        private void OnDisable()
        {
            // 组件禁用时取消事件监听，避免已禁用敌人继续响应切人事件
            UnsubscribeEvents();

            //清空当前目标，避免敌人重新启用后残留旧角色引用
            ClearTarget();
        }
        #endregion

        #region 订阅与取消订阅事件
        private void SubscribeEvents()
        {
            // 1.先取消旧订阅，避免重复启用或重新初始化后重复响应切人事件
            GameEvents.OnCharacterSwitched -= HandleCharacterSwitched;

            // 2.监听玩家切换事件，切人后自动更新敌人目标
            GameEvents.OnCharacterSwitched += HandleCharacterSwitched;
        }
        private void UnsubscribeEvents()
        {
            // 1.组件禁用时取消事件监听，避免已禁用敌人继续响应切人事件
            GameEvents.OnCharacterSwitched -= HandleCharacterSwitched;
        }

        #endregion

        #region 初始化
        public void Initialize(EnemyContext context)
        {
            // 1.缓存敌人共享上下文
            this.context = context;

            // 2.上下文为空时无法完成敌人目标组件初始化
            if (this.context == null)
            {
                Debug.LogError($"敌人 {name} 的EnemyTargeting没有获得EnemyContext。", this);
                return;
            }
            // 3.初始化时再次获取当前受控角色
            // 用于处理OnEnable执行时PlayerController尚未准备完成的情况
            TryAcquireCurrentPlayerTarget();
        }
        #endregion

        #region 获取当前玩家
        // 尝试将当前受控角色设置为敌人目标
        public bool TryAcquireCurrentPlayerTarget()
        {
            // 1.玩家控制器尚未创建时不能获取当前角色
            PlayerController playerController = FindFirstObjectByType< PlayerController>();
            if (playerController == null)
            {
                return false;
            }

            // 2.读取玩家控制器当前真正受控的角色
            CharacterContext currentCharacterContext =
                playerController.CurrentCharacterContext;

            // 3.当前受控角色为空时不能设置目标
            if (currentCharacterContext == null)
            {
                return false;
            }

            // 4.统一通过设置目标方法保存当前角色
            SetTarget(currentCharacterContext);
            return true;
        }
        #endregion

        #region 设置与清理目标
        // 设置当前目标：所有目标变化统一通过此方法处理
        public void SetTarget(CharacterContext newTargetContext)
        {
            // 1.传入目标为空时统一执行清理
            if (newTargetContext == null)
            {
                ClearTarget();
                return;
            }

            // 2.新旧目标相同时不重复赋值
            if (targetContext == newTargetContext)
            {
                return;
            }

            // 3.保存新的玩家目标
            targetContext = newTargetContext;
        }

        // 清空当前目标
        public void ClearTarget()
        {
            // 1.清空当前角色上下文引用
            targetContext = null;
        }
        #endregion

        #region 玩家切换事件
        // 玩家切换角色后，将新受控角色更新为敌人目标
        private void HandleCharacterSwitched(
            CharacterContext previousContext,
            CharacterContext currentContext)
        {
            // 1.旧角色只用于事件信息，敌人只关心切换后的当前角色
            SetTarget(currentContext);
        }
        #endregion
    }
}

