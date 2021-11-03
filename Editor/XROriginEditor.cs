using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;
using UnityEditor;

namespace Unity.XR.CoreUtils.Editor
{
    /// <summary>
    /// Custom editor for an <see cref="XROrigin"/>.
    /// </summary>
    [CustomEditor(typeof(XROrigin), true), CanEditMultipleObjects]
    public class XROriginEditor : UnityEditor.Editor
    {
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XROrigin.origin"/>.</summary>
        protected SerializedProperty m_OriginBaseGameObject;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XROrigin.cameraFloorOffsetObject"/>.</summary>
        protected SerializedProperty m_CameraFloorOffsetObject;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XROrigin.camera"/>.</summary>
        protected SerializedProperty m_Camera;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XROrigin.currentTrackingOriginMode"/>.</summary>
        [Obsolete("m_TrackingOriginMode has been deprecated. Use m_RequestedTrackingOriginMode instead.")]
        protected SerializedProperty m_TrackingOriginMode;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XROrigin.requestedTrackingOriginMode"/>.</summary>
        protected SerializedProperty m_RequestedTrackingOriginMode;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XROrigin.cameraYOffset"/>.</summary>
        protected SerializedProperty m_CameraYOffset;

        List<XROrigin> m_Origins;

        readonly GUIContent[] m_MixedValuesOptions = { Contents.mixedValues };

        /// <summary>
        /// Contents of GUI elements used by this editor.
        /// </summary>
        protected static class Contents
        {
            /// <summary><see cref="GUIContent"/> for <see cref="XROrigin.origin"/>.</summary>
            public static readonly GUIContent origin = EditorGUIUtility.TrTextContent("Origin Base GameObject", "The \"Origin\" GameObject is used to refer to the base of the XR Origin, by default it is this GameObject. This is the GameObject that will be manipulated via locomotion.");
            /// <summary><see cref="GUIContent"/> for <see cref="XROrigin.cameraFloorOffsetObject"/>.</summary>
            public static readonly GUIContent cameraFloorOffsetObject = EditorGUIUtility.TrTextContent("Camera Floor Offset Object", "The GameObject to move to desired height off the floor (defaults to this object if none provided).");
            /// <summary><see cref="GUIContent"/> for <see cref="XROrigin.camera"/>.</summary>
            public static readonly GUIContent camera = EditorGUIUtility.TrTextContent("Camera GameObject", "The GameObject that contains the camera, this is usually the \"Head\" of XR Origins.");
            /// <summary><see cref="GUIContent"/> for <see cref="XROrigin.requestedTrackingOriginMode"/>.</summary>
            public static readonly GUIContent trackingOriginMode = EditorGUIUtility.TrTextContent("Tracking Origin Mode", "The type of tracking origin to use for this Origin. Tracking origins identify where (0, 0, 0) is in the world of tracking.");
            /// <summary><see cref="GUIContent"/> for <see cref="XROrigin.currentTrackingOriginMode"/>.</summary>
            public static readonly GUIContent currentTrackingOriginMode = EditorGUIUtility.TrTextContent("Current Tracking Origin Mode", "The Tracking Origin Mode that this Origin is in.");
            /// <summary><see cref="GUIContent"/> for <see cref="XROrigin.cameraYOffset"/>.</summary>
            public static readonly GUIContent cameraYOffset = EditorGUIUtility.TrTextContent("Camera Y Offset", "Camera height to be used when in \"Device\" Tracking Origin Mode to define the height of the user from the floor.");
            /// <summary><see cref="GUIContent"/> to indicate mixed values when multi-object editing.</summary>
            public static readonly GUIContent mixedValues = EditorGUIUtility.TrTextContent("\u2014", "Mixed Values");
        }

        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        /// <seealso cref="MonoBehaviour"/>
        protected virtual void OnEnable()
        {
            m_OriginBaseGameObject = serializedObject.FindProperty("m_OriginBaseGameObject");
            m_CameraFloorOffsetObject = serializedObject.FindProperty("m_CameraFloorOffsetObject");
            m_Camera = serializedObject.FindProperty("m_Camera");
#pragma warning disable 618 // Setting deprecated field to help with backwards compatibility with existing user code.
            m_TrackingOriginMode = serializedObject.FindProperty("m_TrackingOriginMode");
#pragma warning restore 618
            m_RequestedTrackingOriginMode = serializedObject.FindProperty("m_RequestedTrackingOriginMode");
            m_CameraYOffset = serializedObject.FindProperty("m_CameraYOffset");

            m_Origins = targets.Cast<XROrigin>().ToList();
        }

