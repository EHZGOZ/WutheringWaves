using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WutheringWaves
{
    [DisallowMultipleComponent]
    // 小地图控制器：负责相机跟随、箭头朝向和世界标记刷新
    public class MiniMapController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RectTransform backplate; // 小地图底板
        [SerializeField] private RectTransform circleMask; // 圆形遮罩
        [SerializeField] private RawImage mapRawImage; // 地图底图
        [SerializeField] private RectTransform markerRoot; // 标记点根节点
        [SerializeField] private RectTransform border; // 边框
        [SerializeField] private RectTransform facingArrowRoot; // 朝向箭头父节点
        [SerializeField] private RectTransform viewArrow; // 视角箭头
        [SerializeField] private RectTransform facingArrow; // 角色朝向箭头

        [Header("World References")]
        [SerializeField] private Transform miniMapCameraTransform; // 小地图相机 Transform
        [SerializeField] private Transform followTarget; // 相机跟随目标
        [Tooltip("绑定代表主相机朝向的世界坐标 Transform，而不是 UI 箭头本身")]
        [SerializeField] private Transform viewTarget;
        [Tooltip("绑定代表玩家朝向的世界坐标 Transform，而不是 UI 箭头本身")]
        [SerializeField] private Transform facingTarget;

        [Header("Camera Follow")]
        [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 30f, 0f); // 小地图相机相对跟随偏移
        [SerializeField] [Min(0f)] private float followSmoothTime = 0.1f; // 相机跟随平滑时间

        [Header("Marker Settings")]
        [SerializeField] [Min(1f)] private float fallbackWorldRadius = 25f;
        [SerializeField] [Range(0.1f, 1f)] private float markerClampPadding = 0.92f;
        [SerializeField] private bool hideMarkersWhenControllerHidden = true;

        private readonly Dictionary<MiniMapMarker, MarkerWidget> markerWidgets = new Dictionary<MiniMapMarker, MarkerWidget>(); // 标记对象到 UI 节点的映射
        private Camera miniMapCamera; // 小地图相机缓存
        private bool initialized; // 是否已完成初始化
        private bool isVisible = true; // 当前是否显示
        private Vector3 cameraVelocity; // SmoothDamp 速度缓存
        private Sprite defaultMarkerSprite; // 默认标记图标
        private Texture2D fallbackMarkerTexture; // 默认图标运行时纹理
        private CharacterFacade boundFacade; // 当前绑定的角色门面

        public RectTransform Backplate => backplate;
        public RectTransform CircleMask => circleMask;
        public RawImage MapRawImage => mapRawImage;
        public RectTransform MarkerRoot => markerRoot;
        public RectTransform Border => border;
        public RectTransform FacingArrowRoot => facingArrowRoot;
        public RectTransform ViewArrow => viewArrow;
        public RectTransform FacingArrow => facingArrow;
        public Transform MiniMapCameraTransform => miniMapCameraTransform;
        public Transform FollowTarget => followTarget;
        public Transform ViewTarget => viewTarget;
        public Transform FacingTarget => facingTarget;

        private void Awake()
        {
            CacheCamera();
        }

        private void OnEnable()
        {
            if (initialized)
            {
                RefreshVisibility();
            }
        }

        private void Start()
        {
            if (!initialized)
            {
                Initialize();
            }
        }

        private void LateUpdate()
        {
            if (!initialized)
            {
                return;
            }

            UpdateCameraFollow();
            UpdateArrowRotation(viewArrow, viewTarget);
            UpdateArrowRotation(facingArrow, facingTarget);
            RefreshMarkers();
        }

        private void OnDisable()
        {
            SetMarkerWidgetsActive(false);
        }

        private void OnDestroy()
        {
            ClearMarkerWidgets();

            if (fallbackMarkerTexture != null)
            {
                Destroy(fallbackMarkerTexture);
                fallbackMarkerTexture = null;
            }
        }

        public void Initialize(CharacterFacade facade = null)
        {
            // 初始化时统一补齐相机、角色依赖和默认标记资源
            CacheCamera();
            BindCharacterFacade(facade);
            EnsureDefaultMarkerSprite();
            initialized = true;
            cameraVelocity = Vector3.zero;
            RefreshVisibility();
            RefreshMarkers();
        }

        public void InjectDependencies(CharacterFacade facade)
        {
            // 允许外部在角色生成后补充注入依赖
            BindCharacterFacade(facade);
        }

        public void SetVisible(bool visible)
        {
            isVisible = visible;
            RefreshVisibility();
        }

        public void ToggleVisible()
        {
            SetVisible(!isVisible);
        }

        public void SetFollowTarget(Transform target)
        {
            followTarget = target;
        }

        public void SetViewTarget(Transform target)
        {
            viewTarget = target;
        }

        public void SetFacingTarget(Transform target)
        {
            facingTarget = target;
        }

        private void BindCharacterFacade(CharacterFacade facade)
        {
            // 传入为空时不覆盖现有绑定，避免把当前目标清空
            if (facade == null)
            {
                return;
            }

            boundFacade = facade;

            if (followTarget == null)
            {
                followTarget = boundFacade.transform;
            }

            if (facingTarget == null)
            {
                facingTarget = boundFacade.transform;
            }

            CharacterContext context = boundFacade.Context;
            //if (viewTarget == null && context != null && context.PlayerCamera != null && context.PlayerCamera.cameraPivot != null)
            //{
            //    viewTarget = context.PlayerCamera.cameraPivot;
            //}
        }

        private void CacheCamera()
        {
            // 小地图相机只从显式配置的 Transform 上获取，避免误抓场景主相机
            miniMapCamera = miniMapCameraTransform != null ? miniMapCameraTransform.GetComponent<Camera>() : null;
        }

        private void RefreshVisibility()
        {
            // 控制器整体显隐时，同步处理底图、箭头和标记点显示状态
            bool shouldShow = isVisible && isActiveAndEnabled;

            if (mapRawImage != null)
            {
                mapRawImage.enabled = shouldShow;
            }

            if (markerRoot != null && hideMarkersWhenControllerHidden)
            {
                markerRoot.gameObject.SetActive(shouldShow);
            }

            if (facingArrowRoot != null)
            {
                facingArrowRoot.gameObject.SetActive(shouldShow);
            }

            SetOptionalGraphicVisible(backplate, shouldShow);
            SetOptionalGraphicVisible(circleMask, shouldShow);
            SetOptionalGraphicVisible(border, shouldShow);

            if (!shouldShow)
            {
                SetMarkerWidgetsActive(false);
            }
        }

        private void SetOptionalGraphicVisible(RectTransform target, bool visible)
        {
            if (target == null)
            {
                return;
            }

            Graphic graphic = target.GetComponent<Graphic>();
            if (graphic != null)
            {
                graphic.enabled = visible;
            }
            else
            {
                target.gameObject.SetActive(visible);
            }
        }

        private void UpdateCameraFollow()
        {
            // 缺少相机或跟随目标时不执行跟随
            if (miniMapCameraTransform == null || followTarget == null)
            {
                return;
            }

            Vector3 desiredPosition = followTarget.position + cameraOffset;
            if (followSmoothTime <= 0f)
            {
                miniMapCameraTransform.position = desiredPosition;
                return;
            }

            miniMapCameraTransform.position = Vector3.SmoothDamp(
                miniMapCameraTransform.position,
                desiredPosition,
                ref cameraVelocity,
                followSmoothTime);
        }

        private void UpdateArrowRotation(RectTransform arrow, Transform target)
        {
            // 箭头或目标为空时，直接跳过旋转更新
            if (arrow == null || target == null)
            {
                return;
            }

            Vector3 flatForward = Vector3.ProjectOnPlane(target.forward, Vector3.up);
            if (flatForward.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            float angle = Mathf.Atan2(flatForward.x, flatForward.z) * Mathf.Rad2Deg;
            arrow.localEulerAngles = new Vector3(0f, 0f, -angle);
        }

        private void EnsureDefaultMarkerSprite()
        {
            // 外部未提供图标时，运行时创建一个最小白色精灵作为默认占位
            if (defaultMarkerSprite != null)
            {
                return;
            }

            fallbackMarkerTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false)
            {
                name = "MiniMapMarkerFallback"
            };
            fallbackMarkerTexture.SetPixel(0, 0, Color.white);
            fallbackMarkerTexture.Apply();

            defaultMarkerSprite = Sprite.Create(
                fallbackMarkerTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
        }

        private void RefreshMarkers()
        {
            // 小地图隐藏或缺少根节点时，不刷新标记
            if (markerRoot == null || followTarget == null || !isVisible)
            {
                return;
            }

            IReadOnlyList<MiniMapMarker> markers = MiniMapMarker.ActiveMarkers;
            HashSet<MiniMapMarker> activeSet = new HashSet<MiniMapMarker>();

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

            List<MiniMapMarker> toRemove = new List<MiniMapMarker>();
            foreach (KeyValuePair<MiniMapMarker, MarkerWidget> pair in markerWidgets)
            {
                if (!activeSet.Contains(pair.Key) || pair.Key == null)
                {
                    if (pair.Value.Root != null)
                    {
                        Destroy(pair.Value.Root.gameObject);
                    }

                    toRemove.Add(pair.Key);
                }
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                markerWidgets.Remove(toRemove[i]);
            }
        }

        private MarkerWidget GetOrCreateMarkerWidget(MiniMapMarker marker)
        {
            // 复用已存在的 UI 节点，减少运行时反复创建对象的开销
            if (markerWidgets.TryGetValue(marker, out MarkerWidget widget) && widget.Root != null)
            {
                return widget;
            }

            GameObject markerObject = new GameObject($"{marker.name}_MiniMapMarker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rectTransform = markerObject.GetComponent<RectTransform>();
            rectTransform.SetParent(markerRoot, false);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            Image image = markerObject.GetComponent<Image>();
            image.raycastTarget = false;

            widget = new MarkerWidget(rectTransform, image);
            markerWidgets[marker] = widget;
            return widget;
        }

        private void UpdateMarkerWidget(MiniMapMarker marker, MarkerWidget widget)
        {
            // 优先使用 MarkerRoot 作为坐标计算基准，缺失时回退到圆形遮罩
            RectTransform rootRect = markerRoot.rect.size.sqrMagnitude > 0f ? markerRoot : circleMask;
            if (rootRect == null)
            {
                return;
            }

            float worldRadius = GetWorldRadius();
            if (worldRadius <= 0f)
            {
                worldRadius = fallbackWorldRadius;
            }

            Vector3 worldDelta = marker.WorldPosition - followTarget.position;
            Vector2 localPosition = WorldDeltaToAnchoredPosition(worldDelta, rootRect.rect.size, worldRadius);
            bool clamped = false;

            if (marker.ClampToBorder)
            {
                localPosition = ClampToMarkerBounds(localPosition, rootRect.rect.size, out clamped);
            }

            widget.Root.anchoredPosition = localPosition;
            widget.Root.sizeDelta = Vector2.one * marker.MarkerSize;
            widget.Root.gameObject.SetActive(true);

            widget.Image.sprite = marker.IconOverride != null ? marker.IconOverride : defaultMarkerSprite;
            widget.Image.color = marker.MarkerColor;
            widget.Image.enabled = widget.Image.sprite != null;

            if (marker.RotateWithWorldObject && marker.Target != null)
            {
                Vector3 flatForward = Vector3.ProjectOnPlane(marker.Target.forward, Vector3.up);
                float angle = flatForward.sqrMagnitude > 0.0001f
                    ? Mathf.Atan2(flatForward.x, flatForward.z) * Mathf.Rad2Deg
                    : 0f;
                widget.Root.localEulerAngles = new Vector3(0f, 0f, -angle);
            }
            else
            {
                widget.Root.localEulerAngles = Vector3.zero;
            }

            if (clamped)
            {
                widget.Root.SetAsLastSibling();
            }
        }

        private Vector2 WorldDeltaToAnchoredPosition(Vector3 worldDelta, Vector2 mapSize, float worldRadius)
        {
            // 使用小地图相机的前向和右向，把世界位移映射到 UI 局部坐标
            Vector3 mapForward = miniMapCameraTransform != null ? miniMapCameraTransform.forward : Vector3.forward;
            Vector3 mapRight = miniMapCameraTransform != null ? miniMapCameraTransform.right : Vector3.right;
            mapForward = Vector3.ProjectOnPlane(mapForward, Vector3.up).normalized;
            mapRight = Vector3.ProjectOnPlane(mapRight, Vector3.up).normalized;

            if (mapForward.sqrMagnitude <= 0.0001f)
            {
                mapForward = Vector3.forward;
            }

            if (mapRight.sqrMagnitude <= 0.0001f)
            {
                mapRight = Vector3.right;
            }

            float x = Vector3.Dot(worldDelta, mapRight);
            float y = Vector3.Dot(worldDelta, mapForward);
            float normalizedX = x / worldRadius;
            float normalizedY = y / worldRadius;

            return new Vector2(
                normalizedX * (mapSize.x * 0.5f),
                normalizedY * (mapSize.y * 0.5f));
        }

        private Vector2 ClampToMarkerBounds(Vector2 anchoredPosition, Vector2 mapSize, out bool clamped)
        {
            // 将超出边界的标记点压回小地图范围内，常用于边界指示
            float halfWidth = mapSize.x * 0.5f * markerClampPadding;
            float halfHeight = mapSize.y * 0.5f * markerClampPadding;

            float normalizedX = halfWidth > 0f ? anchoredPosition.x / halfWidth : 0f;
            float normalizedY = halfHeight > 0f ? anchoredPosition.y / halfHeight : 0f;
            float magnitude = Mathf.Sqrt((normalizedX * normalizedX) + (normalizedY * normalizedY));
            clamped = magnitude > 1f;

            if (!clamped || magnitude <= 0f)
            {
                return anchoredPosition;
            }

            float scale = 1f / magnitude;
            return new Vector2(
                anchoredPosition.x * scale,
                anchoredPosition.y * scale);
        }

        private float GetWorldRadius()
        {
            // 正交相机时优先使用相机尺寸，否则回退到手动配置半径
            if (miniMapCamera != null && miniMapCamera.orthographic)
            {
                return miniMapCamera.orthographicSize;
            }

            return fallbackWorldRadius;
        }

        private void SetMarkerWidgetVisible(MiniMapMarker marker, bool visible)
        {
            if (!markerWidgets.TryGetValue(marker, out MarkerWidget widget) || widget.Root == null)
            {
                return;
            }

            widget.Root.gameObject.SetActive(visible);
        }

        private void SetMarkerWidgetsActive(bool active)
        {
            foreach (KeyValuePair<MiniMapMarker, MarkerWidget> pair in markerWidgets)
            {
                if (pair.Value.Root != null)
                {
                    pair.Value.Root.gameObject.SetActive(active);
                }
            }
        }

        private void ClearMarkerWidgets()
        {
            // 销毁所有运行时生成的标记点 UI，避免场景切换后残留
            foreach (KeyValuePair<MiniMapMarker, MarkerWidget> pair in markerWidgets)
            {
                if (pair.Value.Root != null)
                {
                    Destroy(pair.Value.Root.gameObject);
                }
            }

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
    }
}
