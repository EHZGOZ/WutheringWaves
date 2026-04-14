using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WutheringWaves
{
    public class CharacterMovement : MonoBehaviour
    {
        #region 1. 配置字段
        [Header("=== 核心依赖配置 ===")]
        [Tooltip("角色核心控制")]
        [SerializeField] private CharacterCore Core; //角色核心控制      

        [Header("=== 平滑配置 ===")]
        [Tooltip("角色旋转平滑")]
        public float RotationSmoothTime = 0.1f;
        [Tooltip("速度平滑过渡的速度")]
        public float speedSmoothSpeed = 5f;

        [Header("=== 阈值配置 ===")]
        [Tooltip("移动输入阈值")]
        public float moveThreshold = 0.1f;
        #endregion

        #region 2. 组件依赖 + 状态机上下文
        public GameObject mainCamera;//主相机
        private CharacterController controller; //角色控制器
        private Animator animator; // 动画控制器
        #endregion

        #region 初始化逻辑
        // 外部初始化（由CharacterCore调用，注入状态机）
        public void Initialize(CharacterCore Core)
        {
            InitializeComponents(Core); // 初始化组件
            InitializeStateData(); // 初始化状态数据
        }

        private void InitializeComponents(CharacterCore Core)
        {
            this.Core = Core; //角色核心控制 
            mainCamera = Core.cameraLogic._mainCamera;//主相机
            controller = Core.characterController;//角色控制器
            animator = Core.animator;// 动画控制器
        }

        private void InitializeStateData()
        {
            // 基础移动状态：初始为基础步行速度，与角色默认静止/步行逻辑匹配
            _characterSpeed = moveSpeed;
            // 目标旋转角度：初始与角色自身Y轴旋转一致，避免无输入时旋转偏差
            _targetRot = transform.eulerAngles.y;
            // 旋转平滑速度：SmoothDampAngle要求ref参数，初始化为0保证平滑计算起步正常
            _rotationVelocity = 0f;

        }
        #endregion
        private void Update()
        {
            //冲刺计时器
            UpdateDashCDTimers();
            //是否长按shift过
            HasPressedShift();
        }

        #region 动画逻辑
        //正常动画
        public void UpdateFreeMoveAnimation(Vector2 move, bool isHoldingRun)
        {
            float targetAxisY = move.magnitude;
            bool actualRun = ResolveRunVisualState(move, isHoldingRun);
            // 根据是否奔跑确定最终目标值
            float finalTargetY = actualRun ? 2 * targetAxisY : targetAxisY;

            // 统一插值到最终目标值，过渡更平滑
            float axisY = Mathf.MoveTowards(animator.GetFloat("AxisY"), finalTargetY, 5f * Time.deltaTime);
            float axisX = Mathf.MoveTowards(animator.GetFloat("AxisX"), 0, 5f * Time.deltaTime);

            animator.SetFloat("AxisY", axisY);
            animator.SetFloat("AxisX", axisX);
        }
      
        #endregion

        #region 移动逻辑
        [Header("=== 基础移动速度配置 ===")]
        [Tooltip("移动速度")]
        public float moveSpeed = 1f;
        [Tooltip("奔跑速度")]
        public float runSpeed = 3f;

        // 基础移动状态
        private float _characterSpeed; // 当前移动速度
        private float _targetRot; // 目标旋转角度
        private float _rotationVelocity; // 旋转平滑速度
        private bool _isRunning;

        //核心执行入口
        // 帧更新移动逻辑 由状态机传入（由状态机Update调用）
        public void UpdateMovement(Vector2 move, bool isHoldingRun = false)
        {
            bool actualRun = ResolveRunState(move, isHoldingRun);
            CalculationSpeed(actualRun); // 计算速算
            HandleCharacterRotation(move);//角色旋转
            HandleNormalMovement(_characterSpeed);//角色移动
        }
        //计算速度
        private void CalculationSpeed(bool isHoldingRun)
        {
            _characterSpeed = isHoldingRun ? runSpeed : moveSpeed;
        }
        //旋转
        public void HandleCharacterRotation(Vector2 move)
        {
           
            Vector3 inputDir = new Vector3(move.x, 0.0f, move.y);
            // 只有输入向量长度超过阈值（有效输入），才计算旋转
            if (inputDir.magnitude > moveThreshold)
            {
                inputDir = inputDir.normalized;
                _targetRot = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRot, ref _rotationVelocity, RotationSmoothTime);
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }
        }
        //移动
        private void HandleNormalMovement(float speed)
        {
            Vector3 moveDir = Quaternion.Euler(0.0f, _targetRot, 0.0f) * Vector3.forward;
            controller.Move(moveDir.normalized * speed * Time.deltaTime);
        }


        #endregion

        #region 方向计算
        //计算起步方向
        public bool CalculateDirection(Vector2 move)
        {
            // 1. 构建移动输入向量并标准化
            Vector3 inputDir = new Vector3(move.x, 0, move.y).normalized;

            // 2. 判断输入是否有效（超过移动阈值，过滤噪音）
            if (inputDir.magnitude > moveThreshold)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        #region 跳跃逻辑
        [Header("=== 跳跃配置 ===")]
        [Tooltip("空中移动速度系数（1为和地面一样快）")]
        public float JumpairControlMultiplier = 1f;
        //判断跳跃是否可用
        public bool IsJumpAvailable()
        {
            // 条件1：可打断状态
            bool isCanInterrupt =Core.stateMachine.IsInterruptible();
            // 条件2：在地面上
            bool isGrounded = CustomCheckGrounded();
            return isCanInterrupt && isGrounded;
        }
        //实现跳跃
        public void HandleJumpMovement(Vector2 move)
        {
            // 1. 将2D输入向量转换为3D世界空间的水平方向向量
            Vector3 inputDir = new Vector3(move.x, 0, move.y);
            Vector3 moveDir = Vector3.zero; // 初始化最终移动方向为0

            // 2.  归一化输入方向（让角色朝向相对于相机的输入方向）
            if (inputDir.magnitude > moveThreshold)
            {
                // 归一化输入方向：确保斜向移动时速度不会超过轴向移动速度（保持一致的移动速率）
                inputDir = inputDir.normalized;
                // 计算角色目标旋转角度：
                float targetRot = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + mainCamera.transform.eulerAngles.y;
                // 根据目标旋转角度，计算角色的实际移动方向（沿目标朝向的正前方）
                moveDir = Quaternion.Euler(0, targetRot, 0) * Vector3.forward;
                // 平滑旋转角色朝向：避免旋转突变，让转向更流畅
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRot, ref _rotationVelocity, RotationSmoothTime);
                // 应用平滑后的旋转角度到角色Transform
                transform.rotation = Quaternion.Euler(0, rotation, 0);
            }

            // 3. 计算空中水平移动速度：基础移动速度 * 空中操控系数（airControlMultiplier通常<1，空中操控弱于地面）
            float horizontalSpeed = moveSpeed * JumpairControlMultiplier;

            // 4. 组合最终移动向量：水平移动（归一化确保速率稳定） + 垂直重力移动
            Vector3 finalMovement = (moveDir.normalized * horizontalSpeed);

            // 5. 执行角色移动：通过CharacterController（推测）应用移动向量，乘以Time.deltaTime保证帧率无关
            controller.Move(finalMovement * Time.deltaTime);
        }
        #endregion

        #region 下坠逻辑
        [Header("=== 下坠配置 ===")]
        [Tooltip("下坠中移动速度系数（1为和地面一样快）")]
        public float FallingairControlMultiplier = 1f;
        //实现下坠
        public void HandleFallingMovement(Vector2 move)
        {
            // 1. 将2D输入向量转换为3D世界空间的水平方向向量
            Vector3 inputDir = new Vector3(move.x, 0, move.y);
            Vector3 moveDir = Vector3.zero; // 初始化最终移动方向为0

            // 2.  归一化输入方向（让角色朝向相对于相机的输入方向）
            if (inputDir.magnitude > moveThreshold)
            {
                // 归一化输入方向：确保斜向移动时速度不会超过轴向移动速度（保持一致的移动速率）
                inputDir = inputDir.normalized;
                // 计算角色目标旋转角度：
                float targetRot = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + mainCamera.transform.eulerAngles.y;
                // 根据目标旋转角度，计算角色的实际移动方向（沿目标朝向的正前方）
                moveDir = Quaternion.Euler(0, targetRot, 0) * Vector3.forward;
                // 平滑旋转角色朝向：避免旋转突变，让转向更流畅
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRot, ref _rotationVelocity, RotationSmoothTime);
                // 应用平滑后的旋转角度到角色Transform
                transform.rotation = Quaternion.Euler(0, rotation, 0);
            }

            // 3. 计算空中水平移动速度：基础移动速度 * 空中操控系数（airControlMultiplier通常<1，空中操控弱于地面）
            float horizontalSpeed = moveSpeed * FallingairControlMultiplier;

            // 4. 组合最终移动向量：水平移动（归一化确保速率稳定） + 垂直重力移动
            Vector3 finalMovement = (moveDir.normalized * horizontalSpeed) ;

            // 5. 执行角色移动：通过CharacterController（推测）应用移动向量，乘以Time.deltaTime保证帧率无关
            controller.Move(finalMovement * Time.deltaTime);
        }
        #endregion

        #region 冲刺逻辑
        [Header("=== 冲刺配置===")]
        [Header("冲刺耗时（不可打断）")]
        public float dashCostTime = 0.33f;
        [Header("连续两次冲刺的内置CD（秒")]
        public float dashInternalCD = 0.2f;
        [Header("全局CD阈值：两次冲刺间隔小于此值判定为'快速连冲'（秒）")]
        public float dashGlobalCDThreshold = 1.5f;
        [Header("全局CD惩罚时长：快速连冲触发的冷却时间（秒）")]
        public float dashGlobalCDPenalty = 1.3f;

        #region Dashing
        // 冲刺相关（CD计时器）
        public int DashCount { get; set; } = 0;//剩余可冲刺数
        public float DashInternalCDTimer { get; set; } = 0f; // 内置CD计时器
        public float DashGlobalCDTimer { get; set; } = 0f;  // 全局CD计时器

        // 记录上一次冲刺的时间戳（用于判断是否触发全局CD）
        public float LastDashTimestamp { get; set; } = -100f; // 初始值设为-100，保证第一次冲刺不会误判
        public Vector3 DashDirection { get; set; } = Vector3.forward;//冲刺方向
        public bool DashSteerable { get; set; } = false;//是否可转向 true向前 false向后
        #endregion

        // 判断地面冲刺是否可用
        public bool IsDashAvailable()
        {
            // 条件1：可打断状态
            bool isCanInterrupt = Core.stateMachine.IsInterruptible();
            // 条件2：在地面（未触发坠落）
            bool isGrounded = CustomCheckGrounded();
            // 条件3：全局CD结束
            bool isGlobalCDOver = DashGlobalCDTimer <= 0;
            // 条件4：冲刺次数未达上限
            bool isUnderMaxCount = DashCount < 2;
            // 条件5：内置CD结束（非首次冲刺需要）
            bool isInternalCDOver = DashInternalCDTimer <= 0;
            // 条件6：是否有足够体力
            bool hasEnoughStamina = Core.staminaLogic == null || Core.staminaLogic.CanSprint();

            return isCanInterrupt && isGrounded && isGlobalCDOver && isUnderMaxCount && (DashCount == 0 || isInternalCDOver) && hasEnoughStamina;
        }
        
        //3.计算上次冲刺间隔  重置连冲资格 消耗1次冲刺次数 启动内置 CD 并更新时间戳
        public void CalculateAndUpdateDashCounter()
        {
            //计算上次冲刺间隔
            float timeSinceLastDash = Time.time -LastDashTimestamp;
            // 计算冷却是否重置连冲资格
            if (timeSinceLastDash >= dashGlobalCDThreshold)
            {
                DashCount = 0;
            }
            // 消耗1次冲刺次数
            DashCount++;
            //启动内置 CD
            DashInternalCDTimer = dashInternalCD;
            //并更新时间戳
            LastDashTimestamp = Time.time;
        }
        
        //4.检查并施加全局 CD 惩罚
        public void ApplyPenaltyIfNecessary()
        {
            float timeSinceLastDash = Time.time - LastDashTimestamp;

            if (DashCount >= 2 && timeSinceLastDash < dashGlobalCDThreshold)
            {
                DashGlobalCDTimer = dashGlobalCDPenalty;
                DashCount = 0;
            }
        }
        //更新冲刺所有计时器
        private void UpdateDashCDTimers()
        {
            // 1. 计时器每帧递减（仅在大于0时减少，避免无效计算）
            if (DashInternalCDTimer > 0) DashInternalCDTimer -= Time.deltaTime;
            if (DashGlobalCDTimer > 0) DashGlobalCDTimer -= Time.deltaTime;

            // 2. 强制限制计时器最小值为0（必须在重置逻辑之前）
            DashInternalCDTimer = Mathf.Max(0, DashInternalCDTimer);
            DashGlobalCDTimer = Mathf.Max(0, DashGlobalCDTimer);
        }

        public bool TryConsumeDashStamina()
        {
            return Core.staminaLogic == null || Core.staminaLogic.TryConsumeSprint();
        }
        #endregion   

        #region 空中冲刺
        [Header("=== 空中冲刺配置 ===")]
        [Tooltip("空中冲刺距离（米）")]
        public float airDashDistance = 5f;
        [Tooltip("空中冲刺向上距离（米）")]
        public float airDashUpDistance = 2f;
        [Tooltip("空中冲刺耗时（秒）")]
        public float airDashCostTime = 0.3f;

        public bool HasAirDashed { get; set; } = false;  // 空中冲刺是否已使用（落地重置）
         // 判断空中冲刺是否可用
        public bool IsAirDashAvailable()
        {
            bool isCanInterrupt = Core.stateMachine.IsInterruptible();
            bool isInAir = !CustomCheckGrounded();
            bool hasNotUsed = !HasAirDashed;

            return isCanInterrupt && isInAir && hasNotUsed;
        }
        // 计算空中冲刺方向（无输入时向前，后冲刺）

        public Vector3 CalculateAirDashDirection(Vector2 move, out bool Direction)
        {
            Vector3 inputDir = new Vector3(move.x, 0, move.y).normalized;
            if (inputDir.magnitude > moveThreshold)
            {
                Vector3 dashDir = Quaternion.Euler(0, mainCamera.transform.eulerAngles.y, 0) * inputDir;
                Direction = true;
                return dashDir.normalized.normalized;
            }
            else
            {
                Direction = false;
                return (-transform.forward).normalized;
            }
        }

        //空中冲刺移动：冻结垂直速度 + 水平冲刺位移（含转向）
        public void HandleAirDashMovement(Vector2 move, ref Vector3 dashDir, bool isLocked,bool Direction)
        {
            // 1. 重置垂直速度：空中冲刺时，取消重力/下落速度，保持水平冲刺
            _verticalVelocity = 0f;

            // 2. 非锁定状态：允许玩家通过输入调整冲刺方向
            if (!isLocked)
            {
                // 将二维输入转换为三维水平方向（X=左右，Z=前后，Y=0 纯水平）
                Vector3 inputDir = new Vector3(move.x, 0, move.y).normalized;

                // 判断输入有效（输入强度大于阈值，避免微小误触）
                if (inputDir.magnitude > moveThreshold)
                {
                    // 将输入方向转换为【相机视角】的方向（符合玩家操作直觉）
                    Vector3 desiredDir = Quaternion.Euler(0, mainCamera.transform.eulerAngles.y, 0) * inputDir;
                    desiredDir.y = 0;       // 强制纯水平方向，禁止上下倾斜
                    desiredDir.Normalize(); // 标准化方向（长度为1，保证速度均匀）

                    // 核心：平滑旋转冲刺方向（从当前方向 → 目标方向，限制旋转速度）
                    // ref dashDir：这里直接修改了外部的冲刺方向变量！
                    dashDir = Vector3.RotateTowards(
                        dashDir,           // 当前冲刺方向
                        desiredDir,        // 玩家想要的目标方向
                        300 * Mathf.Deg2Rad * Time.deltaTime, // 每帧最大旋转角度（弧度）
                        0f                 // 不限制长度变化
                    );
                }
            }

            // 3. 根据冲刺方向，旋转角色朝向（让角色脸朝冲刺方向）
            if (dashDir.sqrMagnitude > 0.001f) // 优化：用平方值判断方向有效，比magnitude性能更高
            {
                // 生成朝向冲刺方向的旋转量（头顶向上，脸朝dashDir）
                Quaternion targetRot = Quaternion.LookRotation(dashDir, Vector3.up);

                //（更流畅，不生硬）
                if (Direction)
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,  // 当前旋转
                        targetRot,           // 目标旋转
                        300 / 180f * Time.deltaTime * 10f // 旋转平滑度
                    );
            }

            // 4. 计算冲刺位移，并执行移动
            // 公式：方向 × (总距离/总时间) × 每帧时间 = 每帧移动距离
            Vector3 dashMovement = dashDir * (airDashDistance / airDashCostTime) * Time.deltaTime;
            // 计算匀速向上速度
            float upwardSpeed = airDashUpDistance / airDashCostTime;
            // 叠加向上位移，所有冲刺方向通用
            dashMovement.y = upwardSpeed * Time.deltaTime;
            controller.Move(dashMovement); // 角色控制器执行移动
        }
        #endregion

        #region 御空冲刺
        [Header("=== 御空冲刺配置===")]
        [Header("御空冲刺耗时（不可打断）")]
        public float floatDashCostTime = 1.9f;
        // 判断御空冲刺是否可用
        public bool IsFloatDashAvailable()
        {
            // 条件1：可打断状态
            bool isCanInterrupt = Core.stateMachine.IsInterruptible();
            // 条件2：不在地面（未触发坠落）
            bool isGrounded = CustomCheckGrounded();
            // 条件3：处于御空状态
            bool isFloating = Core.attackLogic.IsFloating;
            // 条件4：是否有足够体力
            bool hasEnoughStamina = Core.staminaLogic == null || Core.staminaLogic.CanFloatDash();
            return isCanInterrupt && !isGrounded && isFloating && hasEnoughStamina;
        }
        // 计算御空冲刺方向 + 旋转至八方向(前/后/左/右/四斜向)
        public bool CalculateFloatDashDirection(Vector2 move)
        {
            Vector2 snappedMove = SnapMoveInputToEightDirections(move);
            if (snappedMove == Vector2.zero)
            {
                // 无输入时保持当前朝向，不旋转
                return false;
            }

            // 先在输入空间吸附为八方向，再转换到相机朝向，结果会更稳定。
            Vector3 localDashDir = new Vector3(snappedMove.x, 0f, snappedMove.y).normalized;
            Vector3 worldDashDir = Quaternion.Euler(0f, mainCamera.transform.eulerAngles.y, 0f) * localDashDir;
            worldDashDir.y = 0f;
            worldDashDir.Normalize();

            _targetRot = Mathf.Atan2(worldDashDir.x, worldDashDir.z) * Mathf.Rad2Deg;

            // 御空冲刺进入时只计算一次方向，而位移又主要由 Root Motion 驱动，
            // 这里如果继续平滑旋转，角色往往还没转到位，动画已经按旧朝向推出去了。
            _rotationVelocity = 0f;
            transform.rotation = Quaternion.Euler(0f, _targetRot, 0f);

            return true;
        }

        private Vector2 SnapMoveInputToEightDirections(Vector2 move)
        {
            if (move.sqrMagnitude <= moveThreshold * moveThreshold)
            {
                return Vector2.zero;
            }

            float absX = Mathf.Abs(move.x);
            float absY = Mathf.Abs(move.y);

            float snappedX = absX > moveThreshold ? Mathf.Sign(move.x) : 0f;
            float snappedY = absY > moveThreshold ? Mathf.Sign(move.y) : 0f;

            if (snappedX == 0f && snappedY == 0f)
            {
                return Vector2.zero;
            }

            // 模拟“哪个方向按得更多”的判断：
            // 只有弱轴至少达到强轴的一半时，才认为玩家想走斜向，否则吸附到主方向。
            const float diagonalKeepRatio = 0.5f;
            if (snappedX != 0f && snappedY != 0f)
            {
                if (absX > absY && absY < absX * diagonalKeepRatio)
                {
                    snappedY = 0f;
                }
                else if (absY > absX && absX < absY * diagonalKeepRatio)
                {
                    snappedX = 0f;
                }
            }

            return new Vector2(snappedX, snappedY);
        }


        public bool TryConsumeFloatDashStamina()
        {
            return Core.staminaLogic == null || Core.staminaLogic.TryConsumeFloatDash();
        }

        #endregion

        #region 缓冲逻辑
        [Header("=== 急停缓冲配置 ===")]
        [Tooltip("移动缓冲的总滑行距离（米）")]
        public float moveStoppingDistance = 0.7f;
        [Tooltip("移动缓冲的总时长（秒）")]
        public float moveStoppingTime = 1.33f;
        [Tooltip("奔跑缓冲的总滑行距离（米）")]
        public float runStoppingDistance = 0.7f;
        [Tooltip("奔跑缓冲的总时长（秒）")]
        public float runStoppingTime = 1.33f;
        [Tooltip("急停缓冲的总滑行距离（米）")]
        public float dashStoppingDistance = 1f;
        [Tooltip("急停缓冲的总时长（秒）")]
        public float dashStoppingTime = 0.66f;

        // 缓冲阶段的私有临时数据
        private float _stoppingTimer;//已经运行的时间
        private Vector3 _stoppingDirection;//方向
        private float _stoppingBaseSpeed;//急停基础滑行
        private float StoppingTime;//目标停止时间
        private DateTime _lastHoldPressTime = DateTime.MinValue;

        //缓冲动画
        public void UpdateStopMoveAnimation(Vector2 move, bool isHoldingRun)
        {
            float targetAxisY = move.magnitude;
            bool actualRun = ResolveRunVisualState(move, isHoldingRun);
            // 根据是否奔跑确定最终目标值
            float finalTargetY = actualRun ? 2 * targetAxisY : targetAxisY;

            // 统一插值到最终目标值，过渡更平滑
            float axisY = Mathf.MoveTowards(animator.GetFloat("AxisY"), finalTargetY, 1f * Time.deltaTime);
            float axisX = Mathf.MoveTowards(animator.GetFloat("AxisX"), 0, 5f * Time.deltaTime);

            animator.SetFloat("AxisY", axisY);
            animator.SetFloat("AxisX", axisX);
        }

        // 初始化急停缓冲参数（位移阶段结束时调用一次）作用：重置急停状态、计算滑行方向和初始速度，为惯性滑行做准备
        public void InitializeStopping(float StoppingDistance, float StoppingTime, bool steerable = true)
        {
            this.StoppingTime = StoppingTime;
            // 重置急停滑行计时器，从头开始计算滑行时间
            _stoppingTimer = 0f;
            // 计算急停滑行方向：前冲→沿冲刺方向滑行；后撤步→沿角色正后方滑行
            _stoppingDirection = steerable ? transform.forward : (-transform.forward).normalized;
            // 计算急停基础滑行速度（公式：根据设定的滑行距离和时间，计算初始速度）避免除零错误：如果滑行时间为0，速度直接设为0
            _stoppingBaseSpeed = (StoppingTime > 0f) ? (3f * StoppingDistance / StoppingTime) : 0f;
        }

        // 实现急停缓冲逻辑  作用：二次衰减式惯性滑行，模拟真实冲刺后收步的惯性效果
        public bool HandleStoppingMovement()
        {
            // 如果滑行时间已耗尽，直接返回滑行完成
            if (_stoppingTimer >= StoppingTime) return true;
            // 累加滑行计时
            _stoppingTimer += Time.deltaTime;
            // 计算滑行进度（0~1）：0=刚开始滑行，1=滑行结束
            float progress = Mathf.Clamp01(_stoppingTimer / StoppingTime);

            // 二次衰减系数：(1-progress)² → 速度衰减越来越快，模拟真实物理惯性
            // 线性衰减会很生硬，平方衰减更符合动作游戏的手感
            float speedFactor = (1f - progress) * (1f - progress);
            // 计算当前帧的实际滑行速度（基础速度 × 衰减系数）
            float currentSpeed = _stoppingBaseSpeed * speedFactor;

            // 计算本帧的移动向量（仅水平方向，禁止Y轴位移）
            Vector3 movement = _stoppingDirection * currentSpeed * Time.deltaTime;
            movement.y = 0f;
            // 执行角色移动（CharacterController移动）
            controller.Move(movement);

            // 返回滑行是否完全结束
            return progress >= 1f;
        }

        //x秒内是否按过shift
        public bool HasPressedShift(float time = 0.5f)
        {
            // 1. 检测当前是否处于按住状态，若按住，更新最近按下时间
            if (_isRunning)
            {
                _lastHoldPressTime = DateTime.Now;
            }

            // 2. 计算当前时间与最近按下时间的差值（秒）
            double timeSinceLastPress = (DateTime.Now - _lastHoldPressTime).TotalSeconds;

            // 3. 判断差值是否 ≤ time秒：是则返回true，否则false
            return timeSinceLastPress <= time;
        }

        private bool ResolveRunState(Vector2 move, bool isHoldingRun)
        {
            if (move.magnitude <= moveThreshold || !isHoldingRun)
            {
                _isRunning = false;
                return false;
            }

            if (Core.staminaLogic == null)
            {
                _isRunning = true;
                return true;
            }

            _isRunning = Core.staminaLogic.ConsumeRun(Time.deltaTime);
            return _isRunning;
        }

        private bool ResolveRunVisualState(Vector2 move, bool isHoldingRun)
        {
            if (move.magnitude <= moveThreshold || !isHoldingRun)
            {
                return false;
            }

            if (_isRunning)
            {
                return true;
            }

            return Core.staminaLogic == null || Core.staminaLogic.CanRun();
        }

        #endregion

        #region 重力逻辑
        [Header("=== 重力配置 ===")]
        public float gravity = -30f;
        [Header("===坠落检测延迟时间===")]
        public float fallCheckDelay = 0.3f;//坠落检测延迟时间（秒）：需连续未接地超过此时间才切换到坠落

        private bool _isGroud = true;//是否在地面
        public float _verticalVelocity;//重力速度                                
        private float _airborneTimer = 0f; // 内部计时器：记录当前连续未接地的时间

        //带时间缓冲的坠落检测
        public bool CheckIsFalling()
        {

            if (CustomCheckGrounded())
            {
                _airborneTimer = 0f;
                return false;
            }
            else
            {
                _airborneTimer += Time.deltaTime;
                return _airborneTimer >= fallCheckDelay;
            }
        }

        //地面状态每帧调用：施加下压力保持 isGrounded
        public void ApplyGroundingForce()
        {
            if (controller.isGrounded||Core.attackLogic.IsFloating|| Core.stateMachine.CurrentStateType == CharacterState.QBurst)
            {
                _verticalVelocity = -5f;
            }
            else
            {
                // 未接地时也施加向下力，确保角色能"找到"地面
                _verticalVelocity += gravity * Time.deltaTime;
            }
            controller.Move(Vector3.up * _verticalVelocity * Time.deltaTime);
        }
        //重置垂直速度（落地时调用）
        public void ResetVerticalVelocity()
        {
            _verticalVelocity = 0f;
        }
        #endregion

        #region 地面检测：使用 SphereCast（自定义球体射线）
        [Header("=== 自定义地面检测配置 ===")]
        [Header("地面物体所在的层级（请在Unity中创建名为 Ground 的Layer并赋值）")]
        public LayerMask groundLayer;

        [Header("检测球体半径（建议略小于 CharacterController 的 Radius）")]
        public float groundCheckRadius = 0.35f;

        [Header("检测起点距离胶囊体底部的高度（避免卡在模型内部）")]
        public float groundCheckOriginOffset = 0.1f;

        [Header("向下检测的最大距离")]
        public float groundCheckDistance = 0.3f;

        [Header("=== 调试 ===")]
        [Tooltip("在Scene视图中绘制检测范围")]
        public bool drawGizmos = true;
        // 自定义地面检测：使用 SphereCast（球体射线）
        public bool CustomCheckGrounded()
        {
            // 1. 计算检测起点：角色底部 + 微小偏移
            // CharacterController.center.y 是中心偏移，height是高度
            float bottomY = transform.position.y + controller.center.y - controller.height / 2f;
            Vector3 origin = new Vector3(transform.position.x, bottomY + groundCheckOriginOffset, transform.position.z);

            // 2. 球体射线检测
            // 参数：起点、半径、方向、输出碰撞信息、距离、检测层级
            // 注意：我们稍微多检测一点距离 (groundCheckDistance + groundCheckOriginOffset) 以确保容错
            if (Physics.SphereCast(origin, groundCheckRadius, Vector3.down, out RaycastHit hit,
                                   groundCheckDistance + groundCheckOriginOffset, groundLayer))
            {
                HasAirDashed = false;
                // 检测到了地面
                return true;
            }

            // 没检测到
            return false;
        }
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos) return;
            if (controller == null) controller = GetComponent<CharacterController>();

            // 计算起点（同上）
            float bottomY = transform.position.y + controller.center.y - controller.height / 2f;
            Vector3 origin = new Vector3(transform.position.x, bottomY + groundCheckOriginOffset, transform.position.z);
            Vector3 endPoint = origin + Vector3.down * (groundCheckDistance + groundCheckOriginOffset);

            // 绘制颜色：绿色=在地面，红色=不在地面
            Gizmos.color = _isGroud ? Color.green : Color.red;

            // 绘制起点球体
            Gizmos.DrawWireSphere(origin, groundCheckRadius);
            // 绘制终点球体
            Gizmos.DrawWireSphere(endPoint, groundCheckRadius);
            // 绘制连接线
            Gizmos.DrawLine(origin, endPoint);
        }
#endif
        #endregion

    }
}

