#if UNITY_EDITOR
using UnityEngine;

namespace SuperGenius.FullHeadMapping.Editor
{
    internal static class TextureArrayOps
    {
        public static Color[] Blur5(Color[] src, int width, int height)
        {
            Color[] tmp = new Color[src.Length];
            Color[] dst = new Color[src.Length];
            int[] k = { 1, 4, 6, 4, 1 };

            for (int y = 0; y < height; ++y)
            for (int x = 0; x < width; ++x)
            {
                Color sum = Color.clear;
                float norm = 0.0f;
                for (int i = -2; i <= 2; ++i)
                {
                    int sx = Mathf.Clamp(x + i, 0, width - 1);
                    float w = k[i + 2];
                    sum += src[y * width + sx] * w;
                    norm += w;
                }
                tmp[y * width + x] = sum / norm;
            }

            for (int y = 0; y < height; ++y)
            for (int x = 0; x < width; ++x)
            {
                Color sum = Color.clear;
                float norm = 0.0f;
                for (int i = -2; i <= 2; ++i)
                {
                    int sy = Mathf.Clamp(y + i, 0, height - 1);
                    float w = k[i + 2];
                    sum += tmp[sy * width + x] * w;
                    norm += w;
                }
                dst[y * width + x] = sum / norm;
            }
            return dst;
        }

        public static float[] Blur5(float[] src, int width, int height)
        {
            float[] tmp = new float[src.Length];
            float[] dst = new float[src.Length];
            int[] k = { 1, 4, 6, 4, 1 };

            for (int y = 0; y < height; ++y)
            for (int x = 0; x < width; ++x)
            {
                float sum = 0.0f, norm = 0.0f;
                for (int i = -2; i <= 2; ++i)
                {
                    int sx = Mathf.Clamp(x + i, 0, width - 1);
                    float w = k[i + 2];
                    sum += src[y * width + sx] * w;
                    norm += w;
                }
                tmp[y * width + x] = sum / norm;
            }

            for (int y = 0; y < height; ++y)
            for (int x = 0; x < width; ++x)
            {
                float sum = 0.0f, norm = 0.0f;
                for (int i = -2; i <= 2; ++i)
                {
                    int sy = Mathf.Clamp(y + i, 0, height - 1);
                    float w = k[i + 2];
                    sum += tmp[sy * width + x] * w;
                    norm += w;
                }
                dst[y * width + x] = sum / norm;
            }
            return dst;
        }

        public static Color[] Downsample(Color[] src, int width, int height, out int outWidth, out int outHeight)
        {
            Color[] blurred = Blur5(src, width, height);
            outWidth = Mathf.Max(1, width / 2);
            outHeight = Mathf.Max(1, height / 2);
            Color[] dst = new Color[outWidth * outHeight];
            for (int y = 0; y < outHeight; ++y)
            for (int x = 0; x < outWidth; ++x)
                dst[y * outWidth + x] = blurred[Mathf.Min(y * 2, height - 1) * width + Mathf.Min(x * 2, width - 1)];
            return dst;
        }

        public static float[] Downsample(float[] src, int width, int height, out int outWidth, out int outHeight)
        {
            float[] blurred = Blur5(src, width, height);
            outWidth = Mathf.Max(1, width / 2);
            outHeight = Mathf.Max(1, height / 2);
            float[] dst = new float[outWidth * outHeight];
            for (int y = 0; y < outHeight; ++y)
            for (int x = 0; x < outWidth; ++x)
                dst[y * outWidth + x] = blurred[Mathf.Min(y * 2, height - 1) * width + Mathf.Min(x * 2, width - 1)];
            return dst;
        }

        public static Color[] ResizeBilinear(Color[] src, int srcW, int srcH, int dstW, int dstH)
        {
            Color[] dst = new Color[dstW * dstH];
            for (int y = 0; y < dstH; ++y)
            {
                float fy = dstH == 1 ? 0.0f : y * (srcH - 1.0f) / (dstH - 1.0f);
                int y0 = Mathf.FloorToInt(fy);
                int y1 = Mathf.Min(y0 + 1, srcH - 1);
                float ty = fy - y0;
                for (int x = 0; x < dstW; ++x)
                {
                    float fx = dstW == 1 ? 0.0f : x * (srcW - 1.0f) / (dstW - 1.0f);
                    int x0 = Mathf.FloorToInt(fx);
                    int x1 = Mathf.Min(x0 + 1, srcW - 1);
                    float tx = fx - x0;
                    Color a = Color.Lerp(src[y0 * srcW + x0], src[y0 * srcW + x1], tx);
                    Color b = Color.Lerp(src[y1 * srcW + x0], src[y1 * srcW + x1], tx);
                    dst[y * dstW + x] = Color.Lerp(a, b, ty);
                }
            }
            return dst;
        }
    }
}
#endif
