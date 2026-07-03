using UnityEngine;


namespace WutheringWaves
{

    public class GameTimeService : MonoBehaviour
    {
        public static GameTimeService Instance { get; private set; }

        [SerializeField] private float defaultTimeScale = 1f; // 默认时间缩放系数，1为正常游戏速度   
        [Header("=== 时间流速测试 ===")]
        [SerializeField] private bool enableTimeScaleTest = true; // 是否启用Tab切换时间流速测试
        [SerializeField] private float testTimeScaleA = 1f; // 正常速度
        [SerializeField] private float testTimeScaleB = 0.3f; // 慢动作速度

        private float _resumeTimeScale = 1f; // 存储暂停前的时间缩放，用于恢复游戏
        public bool IsPaused => Time.timeScale <= 0f; // 判断游戏是否暂停（时间缩放小于等于0即为暂停）
        public float CurrentTimeScale => Time.timeScale; // 获取当前的时间缩放值
        public bool IsInitialized { get; private set; } // 是否已初始化

        #region 生命周期
        private void Awake()
        {
            // 1.保持单例，避免多个GameTimeService争抢时间状态
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // 2.缓存单例引用
            Instance = this;
        }
        private void Update()
        {
            UpdateTimeScaleTest();
        }
        private void OnDestroy()
        {
            // 1.如果销毁的是当前单例，清空单例引用
            if (Instance == this)
            {
                Instance = null;
            }
        }
        #endregion

        #region 初始化
        // 初始化时间服务，设置默认参数
        public void Initialize()
        {
            // 1.已经初始化过时直接返回，避免重复重置时间状态
            if (IsInitialized)
            {
                return;
            }

            // 2.确保默认时间缩放为有效值，不小于0.0001
            defaultTimeScale = Mathf.Max(0.0001f, defaultTimeScale);

            // 3.初始化恢复时间缩放为默认值
            _resumeTimeScale = defaultTimeScale;

            // 4.设置初始时间缩放
            SetTimeScale(defaultTimeScale);

            // 5.标记初始化完成
            IsInitialized = true;
        }
        #endregion

        // 暂停游戏，将时间缩放设为0
        public void Pause()
        {
            // 记录暂停前的时间缩放值
            if (Time.timeScale > 0f)
            {
                _resumeTimeScale = Time.timeScale;
            }
            // 执行暂停操作
            SetTimeScale(0f);
        }

        // 恢复游戏，回到暂停前的时间缩放
        public void Resume()
        {
            // 恢复时间缩放，无有效值则使用默认值
            SetTimeScale(_resumeTimeScale > 0f ? _resumeTimeScale : defaultTimeScale);
        }

        // 设置时间缩放，限制数值为非负数
        public void SetTimeScale(float scale)
        {
            // 限制时间缩放最小值为0
            float clampedScale = Mathf.Max(0f, scale);
            // 应用时间缩放到引擎
            Time.timeScale = clampedScale;

            // 仅非暂停状态下，更新恢复用的时间缩放
            if (clampedScale > 0f)
            {
                _resumeTimeScale = clampedScale;
            }
        }

        // 在两个指定的时间缩放值之间切换
        public void ToggleBetweenScales(float firstScale, float secondScale)
        {
            // 确保两个缩放值为非负数
            float safeA = Mathf.Max(0f, firstScale);
            float safeB = Mathf.Max(0f, secondScale);
            // 判断当前缩放值，切换到另一个值
            float target = Mathf.Approximately(CurrentTimeScale, safeA) ? safeB : safeA;
            // 应用切换后的时间缩放
            SetTimeScale(target);
        }
        // 测试时间流速切换
        private void UpdateTimeScaleTest()
        {
            if (!enableTimeScaleTest)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                ToggleBetweenScales(testTimeScaleA, testTimeScaleB);
                //Debug.Log($"当前时间流速：{CurrentTimeScale}");
            }
        }

    }
}