using System.Collections;
using UnityEngine;

// 类魂游戏核心命名空间
namespace WutheringWaves
{
    /// <summary>
    /// 角色外观表现控制器
    /// 负责：装饰剑、龙角的显隐、淡入淡出、材质控制
    /// </summary>
    public class CharacterManifestation : MonoBehaviour
    {
        private CharacterContext context;
        private bool _subscribedGameEvents;

        #region 装饰剑与龙角配置
        [Header("=== 装饰剑淡入淡出配置 ===")]
        [Header("是否启用装饰剑")]
        [SerializeField] private bool useDecorationSword = true; // 当前角色是否拥有装饰剑
        [Header("装饰剑")]
        public GameObject DecorationSword; // 装饰剑对象
        [Tooltip("装饰剑渐变速度，值越大越快")]
        public float decorationSwordFadeSpeed = 1f;

        [Header("=== 龙角淡入淡出配置 ===")]
        [Header("是否启用龙角")]
        [SerializeField] private bool useDragonHorn = true; // 当前角色是否拥有龙角
        [Header("龙角")]
        public GameObject DragonHorn; // 龙角对象
        [Tooltip("龙角渐变速度，值越大越快")]
        public float dragonHornFadeSpeed = 1f;
        #endregion

        #region 私有渲染组件
        private SkinnedMeshRenderer _dragonHornRenderer;
        private Material _dragonHornMaterial;
        private Coroutine _dragonHornFadeCoroutine;

        private MeshRenderer _decorationSwordRenderer;
        private Material _decorationSwordMaterial;
        private Coroutine _decorationSwordFadeCoroutine;

        private readonly string _unlitColorProperty = "_BaseColor";
        #endregion

        #region 角色特殊状态读取
        // 统一读取当前角色是否处于御空状态，避免表现层只绑定到今汐专属驱动
        private bool IsCharacterFloating()
        {
            if (context == null || context.StateMachine == null)
            {
                return false;
            }

            if (context.StateMachine.JinxiSpecialSkillLinker != null)
            {
                return context.StateMachine.JinxiSpecialSkillLinker.IsFloating;
            }

            if (context.StateMachine.KatixiyaSpecialSkillLinker != null)
            {
                return context.StateMachine.KatixiyaSpecialSkillLinker.IsFloating;
            }

            return false;
        }

        // 统一读取当前角色是否处于爆发状态，避免表现层继续写死到单个角色状态枚举
        private bool IsBursting()
        {
            if (context == null || context.StateMachine == null)
            {
                return false;
            }

            CharacterState currentState = context.StateMachine.CurrentStateType;
            return currentState == CharacterState.JinxiQBurst
                || currentState == CharacterState.KatixiyaQBurst;
        }
        #endregion

        #region 初始化
        public void Initialize(CharacterContext context)
        {
            this.context = context;

            // 1.先初始化渲染组件和材质，避免刷新表现时材质为空
            RendererInitialize();

            // 2.订阅角色表现相关事件
            SubscribeGameEvents();

            // 3.根据当前角色状态刷新外观表现
            RefreshDragonHorn();
        }

        private void OnDestroy()
        {
            UnsubscribeGameEvents();
        }

        private void RendererInitialize()
        {
            // ========== 龙角初始化 ==========
            if (useDragonHorn && DragonHorn != null)
            {
                _dragonHornRenderer = DragonHorn.GetComponent<SkinnedMeshRenderer>();
                if (_dragonHornRenderer != null)
                {
                    _dragonHornMaterial = new Material(_dragonHornRenderer.material);
                    _dragonHornRenderer.material = _dragonHornMaterial;
                }
            }

            // ========== 装饰剑初始化 ==========
            if (useDecorationSword && DecorationSword != null)
            {
                _decorationSwordRenderer = DecorationSword.GetComponent<MeshRenderer>();
                if (_decorationSwordRenderer != null)
                {
                    _decorationSwordMaterial = new Material(_decorationSwordRenderer.material);
                    _decorationSwordRenderer.material = _decorationSwordMaterial;
                    SetDecorationSwordAlpha(1f);
                }
            }
        }
        #endregion

