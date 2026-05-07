using System.Collections;
using UnityEngine;

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

        [Header("=== 武器剑淡入淡出配置 ===")]
        [Header("是否启用武器剑")]
        [SerializeField] private bool useBattleWeapon = true; // 当前角色是否拥有武器剑
        [Header("武器剑")]
        public GameObject BattleWeapon; // 武器剑对象
        [Tooltip("武器剑渐变速度，值越大越快")]
        public float battleWeaponFadeSpeed = 1f;

        [Header("=== 武器剑2淡入淡出配置 ===")]
        [Header("是否启用武器剑2")]
        [SerializeField] private bool useBattleWeapon2 = true; // 当前角色是否拥有武器剑2
        [Header("武器剑2")]
        public GameObject BattleWeapon2; // 武器剑2对象
        [Tooltip("武器剑2渐变速度，值越大越快")]
        public float battleWeapon2FadeSpeed = 1f;

        [Header("=== 龙角淡入淡出配置 ===")]
        [Header("是否启用龙角")]
        [SerializeField] private bool useDragonHorn = true; // 当前角色是否拥有龙角
        [Header("龙角")]
        public GameObject DragonHorn; // 龙角对象
        [Tooltip("龙角渐变速度，值越大越快")]
        public float dragonHornFadeSpeed = 1f;
        #endregion

        #region 私有渲染组件
        private Renderer _dragonHornRenderer;
        private Material[] _dragonHornMaterials;
        private Coroutine _dragonHornFadeCoroutine;

        private MeshRenderer _decorationSwordRenderer;
        private Material _decorationSwordMaterial;
        private Coroutine _decorationSwordFadeCoroutine;

        private MeshRenderer _battleWeaponRenderer;
        private Material[] _battleWeaponMaterials;
        private Coroutine _battleWeaponFadeCoroutine;

        private MeshRenderer _battleWeapon2Renderer;
        private Material[] _battleWeapon2Materials;
        private Coroutine _battleWeapon2FadeCoroutine;

        private readonly string _unlitColorProperty = "_BaseColor";
        #endregion

        #region 生命周期
        private void OnDestroy()
        {
            UnsubscribeGameEvents();
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

            // 3.刷新角色龙角
            ReflashDragonHornFade();

        }


        private void RendererInitialize()
        {
            // ========== 龙角初始化 ==========
            if (useDragonHorn && DragonHorn != null)
            {
                _dragonHornRenderer = DragonHorn.GetComponent<Renderer>();
                if (_dragonHornRenderer == null)
                {
                    _dragonHornRenderer = DragonHorn.GetComponentInChildren<Renderer>(true);
                }

                if (_dragonHornRenderer != null)
                {
                    _dragonHornMaterials = _dragonHornRenderer.materials;

                    for (int i = 0; i < _dragonHornMaterials.Length; i++)
                    {
                        _dragonHornMaterials[i] = new Material(_dragonHornMaterials[i]);
                    }

                    _dragonHornRenderer.materials = _dragonHornMaterials;
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

            // ========== 武器剑初始化 ==========
            if (useBattleWeapon && BattleWeapon != null)
            {
                _battleWeaponRenderer = BattleWeapon.GetComponent<MeshRenderer>();
                if (_battleWeaponRenderer == null)
                {
                    _battleWeaponRenderer = BattleWeapon.GetComponentInChildren<MeshRenderer>(true);
                }

                if (_battleWeaponRenderer != null)
                {
                    _battleWeaponMaterials = _battleWeaponRenderer.materials;

                    for (int i = 0; i < _battleWeaponMaterials.Length; i++)
                    {
                        _battleWeaponMaterials[i] = new Material(_battleWeaponMaterials[i]);
                    }

                    _battleWeaponRenderer.materials = _battleWeaponMaterials;
                    SetBattleWeaponAlpha(1f);
                }
            }

            // ========== 武器剑2初始化 ==========
            if (useBattleWeapon2 && BattleWeapon2 != null)
            {
                _battleWeapon2Renderer = BattleWeapon2.GetComponent<MeshRenderer>();
                if (_battleWeapon2Renderer == null)
                {
                    _battleWeapon2Renderer = BattleWeapon2.GetComponentInChildren<MeshRenderer>(true);
                }

                if (_battleWeapon2Renderer != null)
                {
                    _battleWeapon2Materials = _battleWeapon2Renderer.materials;

                    for (int i = 0; i < _battleWeapon2Materials.Length; i++)
                    {
                        _battleWeapon2Materials[i] = new Material(_battleWeapon2Materials[i]);
                    }

                    _battleWeapon2Renderer.materials = _battleWeapon2Materials;
                    SetBattleWeapon2Alpha(1f);
                }
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

            GameEvents.OnFloatingChanged += HandleFloatingChanged;
            GameEvents.OnBrustChanged += HandleBrustChanged;
            GameEvents.OnCharacterSwitched += HandleCharacterSwitched;
            _subscribedGameEvents = true;
        }

        private void UnsubscribeGameEvents()
        {
            if (!_subscribedGameEvents)
            {
                return;
            }


            GameEvents.OnFloatingChanged -= HandleFloatingChanged;
            GameEvents.OnBrustChanged -= HandleBrustChanged;
            GameEvents.OnCharacterSwitched -= HandleCharacterSwitched;
            _subscribedGameEvents = false;
        }

        private void HandleFloatingChanged(bool isFloating)
        {
            if (context == null)
            {
                return;
            }
            this.isFloating = isFloating;
            ReflashDragonHornFade();
        }
        private void HandleBrustChanged(bool isBrust)
        {
            if (context == null)
            {
                return;
            }
            this.isBrust = isBrust;
            ReflashDragonHornFade();
        }
        private void HandleCharacterSwitched(CharacterContext previousContext, CharacterContext currentContext)
        {
            if (context == null)
            {
                return;
            }

            // 当前角色被切出去时，只做安全隐藏，不走渐变协程
            if (previousContext == context)
            {
                HideDragonHornInstantly();
                return;
            }

            // 当前角色被切回来时，重新同步今汐当前状态，再刷新龙角表现
            if (currentContext == context)
            {
                isFloating = context.CharacterRuntimeData != null && context.CharacterRuntimeData.jinxiIsFloating;

                // 如果爆发状态没有存在RuntimeData里，这里先保持false，避免切回来误显示
                isBrust = false;

                ReflashDragonHornFade();
                EnterJudgmentSwordShoworHide();
            }
        }

        #endregion

        #region 判断当前角色是否可以使用对应表现
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
        // 判断当前角色是否可以使用武器剑表现
        private bool CanUseBattleWeapon()
        {
            // 1.当前角色没有武器剑时，直接跳过
            if (!useBattleWeapon)
            {
                return false;
            }

            // 2.启用了武器剑但没有拖引用，说明Inspector配置缺失
            if (BattleWeapon == null)
            {
                Debug.LogWarning("[CharacterManifestation] 当前角色启用了武器剑表现，但BattleWeapon未赋值。", this);
                return false;
            }

            return true;
        }
        // 判断当前角色是否可以使用武器剑2表现
        private bool CanUseBattleWeapon2()
        {
            // 1.当前角色没有武器剑2时，直接跳过
            if (!useBattleWeapon2)
            {
                return false;
            }

            // 2.启用了武器剑2但没有拖引用，说明Inspector配置缺失
            if (BattleWeapon2 == null)
            {
                Debug.LogWarning("[CharacterManifestation] 当前角色启用了武器剑2表现，但BattleWeapon2未赋值。", this);
                return false;
            }

            return true;
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

        #region 攻击行为判断两把剑的隐显
        public void EnterJudgmentSwordShoworHide()
        {
            HideDecorationSwordInstantly();
            HideBattleWeapon2Instantly();
            ShowBattleWeaponInstantly();
        }
        public void EnterJudgmentSwordShoworHide2()
        {
            HideDecorationSwordInstantly();
            HideBattleWeaponInstantly();
            ShowBattleWeapon2Instantly();
        }
        public void ExitJudgmentSwordShoworHide()
        {
            HideBattleWeaponInstantly();
            HideBattleWeapon2Instantly();
            ShowDecorationSwordInstantly();
        }
        #endregion

        #region 装饰剑 透明控制
        //立刻显现
        public void ShowDecorationSwordInstantly()
        {
            if (!CanUseDecorationSword())
            {
                return;
            }

            DecorationSword.SetActive(true);
            SetDecorationSwordAlpha(1f);
        }
        //立刻隐藏
        public void HideDecorationSwordInstantly()
        {
            if (!CanUseDecorationSword())
            {
                return;
            }

            SetDecorationSwordAlpha(0f);
            DecorationSword.SetActive(false);
        }
        //缓慢显现
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
        //缓慢隐藏
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
        //设置透明度
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
        //相关协程
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
        #endregion

        #region 武器剑 透明控制
        //立刻显现
        public void ShowBattleWeaponInstantly()
        {
            if (!CanUseBattleWeapon())
            {
                return;
            }

            BattleWeapon.SetActive(true);
            SetBattleWeaponAlpha(1f);
            
        }
        //立刻隐藏
        public void HideBattleWeaponInstantly()
        {
            if (!CanUseBattleWeapon())
            {
                return;
            }

            SetBattleWeaponAlpha(0f);
            BattleWeapon.SetActive(false);
        }
        //缓慢显现
        public void ShowBattleWeaponFade()
        {
            if (!CanUseBattleWeapon())
            {
                return;
            }

            if (_battleWeaponRenderer == null || _battleWeaponMaterials == null)
            {
                Debug.LogWarning("[CharacterManifestation] 武器剑缺少MeshRenderer或材质初始化失败。", this);
                return;
            }

            if (_battleWeaponFadeCoroutine != null)
            {
                StopCoroutine(_battleWeaponFadeCoroutine);
            }

            BattleWeapon.SetActive(true);

            float startAlpha = GetBattleWeaponAlpha();

            _battleWeaponFadeCoroutine = StartCoroutine(BattleWeaponFadeCoroutine(startAlpha, 1f));
        }
        //缓慢隐藏
        public void HideBattleWeaponFade()
        {
            if (!CanUseBattleWeapon())
            {
                return;
            }

            if (_battleWeaponRenderer == null || _battleWeaponMaterials == null)
            {
                return;
            }

            if (_battleWeaponFadeCoroutine != null)
            {
                StopCoroutine(_battleWeaponFadeCoroutine);
            }

            float startAlpha = GetBattleWeaponAlpha();

            _battleWeaponFadeCoroutine = StartCoroutine(BattleWeaponFadeCoroutine(startAlpha, 0f));
        }
        //获取当前透明度
        private float GetBattleWeaponAlpha()
        {
            if (_battleWeaponMaterials == null || _battleWeaponMaterials.Length == 0)
            {
                return 1f;
            }

            if (!_battleWeaponMaterials[0].HasProperty(_unlitColorProperty))
            {
                return 1f;
            }

            return _battleWeaponMaterials[0].GetColor(_unlitColorProperty).a;
        }
        //设置透明度
        private void SetBattleWeaponAlpha(float alpha)
        {
            if (_battleWeaponMaterials == null)
            {
                return;
            }

            alpha = Mathf.Clamp01(alpha);

            for (int i = 0; i < _battleWeaponMaterials.Length; i++)
            {
                if (_battleWeaponMaterials[i] == null || !_battleWeaponMaterials[i].HasProperty(_unlitColorProperty))
                {
                    continue;
                }

                Color currentColor = _battleWeaponMaterials[i].GetColor(_unlitColorProperty);
                currentColor.a = alpha;
                _battleWeaponMaterials[i].SetColor(_unlitColorProperty, currentColor);
            }
        }
        //相关协程
        private IEnumerator BattleWeaponFadeCoroutine(float startAlpha, float targetAlpha)
        {
            float currentAlpha = startAlpha;

            while (Mathf.Abs(currentAlpha - targetAlpha) > 0.01f)
            {
                currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, battleWeaponFadeSpeed * Time.deltaTime);
                SetBattleWeaponAlpha(currentAlpha);
                yield return null;
            }

            SetBattleWeaponAlpha(targetAlpha);

            if (targetAlpha == 0f && BattleWeapon != null)
            {
                BattleWeapon.SetActive(false);
            }

            _battleWeaponFadeCoroutine = null;
        }
        #endregion

        #region 武器剑2 透明控制
        //立刻显现
        public void ShowBattleWeapon2Instantly()
        {
            if (!CanUseBattleWeapon2())
            {
                return;
            }

            BattleWeapon2.SetActive(true);
            SetBattleWeapon2Alpha(1f);
        }
        //立刻隐藏
        public void HideBattleWeapon2Instantly()
        {
            if (!CanUseBattleWeapon2())
            {
                return;
            }

            SetBattleWeapon2Alpha(0f);
            BattleWeapon2.SetActive(false);
        }
        //缓慢显现
        public void ShowBattleWeapon2Fade()
        {
            if (!CanUseBattleWeapon2())
            {
                return;
            }

            if (_battleWeapon2Renderer == null || _battleWeapon2Materials == null)
            {
                Debug.LogWarning("[CharacterManifestation] 武器剑2缺少MeshRenderer或材质初始化失败。", this);
                return;
            }

            if (_battleWeapon2FadeCoroutine != null)
            {
                StopCoroutine(_battleWeapon2FadeCoroutine);
            }

            BattleWeapon2.SetActive(true);

            float startAlpha = GetBattleWeapon2Alpha();

            _battleWeapon2FadeCoroutine = StartCoroutine(BattleWeapon2FadeCoroutine(startAlpha, 1f));
        }
        //缓慢隐藏
        public void HideBattleWeapon2Fade()
        {
            if (!CanUseBattleWeapon2())
            {
                return;
            }

            if (_battleWeapon2Renderer == null || _battleWeapon2Materials == null)
            {
                return;
            }

            if (_battleWeapon2FadeCoroutine != null)
            {
                StopCoroutine(_battleWeapon2FadeCoroutine);
            }

            float startAlpha = GetBattleWeapon2Alpha();

            _battleWeapon2FadeCoroutine = StartCoroutine(BattleWeapon2FadeCoroutine(startAlpha, 0f));
        }
        //获取当前透明度
        private float GetBattleWeapon2Alpha()
        {
            if (_battleWeapon2Materials == null || _battleWeapon2Materials.Length == 0)
            {
                return 1f;
            }

            if (!_battleWeapon2Materials[0].HasProperty(_unlitColorProperty))
            {
                return 1f;
            }

            return _battleWeapon2Materials[0].GetColor(_unlitColorProperty).a;
        }
        //设置透明度
        private void SetBattleWeapon2Alpha(float alpha)
        {
            if (_battleWeapon2Materials == null)
            {
                return;
            }

            alpha = Mathf.Clamp01(alpha);

            for (int i = 0; i < _battleWeapon2Materials.Length; i++)
            {
                if (_battleWeapon2Materials[i] == null || !_battleWeapon2Materials[i].HasProperty(_unlitColorProperty))
                {
                    continue;
                }

                Color currentColor = _battleWeapon2Materials[i].GetColor(_unlitColorProperty);
                currentColor.a = alpha;
                _battleWeapon2Materials[i].SetColor(_unlitColorProperty, currentColor);
            }
        }
        //相关协程
        private IEnumerator BattleWeapon2FadeCoroutine(float startAlpha, float targetAlpha)
        {
            float currentAlpha = startAlpha;

            while (Mathf.Abs(currentAlpha - targetAlpha) > 0.01f)
            {
                currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, battleWeapon2FadeSpeed * Time.deltaTime);
                SetBattleWeapon2Alpha(currentAlpha);
                yield return null;
            }

            SetBattleWeapon2Alpha(targetAlpha);

            if (targetAlpha == 0f && BattleWeapon2 != null)
            {
                BattleWeapon2.SetActive(false);
            }

            _battleWeapon2FadeCoroutine = null;
        }
        #endregion

        #region 龙角 透明控制
        private bool isFloating = false;
        private bool isBrust = false;

        // 判断当前外观控制器是否可以启动协程
        private bool CanStartFadeCoroutine()
        {
            return isActiveAndEnabled && gameObject.activeInHierarchy;
        }

        private void ReflashDragonHornFade()
        {
            if(isFloating|| isBrust)
            {
                ShowDragonHornFade();
            }
            else
            {
                HideDragonHornFade();
            }
        }

        //缓慢显现
        public void ShowDragonHornFade()
        {
            if (!CanUseDragonHorn())
            {
                return;
            }

            if (_dragonHornRenderer == null || _dragonHornMaterials == null || _dragonHornMaterials.Length == 0)
            {
                Debug.LogWarning("[CharacterManifestation] 龙角缺少Renderer或材质初始化失败。", this);
                return;
            }
            // 当前物体未激活时不能启动协程，直接设置最终显示状态
            if (!CanStartFadeCoroutine())
            {
                SetDragonHornAlpha(1f);
                DragonHorn.SetActive(true);
                _dragonHornFadeCoroutine = null;
                return;
            }

            if (_dragonHornFadeCoroutine != null)
            {
                StopCoroutine(_dragonHornFadeCoroutine);
            }

            DragonHorn.SetActive(true);

            _dragonHornFadeCoroutine = StartCoroutine(DragonHornFadeCoroutine(GetDragonHornAlpha(), 1f));
        }
        //缓慢隐藏
        public void HideDragonHornFade()
        {
            if (!CanUseDragonHorn())
            {
                return;
            }

            if (_dragonHornRenderer == null || _dragonHornMaterials == null || _dragonHornMaterials.Length == 0)
            {
                return;
            }
            // 当前物体未激活时不能启动协程，直接设置最终隐藏状态
            if (!CanStartFadeCoroutine())
            {
                SetDragonHornAlpha(0f);
                DragonHorn.SetActive(false);
                _dragonHornFadeCoroutine = null;
                return;
            }

            if (_dragonHornFadeCoroutine != null)
            {
                StopCoroutine(_dragonHornFadeCoroutine);
            }

            _dragonHornFadeCoroutine = StartCoroutine(DragonHornFadeCoroutine(GetDragonHornAlpha(), 0f));
        }
        //立刻隐藏
        public void HideDragonHornInstantly()
        {
            if (!CanUseDragonHorn())
            {
                return;
            }

            if (_dragonHornFadeCoroutine != null)
            {
                StopCoroutine(_dragonHornFadeCoroutine);
                _dragonHornFadeCoroutine = null;
            }

            SetDragonHornAlpha(0f);
            DragonHorn.SetActive(false);
        }
        //设置透明度
        private void SetDragonHornAlpha(float alpha)
        {
            if (_dragonHornMaterials == null)
            {
                return;
            }

            alpha = Mathf.Clamp01(alpha);

            for (int i = 0; i < _dragonHornMaterials.Length; i++)
            {
                Material material = _dragonHornMaterials[i];
                string colorProperty = GetColorProperty(material);
                if (string.IsNullOrEmpty(colorProperty))
                {
                    continue;
                }

                Color currentColor = material.GetColor(colorProperty);
                currentColor.a = alpha;
                material.SetColor(colorProperty, currentColor);
            }
        }

        private float GetDragonHornAlpha()
        {
            if (_dragonHornMaterials == null)
            {
                return 0f;
            }

            for (int i = 0; i < _dragonHornMaterials.Length; i++)
            {
                Material material = _dragonHornMaterials[i];
                string colorProperty = GetColorProperty(material);
                if (!string.IsNullOrEmpty(colorProperty))
                {
                    return material.GetColor(colorProperty).a;
                }
            }

            return DragonHorn != null && DragonHorn.activeSelf ? 1f : 0f;
        }

        private string GetColorProperty(Material material)
        {
            if (material == null)
            {
                return null;
            }

            if (material.HasProperty(_unlitColorProperty))
            {
                return _unlitColorProperty;
            }

            return material.HasProperty("_Color") ? "_Color" : null;
        }
        //相关协程
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
        #endregion


        
    }
}
