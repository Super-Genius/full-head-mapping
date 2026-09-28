#if UNITY_EDITOR
using UnityEngine;

namespace SuperGenius.FullHeadMapping.Editor
{
    internal static class HoleFiller
    {
        public static void Dilate(Color[] colors, float[] coverage, int width, int height, int iterations)
        {
            iterations = Mathf.Max(0, iterations);
            Color[] colorTmp = new Color[colors.Length];
            float[] covTmp = new float[coverage.Length];

            for (int iter = 0; iter < iterations; ++iter)
            {
                System.Array.Copy(colors, colorTmp, colors.Length);
                System.Array.Copy(coverage, covTmp, coverage.Length);
                bool changed = false;

                for (int y = 0; y < height; ++y)
                for (int x = 0; x < width; ++x)
                {
                    int idx = y * width + x;
                    if (coverage[idx] > 1e-6f)
                        continue;

                    Color sum = Color.clear;
                    float count = 0.0f;
                    for (int oy = -1; oy <= 1; ++oy)
                    for (int ox = -1; ox <= 1; ++ox)
                    {
                        if (ox == 0 && oy == 0)
                            continue;
                        int sx = x + ox;
                        int sy = y + oy;
                        if (sx < 0 || sy < 0 || sx >= width || sy >= height)
                            continue;
                        int sidx = sy * width + sx;
                        if (coverage[sidx] <= 1e-6f)
                            continue;
                        sum += colors[sidx];
                        count += 1.0f;
                    }

                    if (count > 0.0f)
                    {
                        colorTmp[idx] = sum / count;
                        covTmp[idx] = 0.0001f;
                        changed = true;
                    }
                }

                System.Array.Copy(colorTmp, colors, colors.Length);
                System.Array.Copy(covTmp, coverage, coverage.Length);
                if (!changed)
                    break;
            }
        }
    }
}
#endif
