using System;
using UnityEngine;

namespace Unity.XR.CoreUtils
{
    /// <summary>
    /// Behavior that fires a callback when it is destroyed
    /// </summary>
    [ExecuteInEditMode]
    public class OnDestroyNotifier : MonoBehaviour
    {
        /// <summary>
        /// Called when this behavior is destroyed
        /// </summary>
        public Action<OnDestroyNotifier> Destroyed { private get; set; }

        void OnDestroy()
        {
            Destroyed?.Invoke(this);
        }
    }
}
