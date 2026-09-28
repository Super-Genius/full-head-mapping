#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace SuperGenius.FullHeadMapping.Editor
{
    internal static class MultibandBlender
    {
        private sealed class ColorLevel
        {
            public int width;
            public int height;
            public Color[] data;
        }

        private sealed class FloatLevel
        {
            public int width;
            public int height;
            public float[] data;
        }

        public static Color[] Blend(IReadOnlyList<Color[]> images, IReadOnlyList<float[]> masks, int width, int height, int levels)
        {
            if (images == null || masks == null || images.Count == 0 || images.Count != masks.Count)
                return new Color[width * height];

            levels = Mathf.Clamp(levels, 1, 8);
            var lapPyramids = new List<List<ColorLevel>>(images.Count);
            var maskPyramids = new List<List<FloatLevel>>(masks.Count);

            for (int i = 0; i < images.Count; ++i)
            {
                lapPyramids.Add(BuildLaplacian(images[i], width, height, levels));
                maskPyramids.Add(BuildGaussian(masks[i], width, height, levels));
            }

            int actualLevels = lapPyramids[0].Count;
            var blended = new List<ColorLevel>(actualLevels);
            for (int level = 0; level < actualLevels; ++level)
            {
                int w = lapPyramids[0][level].width;
                int h = lapPyramids[0][level].height;
                Color[] data = new Color[w * h];
                for (int p = 0; p < data.Length; ++p)
                {
                    Color sum = Color.clear;
                    float total = 0.0f;
                    for (int i = 0; i < images.Count; ++i)
                    {
                        float m = Mathf.Max(0.0f, maskPyramids[i][level].data[p]);
                        sum += lapPyramids[i][level].data[p] * m;
                        total += m;
                    }
                    data[p] = total > 1e-7f ? sum / total : Color.clear;
                }
                blended.Add(new ColorLevel { width = w, height = h, data = data });
            }

            ColorLevel current = blended[blended.Count - 1];
            for (int level = blended.Count - 2; level >= 0; --level)
            {
                ColorLevel target = blended[level];
                Color[] up = TextureArrayOps.ResizeBilinear(current.data, current.width, current.height, target.width, target.height);
                Color[] sum = new Color[target.data.Length];
                for (int p = 0; p < sum.Length; ++p)
                    sum[p] = target.data[p] + up[p];
                current = new ColorLevel { width = target.width, height = target.height, data = sum };
            }
            return current.data;
        }

        private static List<ColorLevel> BuildLaplacian(Color[] source, int width, int height, int levels)
        {
            var gaussian = new List<ColorLevel>();
            gaussian.Add(new ColorLevel { width = width, height = height, data = source });
            for (int level = 1; level < levels; ++level)
            {
                ColorLevel prev = gaussian[level - 1];
                if (prev.width <= 2 || prev.height <= 2)
                    break;
                Color[] down = TextureArrayOps.Downsample(prev.data, prev.width, prev.height, out int w, out int h);
                gaussian.Add(new ColorLevel { width = w, height = h, data = down });
            }

            var lap = new List<ColorLevel>();
            for (int level = 0; level < gaussian.Count - 1; ++level)
            {
                ColorLevel g = gaussian[level];
                ColorLevel next = gaussian[level + 1];
                Color[] up = TextureArrayOps.ResizeBilinear(next.data, next.width, next.height, g.width, g.height);
                Color[] detail = new Color[g.data.Length];
                for (int p = 0; p < detail.Length; ++p)
                    detail[p] = g.data[p] - up[p];
                lap.Add(new ColorLevel { width = g.width, height = g.height, data = detail });
            }
            lap.Add(gaussian[gaussian.Count - 1]);
            return lap;
        }

        private static List<FloatLevel> BuildGaussian(float[] source, int width, int height, int levels)
        {
            var result = new List<FloatLevel>();
            result.Add(new FloatLevel { width = width, height = height, data = source });
            for (int level = 1; level < levels; ++level)
            {
                FloatLevel prev = result[level - 1];
                if (prev.width <= 2 || prev.height <= 2)
                    break;
                float[] down = TextureArrayOps.Downsample(prev.data, prev.width, prev.height, out int w, out int h);
                result.Add(new FloatLevel { width = w, height = h, data = down });
            }
            return result;
        }
    }
}
#endif
