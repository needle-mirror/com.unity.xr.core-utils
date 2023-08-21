---
uid: xr-core-utils-building-blocks
---
# XR Building Blocks

![Building Blocks](images/building-blocks.gif)

The building blocks system is an overlay window in the scene view that can help you quickly access commonly used items in your project. To open the building blocks overlay click on the hamburger menu on the scene view &gt; Overlay menu Or move the mouse over the scene view and press the "tilde" key. Afterwards just enable the Building Blocks overlay 

![Open Building Blocks Overlay](images/open-building-blocks-overlay.png)

A Building block is essentially a button that executes a script; be this to instantiate a prefab, create a scripted object or execute an action.

## Building Blocks and Building Block Sections

Building blocks are arranged by sections or can belong to no section at all; Building blocks that don't belong to any section are considered *Unsectioned building blocks* which are not recommended to use for organization purposes. a Building block section contains a set of Building Blocks that can belong to the same package, workflow or topic.

To create a building block section, your class should implement the `IBuildingBlockSection` interface and for adding a building block; your class should implement the `IBuildingBlock` interface as well.

Both sections and blocks have a name and an icon path so they can be distinguished in the UI either when they are docked or when being displayed as an UI overlay.

Building blocks sections are in charge of creating building blocks they contain; they do this by returning a collection of the Building blocks instances through the `IEnumerable<IBuildingBlock> GetBuildingBlocks()` method.

Each building block section implementation should have a `[BuildingBlockItem(Priority = k_SectionPriority)]` attribute which is used by the `BuildingBlockManager`. If a Building Block uses this attribute; this building block will be considered an unsectioned building block and will be displayed outside of any section. Again; for organization purposes this is not recommended.

The `BuildingBlockItem` attribute is also needed for reusable building blocks bases (such as the `PrefabCreatorBuildingBlock`, see later). Else, they would be considered as unsectioned building blocks and shown in the overlay if they are not used by any sections. The `Priority` member in this attribute is used to define the order on which sections appear in the building blocks overlay.

The smaller value (`int`) assigned in the `Priority` member the more towards the top the overlay window the section will appear compared to other building block sections.

## Prefab creator building block.

The `PrefabCreatorBuildingBlock` is a special type of building block that can be used in any building block section to simply instantiate a prefab. With this you don't need to create a building block for the sole purpose of instantiating prefabs.

Prefab creator building blocks are created inside the `BuildingBlockSection` implementations  and added in the `GetBuildingBlocks()` method as mentioned above. 

# Example of a Building Block implementation

The example below creates two building blocks (_Scripted Building Block_ and _Prefab Creator Block_) under a section called _My Block Section_.

![Building Block Example](images/building-block-example.png)

The _scripted building block_ (when pressed) will create an empty game object called "Empty Object" and the _Prefab Creator Block_ building block shows how to use the `PrefabCreatorBuildingBlock` block mentioned above; you will need to set the `m_PrefabAssetPath` variable accordingly to point to a prefab for it to instantiate correctly.

```
using System.Collections.Generic;
using System.Linq;
using Unity.XR.CoreUtils.Editor.BuildingBlocks;
using UnityEditor;
using UnityEngine;

class ScriptedBuildingBlockSample : IBuildingBlock
{
    const string k_Id = "Scripted Building Block";
    const string k_BuildingBlockPath = "GameObject/MySection/"+k_Id;
    const string k_IconPath = "buildingblockIcon";
    const int k_SectionPriority = 10;

    public string Id => k_Id;
    public string IconPath => k_IconPath;

    static void DoInterestingStuff()
    {
        var createdInstance = new GameObject("Empty Object");
        // Do more interesting stuff her
    }

    public void ExecuteBuildingBlock() => DoInterestingStuff();

    // Each building block should have an accompanying MenuItem as a good practice, we add them here.
    [MenuItem(k_BuildingBlockPath, false, k_SectionPriority)]
    public static void ExecuteMenuItem(MenuCommand command) => DoInterestingStuff();
}

[BuildingBlockItem(Priority = k_SectionPriority)]
class BuildingBlockSection1 : IBuildingBlockSection
{
    const string k_SectionId = "My Block Section";
    public string SectionId => k_SectionId;

    const string k_SectionIconPath = "Building/Block/Section/Icon/Path";
    public string SectionIconPath => k_SectionIconPath;
    const int k_SectionPriority = 1;


    string m_PrefabAssetPath = "Assets/Prefabs/SmallCube.prefab";
    GameObject m_Prefab1;

    static PrefabCreatorBuildingBlock s_Prefab1BuildingBlock;
    const int k_Prefab1BuildingBlockPriority = 10;
    const string k_Prefab1BuildingBlockPath = "GameObject/MySection/" + k_SectionId;

    // We add this Menu Item to the prefab building block here.
    [MenuItem(k_Prefab1BuildingBlockPath, false, k_Prefab1BuildingBlockPriority)]
    public static void ExecuteMenuItem(MenuCommand command) => s_Prefab1BuildingBlock.ExecuteBuildingBlock();

    readonly IBuildingBlock[] m_BBlocksElementIds = new IBuildingBlock[]
    {
        new ScriptedBuildingBlockSample()
    };

    public IEnumerable<IBuildingBlock> GetBuildingBlocks()
    {
        if (m_Prefab1 == null)
            m_Prefab1 = AssetDatabase.LoadAssetAtPath<GameObject>(m_PrefabAssetPath);

        if (m_Prefab1 != null)
        {
            //Using the already defined Building Block `PrefabCreatorBuildingBlock` and creating an instance of it with a prefab
            s_Prefab1BuildingBlock = new PrefabCreatorBuildingBlock(m_Prefab1, "Prefab Creator Block", "an/Icon/Path");

            var elements = m_BBlocksElementIds.ToList();
            elements.Add(s_Prefab1BuildingBlock);
            return  elements;
        }

        return m_BBlocksElementIds;
    }
}
```