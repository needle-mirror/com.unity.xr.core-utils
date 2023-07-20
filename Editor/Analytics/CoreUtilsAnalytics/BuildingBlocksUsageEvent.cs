#if ENABLE_CLOUD_SERVICES_ANALYTICS
using System;
using UnityEngine;

namespace Unity.XR.CoreUtils.Editor.Analytics
{
    /// <summary>
    /// The building blocks usage analytics event.
    /// </summary>
    class BuildingBlocksUsageEvent : CoreUtilsEditorAnalyticsEvent<BuildingBlocksUsageEvent.Payload>
    {
        const string k_EventName = "xrcoreutils_buildingblocks_usage";
        const int k_EventVersion = 1;

        /// <summary>
        /// The event parameter.
        /// Do not rename any field, the field names are used the identify the table/event column of this event payload.
        /// </summary>
        [Serializable]
        internal struct Payload
        {
            internal const string OverlayButtonClickedName = "OverlayButtonClicked";
            internal const string ToolbarButtonClickedName = "ToolbarButtonClicked";

            [SerializeField]
            internal string Name;

            [SerializeField]
            internal string SectionId;

            [SerializeField]
            internal string BuildingBlockId;
        }

        internal BuildingBlocksUsageEvent() : base(k_EventName, k_EventVersion)
        {
        }

        internal bool SendOverlayButtonClicked(string sectionId, string buildingBlockId) =>
            Send(Payload.OverlayButtonClickedName, sectionId, buildingBlockId);

        internal bool SendToolbarButtonClicked(string sectionId, string buildingBlockId) =>
            Send(Payload.ToolbarButtonClickedName, sectionId, buildingBlockId);

        bool Send(string name, string sectionId, string buildingBlockId) =>
            Send(new Payload { Name = name, SectionId = sectionId, BuildingBlockId = buildingBlockId});
    }
}
#endif