        #region 装饰剑 透明控制
        private void SetDecorationSwordAlpha(float alpha)
        {
            if (_decorationSwordMaterial == null)
            {
                return;
            }

            alpha = Mathf.Clamp01(alpha);
            Color currentColor = _decorationSwordMaterial.GetColor(_unlitColorProperty);
            currentColor.a = alpha;
            _decorationSwordMaterial.SetColor(_unlitColorProperty, currentColor);
        }

        public void ShowDecorationSwordFade()
        {
            if (!CanUseDecorationSword())
            {
                return;
            }

            if (_decorationSwordFadeCoroutine != null)
            {
                StopCoroutine(_decorationSwordFadeCoroutine);
            }

            DecorationSword.SetActive(true);

            float startAlpha = _decorationSwordMaterial != null
                ? _decorationSwordMaterial.GetColor(_unlitColorProperty).a
                : 1f;

            _decorationSwordFadeCoroutine = StartCoroutine(DecorationSwordFadeCoroutine(startAlpha, 1f));
        }

        public void HideDecorationSwordFade()
        {
            if (!CanUseDecorationSword())
            {
                return;
            }

            if (_decorationSwordFadeCoroutine != null)
            {
                StopCoroutine(_decorationSwordFadeCoroutine);
            }

            float startAlpha = _decorationSwordMaterial != null
                ? _decorationSwordMaterial.GetColor(_unlitColorProperty).a
                : 1f;

            _decorationSwordFadeCoroutine = StartCoroutine(DecorationSwordFadeCoroutine(startAlpha, 0f));
        }

        private IEnumerator DecorationSwordFadeCoroutine(float startAlpha, float targetAlpha)
        {
            float currentAlpha = startAlpha;

            while (Mathf.Abs(currentAlpha - targetAlpha) > 0.01f)
            {
                currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, decorationSwordFadeSpeed * Time.deltaTime);
                SetDecorationSwordAlpha(currentAlpha);
                yield return null;
            }

            SetDecorationSwordAlpha(targetAlpha);

            if (targetAlpha == 0f && DecorationSword != null)
            {
                DecorationSword.SetActive(false);
            }

            _decorationSwordFadeCoroutine = null;
        }

        public void ShowDecorationSwordInstantly()
        {
            if (!CanUseDecorationSword())
            {
                return;
            }

            DecorationSword.SetActive(true);
            SetDecorationSwordAlpha(1f);
        }

        public void HideDecorationSwordInstantly()
        {
            if (!CanUseDecorationSword())
            {
                return;
            }

            SetDecorationSwordAlpha(0f);
            DecorationSword.SetActive(false);
        }

        // 判断当前角色是否可以使用装饰剑表现
        private bool CanUseDecorationSword()
        {
            // 1.当前角色没有装饰剑时，直接跳过
            if (!useDecorationSword)
            {
                return false;
            }

            // 2.启用了装饰剑但没有拖引用，说明Inspector配置缺失
            if (DecorationSword == null)
            {
                Debug.LogWarning("[CharacterManifestation] 当前角色启用了装饰剑表现，但DecorationSword未赋值。", this);
                return false;
            }

            return true;
        }
        #endregion

        #region 龙角 透明控制
        private void SetDragonHornAlpha(float alpha)
        {
            if (_dragonHornMaterial == null)
            {
                return;
            }

            alpha = Mathf.Clamp01(alpha);
            Color currentColor = _dragonHornMaterial.GetColor(_unlitColorProperty);
            currentColor.a = alpha;
            _dragonHornMaterial.SetColor(_unlitColorProperty, currentColor);
        }

