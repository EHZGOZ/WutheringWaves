using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

namespace WutheringWaves
{
    public class CharacterCamera : MonoBehaviour
    {
        #region 序列化字段
        // 拖拽赋值：你的虚拟相机
        [Tooltip("虚拟相机")]
        [Header("虚拟相机")]
        public CinemachineVirtualCamera VirtualCamera; // Cinemachine虚拟相机引用
        public CinemachineVirtualCamera virtualCamera2;
        public CinemachineVirtualCamera virtualCamera3;

        [Header("Cinemachine相关")]
        public GameObject CameraTarget; // 相机跟随目标（视角旋转中心）
       

        [Header("视角限制")]
        public float TopClamp = 70.0f; // 视角上仰最大角度
        public float BottomClamp = -30.0f; // 视角下俯最大角度

        [Header("视角灵敏度")]
        public float SensitivityX = 1.0f; // 水平视角灵敏度
        public float SensitivityY = 1.0f; // 垂直视角灵敏度

        [Header("镜头缩放设置")]
        public float MinZoomDistance = 1f; // 镜头最小距离
        public float MaxZoomDistance = 5f; // 镜头最大距离
        public float ZoomSpeed = 2.0f; // 镜头缩放平滑速度
        public float DefaultZoomDistance = 5.0f; // 镜头默认距离
        #endregion

        #region 私有字段
        internal GameObject _mainCamera; // 主相机对象
        private const float _threshold = 0.01f; // 输入阈值（过滤无效输入）
        private float _cinemachineTargetYaw; // 目标对象水平旋转角度
        private float _cinemachineTargetPitch; // 目标对象垂直旋转角度
       
        private Cinemachine3rdPersonFollow _thirdPersonFollow; // 第三人称跟随组件
        public float _currentZoomDistance; // 当前镜头距离
        #endregion

       

        #region 初始化
        public void Initialize()
        {
            // 获取主相机
            if (_mainCamera == null) _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");

            // 初始化Cinemachine组件和镜头距离
            if (VirtualCamera != null) _thirdPersonFollow = VirtualCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
  
            //初始化相机初始位置与缩放
            if (_thirdPersonFollow != null)
            {
                _currentZoomDistance = DefaultZoomDistance;
                _thirdPersonFollow.CameraDistance = _currentZoomDistance;
            }

            // 初始化视角旋转角度
            _cinemachineTargetYaw = CameraTarget.transform.rotation.eulerAngles.y;
        }
        #endregion

        #region 核心逻辑

        #endregion

        #region 更新第三人称镜头
        public void UpdateCameraLook(Vector2 _look)//更新镜头位置
        {
            // 如果当前不是自由相机激活，直接返回，不修改旋转
            if (!IsFreeCameraActive())
                return;
            // 处理视角旋转（过滤无效输入）
            if (_look.sqrMagnitude >= _threshold)
            {
                _cinemachineTargetYaw += _look.x * SensitivityX;//更新水平旋转角
                _cinemachineTargetPitch -= _look.y * SensitivityY;//更新垂直旋转角
            }

            // 限制视角旋转角度
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);//水平旋转角
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);//垂直旋转角限制

            // 应用视角旋转到目标对象
            CameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch,
                _cinemachineTargetYaw, 0.0f);
        }

        public void UpdateCameraZoom(Vector2 _zoom) //平滑更新镜头缩放距离
        {
            if (!IsFreeCameraActive() || _thirdPersonFollow == null)
                return;

            float scrollValue = _zoom.y;

            // 根据滚轮方向调整镜头距离
            if (scrollValue > 0)
            {
                // 滚轮向上：拉近镜头（限制最小值）
                _currentZoomDistance = Mathf.Max(_currentZoomDistance - 1, MinZoomDistance);
            }
            else if (scrollValue < 0)
            {
                // 滚轮向下：拉远镜头（限制最大值）
                _currentZoomDistance = Mathf.Min(_currentZoomDistance + 1, MaxZoomDistance);
            }

            // 插值平滑更新镜头距离
            _thirdPersonFollow.CameraDistance = Mathf.Lerp(_thirdPersonFollow.CameraDistance, _currentZoomDistance, ZoomSpeed * Time.deltaTime);

        }
        #endregion

        // ：判断当前是否是自由第三人称相机（VirtualCamera）激活
        private bool IsFreeCameraActive()
        {
            // 比较优先级：如果 VirtualCamera 优先级高于 virtualCamera2，则认为自由相机激活
            // 请确保在 Inspector 中，VirtualCamera 的初始 Priority 设为 10（介于 9 和 11 之间）
            return VirtualCamera.Priority > virtualCamera2.Priority;
        }

        #region  视角限制
        //角度循环
        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }
        #endregion
    }
}
