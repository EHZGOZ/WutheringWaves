using System.Collections.Generic;
using UnityEngine;

namespace WutheringWaves
{
    // 玩家角色切换器：负责当前角色绑定、运行中切人和相关依赖重新绑定
    public class PlayerCharacterSwitcher : MonoBehaviour
    {
        #region 核心引用
        private PlayerTeamController playerTeamController; // 玩家队伍控制器
        private PlayerRuntimeData playerRuntimeData; // 玩家运行时数据
        private PlayerInputReader playerInputReader; // 玩家输入读取器
        private PlayerCamera playerCamera; // 玩家相机控制器
        #endregion

        #region 角色切换设置
        [Header("=== 角色切换设置 ===")]
        [Header("切人最小间隔时间")]
        [SerializeField][Min(0f)] private float switchCharacterInterval = 0.3f;

        private float lastSwitchCharacterTime = -999f; // 上一次成功切人的时间
        #endregion

        #region 当前受控角色
        [Header("=== 当前受控角色 ===")]
        [Header("当前受控角色的共享上下文（不可手动修改）")]
        [SerializeField] private CharacterContext currentCharacterContext;
        #endregion

        #region 外部访问
        public bool IsInitialized { get; private set; }
        public CharacterContext CurrentCharacterContext => currentCharacterContext;
        #endregion

        #region 角色绑定模式
        // 角色绑定模式：区分读档落位和运行中切人落位
        private enum CharacterBindMode
        {
            FromSavedTransform, // 使用存档记录的位置旋转
            FromCurrentCharacter // 继承当前受控角色的位置旋转
        }
        #endregion

        #region 生命周期
        private void OnDestroy()
        {
            // 组件销毁时解绑输入事件，避免残留委托引用
            UnsubscribeInputEvents();
        }
        #endregion

        #region 初始化
        // 初始化角色切换器
        public void Initialize(
            PlayerTeamController playerTeamController,
            PlayerRuntimeData playerRuntimeData,
            PlayerInputReader playerInputReader,
            PlayerCamera playerCamera
        )
        {
            // 1.重新初始化前先解绑旧输入事件
            UnsubscribeInputEvents();

            // 2.缓存角色切换需要的核心依赖
            this.playerTeamController = playerTeamController;
            this.playerRuntimeData = playerRuntimeData;
            this.playerInputReader = playerInputReader;
            this.playerCamera = playerCamera;

            // 3.校验核心依赖
            if (!ValidateDependencies())
            {
                IsInitialized = false;
                return;
            }

            // 4.订阅输入层切人事件
            SubscribeInputEvents();

            // 5.标记初始化完成
            IsInitialized = true;
        }

        // 校验角色切换器需要的核心依赖
        private bool ValidateDependencies()
        {
            bool isValid = true;

            // 1.校验玩家队伍控制器
            if (playerTeamController == null)
            {
                Debug.LogError("[PlayerCharacterSwitcher] 初始化失败：PlayerTeamController为空。", this);
                isValid = false;
            }
            else if (!playerTeamController.IsInitialized)
            {
                Debug.LogError("[PlayerCharacterSwitcher] 初始化失败：PlayerTeamController尚未初始化。", this);
                isValid = false;
            }

            // 2.校验玩家运行时数据
            if (playerRuntimeData == null)
            {
                Debug.LogError("[PlayerCharacterSwitcher] 初始化失败：PlayerRuntimeData为空。", this);
                isValid = false;
            }

            // 3.校验玩家输入读取器
            if (playerInputReader == null)
            {
                Debug.LogError("[PlayerCharacterSwitcher] 初始化失败：PlayerInputReader为空。", this);
                isValid = false;
            }

            // 4.校验玩家相机控制器
            if (playerCamera == null)
            {
                Debug.LogError("[PlayerCharacterSwitcher] 初始化失败：PlayerCamera为空。", this);
                isValid = false;
            }

            return isValid;
        }
        #endregion

        #region 输入事件
        // 订阅输入层切人事件
        private void SubscribeInputEvents()
        {
            if (playerInputReader == null)
            {
                return;
            }

            playerInputReader.OnSwitchCharacterRequested -= HandleSwitchCharacterRequest;
            playerInputReader.OnSwitchCharacterRequested += HandleSwitchCharacterRequest;
        }

        // 解绑输入层切人事件
        private void UnsubscribeInputEvents()
        {
            if (playerInputReader == null)
            {
                return;
            }

            playerInputReader.OnSwitchCharacterRequested -= HandleSwitchCharacterRequest;
        }

        // 处理输入层发来的切人请求：targetSlot为1、2、3
        private void HandleSwitchCharacterRequest(int targetSlot)
        {
            // 1.把输入槽位转换成从0开始的数组索引
            int targetIndex = targetSlot - 1;

            // 2.尝试切换到目标角色
            SwitchToCharacter(targetIndex);
        }
        #endregion

