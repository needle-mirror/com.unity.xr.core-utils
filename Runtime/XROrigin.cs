using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.XR;
using UnityEngine.Assertions;
#if INCLUDE_INPUT_SYSTEM
using UnityEngine.InputSystem.XR;
#endif

namespace Unity.XR.CoreUtils
{
    /// <summary>
    /// The XR Origin component is typically attached to the base object of the XR Origin,
    /// and stores the <see cref="GameObject"/> that will be manipulated via locomotion.
    /// It is also used for offsetting the camera.
    /// </summary>
    [DisallowMultipleComponent]
    public class XROrigin : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The Camera to associate with the AR device.")]
        Camera m_Camera;

        /// <summary>
        /// The <c>Camera</c> to associate with the AR device. It must be a child of this <c>XROrigin</c>.
        /// </summary>
        /// <remarks>
        /// The <c>Camera</c> should update its position and rotation according to the AR device.
        /// This is typically accomplished by adding a <c>TrackedPoseDriver</c> component to the
        /// <c>Camera</c>.
        /// </remarks>
#if UNITY_EDITOR
        public new Camera camera
#else
        public Camera camera
#endif
        {
            get => m_Camera;
            set => m_Camera = value;
        }

        /// <summary>
        /// The parent <c>Transform</c> for all "trackables" (for example, planes and feature points).
        /// </summary>
        public Transform trackablesParent { get; private set; }

        /// <summary>
        /// Invoked during
        /// [Application.onBeforeRender](xref:UnityEngine.Application.onBeforeRender(UnityEngine.Events.UnityAction))
        /// whenever the <see cref="trackablesParent"/> [transform](xref:UnityEngine.Transform) changes.
        /// </summary>
        public event Action<ARTrackablesParentTransformChangedEventArgs> trackablesParentTransformChanged;

        /// <summary>
        /// Sets which Tracking Origin Mode to use when initializing the input device.
        /// </summary>
        /// <seealso cref="requestedTrackingOriginMode"/>
        /// <seealso cref="TrackingOriginModeFlags"/>
        /// <seealso cref="XRInputSubsystem.TrySetTrackingOriginMode"/>
        public enum TrackingOriginMode
        {
            /// <summary>
            /// Uses the default Tracking Origin Mode of the input device.
            /// </summary>
            /// <remarks>
            /// When changing to this value after startup, the Tracking Origin Mode will not be changed.
            /// </remarks>
            NotSpecified,

            /// <summary>
            /// Sets the Tracking Origin Mode to <see cref="TrackingOriginModeFlags.Device"/>.
            /// Input devices will be tracked relative to the first known location.
            /// </summary>
            /// <remarks>
            /// Represents a device-relative tracking origin. A device-relative tracking origin defines a local origin
            /// at the position of the device in space at some previous point in time, usually at a recenter event,
            /// power-on, or AR/VR session start. Pose data provided by the device will be in this space relative to
            /// the local origin. This means that poses returned in this mode will not include the user height (for VR)
            /// or the device height (for AR) and any camera tracking from the XR device will need to be manually offset accordingly.
            /// </remarks>
            /// <seealso cref="TrackingOriginModeFlags.Device"/>
            Device,

            /// <summary>
            /// Sets the Tracking Origin Mode to <see cref="TrackingOriginModeFlags.Floor"/>.
            /// Input devices will be tracked relative to a location on the floor.
            /// </summary>
            /// <remarks>
            /// Represents the tracking origin whereby (0, 0, 0) is on the "floor" or other surface determined by the
            /// XR device being used. The pose values reported by an XR device in this mode will include the height
            /// of the XR device above this surface, removing the need to offset the position of the camera tracking
            /// the XR device by the height of the user (VR) or the height of the device above the floor (AR).
            /// </remarks>
            /// <seealso cref="TrackingOriginModeFlags.Floor"/>
            Floor,
        }

        //This is the average seated height, which is 44 inches.
        const float k_DefaultCameraYOffset = 1.1176f;

        [SerializeField]
        GameObject m_OriginBaseGameObject;

        /// <summary>
        /// The "Rig" <see cref="GameObject"/> is used to refer to the base of the XR Rig, by default it is this <see cref="GameObject"/>.
        /// This is the <see cref="GameObject"/> that will be manipulated via locomotion.
        /// </summary>
        public GameObject origin
        {
            get => m_OriginBaseGameObject;
            set => m_OriginBaseGameObject = value;
        }

        [SerializeField]
        GameObject m_CameraFloorOffsetObject;

