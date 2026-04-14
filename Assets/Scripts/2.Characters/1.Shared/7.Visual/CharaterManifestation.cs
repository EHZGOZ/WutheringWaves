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
        private CharacterCore core;
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
        // 龙角渲染组件
        private SkinnedMeshRenderer _dragonHornRenderer;
        // 龙角专属实例材质（避免修改全局材质）
        private Material _dragonHornMaterial;
        // 淡入淡出协程引用（防止重复启动）
        private Coroutine _dragonHornFadeCoroutine;

        // 装饰剑渲染组件
        private MeshRenderer _decorationSwordRenderer;
        // 装饰剑专属实例材质
        private Material _decorationSwordMaterial;
        // 装饰剑淡入淡出协程
        private Coroutine _decorationSwordFadeCoroutine;

        // URP Unlit 材质颜色属性名（固定写法，不要改）
        private readonly string _unlitColorProperty = "_BaseColor";
        #endregion

        #region 初始化
        public void Initialize(CharacterCore core)
        {
            this.core = core;
            SubscribeGameEvents();
            RefreshDragonHorn();
            RendererInitialize();
        }

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
                    //SetDragonHornAlpha(0);
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

        // 渐变显示装饰剑（淡入）
        public void ShowDecorationSwordFade()
        {
            ShowDecorationSwordInstantly();

        }

        // 渐变隐藏装饰剑（淡出）
        public void HideDecorationSwordFade()
        {
            HideDecorationSwordInstantly();

        }

        // 装饰剑渐变协程
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

        // 装饰剑【立即显现】
        public void ShowDecorationSwordInstantly()
        {
            DecorationSword.SetActive(true);
        }
        // 装饰剑【立即消失】
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

        // 渐变显示龙角（淡入）
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
        // 渐变隐藏龙角（淡出）
        public void HideDragonHornFade()
        {
            if (_dragonHornRenderer == null || _dragonHornMaterial == null) return;
            if (_dragonHornFadeCoroutine != null)
                StopCoroutine(_dragonHornFadeCoroutine);
            Color currentColor = _dragonHornMaterial.GetColor(_unlitColorProperty);
            _dragonHornFadeCoroutine = StartCoroutine(DragonHornFadeCoroutine(currentColor.a, 0));
        }

        // 龙角渐变协程核心
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
            if (core.attackLogic.IsFloating || core.stateMachine.CurrentStateType == CharacterState.QBurst)
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
            if (core == null || source != core.stateMachine) return;
            RefreshDragonHorn();
        }

        private void HandleFloatingChanged(CharacterAttack source, bool isFloating)
        {
            if (core == null || source != core.attackLogic) return;
            RefreshDragonHorn();
        }
    }
}
