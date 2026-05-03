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

        [Header("今汐专用御剑控制器")]
        [SerializeField] private JinxiSpecialSwordController specialSwordController;


        public JinxiSpecialSkillLinker SpecialSkillLinker => specialSkillLinker;
        public JinxiDragonController DragonController => dragonController;
        public JinxiSpecialSwordController SpecialSwordController => specialSwordController;

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
            specialSwordController?.Initialize(context);


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
            if (specialSwordController == null)
            {
                specialSwordController = GetComponent<JinxiSpecialSwordController>();
            }


        }
    }
}