        /// <summary>
        /// The <see cref="GameObject"/> to move to desired height off the floor (defaults to this object if none provided).
        /// This is used to transform the XR device from camera space to XR Origin space.
        /// </summary>
        public GameObject cameraFloorOffsetObject
        {
            get => m_CameraFloorOffsetObject;
            set
            {
                m_CameraFloorOffsetObject = value;
                MoveOffsetHeight();
            }
        }

        [SerializeField]
        TrackingOriginMode m_RequestedTrackingOriginMode = TrackingOriginMode.NotSpecified;

        /// <summary>
        /// The type of tracking origin to use for this Rig. Tracking origins identify where (0, 0, 0) is in the world
        /// of tracking. Not all devices support all tracking origin modes.
        /// </summary>
        /// <seealso cref="TrackingOriginMode"/>
        public TrackingOriginMode requestedTrackingOriginMode
        {
            get => m_RequestedTrackingOriginMode;
            set
            {
                m_RequestedTrackingOriginMode = value;
                TryInitializeCamera();
            }
        }

        [SerializeField]
        float m_CameraYOffset = k_DefaultCameraYOffset;

        /// <summary>
        /// Camera height to be used when in <c>Device</c> Tracking Origin Mode to define the height of the user from the floor.
        /// This is the amount that the camera is offset from the floor when moving the <see cref="cameraFloorOffsetObject"/>.
        /// </summary>
        public float cameraYOffset
        {
            get => m_CameraYOffset;
            set
            {
                m_CameraYOffset = value;
                MoveOffsetHeight();
            }
        }

        /// <summary>
        /// (Read Only) The Tracking Origin Mode of this XR Origin.
        /// </summary>
        /// <seealso cref="requestedTrackingOriginMode"/>
        public TrackingOriginModeFlags currentTrackingOriginMode { get; private set; }

        /// <summary>
        /// (Read Only) The rig's local position in camera space.
        /// </summary>
        public Vector3 originInCameraSpacePos => m_Camera.transform.InverseTransformPoint(m_OriginBaseGameObject.transform.position);

        /// <summary>
        /// (Read Only) The camera's local position in origin space.
        /// </summary>
        public Vector3 cameraInOriginSpacePos => m_Camera.transform.InverseTransformPoint(m_Camera.transform.position);

        /// <summary>
        /// (Read Only) The camera's height relative to the rig.
        /// </summary>
        public float cameraInOriginSpaceHeight => cameraInOriginSpacePos.y;

        /// <summary>
        /// Used to cache the input subsystems without creating additional GC allocations.
        /// </summary>
        static readonly List<XRInputSubsystem> s_InputSubsystems = new List<XRInputSubsystem>();

        // Bookkeeping to track lazy initialization of the tracking origin mode type.
        bool m_CameraInitialized;
        bool m_CameraInitializing;

        //XRI Functions

        /// <summary>
        /// Sets the height of the camera based on the current tracking origin mode by updating the <see cref="cameraFloorOffsetObject"/>.
        /// </summary>
        void MoveOffsetHeight()
        {
            if (!Application.isPlaying)
                return;

            switch (currentTrackingOriginMode)
            {
                case TrackingOriginModeFlags.Floor:
                    MoveOffsetHeight(0f);
                    break;
                case TrackingOriginModeFlags.Device:
                    MoveOffsetHeight(m_CameraYOffset);
                    break;
                default:
                    return;
            }
        }

        /// <summary>
        /// Sets the height of the camera to the given <paramref name="y"/> value by updating the <see cref="cameraFloorOffsetObject"/>.
        /// </summary>
        /// <param name="y">The local y-position to set.</param>
        void MoveOffsetHeight(float y)
        {
            if (m_CameraFloorOffsetObject != null)
            {
                var offsetTransform = m_CameraFloorOffsetObject.transform;
                var desiredPosition = offsetTransform.localPosition;
                desiredPosition.y = y;
                offsetTransform.localPosition = desiredPosition;
            }
        }
        
        /// <summary>
        /// Repeatedly attempt to initialize the camera.
        /// </summary>
        void TryInitializeCamera()
        {
            if (!Application.isPlaying)
                return;

            m_CameraInitialized = SetupCamera();
            if (!m_CameraInitialized & !m_CameraInitializing)
                StartCoroutine(RepeatInitializeCamera());
        }

