#if UNITY_EDITOR
using UnityEngine;

namespace SuperGenius.FullHeadMapping.Editor
{
    internal static class ProjectionMath
    {
        public static bool Barycentric(Vector2 p, Vector2 a, Vector2 b, Vector2 c, out Vector3 bary)
        {
            Vector2 v0 = b - a;
            Vector2 v1 = c - a;
            Vector2 v2 = p - a;
            float d00 = Vector2.Dot(v0, v0);
            float d01 = Vector2.Dot(v0, v1);
            float d11 = Vector2.Dot(v1, v1);
            float d20 = Vector2.Dot(v2, v0);
            float d21 = Vector2.Dot(v2, v1);
            float denom = d00 * d11 - d01 * d01;
            if (Mathf.Abs(denom) < 1e-12f)
            {
                bary = default;
                return false;
            }

            float v = (d11 * d20 - d01 * d21) / denom;
            float w = (d00 * d21 - d01 * d20) / denom;
            float u = 1.0f - v - w;
            bary = new Vector3(u, v, w);
            const float epsilon = -1e-5f;
            return u >= epsilon && v >= epsilon && w >= epsilon;
        }

        public static float BorderWeight(Vector2 viewport, float fade)
        {
            if (fade <= 0.0f)
                return 1.0f;

            float edge = Mathf.Min(Mathf.Min(viewport.x, 1.0f - viewport.x),
                                   Mathf.Min(viewport.y, 1.0f - viewport.y));
            return Mathf.SmoothStep(0.0f, 1.0f, Mathf.Clamp01(edge / fade));
        }

        public static bool IsOccluded(Camera camera, Vector3 positionWS, LayerMask mask)
        {
            Vector3 origin = camera.transform.position;
            Vector3 toPoint = positionWS - origin;
            float distance = toPoint.magnitude;
            if (distance <= 1e-5f)
                return false;

            Ray ray = new Ray(origin, toPoint / distance);
            // Bias the requested distance so the surface itself does not count as an occluder.
            return Physics.Raycast(ray, distance - 1e-3f, mask, QueryTriggerInteraction.Ignore);
        }
    }
}
#endif
