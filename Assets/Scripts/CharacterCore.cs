using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

namespace WutheringWaves
{
    //单个角色的核心控制器（总控脚本）
    public class CharacterCore : MonoBehaviour
    {
        #region 数据配置 
        [Header("=== 核心数据配置 ===")]
        [Tooltip("角色动画控制器")]
        [SerializeField] internal Animator animator;
        [Tooltip("角色专属数值数据")]
        [SerializeField] internal CharacterData characterData;

        [Header("=== 辅助脚本配置（优先自动获取，可手动补全） ===")]
        [Tooltip("根运动处理器")]
        [SerializeField] internal CharacterRootMotion rootMotion;
        [Tooltip("效果表现逻辑")]
        [SerializeField] internal CharacterManifestation manifestationLogic;

        [Header("=== 功能脚本配置（优先自动获取，可手动补全） ===")]
        [Tooltip("输入读取器")]
        [SerializeField] internal CharacterInputReader inputReader;
        [Tooltip("输入缓冲器")]
        [SerializeField] internal InputBuffer inputBuffer;
        [Tooltip("角色第三人称相机")]
        [SerializeField] internal CharacterCamera cameraLogic;
        [Tooltip("角色状态机")]
        [SerializeField] internal CharacterStateMachine stateMachine;
        [Tooltip("移动逻辑")]
        [SerializeField] internal CharacterMovement movementLogic;
        [Tooltip("体力逻辑")]
        [SerializeField] internal CharacterStamina staminaLogic;
        [Tooltip("战斗逻辑")]
        [SerializeField] internal CharacterAttack attackLogic;

        internal CharacterController characterController; // Unity内置角色物理控制器


        #endregion

        #region 初始化
        //1.自动获取子组件/自身组件
        private void AutoGetComponents()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (rootMotion == null) rootMotion = GetComponentInChildren<CharacterRootMotion>();
            if (stateMachine == null) stateMachine = GetComponent<CharacterStateMachine>();
            if (inputReader == null) inputReader = GetComponent<CharacterInputReader>();
            if (inputBuffer == null) inputBuffer = GetComponent<InputBuffer>();
            if (attackLogic == null) attackLogic = GetComponent<CharacterAttack>();
            if (movementLogic == null) movementLogic = GetComponent<CharacterMovement>();
            if (staminaLogic == null) staminaLogic = GetComponent<CharacterStamina>();
            if (characterController == null) characterController = GetComponent<CharacterController>();
            if (cameraLogic == null) cameraLogic = GetComponent<CharacterCamera>();
            if (manifestationLogic == null) manifestationLogic = GetComponent<CharacterManifestation>();

        }
        //2.校验核心组件，缺失则打印错误提示
        private void ValidateComponents()
        {
            if (characterData == null)
                Debug.LogError($"【{gameObject.name}】CharacterCore 未绑定 CharacterData！", this);
            if (animator == null)
                Debug.LogError($"【{gameObject.name}】CharacterCore 未找到 Animator 组件！", this);
            if (stateMachine == null)
                Debug.LogError($"【{gameObject.name}】CharacterCore 未找到 CharacterStateMachine 组件！", this);
            if (inputReader == null)
                Debug.LogError($"【{gameObject.name}】CharacterCore 未找到 CharacterInputReader 组件！", this);
            if (inputBuffer == null)
                Debug.LogError($"【{gameObject.name}】CharacterCore 未找到 InputBuffer 组件！", this);
            if (characterController == null)
                Debug.LogError($"【{gameObject.name}】CharacterCore 未找到 CharacterController 组件！请在角色根物体挂载该Unity内置组件", this);
            if (cameraLogic == null)
                Debug.LogError($"【{gameObject.name}】CharacterCore 未找到 characterCamera 组件！请在角色根物体挂载该Unity内置组件", this);
            if (staminaLogic == null)
                Debug.LogError($"【{gameObject.name}】CharacterCore 未找到 CharacterStamina 组件！", this);
            if (rootMotion == null)
                Debug.LogError($"【{gameObject.name}】CharacterCore 未找到 CharacterRootMotion 组件！请在角色根物体挂载该Unity内置组件", this);
            if (manifestationLogic == null)
                Debug.LogError($"【{gameObject.name}】CharacterCore 未找到 manifestationLogic 组件！请在角色根物体挂载该Unity内置组件", this);
        }
        //3.初始化所有功能脚本（传递核心依赖）
        private void InitializeFunctionScripts()
        {

            //1.初始化核心数据
            characterData?.Initialize();
            //2.初始化体力
            staminaLogic?.Initialize(this);
            //2.初始化动画根运动
            rootMotion?.Initialize(this);
            //3.初始化效果表现器
            manifestationLogic?.Initialize(this);
            //3.初始化输入底层
            inputReader?.Initialize();
            inputBuffer?.Initialize();
            //4.初始化相机
            cameraLogic?.Initialize();
            //5.初始化状态机
            stateMachine?.Initialize(this, characterData, animator, characterController, movementLogic, inputReader, inputBuffer, attackLogic, manifestationLogic);
            //6.初始化移动逻辑
            movementLogic?.Initialize(this);
            //7.初始化战斗逻辑
            attackLogic?.Initialize(this);
        }