        /// <summary>
        /// Handles re-centering and off-setting the camera in space depending on which tracking origin mode it is setup in.
        /// </summary>
        bool SetupCamera()
        {
            var initialized = true;

            SubsystemManager.GetInstances(s_InputSubsystems);
            if (s_InputSubsystems.Count > 0)
            {
                foreach (var inputSubsystem in s_InputSubsystems)
                {
                    if (SetupCamera(inputSubsystem))
                    {
                        // It is possible this could happen more than
                        // once so unregister the callback first just in case.
                        inputSubsystem.trackingOriginUpdated -= OnInputSubsystemTrackingOriginUpdated;
                        inputSubsystem.trackingOriginUpdated += OnInputSubsystemTrackingOriginUpdated;
                    }
                    else
                    {
                        initialized = false;
                    }
                }
            }

            return initialized;
        }

        bool SetupCamera(XRInputSubsystem inputSubsystem)
        {
            if (inputSubsystem == null)
                return false;

            var successful = true;

            switch (m_RequestedTrackingOriginMode)
            {
                case TrackingOriginMode.NotSpecified:
                    currentTrackingOriginMode = inputSubsystem.GetTrackingOriginMode();
                    break;
                case TrackingOriginMode.Device:
                case TrackingOriginMode.Floor:
                {
                    var supportedModes = inputSubsystem.GetSupportedTrackingOriginModes();

                    // We need to check for Unknown because we may not be in a state where we can read this data yet.
                    if (supportedModes == TrackingOriginModeFlags.Unknown)
                        return false;

                    // Convert from the request enum to the flags enum that is used by the subsystem
                    var equivalentFlagsMode = m_RequestedTrackingOriginMode == TrackingOriginMode.Device
                        ? TrackingOriginModeFlags.Device
                        : TrackingOriginModeFlags.Floor;

                    // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags -- Treated like Flags enum when querying supported modes
                    if ((supportedModes & equivalentFlagsMode) == 0)
                    {
                        m_RequestedTrackingOriginMode = TrackingOriginMode.NotSpecified;
                        currentTrackingOriginMode = inputSubsystem.GetTrackingOriginMode();
                        Debug.LogWarning($"Attempting to set the tracking origin mode to {equivalentFlagsMode}, but that is not supported by the SDK." +
                            $" Supported types: {supportedModes:F}. Using the current mode of {currentTrackingOriginMode} instead.", this);
                    }
                    else
                    {
                        successful = inputSubsystem.TrySetTrackingOriginMode(equivalentFlagsMode);
                    }
                }
                    break;
                default:
                    Assert.IsTrue(false, $"Unhandled {nameof(TrackingOriginMode)}={m_RequestedTrackingOriginMode}");
                    return false;
            }

            if (successful)
                MoveOffsetHeight();

            if (currentTrackingOriginMode == TrackingOriginModeFlags.Device || m_RequestedTrackingOriginMode == TrackingOriginMode.Device)
                successful = inputSubsystem.TryRecenter();

            return successful;
        }

        IEnumerator RepeatInitializeCamera()
        {
            m_CameraInitializing = true;
            while (!m_CameraInitialized)
            {
                yield return null;
                if (!m_CameraInitialized)
                    m_CameraInitialized = SetupCamera();
            }
            m_CameraInitializing = false;
        }

        void OnInputSubsystemTrackingOriginUpdated(XRInputSubsystem inputSubsystem)
        {
            currentTrackingOriginMode = inputSubsystem.GetTrackingOriginMode();
            MoveOffsetHeight();
        }

        /// <summary>
        /// Rotates the XR origin object around the camera object by the provided <paramref name="angleDegrees"/>.
        /// This rotation only occurs around the rig's Up vector
        /// </summary>
        /// <param name="angleDegrees">The amount of rotation in degrees.</param>
        /// <returns>Returns <see langword="true"/> if the rotation is performed. Otherwise, returns <see langword="false"/>.</returns>
        public bool RotateAroundCameraUsingOriginUp(float angleDegrees)
        {
            return RotateAroundCameraPosition(m_OriginBaseGameObject.transform.up, angleDegrees);
        }
        
        /// <summary>
        /// Rotates the XR origin object around the camera object's position in world space using the provided <paramref name="vector"/>
        /// as the rotation axis. The XR Origin object is rotated by the amount of degrees provided in <paramref name="angleDegrees"/>.
        /// </summary>
        /// <param name="vector">The axis of the rotation.</param>
        /// <param name="angleDegrees">The amount of rotation in degrees.</param>
        /// <returns>Returns <see langword="true"/> if the rotation is performed. Otherwise, returns <see langword="false"/>.</returns>
        public bool RotateAroundCameraPosition(Vector3 vector, float angleDegrees)
        {
            if (m_Camera == null || m_OriginBaseGameObject == null)
            {
                return false;
            }

            // Rotate around the camera position
            m_OriginBaseGameObject.transform.RotateAround(m_Camera.transform.position, vector, angleDegrees);

            return true;
        }
        
