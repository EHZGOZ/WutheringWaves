using System.Collections.Generic;
using UnityEngine;

// 命名空间：类魂游戏专用的小地图功能模块
namespace WutheringWaves
{
    /// <summary>
    /// 小地图标记类型枚举
    /// 定义所有可在小地图上显示的物体类型
    /// </summary>
    public enum MiniMapMarkerType
    {
        Enemy,       // 敌人
        Objective,   // 任务目标
        Interactable,// 可交互物体（如开关、道具）
        Custom,      // 自定义标记
        NPC,         // 非玩家角色
        Treasure,    // 宝藏/战利品
        Teleport     // 传送点
    }

    /// <summary>
    /// 小地图标记数据源组件
    /// 功能：将场景中的物体注册为小地图标记，提供标记的所有配置数据
    /// 使用方式：挂载到需要显示在小地图上的任意物体上
    /// </summary>
    [DisallowMultipleComponent]  // 特性：禁止同一个物体挂载多个该组件
    public class MiniMapMarker : MonoBehaviour
    {
        /// <summary>
        /// 静态列表：全局存储所有**激活状态**的小地图标记
        /// 小地图管理器会通过这个列表获取所有需要绘制的标记
        /// </summary>
        private static readonly List<MiniMapMarker> activeMarkers = new List<MiniMapMarker>();

        #region 序列化字段（Inspector面板可编辑）
        [Header("跟随目标设置")]
        [Tooltip("可选：指定标记跟随的目标物体\n为空则默认跟随当前挂载的物体")]
        [SerializeField] private Transform targetOverride;

        [Header("外观样式设置")]
        [Tooltip("标记的类型（决定默认图标/样式）")]
        [SerializeField] private MiniMapMarkerType markerType = MiniMapMarkerType.Enemy;
        [Tooltip("可选：自定义图标（覆盖类型对应的默认图标）")]
        [SerializeField] private Sprite iconOverride;
        [Tooltip("标记的颜色")]
        [SerializeField] private Color markerColor = new Color(1f, 0.42f, 0.30f, 1f);
        [Tooltip("标记的尺寸（范围：8~32像素）")]
        [SerializeField][Range(8f, 32f)] private float markerSize = 14f;

        [Header("行为逻辑设置")]
        [Tooltip("是否在小地图上显示该标记")]
        [SerializeField] private bool showOnMiniMap = true;
        [Tooltip("标记是否跟随3D世界中的物体一起旋转")]
        [SerializeField] private bool rotateWithWorldObject = false;
        [Tooltip("标记超出小地图范围时，是否贴在小地图边缘显示")]
        [SerializeField] private bool clampToBorder = true;
        [Tooltip("标记在世界坐标中的偏移量（调整标记在物体上的显示位置）")]
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.5f, 0f);
        #endregion

        #region 只读属性（对外提供数据访问，禁止外部修改）
        /// <summary>
        /// 获取所有激活的小地图标记（只读，外部无法修改列表）
        /// </summary>
        public static IReadOnlyList<MiniMapMarker> ActiveMarkers => activeMarkers;

        /// <summary>
        /// 获取标记最终跟随的目标Transform
        /// 优先级：自定义目标 > 当前物体自身
        /// </summary>
        public Transform Target => targetOverride != null ? targetOverride : transform;

        /// <summary>
        /// 标记类型
        /// </summary>
        public MiniMapMarkerType MarkerType => markerType;

        /// <summary>
        /// 自定义图标
        /// </summary>
        public Sprite IconOverride => iconOverride;

        /// <summary>
        /// 标记颜色
        /// </summary>
        public Color MarkerColor => markerColor;

        /// <summary>
        /// 标记尺寸
        /// </summary>
        public float MarkerSize => markerSize;

        /// <summary>
        /// 是否显示在小地图
        /// </summary>
        public bool ShowOnMiniMap => showOnMiniMap;

        /// <summary>
        /// 是否跟随物体旋转
        /// </summary>
        public bool RotateWithWorldObject => rotateWithWorldObject;

        /// <summary>
        /// 是否贴边显示
        /// </summary>
        public bool ClampToBorder => clampToBorder;

        /// <summary>
        /// 标记在3D世界中的最终坐标（目标位置 + 偏移量）
        /// 小地图会根据这个坐标绘制标记
        /// </summary>
        public Vector3 WorldPosition => (Target != null ? Target.position : transform.position) + worldOffset;
        #endregion

        #region 生命周期方法
        /// <summary>
        /// 组件启用时执行
        /// 将当前标记添加到全局激活列表中
        /// 防重复添加：避免列表中出现重复实例
        /// </summary>
        private void OnEnable()
        {
            if (!activeMarkers.Contains(this))
            {
                activeMarkers.Add(this);
            }
        }

        /// <summary>
        /// 组件禁用时执行
        /// 将当前标记从全局激活列表中移除
        /// </summary>
        private void OnDisable()
        {
            activeMarkers.Remove(this);
        }
        #endregion
    }
}
