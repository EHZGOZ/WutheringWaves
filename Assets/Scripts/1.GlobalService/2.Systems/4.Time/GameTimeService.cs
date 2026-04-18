using UnityEngine;


namespace WutheringWaves
{

    public class GameTimeService : MonoBehaviour
    {
        public static GameTimeService Instance { get; private set; }
        [SerializeField] private float defaultTimeScale = 1f; // 默认时间缩放系数，1为正常游戏速度   
        private float _resumeTimeScale = 1f; // 存储暂停前的时间缩放，用于恢复游戏
        public bool IsInitialized { get; private set; }        // 标记服务是否完成初始化
        public bool IsPaused => Time.timeScale <= 0f; // 判断游戏是否暂停（时间缩放小于等于0即为暂停）
        public float CurrentTimeScale => Time.timeScale; // 获取当前的时间缩放值

        // 脚本唤醒时执行，单例初始化
        private void Awake()
        {
            // 防止单例重复，若已存在实例则销毁当前对象
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // 赋值单例实例
            Instance = this;
        }

        // 初始化时间服务，设置默认参数
        public void Initialize()
        {
            // 防止重复初始化
            if (IsInitialized)
            {
                return;
            }

            // 确保默认时间缩放为有效值，不小于0.0001
            defaultTimeScale = Mathf.Max(0.0001f, defaultTimeScale);
            // 初始化恢复时间缩放为默认值
            _resumeTimeScale = defaultTimeScale;
            // 设置初始时间缩放
            SetTimeScale(defaultTimeScale);
            // 标记初始化完成
            IsInitialized = true;
        }

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

        // 设置鼠标光标可见性和锁定状态
        public void SetCursorVisible(bool visible)
        {
            Cursor.visible = visible;
            // 可见则解锁光标，不可见则锁定光标
            Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        }

        // 统一控制游戏暂停/继续和光标显示状态
        public void SetPauseAndCursor(bool pause, bool cursorVisibleWhenPaused = true)
        {
            if (pause)
            {
                // 暂停游戏并设置光标显示
                Pause();
                SetCursorVisible(cursorVisibleWhenPaused);
            }
            else
            {
                // 恢复游戏并设置光标隐藏
                Resume();
                SetCursorVisible(!cursorVisibleWhenPaused);
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
    }
}