        /// <summary>
        /// This function will rotate the XR Origin object such that the XR Origin's up vector will match the provided vector.
        /// </summary>
        /// <param name="destinationUp">the vector to which the XR Origin object's up vector will be matched.</param>
        /// <returns>Returns <see langword="true"/> if the rotation is performed or the vectors have already been matched.
        /// Otherwise, returns <see langword="false"/>.</returns>
        public bool MatchOriginUp(Vector3 destinationUp)
        {
            if (m_OriginBaseGameObject == null)
            {
                return false;
            }

            if (m_OriginBaseGameObject.transform.up == destinationUp)
                return true;

            var rigUp = Quaternion.FromToRotation(m_OriginBaseGameObject.transform.up, destinationUp);
            m_OriginBaseGameObject.transform.rotation = rigUp * transform.rotation;

            return true;
        }

        /// <summary>
        /// This function will rotate the XR Origin object around the camera object using the <paramref name="destinationUp"/> vector such that:
        /// <list type="bullet">
        /// <item>The camera will look at the area in the direction of the <paramref name="destinationForward"/></item>
        /// <item>The projection of camera's forward vector on the plane with the normal <paramref name="destinationUp"/> will be in the direction of <paramref name="destinationForward"/></item>
        /// <item>The up vector of the XR Origin object will match the provided <paramref name="destinationUp"/> vector (note that the camera's Up vector can not be manipulated)</item>
        /// </list>
        /// </summary>
        /// <param name="destinationUp">The up vector that the rig's up vector will be matched to.</param>
        /// <param name="destinationForward">The forward vector that will be matched to the projection of the camera's forward vector on the plane with the normal <paramref name="destinationUp"/>.</param>
        /// <returns>Returns <see langword="true"/> if the rotation is performed. Otherwise, returns <see langword="false"/>.</returns>
        public bool MatchOriginUpCameraForward(Vector3 destinationUp, Vector3 destinationForward)
        {
            if (m_Camera != null && MatchOriginUp(destinationUp))
            {
                // Project current camera's forward vector on the destination plane, whose normal vector is destinationUp.
                var projectedCamForward = Vector3.ProjectOnPlane(m_Camera.transform.forward, destinationUp).normalized;

                // The angle that we want the XROrigin to rotate is the signed angle between projectedCamForward and destinationForward, after the up vectors are matched.
                var signedAngle = Vector3.SignedAngle(projectedCamForward, destinationForward, destinationUp);

                RotateAroundCameraPosition(destinationUp, signedAngle);

                return true;
            }

            return false;
        }

        /// <summary>
        /// This function will rotate the XR Origin object around the camera object using the <paramref name="destinationUp"/> vector such that:
        /// <list type="bullet">
        /// <item>The forward vector of the XR Origin object, which is the direction the player moves in Unity when walking forward in the physical world, will match the provided <paramref name="destinationUp"/> vector</item>
        /// <item>The up vector of the XR Origin object will match the provided <paramref name="destinationUp"/> vector</item>
        /// </list>
        /// </summary>
        /// <param name="destinationUp">The up vector that the rig's up vector will be matched to.</param>
        /// <param name="destinationForward">The forward vector that will be matched to the forward vector of the XR Origin object,
        /// which is the direction the player moves in Unity when walking forward in the physical world.</param>
        /// <returns>Returns <see langword="true"/> if the rotation is performed. Otherwise, returns <see langword="false"/>.</returns>
        public bool MatchOriginUpOriginForward(Vector3 destinationUp, Vector3 destinationForward)
        {
            if (m_OriginBaseGameObject != null && MatchOriginUp(destinationUp))
            {
                // The angle that we want the XR Origin to rotate is the signed angle between the rig's forward and destinationForward, after the up vectors are matched.
                var signedAngle = Vector3.SignedAngle(m_OriginBaseGameObject.transform.forward, destinationForward, destinationUp);

                RotateAroundCameraPosition(destinationUp, signedAngle);

                return true;
            }

            return false;
        }

        /// <summary>
        /// This function moves the camera to the world location provided by desiredWorldLocation.
        /// It does this by moving the XR Origin object so that the camera's world location matches the desiredWorldLocation
        /// </summary>
        /// <param name="desiredWorldLocation">the position in world space that the camera should be moved to</param>
        /// <returns>Returns <see langword="true"/> if the move is performed. Otherwise, returns <see langword="false"/>.</returns>
        public bool MoveCameraToWorldLocation(Vector3 desiredWorldLocation)
        {
            if (m_Camera == null)
            {
                return false;
            }

            var rot = Matrix4x4.Rotate(m_Camera.transform.rotation);
            var delta = rot.MultiplyPoint3x4(originInCameraSpacePos);
            m_OriginBaseGameObject.transform.position = delta + desiredWorldLocation;

            return true;
        }

