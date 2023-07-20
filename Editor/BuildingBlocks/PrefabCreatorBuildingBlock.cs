using UnityEditor;
using UnityEngine;

namespace Unity.XR.CoreUtils.Editor.BuildingBlocks
{
    /// <summary>
    /// This Building Block can be used in a Building Block Section to instantiate a prefab.
    /// The Building Block Section is in charge of setting the prefab to instantiate using that Building Block as well
    /// as setting a unique and recognizable name and icon for this Building Block.
    /// </summary>
    public class PrefabCreatorBuildingBlock : IBuildingBlock
    {
        string m_Id;
        string m_IconPath;
        bool m_IsEnabled;
        string m_Tooltip;
        GameObject m_Prefab = null;

        /// </inheritdoc>
        public string Id => m_Id;

        /// </inheritdoc>
        public string IconPath => m_IconPath;

        /// </inheritdoc>
        public string Tooltip => m_Tooltip;

        /// </inheritdoc>
        public bool IsEnabled => m_IsEnabled;

        public PrefabCreatorBuildingBlock(GameObject prefab, string buildingBlockId = "Prefab Creator", string buildingBlockIconPath = null, bool isEnabled = true, string tooltip = "")
        {
            m_Prefab = prefab;
            m_Id = buildingBlockId;
            m_IconPath = buildingBlockIconPath;
            m_IsEnabled = isEnabled;
            m_Tooltip = tooltip;
        }

        public void ExecuteBuildingBlock()
        {
            if (m_Prefab == null)
                return;

            var objName = GameObjectUtility.GetUniqueNameForSibling(null,m_Prefab.name);
            var createdObj = Object.Instantiate(m_Prefab);
            createdObj.name = objName;

            Undo.RegisterCreatedObjectUndo(createdObj, $"Create {objName}");
            Selection.activeGameObject = createdObj;
        }
    }
}
