using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WutheringWaves
{
    [DisallowMultipleComponent]
    // 小地图控制器：负责小地图相机跟随、朝向箭头和世界标记刷新
    public class MiniMapController : MonoBehaviour
    {
        [Header("小地图根面板")]
        [SerializeField] private GameObject miniMapPanel; // 小地图整体根节点

        [Header("小地图相机")]
        [SerializeField] private Transform miniMapCameraTransform; // 小地图相机Transform

        [Header("小地图底板")]
        [SerializeField] private RectTransform backplate; // 小地图底板，暂时只保留接口，后续可用于换皮肤或动画

        [Header("圆形遮罩")]
        [SerializeField] private RectTransform circleMask; // 圆形遮罩

        [Header("地图底图")]
        [SerializeField] private RawImage mapRawImage; // 地图底图

        [Header("标记点根节点")]
        [SerializeField] private RectTransform markerRoot; // 标记点根节点

        [Header("视角箭头")]
        [SerializeField] private RectTransform viewArrow; // 视角箭头

        [Header("角色朝向箭头")]
        [SerializeField] private RectTransform facingArrow; // 角色朝向箭头

        [Header("=== 相机跟随设置 ===")]
        [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 30f, 0f); // 小地图相机相对跟随偏移
        [SerializeField][Min(1f)] private float miniMapOrthographicSize = 25f; // 小地图正交相机尺寸，控制小地图能看到多少范围
        [SerializeField][Min(0f)] private float followSmoothTime = 0.1f; // 相机跟随平滑时间

        [Header("=== 标记点设置 ===")]
        [SerializeField] private Sprite defaultMarkerSprite; // 默认标记图标
        [SerializeField][Min(1f)] private float fallbackWorldRadius = 25f; // 非正交相机时的世界半径兜底值
        [SerializeField][Range(0.1f, 1f)] private float markerClampPadding = 0.92f; // 标记点贴边内缩比例

        private readonly Dictionary<MiniMapMarker, MarkerWidget> markerWidgets = new Dictionary<MiniMapMarker, MarkerWidget>(); // 标记对象到UI节点的映射

        private Camera miniMapCamera; // 小地图相机缓存
        private bool initialized; // 是否已完成初始化
        private bool isVisible = true; // 当前是否显示
        private Vector3 cameraVelocity; // SmoothDamp速度缓存

        private CharacterContext boundContext; // 当前绑定的角色上下文
        private Transform followTarget; // 小地图相机跟随目标
        private Transform viewTarget; // 视角朝向目标
        private Transform facingTarget; // 角色朝向目标

        #region 对外只读属性
        public CharacterContext BoundContext => boundContext;
        public Transform FollowTarget => followTarget;
        public Transform ViewTarget => viewTarget;
        public Transform FacingTarget => facingTarget;
        #endregion

        #region 生命周期
        private void Awake()
        {
            CacheCamera();
        }

        private void LateUpdate()
        {
            if (!initialized)
            {
                return;
            }

            if (!isVisible)
            {
                return;
            }

            UpdateCameraFollow();
            UpdateArrowRotation(viewArrow, viewTarget);
            UpdateArrowRotation(facingArrow, facingTarget);
            RefreshMarkers();
        }

        private void OnDestroy()
        {
            ClearMarkerWidgets();
        }
        #endregion

        #region 初始化
        // 初始化小地图控制器：只初始化自身，不绑定具体角色
        public void Initialize()
        {
            // 1.已经初始化过时直接返回，避免重复初始化
            if (initialized)
            {
                return;
            }

            // 2.缓存小地图相机
            CacheCamera();

            // 3.应用小地图正交相机尺寸
            ApplyMiniMapOrthographicSize();

            // 4.初始化运行时状态
            cameraVelocity = Vector3.zero;
            initialized = true;

            // 5.刷新当前显隐状态
            SetVisible(isVisible);
        }
        #endregion

        #region 绑定角色
        // 绑定当前角色上下文：由UIRoot.BindCharacterContext调用
        public void Bind(CharacterContext context)
        {
            // 1.空值检查
            if (context == null)
            {
                return;
            }

            // 2.缓存当前角色上下文
            boundContext = context;

            // 3.绑定小地图相机跟随目标：跟随当前角色本体
            followTarget = boundContext.transform;

            // 4.绑定角色朝向目标：使用当前角色本体朝向
            facingTarget = boundContext.transform;

            // 5.绑定视角朝向目标：优先使用PlayerController里的当前相机观察点
            ResolveViewTargetFromPlayerController();

            // 6.切换角色后清空相机平滑速度，避免镜头拖影
            cameraVelocity = Vector3.zero;

            // 7.刷新一次标记显示
            RefreshMarkers();
        }

        // 兼容旧调用：后续UIRoot可以统一改成Bind
        public void InjectDependencies(CharacterContext context)
        {
            Bind(context);
        }

        // 从玩家控制器中解析当前视角朝向目标
        private void ResolveViewTargetFromPlayerController()
        {
            viewTarget = null;

            if (PlayerController.Instance == null)
            {
                return;
            }

            PlayerCamera playerCamera = PlayerController.Instance.CurrentPlayerCamera;
            if (playerCamera == null)
            {
                return;
            }

            viewTarget = playerCamera.cameraPivot;
        }
        #endregion

        #region 外部调用
        // 设置小地图显隐
        public void SetVisible(bool visible)
        {
            // 1.缓存当前显隐状态
            isVisible = visible;

            // 2.统一控制小地图整体根节点显隐
            if (miniMapPanel != null)
            {
                miniMapPanel.SetActive(isVisible);
            }
        }
        #endregion

        #region 相机缓存
        private void CacheCamera()
        {
            // 小地图相机只从显式配置的Transform上获取，避免误抓场景主相机
            miniMapCamera = miniMapCameraTransform != null
                ? miniMapCameraTransform.GetComponent<Camera>()
                : null;
        }

        private void ApplyMiniMapOrthographicSize()
        {
            // 1.小地图相机为空时，无法应用尺寸
            if (miniMapCamera == null)
            {
                return;
            }

            // 2.小地图建议使用正交相机，保证显示范围稳定
            miniMapCamera.orthographic = true;

            // 3.应用Inspector中配置的小地图显示范围
            miniMapCamera.orthographicSize = miniMapOrthographicSize;
        }
        #endregion

        #region 小地图相机与箭头刷新
        private void UpdateCameraFollow()
        {
            // 1.缺少相机或跟随目标时不执行跟随
            if (miniMapCameraTransform == null || followTarget == null)
            {
                return;
            }

            // 2.计算目标位置
            Vector3 desiredPosition = followTarget.position + cameraOffset;

            // 3.不需要平滑时，直接同步位置
            if (followSmoothTime <= 0f)
            {
                miniMapCameraTransform.position = desiredPosition;
                return;
            }

            // 4.平滑跟随当前角色
            miniMapCameraTransform.position = Vector3.SmoothDamp(
                miniMapCameraTransform.position,
                desiredPosition,
                ref cameraVelocity,
                followSmoothTime);
        }

        private void UpdateArrowRotation(RectTransform arrow, Transform target)
        {
            // 1.箭头或目标为空时，直接跳过旋转更新
            if (arrow == null || target == null)
            {
                return;
            }

            // 2.只取水平面方向，避免上下俯仰影响小地图箭头
            Vector3 flatForward = Vector3.ProjectOnPlane(target.forward, Vector3.up);
            if (flatForward.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            // 3.世界朝向转换成UI上的Z轴旋转
            float angle = Mathf.Atan2(flatForward.x, flatForward.z) * Mathf.Rad2Deg;
            arrow.localEulerAngles = new Vector3(0f, 0f, -angle);
        }
        #endregion

        #region 标记点刷新
        private void RefreshMarkers()
        {
            // 1.小地图隐藏或缺少关键引用时，不刷新标记
            if (!isVisible || markerRoot == null || followTarget == null)
            {
                return;
            }

            // 2.记录本轮仍然存在的标记，用于清理已经失效的标记UI
            IReadOnlyList<MiniMapMarker> markers = MiniMapMarker.ActiveMarkers;
            HashSet<MiniMapMarker> activeSet = new HashSet<MiniMapMarker>();

            // 3.刷新所有激活标记
            for (int i = 0; i < markers.Count; i++)
            {
                MiniMapMarker marker = markers[i];
                if (marker == null)
                {
                    continue;
                }

                activeSet.Add(marker);

                if (!marker.ShowOnMiniMap)
                {
                    SetMarkerWidgetVisible(marker, false);
                    continue;
                }

                MarkerWidget widget = GetOrCreateMarkerWidget(marker);
                UpdateMarkerWidget(marker, widget);
            }

            // 4.清理已经不存在或失效的标记UI
            ClearInvalidMarkerWidgets(activeSet);
        }

        private MarkerWidget GetOrCreateMarkerWidget(MiniMapMarker marker)
        {
            // 1.复用已存在的UI节点，减少运行时反复创建对象的开销
            if (markerWidgets.TryGetValue(marker, out MarkerWidget widget) && widget.Root != null)
            {
                return widget;
            }

            // 2.创建新的标记UI对象
            GameObject markerObject = new GameObject($"{marker.name}_MiniMapMarker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rectTransform = markerObject.GetComponent<RectTransform>();
            rectTransform.SetParent(markerRoot, false);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            // 3.初始化标记图片
            Image image = markerObject.GetComponent<Image>();
            image.raycastTarget = false;

            // 4.缓存标记UI
            widget = new MarkerWidget(rectTransform, image);
            markerWidgets[marker] = widget;
            return widget;
        }

        private void UpdateMarkerWidget(MiniMapMarker marker, MarkerWidget widget)
        {
            // 1.空值检查
            if (marker == null || widget == null || widget.Root == null || widget.Image == null)
            {
                return;
            }

            // 2.优先使用MarkerRoot作为坐标计算基准，缺失时回退到圆形遮罩
            RectTransform rootRect = markerRoot != null && markerRoot.rect.size.sqrMagnitude > 0f
                ? markerRoot
                : circleMask;

            if (rootRect == null)
            {
                return;
            }

            // 3.计算小地图世界半径
            float worldRadius = GetWorldRadius();
            if (worldRadius <= 0f)
            {
                worldRadius = fallbackWorldRadius;
            }

            // 4.把世界坐标差值转换为小地图UI坐标
            Vector3 worldDelta = marker.WorldPosition - followTarget.position;
            Vector2 localPosition = WorldDeltaToAnchoredPosition(worldDelta, rootRect.rect.size, worldRadius);
            bool clamped = false;

            // 5.需要贴边时，把超出范围的标记压回小地图边缘
            if (marker.ClampToBorder)
            {
                localPosition = ClampToMarkerBounds(localPosition, rootRect.rect.size, out clamped);
            }

            // 6.刷新标记UI基础显示
            widget.Root.anchoredPosition = localPosition;
            widget.Root.sizeDelta = Vector2.one * marker.MarkerSize;
            widget.Root.gameObject.SetActive(true);

            // 7.刷新标记图标和颜色
            Sprite sprite = marker.IconOverride != null ? marker.IconOverride : defaultMarkerSprite;
            widget.Image.sprite = sprite;
            widget.Image.color = marker.MarkerColor;
            widget.Image.enabled = sprite != null;

            // 8.刷新标记旋转
            UpdateMarkerRotation(marker, widget);

            // 9.贴边标记放到最后显示，避免被其他标记压住
            if (clamped)
            {
                widget.Root.SetAsLastSibling();
            }
        }

        private void UpdateMarkerRotation(MiniMapMarker marker, MarkerWidget widget)
        {
            // 1.不需要跟随世界物体旋转时，保持默认朝向
            if (!marker.RotateWithWorldObject || marker.Target == null)
            {
                widget.Root.localEulerAngles = Vector3.zero;
                return;
            }

            // 2.计算世界物体水平朝向
            Vector3 flatForward = Vector3.ProjectOnPlane(marker.Target.forward, Vector3.up);
            float angle = flatForward.sqrMagnitude > 0.0001f
                ? Mathf.Atan2(flatForward.x, flatForward.z) * Mathf.Rad2Deg
                : 0f;

            // 3.同步到UI旋转
            widget.Root.localEulerAngles = new Vector3(0f, 0f, -angle);
        }

        private Vector2 WorldDeltaToAnchoredPosition(Vector3 worldDelta, Vector2 mapSize, float worldRadius)
        {
            // 1.使用小地图相机的前向和右向，把世界位移映射到UI局部坐标
            Vector3 mapForward = miniMapCameraTransform != null ? miniMapCameraTransform.forward : Vector3.forward;
            Vector3 mapRight = miniMapCameraTransform != null ? miniMapCameraTransform.right : Vector3.right;

            // 2.只保留水平面方向
            mapForward = Vector3.ProjectOnPlane(mapForward, Vector3.up).normalized;
            mapRight = Vector3.ProjectOnPlane(mapRight, Vector3.up).normalized;

            // 3.方向异常时使用默认方向兜底
            if (mapForward.sqrMagnitude <= 0.0001f)
            {
                mapForward = Vector3.forward;
            }

            if (mapRight.sqrMagnitude <= 0.0001f)
            {
                mapRight = Vector3.right;
            }

            // 4.把世界位移投影到小地图坐标轴
            float x = Vector3.Dot(worldDelta, mapRight);
            float y = Vector3.Dot(worldDelta, mapForward);

            // 5.转换成UI局部坐标
            float normalizedX = x / worldRadius;
            float normalizedY = y / worldRadius;

            return new Vector2(
                normalizedX * (mapSize.x * 0.5f),
                normalizedY * (mapSize.y * 0.5f));
        }

        private Vector2 ClampToMarkerBounds(Vector2 anchoredPosition, Vector2 mapSize, out bool clamped)
        {
            // 1.计算允许显示的范围
            float halfWidth = mapSize.x * 0.5f * markerClampPadding;
            float halfHeight = mapSize.y * 0.5f * markerClampPadding;

            // 2.归一化当前位置，判断是否超过圆形/椭圆边界
            float normalizedX = halfWidth > 0f ? anchoredPosition.x / halfWidth : 0f;
            float normalizedY = halfHeight > 0f ? anchoredPosition.y / halfHeight : 0f;
            float magnitude = Mathf.Sqrt((normalizedX * normalizedX) + (normalizedY * normalizedY));

            clamped = magnitude > 1f;

            // 3.没有越界时保持原位置
            if (!clamped || magnitude <= 0f)
            {
                return anchoredPosition;
            }

            // 4.越界时压回边缘
            float scale = 1f / magnitude;
            return new Vector2(
                anchoredPosition.x * scale,
                anchoredPosition.y * scale);
        }

        private float GetWorldRadius()
        {
            // 1.正交相机时优先使用相机尺寸
            if (miniMapCamera != null && miniMapCamera.orthographic)
            {
                return miniMapCamera.orthographicSize;
            }

            // 2.非正交相机时使用手动配置的兜底半径
            return fallbackWorldRadius;
        }

        private void SetMarkerWidgetVisible(MiniMapMarker marker, bool visible)
        {
            // 1.没有创建过UI节点时，不需要处理
            if (!markerWidgets.TryGetValue(marker, out MarkerWidget widget) || widget.Root == null)
            {
                return;
            }

            // 2.设置标记UI显隐
            widget.Root.gameObject.SetActive(visible);
        }

        private void ClearInvalidMarkerWidgets(HashSet<MiniMapMarker> activeSet)
        {
            // 1.收集需要清理的标记
            List<MiniMapMarker> toRemove = new List<MiniMapMarker>();

            foreach (KeyValuePair<MiniMapMarker, MarkerWidget> pair in markerWidgets)
            {
                if (pair.Key != null && activeSet.Contains(pair.Key))
                {
                    continue;
                }

                if (pair.Value.Root != null)
                {
                    Destroy(pair.Value.Root.gameObject);
                }

                toRemove.Add(pair.Key);
            }

            // 2.从缓存字典移除失效标记
            for (int i = 0; i < toRemove.Count; i++)
            {
                markerWidgets.Remove(toRemove[i]);
            }
        }

        private void ClearMarkerWidgets()
        {
            // 1.销毁所有运行时生成的标记点UI，避免场景切换后残留
            foreach (KeyValuePair<MiniMapMarker, MarkerWidget> pair in markerWidgets)
            {
                if (pair.Value.Root != null)
                {
                    Destroy(pair.Value.Root.gameObject);
                }
            }

            // 2.清空缓存
            markerWidgets.Clear();
        }

        private sealed class MarkerWidget
        {
            public MarkerWidget(RectTransform root, Image image)
            {
                Root = root;
                Image = image;
            }

            public RectTransform Root { get; }
            public Image Image { get; }
        }
        #endregion
    }
}
