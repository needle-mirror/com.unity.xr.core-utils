#if ENABLE_CLOUD_SERVICES_ANALYTICS
using System;
using UnityEngine;

namespace Unity.XR.CoreUtils.Editor.Analytics
{
    /// <summary>
    /// The project validation usage analytics event.
    /// </summary>
    class ProjectValidationUsageEvent : CoreUtilsEditorAnalyticsEvent<ProjectValidationUsageEvent.Payload>
    {
        const string k_EventName = "xrcoreutils_projectvalidation_usage";
        const int k_EventVersion = 1;

        internal const string NoneCategoryName = "[NONE]";

        /// <summary>
        /// The event parameter.
        /// Do not rename any field, the field names are used the identify the table/event column of this event payload.
        /// </summary>
        [Serializable]
        internal struct Payload
        {
            internal const string FixIssuesName = "FixIssues";

            [SerializeField]
            internal string Name;

            [SerializeField]
            internal IssuesStatus[] IssuesStatusByCategory;
        }

        /// <summary>
        /// The fixed issues status parameter.
        /// Do not rename any field, the field names are used the identify the table/event data of this event payload.
        /// </summary>
        [Serializable]
        internal struct IssuesStatus
        {
            [SerializeField]
            internal string Category;

            [SerializeField]
            internal int SuccessfullyFixed;

            [SerializeField]
            internal int FailedToFix;
        }

        internal ProjectValidationUsageEvent() : base(k_EventName, k_EventVersion)
        {
        }

        internal bool SendFixIssues(IssuesStatus[] issuesStatusByCategory) =>
            Send(new Payload {Name = Payload.FixIssuesName, IssuesStatusByCategory = issuesStatusByCategory});
    }
}
#endif //ENABLE_CLOUD_SERVICES_ANALYTICS
