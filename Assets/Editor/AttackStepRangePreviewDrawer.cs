using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WutheringWaves;

namespace WutheringWaves.EditorTools
{
    #region AttackStep Inspector绘制
    // AttackStep自定义绘制器：负责攻击段选择、高亮和编辑器预览状态提示
    [CustomPropertyDrawer(typeof(AttackStep))]
    public class AttackStepRangePreviewDrawer : PropertyDrawer
    {
        private const float VerticalSpacing = 2f; // 属性之间的垂直间距
        private const float PreviewStatusHeight = 38f; // 预览状态提示框高度

        // AttackStep需要按照现有顺序绘制的子属性
        private static readonly string[] ChildPropertyNames =
        {
            "attackId",
            "hitConfig",
            "rangeConfig",
            "timingConfig"
        };

        // 计算当前AttackStep在Inspector中需要占用的完整高度
        public override float GetPropertyHeight(
            SerializedProperty property,
            GUIContent label)
        {
            // 1.未展开时只占用一行标题高度
            float totalHeight = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded)
            {
                return totalHeight;
            }

            // 2.展开后累加所有子属性的完整高度
            for (int i = 0; i < ChildPropertyNames.Length; i++)
            {
                SerializedProperty childProperty =
                    property.FindPropertyRelative(ChildPropertyNames[i]);

                if (childProperty == null)
                {
                    continue;
                }

                totalHeight += VerticalSpacing;
                totalHeight += EditorGUI.GetPropertyHeight(
                    childProperty,
                    true
                );
            }

            // 3.当前攻击段被选中时，额外显示预览状态提示
            if (AttackStepRangePreviewSelection.IsSelected(property))
            {
                totalHeight += VerticalSpacing;
                totalHeight += PreviewStatusHeight;
            }

            return totalHeight;
        }

        // 绘制当前AttackStep的完整Inspector内容
        public override void OnGUI(
            Rect position,
            SerializedProperty property,
            GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // 1.读取进入本次绘制前的选中状态
            bool isSelected =
                AttackStepRangePreviewSelection.IsSelected(property);

            // 2.计算攻击段标题区域
            Rect headerRect = new Rect(
                position.x,
                position.y,
                position.width,
                EditorGUIUtility.singleLineHeight
            );

            // 3.当前攻击段处于预览状态时高亮标题
            if (isSelected)
            {
                EditorGUI.DrawRect(
                    headerRect,
                    new Color(0.15f, 0.55f, 1f, 0.22f)
                );
            }

            // 4.点击攻击段任意区域时，将它设为唯一预览项
            // 不消耗点击事件，确保内部字段仍然可以正常编辑
            if (Event.current.type == EventType.MouseDown
                && Event.current.button == 0
                && position.Contains(Event.current.mousePosition))
            {
                AttackStepRangePreviewSelection.Select(property);
                GUI.changed = true;
            }

            EditorGUI.BeginChangeCheck();

            // 5.绘制攻击段折叠标题，并显示AttackId和预览状态
            property.isExpanded = EditorGUI.Foldout(
                headerRect,
                property.isExpanded,
                GetStepLabel(property, label, isSelected),
                true
            );

            // 6.展开后按照原有顺序绘制全部子配置
            if (property.isExpanded)
            {
                float currentY = headerRect.yMax + VerticalSpacing;
                int originalIndentLevel = EditorGUI.indentLevel;
                EditorGUI.indentLevel++;

                for (int i = 0; i < ChildPropertyNames.Length; i++)
                {
                    DrawChildProperty(
                        position,
                        property,
                        ChildPropertyNames[i],
                        ref currentY
                    );
                }

                EditorGUI.indentLevel = originalIndentLevel;

                // 7.当前攻击段被选中时显示场景预览目标状态
                if (isSelected)
                {
                    DrawPreviewStatus(
                        position,
                        property,
                        currentY
                    );
                }
            }

            // 8.配置发生变化时立即刷新Scene视图
            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }

