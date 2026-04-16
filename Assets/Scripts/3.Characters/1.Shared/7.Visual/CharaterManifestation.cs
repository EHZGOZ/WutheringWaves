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
        [Header("装饰剑")]
        public GameObject DecorationSword;
        [Tooltip("装饰剑渐变速度，值越大越快")]
        public float decorationSwordFadeSpeed = 1f;

        [Header("=== 龙角淡入淡出配置 ===")]
        [Header("龙角")]
        public GameObject DragonHorn;
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

        #region 初始化
        public void Initialize(CharacterContext context)
        {
            this.context = context;
            SubscribeGameEvents();
            RefreshDragonHorn();
            RendererInitialize();
        }

        // 兼容旧链路：允许CharacterCore继续转发到新初始化入口
        private void OnDestroy()
        {
            UnsubscribeGameEvents();
        }

        private void RendererInitialize()
        {
            // ========== 龙角初始化 ==========
            if (DragonHorn != null)
            {
                _dragonHornRenderer = DragonHorn.GetComponent<SkinnedMeshRenderer>();
                if (_dragonHornRenderer != null)
                {
                    _dragonHornMaterial = new Material(_dragonHornRenderer.material);
                    _dragonHornRenderer.material = _dragonHornMaterial;
                }
            }

            // ========== 装饰剑初始化 ==========
            if (DecorationSword != null)
            {
                _decorationSwordRenderer = DecorationSword.GetComponent<MeshRenderer>();
                if (_decorationSwordRenderer != null)
                {
                    _decorationSwordMaterial = new Material(_decorationSwordRenderer.material);
                    _decorationSwordRenderer.material = _decorationSwordMaterial;
                    SetDecorationSwordAlpha(1);
                }
            }
        }
        #endregion

        #region 装饰剑 透明控制
        private void SetDecorationSwordAlpha(float alpha)
        {
            if (_decorationSwordMaterial == null) return;
            alpha = Mathf.Clamp01(alpha);
            Color currentColor = _decorationSwordMaterial.GetColor(_unlitColorProperty);
            currentColor.a = alpha;
            _decorationSwordMaterial.SetColor(_unlitColorProperty, currentColor);
        }

        public void ShowDecorationSwordFade()
        {
            ShowDecorationSwordInstantly();
        }

        public void HideDecorationSwordFade()
        {
            HideDecorationSwordInstantly();
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
            if (targetAlpha == 0) DecorationSword.SetActive(false);
            _decorationSwordFadeCoroutine = null;
        }

        public void ShowDecorationSwordInstantly()
        {
            DecorationSword.SetActive(true);
        }

        public void HideDecorationSwordInstantly()
        {
            DecorationSword.SetActive(false);
        }
        #endregion

        #region 龙角 透明控制
        private void SetDragonHornAlpha(float alpha)
        {
            if (_dragonHornMaterial == null)
            {
                Debug.LogError("材质为空！");
                return;
            }
            alpha = Mathf.Clamp01(alpha);
            Color currentColor = _dragonHornMaterial.GetColor(_unlitColorProperty);
            currentColor.a = alpha;
            _dragonHornMaterial.SetColor(_unlitColorProperty, currentColor);
        }

        public void ShowDragonHornFade()
        {
            if (_dragonHornRenderer == null || _dragonHornMaterial == null)
            {
                Debug.LogWarning("龙角未赋值或缺少SkinnedMeshRenderer组件！");
                return;
            }
            if (_dragonHornFadeCoroutine != null)
                StopCoroutine(_dragonHornFadeCoroutine);
            Color currentColor = _dragonHornMaterial.GetColor(_unlitColorProperty);
            _dragonHornFadeCoroutine = StartCoroutine(DragonHornFadeCoroutine(currentColor.a, 1));
        }

        public void HideDragonHornFade()
        {
            if (_dragonHornRenderer == null || _dragonHornMaterial == null) return;
            if (_dragonHornFadeCoroutine != null)
                StopCoroutine(_dragonHornFadeCoroutine);
            Color currentColor = _dragonHornMaterial.GetColor(_unlitColorProperty);
            _dragonHornFadeCoroutine = StartCoroutine(DragonHornFadeCoroutine(currentColor.a, 0));
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
            _dragonHornFadeCoroutine = null;
        }
        #endregion

        private void RefreshDragonHorn()
        {
            bool isFloating = context != null && context.StateMachine != null && context.StateMachine.IsFloating();
            bool isQBurst = context != null && context.StateMachine != null && context.StateMachine.CurrentStateType == CharacterState.QBurst;

            if (isFloating || isQBurst)
                ShowDragonHornFade();
            else
                HideDragonHornFade();
        }

        private void SubscribeGameEvents()
        {
            if (_subscribedGameEvents) return;

            GameEvents.OnCharacterStateChanged += HandleCharacterStateChanged;
            GameEvents.OnFloatingChanged += HandleFloatingChanged;
            _subscribedGameEvents = true;
        }

        private void UnsubscribeGameEvents()
        {
            if (!_subscribedGameEvents) return;

            GameEvents.OnCharacterStateChanged -= HandleCharacterStateChanged;
            GameEvents.OnFloatingChanged -= HandleFloatingChanged;
            _subscribedGameEvents = false;
        }

        private void HandleCharacterStateChanged(CharacterStateMachine source, CharacterState oldState, CharacterState newState)
        {
            if (context == null || source != context.StateMachine) return;
            RefreshDragonHorn();
        }

        private void HandleFloatingChanged(CharacterAttack source, bool isFloating)
        {
            if (context == null || source != context.AttackLogic) return;
            RefreshDragonHorn();
        }
    }
}
