using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace WutheringWaves
{
    public class PlayerCamera : MonoBehaviour
    {
        #region 序列化字段
        [Header("相机观察点")]
        [SerializeField] internal Transform cameraPivot; // 相机观察点/旋转锚点

        [Header("虚拟相机")]
        public CinemachineVirtualCamera VirtualCamera; // 主虚拟相机
        public CinemachineVirtualCamera virtualCamera2; // 副虚拟相机

        [Header("视角限制")]
        public float TopClamp = 70.0f; // 最大仰角
        public float BottomClamp = -30.0f; // 最大俯角

        [Header("视角灵敏度")]
        public float SensitivityX = 1.0f; // 水平灵敏度
        public float SensitivityY = 1.0f; // 垂直灵敏度

        [Header("镜头缩放设置")]
        public float MinZoomDistance = 1f; // 最小缩放距离
        public float MaxZoomDistance = 5f; // 最大缩放距离
        public float ZoomSpeed = 2.0f; // 缩放速度
        public float DefaultZoomDistance = 5.0f; // 默认缩放距离
        #endregion

        #region 私有字段

        private const float _threshold = 0.01f; // 输入阈值
        private float _cinemachineTargetYaw; // 水平旋转角
        private float _cinemachineTargetPitch; // 垂直旋转角

        private Cinemachine3rdPersonFollow _thirdPersonFollow; // 第三人称相机组件
        internal float _currentZoomDistance; // 当前缩放距离
        #endregion

        #region 初始化
        internal void Initialize()
        {
            //1.获取第三人称相机组件
            if (VirtualCamera != null)
            {
                _thirdPersonFollow = VirtualCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
            }

            //2.初始化相机距离
            if (_thirdPersonFollow != null)
            {
                _currentZoomDistance = DefaultZoomDistance;
                _thirdPersonFollow.CameraDistance = _currentZoomDistance;
            }

            //3.初始化观察点旋转角度
            if (cameraPivot == null)
            {
                Debug.LogWarning($"【{gameObject.name}】PlayerCamera 未绑定 cameraPivot，跳过相机朝向初始化。", this);
                return;
            }
            _cinemachineTargetYaw = cameraPivot.rotation.eulerAngles.y;
            _cinemachineTargetPitch = cameraPivot.rotation.eulerAngles.x;
        }
        #endregion

        #region 观察点管理
        // 绑定相机观察点：由外部控制器提供当前角色的统一观察锚点
        internal void BindCameraPivot(Transform targetPivot)
        {
            if (targetPivot == null)
            {
                return;
            }

            // 1.缓存当前角色观察点，用于玩家输入旋转
            cameraPivot = targetPivot;

            // 2.把主虚拟相机绑定到当前角色观察点
            if (VirtualCamera != null)
            {
                VirtualCamera.Follow = cameraPivot;
                VirtualCamera.LookAt = cameraPivot;

                // 初始化绑定时强制刷新相机，避免镜头从旧位置缓慢追到角色身上
                VirtualCamera.PreviousStateIsValid = false;
            }

            // 3.如果副虚拟相机也需要跟随当前角色，也同步绑定
            if (virtualCamera2 != null)
            {
                virtualCamera2.Follow = cameraPivot;
                virtualCamera2.LookAt = cameraPivot;

                // 初始化绑定时强制刷新相机，避免副相机沿用旧位置
                virtualCamera2.PreviousStateIsValid = false;
            }
        }

        #endregion

        #region 更新第三人称镜头
        // 处理视角旋转：旋转观察点，由Cinemachine根据观察点生成最终镜头
        internal void UpdateCameraLook(Vector2 lookInput)
        {
            // 空值判断
            if (cameraPivot == null || _thirdPersonFollow == null) return;
            // 处理视角旋转输入
            if (lookInput.sqrMagnitude >= _threshold)
            {
                _cinemachineTargetYaw += lookInput.x * SensitivityX;
                _cinemachineTargetPitch -= lookInput.y * SensitivityY;
            }

            // 限制旋转角度
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // 应用到观察点：真实渲染相机由Cinemachine驱动
            cameraPivot.rotation = Quaternion.Euler(_cinemachineTargetPitch, _cinemachineTargetYaw, 0.0f);
        }
        // 调整缩放距离：通过第三人称跟随组件控制镜头远近
        internal void UpdateCameraZoom(Vector2 zoomInput)
        {
            // 空值判断
            if (cameraPivot == null || _thirdPersonFollow == null) return;

            float scrollValue = zoomInput.y;
            // 调整缩放距离
            if (scrollValue > 0)
            {
                _currentZoomDistance = Mathf.Max(_currentZoomDistance - 1, MinZoomDistance);
            }
            else if (scrollValue < 0)
            {
                _currentZoomDistance = Mathf.Min(_currentZoomDistance + 1, MaxZoomDistance);
            }

            // 平滑过渡缩放
            _thirdPersonFollow.CameraDistance = Mathf.Lerp(
                _thirdPersonFollow.CameraDistance,
                _currentZoomDistance,
                ZoomSpeed * Time.deltaTime);
        }
        #endregion

        #region 视角限制
        // 限制角度在合法范围内
        private static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360f) angle += 360f;
            if (angle > 360f) angle -= 360f;
            return Mathf.Clamp(angle, min, max);
        }
        #endregion
    }
}