        //Unity Callbacks

        void Awake()
        {
            if (m_CameraFloorOffsetObject == null)
            {
                Debug.LogWarning("No Camera Floor Offset Object specified for XR Rig, using attached GameObject.", this);
                m_CameraFloorOffsetObject = gameObject;
            }

            if (m_Camera == null)
            {
                if (Camera.main != null)
                    m_Camera = Camera.main;
                else
                    Debug.LogWarning("No Main Camera is found for XR Rig, please assign the Camera GameObject field manually.", this);
            }

            // This will be the parent GameObject for any trackables (such as planes) for which
            // we want a corresponding GameObject.
            trackablesParent = (new GameObject("Trackables")).transform;
            trackablesParent.SetParent(transform, false);
            trackablesParent.localPosition = Vector3.zero;
            trackablesParent.localRotation = Quaternion.identity;
            trackablesParent.localScale = Vector3.one;

            if (m_Camera)
            {
#if INCLUDE_INPUT_SYSTEM
                var trackedPoseDriver = camera.GetComponent<TrackedPoseDriver>();
                if (trackedPoseDriver == null)
                {
                    Debug.LogWarning(
                        $"Camera \"{camera.name}\" does not use a Tracked Pose Driver, " +
                        "so its transform will not be updated by an XR device.  In order for this to be " +
                        "updated, please add a Tracked Pose Driver.");
                }
#else
                    Debug.LogWarning(
                        $"Camera \"{camera.name}\" does not use a Tracked Pose Driver and com.unity.inputsystem is not installed, " +
                        "so its transform will not be updated by an XR device.  In order for this to be " +
                        "updated, please install com.unity.inputsystem and add a Tracked Pose Driver.");
#endif
            }
        }

        Pose GetCameraOriginPose()
        {
            var localOriginPose = Pose.identity;
            var parent = m_Camera.transform.parent;

            return parent
                ? parent.TransformPose(localOriginPose)
                : localOriginPose;
        }

        void OnEnable() => Application.onBeforeRender += OnBeforeRender;

        void OnDisable() => Application.onBeforeRender -= OnBeforeRender;

        void OnBeforeRender()
        {
            if (m_Camera)
            {
                var pose = GetCameraOriginPose();
                trackablesParent.position = pose.position;
                trackablesParent.rotation = pose.rotation;
            }

            if (trackablesParent.hasChanged)
            {
                trackablesParentTransformChanged?.Invoke(
                    new ARTrackablesParentTransformChangedEventArgs(this, trackablesParent));
                 trackablesParent.hasChanged = false;
            }
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void OnValidate()
        {
            if (m_OriginBaseGameObject == null)
                m_OriginBaseGameObject = gameObject;

            if (Application.isPlaying && isActiveAndEnabled)
            {
                // Respond to the mode changing by re-initializing the camera,
                // or just update the offset height in order to avoid recentering.
                if (IsModeStale())
                    TryInitializeCamera();
                else
                    MoveOffsetHeight();
            }

            bool IsModeStale()
            {
                if (s_InputSubsystems.Count > 0)
                {
                    foreach (var inputSubsystem in s_InputSubsystems)
                    {
                        // Convert from the request enum to the flags enum that is used by the subsystem
                        TrackingOriginModeFlags equivalentFlagsMode;
                        switch (m_RequestedTrackingOriginMode)
                        {
                            case TrackingOriginMode.NotSpecified:
                                // Don't need to initialize the camera since we don't set the mode when NotSpecified (we just keep the current value)
                                return false;
                            case TrackingOriginMode.Device:
                                equivalentFlagsMode = TrackingOriginModeFlags.Device;
                                break;
                            case TrackingOriginMode.Floor:
                                equivalentFlagsMode = TrackingOriginModeFlags.Floor;
                                break;
                            default:
                                Assert.IsTrue(false, $"Unhandled {nameof(TrackingOriginMode)}={m_RequestedTrackingOriginMode}");
                                return false;
                        }

                        if (inputSubsystem != null && inputSubsystem.GetTrackingOriginMode() != equivalentFlagsMode)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void Start()
        {
            TryInitializeCamera();
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void OnDestroy()
        {
            foreach (var inputSubsystem in s_InputSubsystems)
            {
                if (inputSubsystem != null)
                    inputSubsystem.trackingOriginUpdated -= OnInputSubsystemTrackingOriginUpdated;
            }
        }
    }
}
