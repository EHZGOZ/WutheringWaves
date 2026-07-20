using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WutheringWaves
{
    public class JinxiEvents : MonoBehaviour
    {
        #region 事件定义
        // 角色专属：今汐
        // 御空状态变化事件：参数为攻击逻辑、是否处于御空
        public static event Action<bool> OnFloatingChanged;

        // 爆发状态变化事件
        public static event Action<bool> OnBrustChanged;
        #endregion

        #region 事件派发
        // 派发御空状态变化事件
        public static void RaiseFloatingChanged(bool isFloating)
        {
            OnFloatingChanged?.Invoke(isFloating);
        }
        // 派发爆发状态变化事件
        public static void RaiseBrustChanged(bool isBrust)
        {
            OnBrustChanged?.Invoke(isBrust);
        }
        #endregion

    }
}

