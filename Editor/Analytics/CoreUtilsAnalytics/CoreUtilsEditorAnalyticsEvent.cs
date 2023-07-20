// Uncomment the line below to debug events, you can also use the Analytics Debugger window
// #define DEBUG_COREUTILS_ANALYTICS_EVENT

#if ENABLE_CLOUD_SERVICES_ANALYTICS
using UnityEditor;
using UnityEngine.Analytics;

#if DEBUG_COREUTILS_ANALYTICS_EVENT
using UnityEngine;
#endif

namespace Unity.XR.CoreUtils.Editor.Analytics
{
    abstract class CoreUtilsEditorAnalyticsEvent<T> : EditorAnalyticsEvent<T> where T : struct
    {
        const int k_MaxEventPerHour = 1000;
        const int k_MaxItems = 1000;

        protected override AnalyticsResult SendToAnalyticsServer(T parameter)
        {
            var result = EditorAnalytics.SendEventWithLimit(EventName, parameter, EventVersion);

#if DEBUG_COREUTILS_ANALYTICS_EVENT
            Debug.Log($"[{GetType().Name}] parameter {JsonUtility.ToJson(parameter)} sent with status {result}.");
#endif

            return result;
        }

        protected override AnalyticsResult RegisterWithAnalyticsServer() =>
            EditorAnalytics.RegisterEventWithLimit(EventName, k_MaxEventPerHour, k_MaxItems, CoreUtilsAnalytics.VendorKey, EventVersion);

        protected CoreUtilsEditorAnalyticsEvent(string eventName, int eventVersion) : base(eventName, eventVersion)
        {
        }
    }
}
#endif //ENABLE_CLOUD_SERVICES_ANALYTICS