        #region 首次绑定
        // 绑定新建或者读档后的初始角色
        public bool BindInitialCharacter()
        {
            // 1.校验是否可以首次绑定
            if (!CanBindInitialCharacter())
            {
                return false;
            }

            // 2.解析存档记录的当前角色索引
            int targetIndex = ResolveInitialCharacterIndex();
            if (targetIndex < 0)
            {
                Debug.LogError("[PlayerCharacterSwitcher] 首次绑定失败：队伍中没有可用角色。", this);
                return false;
            }

            // 3.读取对应队伍角色
            CharacterContext targetContext = playerTeamController.GetCharacter(targetIndex);

            // 4.首次绑定使用存档记录的位置和旋转
            return BindCharacter(targetContext, CharacterBindMode.FromSavedTransform);
        }

        // 判断当前是否可以进行首次绑定
        private bool CanBindInitialCharacter()
        {
            // 1.切换器尚未初始化时不能绑定
            if (!IsInitialized)
            {
                Debug.LogError("[PlayerCharacterSwitcher] 首次绑定失败：组件尚未初始化。", this);
                return false;
            }

            // 2.队伍为空时不能绑定
            if (playerTeamController.TeamCharacterCount == 0)
            {
                Debug.LogError("[PlayerCharacterSwitcher] 首次绑定失败：队伍角色数量为0。", this);
                return false;
            }

            return true;
        }

        // 解析首次绑定使用的角色索引
        private int ResolveInitialCharacterIndex()
        {
            IReadOnlyList<CharacterContext> teamCharacters = playerTeamController.TeamCharacters;

            // 1.限制存档索引范围
            int targetIndex = Mathf.Clamp(
                playerRuntimeData.CurrentCharacterIndex,
                0,
                teamCharacters.Count - 1
            );

            // 2.存档记录的槽位存在角色时直接使用
            if (teamCharacters[targetIndex] != null)
            {
                return targetIndex;
            }

            // 3.存档槽位为空时查找第一个可用角色
            for (int i = 0; i < teamCharacters.Count; i++)
            {
                if (teamCharacters[i] != null)
                {
                    return i;
                }
            }

            return -1;
        }
        #endregion

        #region 运行中切人
        // 切换到指定队伍索引的角色：targetIndex从0开始
        public bool SwitchToCharacter(int targetIndex)
        {
            // 1.校验目标角色是否允许切换
            if (!CanSwitchToCharacter(targetIndex))
            {
                return false;
            }

            // 2.记录切换前后的角色
            CharacterContext previousContext = currentCharacterContext;
            CharacterContext targetContext = playerTeamController.GetCharacter(targetIndex);

            // 3.运行中切人时继承当前角色的位置旋转
            if (!BindCharacter(targetContext, CharacterBindMode.FromCurrentCharacter))
            {
                return false;
            }

            // 4.记录本次成功切人的时间
            lastSwitchCharacterTime = Time.time;

            // 5.通知其他系统当前角色已经变化
            GameEvents.RaiseCharacterSwitched(previousContext, currentCharacterContext);

            return true;
        }

        // 判断是否可以切换到指定角色
        private bool CanSwitchToCharacter(int targetIndex)
        {
            // 1.切换器尚未初始化时不能切人
            if (!IsInitialized)
            {
                return false;
            }

            // 2.目标索引非法时不能切人
            if (targetIndex < 0
                || targetIndex >= playerTeamController.TeamCharacterCount)
            {
                return false;
            }

            CharacterContext targetContext = playerTeamController.GetCharacter(targetIndex);

            // 3.目标角色为空时不能切人
            if (targetContext == null)
            {
                return false;
            }

            // 4.目标角色已经是当前角色时不重复切换
            if (targetContext == currentCharacterContext)
            {
                return false;
            }

            // 5.切人间隔尚未结束时不能切人
            if (Time.time - lastSwitchCharacterTime < switchCharacterInterval)
            {
                return false;
            }

            // 6.目标角色死亡时不能切人
            if (targetContext.RuntimeData != null
                && targetContext.RuntimeData.IsDead)
            {
                return false;
            }

            // 7.当前角色处于不可打断状态时不能切人
            if (currentCharacterContext != null
                && currentCharacterContext.StateMachine != null
                && !currentCharacterContext.StateMachine.IsInterruptible())
            {
                return false;
            }

            return true;
        }
        #endregion

