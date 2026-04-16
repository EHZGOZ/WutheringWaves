using UnityEngine;

namespace WutheringWaves
{
    [DisallowMultipleComponent]
    public class JinxiFeatureRoot : MonoBehaviour
    {
        [Header("今汐特殊技能衔接器")]
        [SerializeField] private JinxiSpecialSkillLinker specialSkillLinker;

        [Header("今汐御龙控制器")]
        [SerializeField] private JinxiDragonController dragonController;

        public JinxiSpecialSkillLinker SpecialSkillLinker => specialSkillLinker;
        public JinxiDragonController DragonController => dragonController;
        public bool IsInitialized { get; private set; }

        public void Initialize(CharacterContext context)
        {
            if (IsInitialized)
            {
                return;
            }

            ResolveReferences();

            specialSkillLinker?.Initialize(context);
            dragonController?.Initialize(context);

            IsInitialized = true;
        }

        private void ResolveReferences()
        {
            if (specialSkillLinker == null)
            {
                specialSkillLinker = GetComponent<JinxiSpecialSkillLinker>();
            }

            if (dragonController == null)
            {
                dragonController = GetComponent<JinxiDragonController>();
            }

            if (specialSkillLinker == null)
            {
                specialSkillLinker = gameObject.AddComponent<JinxiSpecialSkillLinker>();
            }

            if (dragonController == null)
            {
                dragonController = gameObject.AddComponent<JinxiDragonController>();
            }
        }
    }
}
