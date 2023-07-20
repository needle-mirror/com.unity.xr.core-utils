#if ENABLE_CLOUD_SERVICES_ANALYTICS
using UnityEditor;

namespace Unity.XR.CoreUtils.Editor.Analytics
{
    static class CoreUtilsAnalytics
    {
        internal const string VendorKey = "unity.xrcoreutils";

        internal static readonly ProjectValidationUsageEvent ProjectValidationUsageEvent = new ProjectValidationUsageEvent();
        internal static readonly BuildingBlocksUsageEvent BuildingBlocksUsageEvent = new BuildingBlocksUsageEvent();

        [InitializeOnLoadMethod]
        static void RegisterEvents()
        {
            ProjectValidationUsageEvent.Register();
            BuildingBlocksUsageEvent.Register();
        }
    }
}
#endif //ENABLE_CLOUD_SERVICES_ANALYTICS