        #region 绑定当前角色
        // 绑定指定角色
        private bool BindCharacter(
            CharacterContext context,
            CharacterBindMode bindMode
        )
        {
            // 1.校验目标角色
            if (!CanBindCharacter(context))
            {
                return false;
            }

            // 2.同步当前受控角色索引
            if (!SyncCurrentCharacterIndex(context))
            {
                return false;
            }

            // 3.先移动目标角色，避免在旧位置激活
            ApplyPlayerTransformToCharacter(context, bindMode);

            // 4.只激活目标角色
            SetOnlyCurrentCharacterActive(context);

            // 5.更新当前角色并重新绑定相关系统
            SetCurrentCharacterContext(context);

            return true;
        }

        // 判断目标角色是否可以绑定
        private bool CanBindCharacter(CharacterContext context)
        {
            // 1.目标角色为空时不能绑定
            if (context == null)
            {
                Debug.LogError("[PlayerCharacterSwitcher] 绑定失败：CharacterContext为空。", this);
                return false;
            }

            // 2.队伍为空时不能绑定
            if (playerTeamController == null
                || playerTeamController.TeamCharacterCount == 0)
            {
                Debug.LogError("[PlayerCharacterSwitcher] 绑定失败：队伍为空。", this);
                return false;
            }

            // 3.运行时数据为空时不能同步索引
            if (playerRuntimeData == null)
            {
                Debug.LogError("[PlayerCharacterSwitcher] 绑定失败：PlayerRuntimeData为空。", this);
                return false;
            }

            return true;
        }

        // 根据角色上下文同步当前角色索引
        private bool SyncCurrentCharacterIndex(CharacterContext context)
        {
            IReadOnlyList<CharacterContext> teamCharacters = playerTeamController.TeamCharacters;

            // 1.查找目标角色对应的队伍索引
            for (int i = 0; i < teamCharacters.Count; i++)
            {
                if (teamCharacters[i] == context)
                {
                    playerRuntimeData.UpdateCurrentCharacterIndex(i);
                    return true;
                }
            }

            // 2.没有找到时说明角色不属于当前队伍
            Debug.LogError(
                $"[PlayerCharacterSwitcher] 同步角色索引失败：角色 {context.name} 不属于当前队伍。",
                context
            );
            return false;
        }

        // 根据绑定模式设置目标角色位置
        private void ApplyPlayerTransformToCharacter(
            CharacterContext context,
            CharacterBindMode bindMode
        )
        {
            // 1.新建或读档后的首次绑定使用存档位置
            if (bindMode == CharacterBindMode.FromSavedTransform)
            {
                context.transform.position = playerRuntimeData.PlayerPosition;
                context.transform.rotation = Quaternion.Euler(
                    playerRuntimeData.PlayerEulerAngles
                );
                return;
            }

            // 2.运行中切人时继承当前角色的位置旋转
            if (bindMode == CharacterBindMode.FromCurrentCharacter
                && currentCharacterContext != null)
            {
                context.transform.position = currentCharacterContext.transform.position;
                context.transform.rotation = currentCharacterContext.transform.rotation;
                return;
            }

            // 3.当前角色为空时兜底使用存档位置
            context.transform.position = playerRuntimeData.PlayerPosition;
            context.transform.rotation = Quaternion.Euler(
                playerRuntimeData.PlayerEulerAngles
            );
        }

        // 只激活当前受控角色
        private void SetOnlyCurrentCharacterActive(CharacterContext context)
        {
            IReadOnlyList<CharacterContext> teamCharacters = playerTeamController.TeamCharacters;

            for (int i = 0; i < teamCharacters.Count; i++)
            {
                CharacterContext teamCharacter = teamCharacters[i];
                if (teamCharacter == null)
                {
                    continue;
                }

                teamCharacter.gameObject.SetActive(teamCharacter == context);
            }
        }

        // 更新当前角色，并重新绑定输入、相机和UI
        private void SetCurrentCharacterContext(CharacterContext context)
        {
            // 1.更新当前角色缓存
            currentCharacterContext = context;

            // 2.把输入缓冲绑定到当前角色
            playerInputReader?.BindInputBuffer(context.InputBuffer);

            // 3.强制目标角色回到默认状态
            context.StateMachine?.ForceResetToDefaultState();

            // 4.把相机绑定到当前角色观察点
            playerCamera?.BindCameraPivot(context.CameraTarget);

            // 5.把HUD和小地图绑定到当前角色
            UIRoot.Instance?.BindCharacterContext(context);
        }
        #endregion

        #region 清理当前角色绑定
        // 清理当前受控角色绑定：清理队伍前调用
        public void ClearCurrentCharacterBinding()
        {
            // 1.清空当前角色缓存
            currentCharacterContext = null;

            // 2.解绑角色输入缓冲
            playerInputReader?.BindInputBuffer(null);

            // 3.解绑相机观察点
            playerCamera?.BindCameraPivot(null);

            // 4.重置切人间隔
            lastSwitchCharacterTime = -999f;
        }
        #endregion
    }
}