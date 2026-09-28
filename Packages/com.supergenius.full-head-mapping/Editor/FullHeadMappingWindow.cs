#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SuperGenius.FullHeadMapping.Editor
{
    public sealed class FullHeadMappingWindow : EditorWindow
    {
        private const string DefaultOutputFolder = "Assets/FullHeadMappingGenerated";

        [SerializeField] private GameObject target;
        [SerializeField] private List<HeadCaptureView> views = new List<HeadCaptureView>();
        [SerializeField] private HeadBakeSettings settings = new HeadBakeSettings();
        [SerializeField] private string outputFolder = DefaultOutputFolder;
        private Vector2 scroll;

        [MenuItem("Tools/Super-Genius/Full Head Mapping/Open Baker")]
        public static void Open()
        {
            GetWindow<FullHeadMappingWindow>("Full Head Mapping");
        }

        private void OnEnable()
        {
            if (views.Count == 0)
            {
                views.Add(new HeadCaptureView { label = "Front" });
                views.Add(new HeadCaptureView { label = "Left" });
                views.Add(new HeadCaptureView { label = "Right" });
            }
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Full Head Mapping", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Project multiple photographs onto an existing UV-mapped head. Align one Unity Camera to each photo, then bake. " +
                "Multiband mode reconstructs the historical Burt-Adelson-style seam treatment with generalized per-view confidence weights.",
                MessageType.Info);

            target = (GameObject)EditorGUILayout.ObjectField("Target Head", target, typeof(GameObject), true);
            outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);

            EditorGUILayout.Space(8);
            DrawSettings();
            EditorGUILayout.Space(8);
            DrawViews();
            EditorGUILayout.Space(8);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create Standard Projector Cameras"))
                    CreateStandardProjectors();
                if (GUILayout.Button("Use Scene View for Selected View"))
                    AssignSceneViewCamera();
            }

            EditorGUILayout.Space(8);
            using (new EditorGUI.DisabledScope(target == null || views.Count == 0))
            {
                if (GUILayout.Button("Bake Full Head Texture", GUILayout.Height(38)))
                    Bake();
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawSettings()
        {
            EditorGUILayout.LabelField("Bake Settings", EditorStyles.boldLabel);
            settings.textureSize = EditorGUILayout.IntPopup("Texture Size", settings.textureSize,
                new[] { "512", "1024", "2048", "4096", "8192" },
                new[] { 512, 1024, 2048, 4096, 8192 });
            settings.blendMode = (HeadBlendMode)EditorGUILayout.EnumPopup("Blend Mode", settings.blendMode);
            settings.anglePower = EditorGUILayout.Slider("View Angle Power", settings.anglePower, 0.25f, 8.0f);
            settings.borderFade = EditorGUILayout.Slider("Image Border Fade", settings.borderFade, 0.0f, 0.25f);
            settings.normalizeExposure = EditorGUILayout.Toggle("Normalize Exposure", settings.normalizeExposure);
            if (settings.blendMode == HeadBlendMode.Multiband)
                settings.pyramidLevels = EditorGUILayout.IntSlider("Pyramid Levels", settings.pyramidLevels, 1, 8);
            settings.holeDilations = EditorGUILayout.IntSlider("Hole Dilation", settings.holeDilations, 0, 64);
            settings.usePhysicsVisibility = EditorGUILayout.Toggle("Physics Visibility Test", settings.usePhysicsVisibility);
            if (settings.usePhysicsVisibility)
                settings.visibilityMask = LayerMaskField("Visibility Mask", settings.visibilityMask);
        }

        private void DrawViews()
        {
            EditorGUILayout.LabelField("Capture Views", EditorStyles.boldLabel);
            int remove = -1;
            for (int i = 0; i < views.Count; ++i)
            {
                HeadCaptureView view = views[i] ?? (views[i] = new HeadCaptureView());
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        view.enabled = EditorGUILayout.Toggle(view.enabled, GUILayout.Width(18));
                        view.label = EditorGUILayout.TextField(view.label);
                        if (GUILayout.Button("X", GUILayout.Width(24)))
                            remove = i;
                    }
                    view.image = (Texture2D)EditorGUILayout.ObjectField("Photo", view.image, typeof(Texture2D), false);
                    view.projector = (Camera)EditorGUILayout.ObjectField("Projector Camera", view.projector, typeof(Camera), true);
                    view.weight = EditorGUILayout.Slider("View Weight", view.weight, 0.0f, 4.0f);
                    if (GUILayout.Button("Align Camera to Scene View"))
                        AlignCameraToSceneView(view);
                }
            }
            if (remove >= 0)
                views.RemoveAt(remove);
            if (GUILayout.Button("+ Add View"))
                views.Add(new HeadCaptureView { label = "View " + (views.Count + 1) });
        }

        private void Bake()
        {
            try
            {
                FullHeadProjectionBaker.BakeResult result = FullHeadProjectionBaker.Bake(target, views, settings, outputFolder);
                Selection.activeObject = result.albedo;
                EditorGUIUtility.PingObject(result.albedo);
                Debug.Log($"Full Head Mapping complete. UV-atlas direct coverage: {result.coverage:P1}. Albedo: {result.albedoPath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("Full Head Mapping", ex.Message, "OK");
            }
        }

        private void CreateStandardProjectors()
        {
            if (target == null)
            {
                EditorUtility.DisplayDialog("Full Head Mapping", "Assign a target head first.", "OK");
                return;
            }

            Renderer renderer = target.GetComponentInChildren<Renderer>();
            Bounds bounds = renderer != null ? renderer.bounds : new Bounds(target.transform.position, Vector3.one * 2.0f);
            Vector3 center = bounds.center;
            float radius = Mathf.Max(bounds.extents.x, Mathf.Max(bounds.extents.y, bounds.extents.z));
            float distance = Mathf.Max(radius * 3.0f, 1.0f);

            string[] labels = { "Front", "Left", "Right", "Back" };
            Vector3[] offsets =
            {
                new Vector3(0, 0, -distance),
                new Vector3(-distance, 0, 0),
                new Vector3(distance, 0, 0),
                new Vector3(0, 0, distance)
            };

            while (views.Count < labels.Length)
                views.Add(new HeadCaptureView());

            for (int i = 0; i < labels.Length; ++i)
            {
                GameObject go = new GameObject("FHM Projector - " + labels[i]);
                Undo.RegisterCreatedObjectUndo(go, "Create Full Head Projector");
                Camera cam = go.AddComponent<Camera>();
                cam.enabled = false;
                cam.fieldOfView = 38.0f;
                go.transform.position = center + offsets[i];
                go.transform.LookAt(center);
                views[i].label = labels[i];
                views[i].projector = cam;
            }
        }

        private void AssignSceneViewCamera()
        {
            if (views.Count == 0)
                return;
            HeadCaptureView selected = views.Find(v => v != null && v.enabled && v.projector != null) ?? views[0];
            AlignCameraToSceneView(selected);
        }

        private static void AlignCameraToSceneView(HeadCaptureView view)
        {
            SceneView scene = SceneView.lastActiveSceneView;
            if (scene == null || scene.camera == null)
                return;
            if (view.projector == null)
            {
                GameObject go = new GameObject("FHM Projector - " + view.label);
                Undo.RegisterCreatedObjectUndo(go, "Create Full Head Projector");
                view.projector = go.AddComponent<Camera>();
                view.projector.enabled = false;
            }
            Undo.RecordObject(view.projector.transform, "Align Full Head Projector");
            view.projector.transform.SetPositionAndRotation(scene.camera.transform.position, scene.camera.transform.rotation);
            view.projector.fieldOfView = scene.camera.fieldOfView;
        }

        private static int LayerMaskField(string label, LayerMask mask)
        {
            string[] layers = UnityEditorInternal.InternalEditorUtility.layers;
            int layerMask = 0;
            for (int i = 0; i < layers.Length; ++i)
            {
                int layer = LayerMask.NameToLayer(layers[i]);
                if ((mask.value & (1 << layer)) != 0)
                    layerMask |= 1 << i;
            }
            layerMask = EditorGUILayout.MaskField(label, layerMask, layers);
            int result = 0;
            for (int i = 0; i < layers.Length; ++i)
            {
                if ((layerMask & (1 << i)) != 0)
                    result |= 1 << LayerMask.NameToLayer(layers[i]);
            }
            return result;
        }
    }
}
#endif
