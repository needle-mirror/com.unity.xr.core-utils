using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.XR.CoreUtils.Editor.BuildingBlocks
{
    /// <summary>
    /// This static class handles the management if the building blocks defined by the users.
    /// All classes using the <see cref="BuildingBlockItemAttribute"/> are retrieved. Then, all sections are created here
    /// and stored for later use. Orphans building blocks (building blocks with no section) are also identified here and
    /// added to a special section.
    /// The internal user can retrieve these 2 sets of building blocks using the following utility methods :
    /// <see cref="BuildingBlockManager.GetOrphanBuildingBlocks"/> and <see cref="BuildingBlockManager.GetSections"/>.
    /// These methods are internal as only the Overlay creator needs to access to this information for now.
    /// </summary>
    public static class BuildingBlockManager
    {
        /// <summary>
        /// Internal class created for a special Section containing all the Building BLocks without any sections (Orphans).
        /// This building blocks are referenced in the user code by using the <see cref="BuildingBlockItemAttribute"/> on
        /// the Building Block class definition.
        /// </summary>
        class OrphanBuildingBlocksSection : IBuildingBlockSection
        {
            public string SectionId => null;
            public string SectionIconPath => null;

            internal List<IBuildingBlock> m_OrphanBuildingBlocks = new List<IBuildingBlock>();
            public IEnumerable<IBuildingBlock> GetBuildingBlocks() => m_OrphanBuildingBlocks;
        }

        static List<IBuildingBlockSection> s_Sections;
        static OrphanBuildingBlocksSection s_OrphansSection;

        /// <summary>
        /// Constructor; here we create all the data structures and fill them with existing building blocks.
        /// </summary>
        static BuildingBlockManager()
        {
            s_Sections = new List<IBuildingBlockSection>();
            s_OrphansSection = null;
            var sectionsIds = new List<string>();
            var attributesToSections = new List<(BuildingBlockItemAttribute attribute, IBuildingBlockSection section)>();

            var buildingBlocksItemTypes = TypeCache.GetTypesWithAttribute<BuildingBlockItemAttribute>();

            var orphansTypesAndAttributes = new List<(Type type, BuildingBlockItemAttribute attribute)>();
            for (int i = 0; i < buildingBlocksItemTypes.Count; ++i)
            {
                var itemType = buildingBlocksItemTypes[i];
                // skip the item if the class is abstract or static
                if (itemType.IsAbstract)
                    continue;

                if (typeof(IBuildingBlockSection).IsAssignableFrom(itemType))
                {
                    var section = (IBuildingBlockSection)Activator.CreateInstance(itemType);
                    var id = section.SectionId;
                    if (string.IsNullOrEmpty(id))
                    {
                        // Skipping the Orphans Section as this is not a regular section
                        if (itemType != typeof(OrphanBuildingBlocksSection))
                            Debug.LogWarning(
                                $"Building Blocks Section with null or empty id are not valid. " +
                                $"The section type {itemType} will be skipped.");
                        continue;
                    }

                    var elements = section.GetBuildingBlocks();
                    // Only adding sections containing elements
                    if (elements != null && elements.Any())
                    {
                        sectionsIds.Add(id);
                        attributesToSections.Add((GetAttribute(itemType), section));
                    }
                }
                else if (typeof(IBuildingBlock).IsAssignableFrom(itemType))
                    orphansTypesAndAttributes.Add((itemType, GetAttribute(itemType)));
            }

            attributesToSections.Sort((el1, el2) => el1.attribute.Priority.CompareTo(el2.attribute.Priority));
            foreach (var (_, section) in attributesToSections)
                s_Sections.Add(section);

            //Adding building blocks without section
            if (orphansTypesAndAttributes.Count > 0)
            {
                orphansTypesAndAttributes.Sort((el1, el2) => el1.attribute.Priority.CompareTo(el2.attribute.Priority));

                s_OrphansSection = new OrphanBuildingBlocksSection();
                foreach (var bblockType in orphansTypesAndAttributes)
                {
                    var bblockInstance = (IBuildingBlock)Activator.CreateInstance(bblockType.type);
                    s_OrphansSection.m_OrphanBuildingBlocks.Add(bblockInstance);
                }
            }
        }

        static BuildingBlockItemAttribute GetAttribute(Type type)
        {
            return (BuildingBlockItemAttribute)type.GetCustomAttributes(typeof(BuildingBlockItemAttribute), false)[0];
        }

        /// <summary>
        /// Method to get the orphans building blocks from the internal section <see cref="OrphanBuildingBlocksSection"/>.
        /// </summary>
        /// <param name="orphansBuildingBlocks">A list of orphans building blocks to populate.</param>
        internal static void GetOrphanBuildingBlocks(List<IBuildingBlock> orphansBuildingBlocks)
        {
            if (orphansBuildingBlocks == null)
                return;

            orphansBuildingBlocks.Clear();
            if (s_OrphansSection == null)
                return;

            var orphansBlocks = s_OrphansSection.GetBuildingBlocks();
            foreach (var orphanBlock in orphansBlocks)
                orphansBuildingBlocks.Add(orphanBlock);
        }

        /// <summary>
        /// Method to get the building block sections from the manager.
        /// </summary>
        /// <param name="sections">A list of building block sections to populate.</param>
        internal static void GetSections(List<IBuildingBlockSection> sections)
        {
            if (sections == null)
                return;

            sections.Clear();
            foreach (var section in s_Sections)
                sections.Add(section);
        }
    }
}
