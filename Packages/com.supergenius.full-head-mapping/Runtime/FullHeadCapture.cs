using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperGenius.FullHeadMapping
{
    public enum HeadBlendMode
    {
        BestView = 0,
        Weighted = 1,
        Multiband = 2
    }

    [Serializable]
    public sealed class HeadCaptureView
    {
        public string label = "View";
        public Texture2D image;
        public Camera projector;
        [Range(0.0f, 4.0f)] public float weight = 1.0f;
        public bool enabled = true;
    }

    [Serializable]
    public sealed class HeadBakeSettings
    {
        [Range(256, 8192)] public int textureSize = 2048;
        [Range(0.25f, 8.0f)] public float anglePower = 2.0f;
        [Range(0.0f, 0.25f)] public float borderFade = 0.05f;
        [Range(1, 8)] public int pyramidLevels = 5;
        [Range(0, 64)] public int holeDilations = 12;
        public bool normalizeExposure = true;
        public bool usePhysicsVisibility = false;
        public LayerMask visibilityMask = ~0;
        public HeadBlendMode blendMode = HeadBlendMode.Multiband;
    }

    /// <summary>
    /// Optional geometry providers can fit or replace the target head mesh before texture baking.
    /// The core package deliberately does not depend on any particular AI or face model.
    /// </summary>
    public interface IHeadGeometryProvider
    {
        string DisplayName { get; }
        bool CanProcess(GameObject target, IReadOnlyList<HeadCaptureView> views);
        GameObject Process(GameObject target, IReadOnlyList<HeadCaptureView> views);
    }
}