        /// <inheritdoc />
        /// <seealso cref="DrawBeforeProperties"/>
        /// <seealso cref="DrawProperties"/>
        protected void DrawInspector()
        {
            DrawBeforeProperties();
            DrawProperties();
        }

        /// <summary>
        /// Draw the standard read-only Script property.
        /// </summary>
        protected virtual void DrawScript()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                if (target is MonoBehaviour behaviour)
                    EditorGUILayout.ObjectField(EditorGUIUtility.TrTempContent("Script"), MonoScript.FromMonoBehaviour(behaviour), typeof(MonoBehaviour), false);
                else if (target is ScriptableObject scriptableObject)
                    EditorGUILayout.ObjectField(EditorGUIUtility.TrTempContent("Script"), MonoScript.FromScriptableObject(scriptableObject), typeof(ScriptableObject), false);
            }
        }

        /// <summary>
        /// This method is automatically called by <see cref="DrawInspector"/> to
        /// draw the section of the custom inspector before <see cref="DrawProperties"/>.
        /// By default, this draws the read-only Script property.
        /// </summary>
        protected virtual void DrawBeforeProperties()
        {
            DrawScript();
        }

        /// <summary>
        /// This method is automatically called by <see cref="DrawInspector"/> to
        /// draw the property fields. Override this method to customize the
        /// properties shown in the Inspector. This is typically the method overridden
        /// when a derived behavior adds additional serialized properties
        /// that should be displayed in the Inspector.
        /// </summary>
        protected virtual void DrawProperties()
        {
            EditorGUILayout.PropertyField(m_OriginBaseGameObject, Contents.origin);
            EditorGUILayout.PropertyField(m_CameraFloorOffsetObject, Contents.cameraFloorOffsetObject);
            EditorGUILayout.PropertyField(m_Camera, Contents.camera);

            EditorGUILayout.PropertyField(m_RequestedTrackingOriginMode, Contents.trackingOriginMode);

            var showCameraYOffset =
                m_RequestedTrackingOriginMode.enumValueIndex == (int)XROrigin.TrackingOriginMode.NotSpecified ||
                m_RequestedTrackingOriginMode.enumValueIndex == (int)XROrigin.TrackingOriginMode.Device ||
                m_RequestedTrackingOriginMode.hasMultipleDifferentValues;
            if (showCameraYOffset)
            {
                // The property should be enabled when not playing since the default for the XR device
                // may be Device, so the property should be editable to define the offset.
                // When playing, disable the property to convey that it isn't having an effect,
                // which is when the current mode is Floor.
                var currentTrackingOriginMode = ((XROrigin)target).currentTrackingOriginMode;
                var allCurrentlyFloor = (m_Origins.Count == 1 && currentTrackingOriginMode == TrackingOriginModeFlags.Floor) ||
                    m_Origins.All(origin => origin.currentTrackingOriginMode == TrackingOriginModeFlags.Floor);
                var disabled = Application.isPlaying &&
                    !m_RequestedTrackingOriginMode.hasMultipleDifferentValues &&
                    m_RequestedTrackingOriginMode.enumValueIndex == (int)XROrigin.TrackingOriginMode.NotSpecified &&
                    allCurrentlyFloor;
                using (new EditorGUI.IndentLevelScope())
                using (new EditorGUI.DisabledScope(disabled))
                {
                    EditorGUILayout.PropertyField(m_CameraYOffset, Contents.cameraYOffset);
                }
            }

            DrawCurrentTrackingOriginMode();
        }

        /// <summary>
        /// Draw the current Tracking Origin Mode while the application is playing.
        /// </summary>
        /// <seealso cref="XROrigin.currentTrackingOriginMode"/>
        protected void DrawCurrentTrackingOriginMode()
        {
            if (!Application.isPlaying)
                return;

            using (new EditorGUI.DisabledScope(true))
            {
                var currentTrackingOriginMode = ((XROrigin)target).currentTrackingOriginMode;
                if (m_Origins.Count == 1 || m_Origins.All(origin => origin.currentTrackingOriginMode == currentTrackingOriginMode))
                    EditorGUILayout.EnumPopup(Contents.currentTrackingOriginMode, currentTrackingOriginMode);
                else
                    EditorGUILayout.Popup(Contents.currentTrackingOriginMode, 0, m_MixedValuesOptions);
            }
        }
    }
}
