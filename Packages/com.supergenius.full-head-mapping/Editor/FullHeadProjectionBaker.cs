#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SuperGenius.FullHeadMapping.Editor
{
    internal static class FullHeadProjectionBaker
    {
        public sealed class BakeResult
        {
            public Texture2D albedo;
            public Texture2D confidence;
            public Texture2D provenance;
            public float coverage;
            public string albedoPath;
        }

        private sealed class MeshSnapshot : IDisposable
        {
            public Mesh mesh;
            public Transform transform;
            public bool ownsMesh;

            public void Dispose()
            {
                if (ownsMesh && mesh != null)
                    Object.DestroyImmediate(mesh);
            }
        }

        public static BakeResult Bake(GameObject target, IReadOnlyList<HeadCaptureView> inputViews, HeadBakeSettings settings, string outputFolder)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (inputViews == null || inputViews.Count == 0)
                throw new InvalidOperationException("Add at least one capture view.");

            List<HeadCaptureView> views = new List<HeadCaptureView>();
            foreach (HeadCaptureView view in inputViews)
            {
                if (view != null && view.enabled && view.image != null && view.projector != null && view.weight > 0.0f)
                    views.Add(view);
            }
            if (views.Count == 0)
                throw new InvalidOperationException("No enabled capture view has both an image and a projector camera.");

            using MeshSnapshot snapshot = GetMesh(target);
            Mesh mesh = snapshot.mesh;
            if (mesh == null)
                throw new InvalidOperationException("Target needs a MeshFilter/MeshRenderer or SkinnedMeshRenderer.");
            if (mesh.uv == null || mesh.uv.Length != mesh.vertexCount)
                throw new InvalidOperationException("Target mesh must have a destination UV0 atlas.");

            if (mesh.normals == null || mesh.normals.Length != mesh.vertexCount)
                mesh.RecalculateNormals();

            int size = Mathf.Clamp(settings.textureSize, 256, 8192);
            int pixelCount = size * size;
            var readableSources = new List<Texture2D>();
            var sourceColors = new List<Color[]>(views.Count);
            var sourceWeights = new List<float[]>(views.Count);
            for (int i = 0; i < views.Count; ++i)
            {
                readableSources.Add(MakeReadableCopy(views[i].image));
                sourceColors.Add(new Color[pixelCount]);
                sourceWeights.Add(new float[pixelCount]);
            }

            float[] totalWeight = new float[pixelCount];
            float[] bestWeight = new float[pixelCount];
            int[] bestSource = new int[pixelCount];
            for (int i = 0; i < bestSource.Length; ++i)
                bestSource[i] = -1;

            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            Vector2[] uvs = mesh.uv;
            int[] triangles = mesh.triangles;
            Matrix4x4 localToWorld = snapshot.transform.localToWorldMatrix;
            Matrix4x4 normalMatrix = localToWorld.inverse.transpose;

            try
            {
                int triangleCount = triangles.Length / 3;
                for (int tri = 0; tri < triangleCount; ++tri)
                {
                    if (tri % 100 == 0)
                        EditorUtility.DisplayProgressBar("Full Head Mapping", $"Projecting triangle {tri + 1}/{triangleCount}", tri / (float)Mathf.Max(1, triangleCount));

                    int i0 = triangles[tri * 3 + 0];
                    int i1 = triangles[tri * 3 + 1];
                    int i2 = triangles[tri * 3 + 2];
                    Vector2 uv0 = uvs[i0];
                    Vector2 uv1 = uvs[i1];
                    Vector2 uv2 = uvs[i2];

                    int minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(uv0.x, Mathf.Min(uv1.x, uv2.x)) * size), 0, size - 1);
                    int maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(uv0.x, Mathf.Max(uv1.x, uv2.x)) * size), 0, size - 1);
                    int minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(uv0.y, Mathf.Min(uv1.y, uv2.y)) * size), 0, size - 1);
                    int maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(uv0.y, Mathf.Max(uv1.y, uv2.y)) * size), 0, size - 1);

                    Vector3 p0 = localToWorld.MultiplyPoint3x4(vertices[i0]);
                    Vector3 p1 = localToWorld.MultiplyPoint3x4(vertices[i1]);
                    Vector3 p2 = localToWorld.MultiplyPoint3x4(vertices[i2]);
                    Vector3 n0 = normalMatrix.MultiplyVector(normals[i0]).normalized;
                    Vector3 n1 = normalMatrix.MultiplyVector(normals[i1]).normalized;
                    Vector3 n2 = normalMatrix.MultiplyVector(normals[i2]).normalized;

                    for (int y = minY; y <= maxY; ++y)
                    for (int x = minX; x <= maxX; ++x)
                    {
                        Vector2 uv = new Vector2((x + 0.5f) / size, (y + 0.5f) / size);
                        if (!ProjectionMath.Barycentric(uv, uv0, uv1, uv2, out Vector3 b))
                            continue;

                        Vector3 positionWS = p0 * b.x + p1 * b.y + p2 * b.z;
                        Vector3 normalWS = (n0 * b.x + n1 * b.y + n2 * b.z).normalized;
                        int pixel = y * size + x;

                        for (int viewIndex = 0; viewIndex < views.Count; ++viewIndex)
                        {
                            HeadCaptureView view = views[viewIndex];
                            Camera cam = view.projector;
                            Vector3 viewport3 = cam.WorldToViewportPoint(positionWS);
                            if (viewport3.z <= 0.0f || viewport3.x < 0.0f || viewport3.x > 1.0f || viewport3.y < 0.0f || viewport3.y > 1.0f)
                                continue;

                            Vector3 toCamera = (cam.transform.position - positionWS).normalized;
                            float facing = Mathf.Max(0.0f, Vector3.Dot(normalWS, toCamera));
                            if (facing <= 1e-5f)
                                continue;

                            if (settings.usePhysicsVisibility && ProjectionMath.IsOccluded(cam, positionWS, settings.visibilityMask))
                                continue;

                            Vector2 viewport = new Vector2(viewport3.x, viewport3.y);
                            float border = ProjectionMath.BorderWeight(viewport, settings.borderFade);
                            float weight = view.weight * Mathf.Pow(facing, settings.anglePower) * border;
                            if (weight <= 1e-6f)
                                continue;

                            Color color = readableSources[viewIndex].GetPixelBilinear(viewport.x, viewport.y);
                            sourceColors[viewIndex][pixel] = color;
                            sourceWeights[viewIndex][pixel] += weight;
                            totalWeight[pixel] += weight;
                            if (weight > bestWeight[pixel])
                            {
                                bestWeight[pixel] = weight;
                                bestSource[pixel] = viewIndex;
                            }
                        }
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                foreach (Texture2D source in readableSources)
                    Object.DestroyImmediate(source);
            }

            if (settings.normalizeExposure)
                ExposureNormalizer.Normalize(sourceColors, sourceWeights);

            Color[] output = settings.blendMode switch
            {
                HeadBlendMode.BestView => BlendBest(sourceColors, bestSource, pixelCount),
                HeadBlendMode.Weighted => BlendWeighted(sourceColors, sourceWeights, pixelCount),
                _ => MultibandBlender.Blend(sourceColors, sourceWeights, size, size, settings.pyramidLevels)
            };

            float[] coverageMask = new float[pixelCount];
            int covered = 0;
            for (int p = 0; p < pixelCount; ++p)
            {
                if (totalWeight[p] > 1e-6f)
                {
                    coverageMask[p] = 1.0f;
                    covered++;
                }
            }
            HoleFiller.Dilate(output, coverageMask, size, size, settings.holeDilations);

            Color[] confidence = new Color[pixelCount];
            Color[] provenance = new Color[pixelCount];
            for (int p = 0; p < pixelCount; ++p)
            {
                float c = 1.0f - Mathf.Exp(-totalWeight[p]);
                confidence[p] = new Color(c, c, c, 1.0f);
                provenance[p] = bestSource[p] < 0
                    ? new Color(0.0f, 0.0f, 0.0f, 1.0f)
                    : Color.HSVToRGB((bestSource[p] * 0.61803398875f) % 1.0f, 0.75f, Mathf.Lerp(0.45f, 1.0f, c));
                output[p].a = coverageMask[p] > 0.0f ? 1.0f : 0.0f;
            }

            EnsureAssetFolder(outputFolder);
            string albedoPath = outputFolder.TrimEnd('/') + "/FullHead_Albedo.png";
            string confidencePath = outputFolder.TrimEnd('/') + "/FullHead_Confidence.png";
            string provenancePath = outputFolder.TrimEnd('/') + "/FullHead_Provenance.png";

            Texture2D albedo = WriteTexture(output, size, albedoPath, true);
            Texture2D conf = WriteTexture(confidence, size, confidencePath, false);
            Texture2D prov = WriteTexture(provenance, size, provenancePath, false);

            return new BakeResult
            {
                albedo = albedo,
                confidence = conf,
                provenance = prov,
                coverage = covered / (float)pixelCount,
                albedoPath = albedoPath
            };
        }

        private static Color[] BlendBest(IReadOnlyList<Color[]> colors, int[] bestSource, int pixelCount)
        {
            Color[] output = new Color[pixelCount];
            for (int p = 0; p < pixelCount; ++p)
            {
                int s = bestSource[p];
                output[p] = s >= 0 ? colors[s][p] : Color.clear;
            }
            return output;
        }

        private static Color[] BlendWeighted(IReadOnlyList<Color[]> colors, IReadOnlyList<float[]> weights, int pixelCount)
        {
            Color[] output = new Color[pixelCount];
            for (int p = 0; p < pixelCount; ++p)
            {
                Color sum = Color.clear;
                float total = 0.0f;
                for (int s = 0; s < colors.Count; ++s)
                {
                    float w = weights[s][p];
                    sum += colors[s][p] * w;
                    total += w;
                }
                output[p] = total > 1e-7f ? sum / total : Color.clear;
            }
            return output;
        }

        private static MeshSnapshot GetMesh(GameObject target)
        {
            SkinnedMeshRenderer skinned = target.GetComponentInChildren<SkinnedMeshRenderer>();
            if (skinned != null)
            {
                Mesh baked = new Mesh { name = skinned.sharedMesh != null ? skinned.sharedMesh.name + "_BakedForProjection" : "BakedHead" };
                skinned.BakeMesh(baked);
                return new MeshSnapshot { mesh = baked, transform = skinned.transform, ownsMesh = true };
            }

            MeshFilter filter = target.GetComponentInChildren<MeshFilter>();
            if (filter != null)
                return new MeshSnapshot { mesh = Object.Instantiate(filter.sharedMesh), transform = filter.transform, ownsMesh = true };

            return new MeshSnapshot();
        }

        private static Texture2D MakeReadableCopy(Texture2D source)
        {
            RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(source, rt);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, false);
            copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            copy.Apply(false, false);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            return copy;
        }

        private static Texture2D WriteTexture(Color[] pixels, int size, string assetPath, bool sRGB)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false, !sRGB);
            tex.SetPixels(pixels);
            tex.Apply(false, false);
            File.WriteAllBytes(assetPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            if (AssetImporter.GetAtPath(assetPath) is TextureImporter importer)
            {
                importer.sRGBTexture = sRGB;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        private static void EnsureAssetFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder) || !folder.StartsWith("Assets", StringComparison.Ordinal))
                throw new ArgumentException("Output folder must be an Assets/... path.");
            if (AssetDatabase.IsValidFolder(folder))
                return;

            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; ++i)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