        #endregion

        #region 生命周期函数

        private void Awake()
        {
            // 1. 自动获取缺失的组件（减少手动拖入的麻烦）
            AutoGetComponents();

            // 2. 校验核心组件是否齐全（避免运行时报错）
            ValidateComponents();

            //3.初始化所有功能脚本
            InitializeFunctionScripts();

            DisablePlayerInput();
        }

        private void Start()
        {
            
        }

        private void Update()
        {
          
            if(Input.GetKeyDown(KeyCode.T))
            {
                animator.SetTrigger("test");  
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                GameTimeService timeService = GameTimeService.Instance;
                if (timeService != null)
                {
                    timeService.ToggleBetweenScales(0.2f, 1f);
                }
                else
                {
                    Debug.LogWarning("[CharacterCore] GameTimeService is missing, skip time-scale toggle.", this);
                }
            }
        }
        private void LateUpdate()
        {
            //常态视角更新
            UpdateCameraLogic();
        }

        #endregion

        #region 角色专属数值数据
        public CharacterData GetCharacterData()
        {
            // 空值校验：如果未在Inspector绑定CharacterData，打印错误提示（与原有校验逻辑一致）
            if (characterData == null)
            {
                Debug.LogError($"【{gameObject.name}】CharacterCore 未绑定 CharacterData，无法获取currentHealth！", this);
                return null;
            }
            // 返回私有配置的CharacterData实例，外部可访问其公共字段（如currentHealth）
            return characterData;
        }

        public CharacterStamina GetCharacterStamina()
        {
            return staminaLogic;
        }
        #endregion

        #region 角色第三人称相机
        //常态视角更新
        private void UpdateCameraLogic()
        {
            if (inputReader == null || cameraLogic == null)
            {
                return;
            }

            cameraLogic.UpdateCameraLook(inputReader.LookInput);
            cameraLogic.UpdateCameraZoom(inputReader.ZoomInput);
        }
        public void CameraMovement()
        {
            if (cameraLogic.virtualCamera2.Priority == 11)
            {
                cameraLogic.virtualCamera2.Priority = 9;
            }
            else
            {
                cameraLogic.virtualCamera2.Priority = 11;
            }
        }
        #endregion

        #region 角色状态机

        #endregion

        #region 战斗逻辑

        #endregion

        #region 耐力消耗
        //奔跑 闪避  攀爬均会消耗体力
        public bool StaminaCost(float staminaCost)
        {
            return staminaLogic != null && staminaLogic.TryConsume(staminaCost);
        }
        #endregion

        #region 输入控制
        // 禁用玩家输入（UI打开时调用）
        public void DisablePlayerInput()
        {
            inputReader?.DisablePlayerInput();
        }

        // 恢复玩家输入（UI关闭时调用）
        public void EnablePlayerInput()
        {
            inputReader?.EnablePlayerInput();
        }
        #endregion


    }
}