        public void ShowDragonHornFade()
        {
            if (!CanUseDragonHorn())
            {
                return;
            }

            if (_dragonHornRenderer == null || _dragonHornMaterial == null)
            {
                Debug.LogWarning("[CharacterManifestation] 龙角缺少SkinnedMeshRenderer或材质初始化失败。", this);
                return;
            }

            if (_dragonHornFadeCoroutine != null)
            {
                StopCoroutine(_dragonHornFadeCoroutine);
            }

            DragonHorn.SetActive(true);

            Color currentColor = _dragonHornMaterial.GetColor(_unlitColorProperty);
            _dragonHornFadeCoroutine = StartCoroutine(DragonHornFadeCoroutine(currentColor.a, 1f));
        }

        public void HideDragonHornFade()
        {
            if (!CanUseDragonHorn())
            {
                return;
            }

            if (_dragonHornRenderer == null || _dragonHornMaterial == null)
            {
                return;
            }

            if (_dragonHornFadeCoroutine != null)
            {
                StopCoroutine(_dragonHornFadeCoroutine);
            }

            Color currentColor = _dragonHornMaterial.GetColor(_unlitColorProperty);
            _dragonHornFadeCoroutine = StartCoroutine(DragonHornFadeCoroutine(currentColor.a, 0f));
        }

        private IEnumerator DragonHornFadeCoroutine(float startAlpha, float targetAlpha)
        {
            float currentAlpha = startAlpha;

            while (Mathf.Abs(currentAlpha - targetAlpha) > 0.01f)
            {
                currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, dragonHornFadeSpeed * Time.deltaTime);
                SetDragonHornAlpha(currentAlpha);
                yield return null;
            }

            SetDragonHornAlpha(targetAlpha);

            if (targetAlpha == 0f && DragonHorn != null)
            {
                DragonHorn.SetActive(false);
            }

            _dragonHornFadeCoroutine = null;
        }

        // 判断当前角色是否可以使用龙角表现
        private bool CanUseDragonHorn()
        {
            // 1.当前角色没有龙角时，直接跳过
            if (!useDragonHorn)
            {
                return false;
            }

            // 2.启用了龙角但没有拖引用，说明Inspector配置缺失
            if (DragonHorn == null)
            {
                Debug.LogWarning("[CharacterManifestation] 当前角色启用了龙角表现，但DragonHorn未赋值。", this);
                return false;
            }

            return true;
        }
        #endregion

        #region 外观刷新
        private void RefreshDragonHorn()
        {
            if (!CanUseDragonHorn())
            {
                return;
            }

            bool isFloating = IsCharacterFloating();
            bool isQBurst = IsBursting();

            if (isFloating || isQBurst)
            {
                ShowDragonHornFade();
            }
            else
            {
                HideDragonHornFade();
            }
        }
        #endregion

        #region 事件订阅
        private void SubscribeGameEvents()
        {
            if (_subscribedGameEvents)
            {
                return;
            }

            GameEvents.OnCharacterStateChanged += HandleCharacterStateChanged;
            GameEvents.OnFloatingChanged += HandleFloatingChanged;
            _subscribedGameEvents = true;
        }

        private void UnsubscribeGameEvents()
        {
            if (!_subscribedGameEvents)
            {
                return;
            }

            GameEvents.OnCharacterStateChanged -= HandleCharacterStateChanged;
            GameEvents.OnFloatingChanged -= HandleFloatingChanged;
            _subscribedGameEvents = false;
        }

        private void HandleCharacterStateChanged(CharacterStateMachine source, CharacterState oldState, CharacterState newState)
        {
            if (context == null || source != context.StateMachine)
            {
                return;
            }

            RefreshDragonHorn();
        }

        private void HandleFloatingChanged(CharacterAttack source, bool isFloating)
        {
            if (context == null || source != context.AttackLogic)
            {
                return;
            }

            RefreshDragonHorn();
        }
        #endregion
    }
}
