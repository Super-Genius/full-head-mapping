#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace SuperGenius.FullHeadMapping.Editor
{
    internal static class ExposureNormalizer
    {
        public static void Normalize(IReadOnlyList<Color[]> images, IReadOnlyList<float[]> masks)
        {
            if (images.Count < 2)
                return;

            // Use the first view as the exposure anchor. Estimate scalar gain from overlap only.
            Color[] reference = images[0];
            float[] referenceMask = masks[0];
            for (int i = 1; i < images.Count; ++i)
            {
                double refLum = 0.0;
                double srcLum = 0.0;
                double total = 0.0;
                Color[] source = images[i];
                float[] sourceMask = masks[i];

                for (int p = 0; p < source.Length; ++p)
                {
                    float w = Mathf.Min(referenceMask[p], sourceMask[p]);
                    if (w <= 1e-4f)
                        continue;
                    refLum += Luminance(reference[p]) * w;
                    srcLum += Luminance(source[p]) * w;
                    total += w;
                }

                if (total <= 1e-5 || srcLum <= 1e-6)
                    continue;

                float gain = Mathf.Clamp((float)(refLum / srcLum), 0.5f, 2.0f);
                for (int p = 0; p < source.Length; ++p)
                {
                    Color c = source[p];
                    c.r *= gain;
                    c.g *= gain;
                    c.b *= gain;
                    source[p] = c;
                }
            }
        }

        private static float Luminance(Color c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
    }
}
#endif
