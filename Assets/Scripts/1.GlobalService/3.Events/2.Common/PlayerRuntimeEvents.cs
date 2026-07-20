using System;
using UnityEngine;

namespace WutheringWaves
{
    // 玩家运行数据事件：负责把场景中的玩家状态变化通知给PlayerRuntimeData
    public static class PlayerRuntimeEvents
    {
        #region 事件定义
        // 当前受控角色索引变化事件
        // 参数为从0开始的队伍角色索引
        public static event Action<int> OnCurrentCharacterIndexChanged;

        // 当前角色位置旋转变化事件
        // 第一个参数为世界坐标，第二个参数为欧拉角旋转
        public static event Action<Vector3, Vector3> OnPlayerTransformChanged;
        #endregion

        #region 事件派发
        // 派发当前受控角色索引变化事件
        public static void RaiseCurrentCharacterIndexChanged(int characterIndex)
        {
            OnCurrentCharacterIndexChanged?.Invoke(characterIndex);
        }

        // 派发当前角色位置旋转变化事件
        public static void RaisePlayerTransformChanged(Vector3 position, Vector3 eulerAngles)
        {
            OnPlayerTransformChanged?.Invoke(position, eulerAngles);
        }
        #endregion
    }
}