            EditorGUI.EndProperty();
        }

        // 绘制AttackStep的单个子属性
        private void DrawChildProperty(
            Rect parentPosition,
            SerializedProperty attackStepProperty,
            string childPropertyName,
            ref float currentY)
        {
            // 1.查找指定子属性
            SerializedProperty childProperty =
                attackStepProperty.FindPropertyRelative(childPropertyName);

            if (childProperty == null)
            {
                return;
            }

            // 2.根据子属性展开状态计算实际高度
            float childHeight = EditorGUI.GetPropertyHeight(
                childProperty,
                true
            );

            Rect childRect = new Rect(
                parentPosition.x,
                currentY,
                parentPosition.width,
                childHeight
            );

            // 3.绘制子属性及其内部配置
            EditorGUI.PropertyField(
                childRect,
                childProperty,
                true
            );

            currentY += childHeight + VerticalSpacing;
        }

        // 获取攻击段标题
        private GUIContent GetStepLabel(
            SerializedProperty property,
            GUIContent originalLabel,
            bool isSelected)
        {
            // 1.读取当前攻击段ID
            SerializedProperty attackIdProperty =
                property.FindPropertyRelative("attackId");

            string attackIdName = "None";

            if (attackIdProperty != null
                && attackIdProperty.enumValueIndex >= 0
                && attackIdProperty.enumValueIndex
                    < attackIdProperty.enumDisplayNames.Length)
            {
                attackIdName =
                    attackIdProperty.enumDisplayNames[
                        attackIdProperty.enumValueIndex
                    ];
            }

            // 2.当前预览项增加明确标记
            string previewPrefix = isSelected ? "▶ " : string.Empty;
            string previewSuffix = isSelected
                ? "（范围预览中）"
                : string.Empty;

            return new GUIContent(
                $"{previewPrefix}{originalLabel.text}"
                + $" [{attackIdName}] {previewSuffix}"
            );
        }

        // 绘制预览状态提示
        private void DrawPreviewStatus(
            Rect parentPosition,
            SerializedProperty property,
            float currentY)
        {
            // 1.获取当前CombatConfigSO
            CombatConfigSO combatConfig =
                property.serializedObject.targetObject as CombatConfigSO;

            // 2.查询场景中使用该配置的启用角色数量
            int matchingCharacterCount =
                AttackStepRangeScenePreview.GetMatchingCharacterCount(
                    combatConfig
                );

            string message;
            MessageType messageType;

            if (matchingCharacterCount > 0)
            {
                message =
                    $"正在 {matchingCharacterCount} 个启用角色上预览攻击范围。"
                    + "范围数值为0时，Scene视图只显示攻击段标签。";

                messageType = MessageType.Info;
            }
            else
            {
                message =
                    "场景中没有找到启用且使用当前CombatConfigSO的角色，"
                    + "因此暂时无法绘制攻击范围。";

                messageType = MessageType.Warning;
            }

            // 3.绘制预览状态提示框
            Rect statusRect = new Rect(
                parentPosition.x,
                currentY,
                parentPosition.width,
                PreviewStatusHeight
            );

            EditorGUI.HelpBox(
                statusRect,
                message,
                messageType
            );
        }
    }
    #endregion

    #region AttackStep预览选择
    // 编辑器预览选择：只保存当前编辑器会话中的选择，不写入配置资产
    internal static class AttackStepRangePreviewSelection
    {
        private static CombatConfigSO selectedCombatConfig; // 当前预览的战斗配置
        private static string selectedPropertyPath; // 当前预览攻击段的SerializedProperty路径

        internal static CombatConfigSO SelectedCombatConfig =>
            selectedCombatConfig;

        internal static string SelectedPropertyPath =>
            selectedPropertyPath;

        internal static bool HasSelection =>
            selectedCombatConfig != null
            && !string.IsNullOrEmpty(selectedPropertyPath);

        // 选择一个AttackStep作为唯一预览项
        internal static void Select(SerializedProperty property)
        {
            // 1.只处理CombatConfigSO中的AttackStep
            if (property == null)
            {
                return;
            }

            CombatConfigSO combatConfig =
                property.serializedObject.targetObject as CombatConfigSO;

            if (combatConfig == null)
            {
                return;
            }

            // 2.同一攻击段重复点击时只刷新Scene视图
            if (selectedCombatConfig == combatConfig
                && selectedPropertyPath == property.propertyPath)
            {
                SceneView.RepaintAll();
                return;
            }

            // 3.保存当前唯一预览项
            selectedCombatConfig = combatConfig;
            selectedPropertyPath = property.propertyPath;

            // 4.刷新角色缓存和Scene视图
            AttackStepRangeScenePreview.InvalidateCharacterCache();
            SceneView.RepaintAll();
        }

        // 判断指定AttackStep是否为当前预览项
        internal static bool IsSelected(SerializedProperty property)
        {
            if (property == null)
            {
                return false;
            }

            CombatConfigSO combatConfig =
                property.serializedObject.targetObject as CombatConfigSO;

            return combatConfig != null
                && selectedCombatConfig == combatConfig
                && selectedPropertyPath == property.propertyPath;
        }

        // 清理已经失效的预览选择
        internal static void Clear()
        {
            selectedCombatConfig = null;
            selectedPropertyPath = string.Empty;

            AttackStepRangeScenePreview.InvalidateCharacterCache();
            SceneView.RepaintAll();
        }
    }
    #endregion

    #region Scene攻击范围预览
    // Scene视图攻击范围绘制器：根据当前选择绘制球形、扇形和矩形范围
    [InitializeOnLoad]
    internal static class AttackStepRangeScenePreview
    {
        private const double CharacterCacheDuration = 0.5d; // 场景角色缓存时间

        private static readonly Color SphereColor =
            new Color(0.2f, 1f, 0.2f, 0.95f); // 球形范围颜色

        private static readonly Color SectorColor =
            new Color(1f, 0.8f, 0.1f, 0.95f); // 扇形范围颜色

        private static readonly Color BoxColor =
            new Color(0f, 0.9f, 1f, 0.95f); // 矩形范围颜色

        private static readonly List<CharacterContext> matchingCharacters =
            new List<CharacterContext>();

        private static CombatConfigSO cachedCombatConfig; // 当前角色缓存对应的战斗配置
        private static double nextCharacterRefreshTime; // 下一次自动刷新缓存的时间
        private static GUIStyle previewLabelStyle; // Scene预览文字样式

        // 编辑器加载时注册Scene视图绘制和缓存刷新事件
        static AttackStepRangeScenePreview()
        {
            SceneView.duringSceneGui -= DrawScenePreview;
            SceneView.duringSceneGui += DrawScenePreview;

            EditorApplication.hierarchyChanged -= InvalidateCharacterCache;
            EditorApplication.hierarchyChanged += InvalidateCharacterCache;

            EditorApplication.projectChanged -= InvalidateCharacterCache;
            EditorApplication.projectChanged += InvalidateCharacterCache;
        }

        // 获取使用指定CombatConfigSO的启用角色数量
        internal static int GetMatchingCharacterCount(
            CombatConfigSO combatConfig)
        {
            return GetMatchingCharacters(combatConfig).Count;
        }

        // 使场景角色缓存失效
        internal static void InvalidateCharacterCache()
        {
            cachedCombatConfig = null;
            nextCharacterRefreshTime = 0d;
            matchingCharacters.Clear();

            SceneView.RepaintAll();
        }

        // 绘制当前选中攻击段
        private static void DrawScenePreview(SceneView sceneView)
        {
            // 1.Scene视图或攻击段选择无效时不绘制
            if (sceneView == null
                || !AttackStepRangePreviewSelection.HasSelection)
            {
                return;
            }

            CombatConfigSO combatConfig =
                AttackStepRangePreviewSelection.SelectedCombatConfig;

            // 2.重新定位当前选中的AttackStep
            SerializedObject serializedCombatConfig =
                new SerializedObject(combatConfig);

            serializedCombatConfig.UpdateIfRequiredOrScript();

            SerializedProperty attackStepProperty =
                serializedCombatConfig.FindProperty(
                    AttackStepRangePreviewSelection.SelectedPropertyPath
                );

            // 3.攻击段被删除或路径失效时清理预览
            if (attackStepProperty == null)
            {
                AttackStepRangePreviewSelection.Clear();
                return;
            }

            SerializedProperty rangeConfigProperty =
                attackStepProperty.FindPropertyRelative("rangeConfig");

            if (rangeConfigProperty == null)
            {
                return;
            }

            // 4.读取当前攻击段ID
            string attackIdName =
                GetAttackIdName(attackStepProperty);

            // 5.在所有使用当前配置的启用角色上绘制范围
            List<CharacterContext> previewTargets =
                GetMatchingCharacters(combatConfig);

            for (int i = 0; i < previewTargets.Count; i++)
            {
                CharacterContext characterContext = previewTargets[i];
                if (characterContext == null)
                {
                    continue;
                }

                DrawAttackRange(
                    characterContext.transform,
                    rangeConfigProperty,
                    attackIdName
                );
            }
        }

        // 获取使用指定战斗配置的启用场景角色
        private static List<CharacterContext> GetMatchingCharacters(
            CombatConfigSO combatConfig)
        {
            // 1.配置为空时清空缓存
            if (combatConfig == null)
            {
                cachedCombatConfig = null;
                matchingCharacters.Clear();
                return matchingCharacters;
            }

            // 2.缓存未过期且配置没有变化时直接复用
            if (cachedCombatConfig == combatConfig
                && EditorApplication.timeSinceStartup
                    < nextCharacterRefreshTime)
            {
                return matchingCharacters;
            }

            // 3.重新搜索当前已加载场景中的全部角色
            cachedCombatConfig = combatConfig;
            nextCharacterRefreshTime =
                EditorApplication.timeSinceStartup
                + CharacterCacheDuration;

            matchingCharacters.Clear();

            CharacterContext[] sceneCharacters =
                Object.FindObjectsOfType<CharacterContext>(true);

            for (int i = 0; i < sceneCharacters.Length; i++)
            {
                CharacterContext characterContext =
                    sceneCharacters[i];

                // 4.只显示场景中处于启用状态的角色
                if (characterContext == null
                    || !characterContext.isActiveAndEnabled
                    || !characterContext.gameObject.scene.IsValid())
                {
                    continue;
                }

                CharacterDataSO characterData =
                    characterContext.CharacterDataSO;

                // 5.只显示使用当前CombatConfigSO的角色
                if (characterData == null
                    || characterData.combatConfig != combatConfig)
                {
                    continue;
                }

                matchingCharacters.Add(characterContext);
            }

            return matchingCharacters;
        }

        // 绘制单个角色的攻击范围
        private static void DrawAttackRange(
            Transform characterTransform,
            SerializedProperty rangeConfigProperty,
            string attackIdName)
        {
            // 1.读取范围叠加模式
            bool enableRangeCombination = GetBoolValue(
                rangeConfigProperty,
                "enableRangeCombination"
            );

            AttackRangeType singleRangeType = GetRangeTypeValue(
                rangeConfigProperty,
                "singleRangeType"
            );

            // 2.根据运行时相同规则决定需要绘制哪些形状
            bool drawSphere = enableRangeCombination
                ? GetBoolValue(rangeConfigProperty, "useSphereRange")
                : singleRangeType == AttackRangeType.Sphere;

            bool drawSector = enableRangeCombination
                ? GetBoolValue(rangeConfigProperty, "useSectorRange")
                : singleRangeType == AttackRangeType.Sector;

            bool drawBox = enableRangeCombination
                ? GetBoolValue(rangeConfigProperty, "useBoxRange")
                : singleRangeType == AttackRangeType.Box;

            bool hasValidRange = false;

            // 3.绘制球形范围
            if (drawSphere)
            {
                hasValidRange |= DrawSphereRange(
                    characterTransform,
                    GetVector3Value(
                        rangeConfigProperty,
                        "sphereOffset"
                    ),
                    GetFloatValue(
                        rangeConfigProperty,
                        "sphereRadius"
                    )
                );
            }

            // 4.绘制扇形范围
            if (drawSector)
            {
                hasValidRange |= DrawSectorRange(
                    characterTransform,
                    GetVector3Value(
                        rangeConfigProperty,
                        "sectorOffset"
                    ),
                    GetFloatValue(
                        rangeConfigProperty,
                        "sectorRadius"
                    ),
                    GetFloatValue(
                        rangeConfigProperty,
                        "sectorAngle"
                    )
                );
            }

            // 5.绘制矩形范围
            if (drawBox)
            {
                hasValidRange |= DrawBoxRange(
                    characterTransform,
                    GetVector3Value(
                        rangeConfigProperty,
                        "boxOffset"
                    ),
                    GetVector3Value(
                        rangeConfigProperty,
                        "boxSize"
                    )
                );
            }

            // 6.在角色头顶显示当前预览攻击段
            string invalidRangeSuffix = hasValidRange
                ? string.Empty
                : "（范围数值为0）";

            Handles.Label(
                characterTransform.position + Vector3.up * 2.2f,
                $"攻击范围预览：{attackIdName}{invalidRangeSuffix}",
                GetPreviewLabelStyle()
            );
        }

        // 绘制球形攻击范围
        private static bool DrawSphereRange(
            Transform characterTransform,
            Vector3 localOffset,
            float radius)
        {
            // 1.半径无效时不绘制
            if (radius <= 0f)
            {
                return false;
            }

            // 2.使用与命中检测相同的本地偏移计算方式
            Vector3 center =
                characterTransform.position
                + characterTransform.TransformDirection(localOffset);

            DrawWireSphere(
                center,
                radius,
                SphereColor
            );

            return true;
        }

        // 绘制扇形攻击范围
        private static bool DrawSectorRange(
            Transform characterTransform,
            Vector3 localOffset,
            float radius,
            float angle)
        {
            // 1.半径或角度无效时不绘制
            if (radius <= 0f || angle <= 0f)
            {
                return false;
            }

            Vector3 center =
                characterTransform.position
                + characterTransform.TransformDirection(localOffset);

            float clampedAngle = Mathf.Clamp(angle, 0f, 360f);

            // 2.获取角色在水平面上的前方
            Vector3 horizontalForward =
                Vector3.ProjectOnPlane(
                    characterTransform.forward,
                    Vector3.up
                );

            if (horizontalForward.sqrMagnitude <= 0.001f)
            {
                horizontalForward = Vector3.forward;
            }

            horizontalForward.Normalize();

            // 3.360度扇形与完整球形范围等价，直接绘制球形线框
            if (clampedAngle >= 359.9f)
            {
                DrawWireSphere(
                    center,
                    radius,
                    SectorColor
                );

                return true;
            }

            float halfAngle = clampedAngle * 0.5f;

            Vector3 leftDirection =
                Quaternion.AngleAxis(
                    -halfAngle,
                    Vector3.up
                ) * horizontalForward;

            Vector3 rightDirection =
                Quaternion.AngleAxis(
                    halfAngle,
                    Vector3.up
                ) * horizontalForward;

            Color previousColor = Handles.color;
            Handles.color = SectorColor;

            // 4.绘制水平扇形弧线和两条边界线
            Handles.DrawWireArc(
                center,
                Vector3.up,
                leftDirection,
                clampedAngle,
                radius
            );

            Handles.DrawLine(
                center,
                center + leftDirection * radius
            );

            Handles.DrawLine(
                center,
                center + rightDirection * radius
            );

            // 5.绘制左右边界的垂直半圆，表现OverlapSphere的垂直高度
            DrawVerticalHalfArc(
                center,
                leftDirection,
                radius
            );

            DrawVerticalHalfArc(
                center,
                rightDirection,
                radius
            );

            Handles.color = previousColor;
            return true;
        }

        // 绘制矩形攻击范围
        private static bool DrawBoxRange(
            Transform characterTransform,
            Vector3 localOffset,
            Vector3 boxSize)
        {
            // 1.任意尺寸无效时不绘制
            if (boxSize.x <= 0f
                || boxSize.y <= 0f
                || boxSize.z <= 0f)
            {
                return false;
            }

            Vector3 center =
                characterTransform.position
                + characterTransform.TransformDirection(localOffset);

            Color previousColor = Handles.color;
            Matrix4x4 previousMatrix = Handles.matrix;

            Handles.color = BoxColor;

            // 2.使用与Physics.OverlapBox相同的位置和旋转
            Handles.matrix = Matrix4x4.TRS(
                center,
                characterTransform.rotation,
                Vector3.one
            );

            Handles.DrawWireCube(
                Vector3.zero,
                boxSize
            );

            Handles.matrix = previousMatrix;
            Handles.color = previousColor;

            return true;
        }

        // 绘制球形线框
        private static void DrawWireSphere(
            Vector3 center,
            float radius,
            Color color)
        {
            Color previousColor = Handles.color;
            Handles.color = color;

            // 使用三个互相垂直的圆组成球形线框
            Handles.DrawWireDisc(
                center,
                Vector3.up,
                radius
            );

            Handles.DrawWireDisc(
                center,
                Vector3.right,
                radius
            );

            Handles.DrawWireDisc(
                center,
                Vector3.forward,
                radius
            );

            Handles.color = previousColor;
        }

        // 绘制扇形边界的垂直半圆
        private static void DrawVerticalHalfArc(
            Vector3 center,
            Vector3 horizontalDirection,
            float radius)
        {
            Vector3 rotationAxis =
                Vector3.Cross(
                    Vector3.up,
                    horizontalDirection
                );

            if (rotationAxis.sqrMagnitude <= 0.001f)
            {
                return;
            }

            rotationAxis.Normalize();

            Handles.DrawWireArc(
                center,
                rotationAxis,
                Vector3.up,
                180f,
                radius
            );
        }

        // 获取当前攻击段ID名称
        private static string GetAttackIdName(
            SerializedProperty attackStepProperty)
        {
            SerializedProperty attackIdProperty =
                attackStepProperty.FindPropertyRelative("attackId");

            if (attackIdProperty == null
                || attackIdProperty.enumValueIndex < 0
                || attackIdProperty.enumValueIndex
                    >= attackIdProperty.enumDisplayNames.Length)
            {
                return "None";
            }

            return attackIdProperty.enumDisplayNames[
                attackIdProperty.enumValueIndex
            ];
        }

        // 安全读取布尔配置
        private static bool GetBoolValue(
            SerializedProperty parentProperty,
            string propertyName)
        {
            SerializedProperty property =
                parentProperty.FindPropertyRelative(propertyName);

            return property != null && property.boolValue;
        }

        // 安全读取浮点配置
        private static float GetFloatValue(
            SerializedProperty parentProperty,
            string propertyName)
        {
            SerializedProperty property =
                parentProperty.FindPropertyRelative(propertyName);

            return property != null ? property.floatValue : 0f;
        }

        // 安全读取Vector3配置
        private static Vector3 GetVector3Value(
            SerializedProperty parentProperty,
            string propertyName)
        {
            SerializedProperty property =
                parentProperty.FindPropertyRelative(propertyName);

            return property != null
                ? property.vector3Value
                : Vector3.zero;
        }

        // 安全读取攻击范围类型
        private static AttackRangeType GetRangeTypeValue(
            SerializedProperty parentProperty,
            string propertyName)
        {
            SerializedProperty property =
                parentProperty.FindPropertyRelative(propertyName);

            if (property == null)
            {
                return AttackRangeType.Sector;
            }

            return (AttackRangeType)property.enumValueIndex;
        }

        // 获取Scene预览文字样式
        private static GUIStyle GetPreviewLabelStyle()
        {
            if (previewLabelStyle != null)
            {
                return previewLabelStyle;
            }

            previewLabelStyle = new GUIStyle(
                EditorStyles.boldLabel
            );

            previewLabelStyle.normal.textColor = Color.white;
            return previewLabelStyle;
        }
    }
    #endregion
}