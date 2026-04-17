using UnityEngine;

namespace WutheringWaves
{
    [DisallowMultipleComponent]
    public class KatixiyaFeatureRoot : MonoBehaviour
    {
        [Header("卡提希娅特殊技能衔接器")]
        [SerializeField] private KatixiyaSpecialSkillLinker specialSkillLinker;

        public KatixiyaSpecialSkillLinker SpecialSkillLinker => specialSkillLinker;
        public bool IsInitialized { get; private set; }

        public void Initialize(CharacterContext context)
        {
            if (IsInitialized)
            {
                return;
            }

            ResolveReferences();

            specialSkillLinker?.Initialize(context);

            IsInitialized = true;
        }

        private void ResolveReferences()
        {
            if (specialSkillLinker == null)
            {
                specialSkillLinker = GetComponent<KatixiyaSpecialSkillLinker>();
            }
        }
    }
}

