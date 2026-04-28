using UnityEngine;

namespace WutheringWaves
{
    [System.Serializable]
    public class UIIconLayoutData
    {
        [Header("图标")]
        public Sprite sprite;

        [Header("尺寸")]
        public Vector2 size = new Vector2(100f, 100f);

        [Header("位置偏移")]
        public Vector2 anchoredPosition = Vector2.zero;

        [Header("是否保持宽高比")]
        public bool preserveAspect = true;
    }

    [CreateAssetMenu(menuName = "WutheringWaves/CharacterUIConfig", fileName = "CharacterUIConfig", order = 10)]
    public class CharacterUIConfigSO : ScriptableObject
    {
        [Header("角色头像")]
        public UIIconLayoutData avatarIcon;

        [Header("E技能图标")]
        public UIIconLayoutData[] eSkillIcons;

        [Header("Q技能未充能图标")]
        public UIIconLayoutData qBurstLockedIcon;

        [Header("Q技能可释放图标")]
        public UIIconLayoutData[] qBurstReadyIcons;

        [Header("共鸣条装饰UI")]
        public UIIconLayoutData resonanceDecor;

        [Header("延奏值装饰UI")]
        public UIIconLayoutData concertoDecor;
    }
}
