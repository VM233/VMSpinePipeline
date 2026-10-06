using System;
using System.Collections.Generic;
using Spine;
using UnityEngine;

namespace VMSpinePipeline
{
    /// <summary>Expands a bone-driven mesh to its full Sprite UV rectangle without modifying authored vertices.</summary>
    public static class SpineMeshRectangle
    {
        public const int OriginalVertexOffset = 4;

        public static MeshAttachment Expand(MeshAttachment source, Slot setupSlot)
        {
            Admit(source, setupSlot);
            int vertexCount = source.WorldVerticesLength / 2;
            int hullCount = source.HullLength / 2;
            var points = new Vector2[vertexCount + OriginalVertexOffset];
            points[0] = Vector2.zero;
            points[1] = Vector2.right;
            points[2] = Vector2.one;
            points[3] = Vector2.up;
            for (int i = 0; i < vertexCount; i++)
                points[i + OriginalVertexOffset] = new Vector2(source.RegionUVs[i * 2], source.RegionUVs[i * 2 + 1]);

            var boundary = new List<int>(hullCount);
            for (int i = 0; i < hullCount; i++) boundary.Add(i + OriginalVertexOffset);
            if (SignedArea(points, boundary) < 0) boundary.Reverse();
            var triangles = new List<int>(source.Triangles.Length + 3 * (hullCount + 8));
            foreach (int index in source.Triangles) triangles.Add(index + OriginalVertexOffset);
            double winding = Math.Sign(Cross(points[triangles[0]], points[triangles[1]], points[triangles[2]]));
            List<int> hull = ConvexHull(points, boundary);

            var triangulator = new Triangulator();
            for (int i = 0; i < hull.Count; i++)
            {
                int first = boundary.IndexOf(hull[i]);
                int last = boundary.IndexOf(hull[(i + 1) % hull.Count]);
                var pocket = new List<int> { boundary[first] };
                for (int j = (first + 1) % boundary.Count; j != last; j = (j + 1) % boundary.Count)
                    pocket.Add(boundary[j]);
                pocket.Add(boundary[last]);
                if (pocket.Count == 2 || SignedArea(points, pocket) == 0) continue;
                // A concave boundary pocket has clockwise winding, as required by Spine's native triangulator.
                var polygon = new ExposedList<float>();
                foreach (int index in pocket) { polygon.Add(points[index].x); polygon.Add(points[index].y); }
                ExposedList<int> fill = triangulator.Triangulate(polygon);
                for (int j = 0; j < fill.Count; j += 3)
                    AddTriangle(points, triangles, pocket[fill.Items[j]], pocket[fill.Items[j + 1]],
                        pocket[fill.Items[j + 2]], winding);
            }
            for (int corner = 0; corner < OriginalVertexOffset; corner++)
            {
                for (int edge = 0; edge < hull.Count; edge++)
                {
                    int a = hull[edge], b = hull[(edge + 1) % hull.Count];
                    if (Cross(points[a], points[b], points[corner]) < 0)
                        AddTriangle(points, triangles, a, b, corner, winding);
                }
                var candidates = new List<int>(hull) { corner };
                hull = ConvexHull(points, candidates);
            }

            var worldVertices = new float[source.WorldVerticesLength];
            source.ComputeWorldVertices(setupSlot, worldVertices);
            int[] boneOffsets = new int[vertexCount];
            int[] vertexOffsets = new int[vertexCount];
            if (source.Bones != null)
            {
                int boneOffset = 0, vertexOffset = 0;
                for (int i = 0; i < vertexCount; i++)
                {
                    boneOffsets[i] = boneOffset; vertexOffsets[i] = vertexOffset;
                    int count = source.Bones[boneOffset];
                    boneOffset += count + 1; vertexOffset += count * 3;
                }
            }
            var cornerBones = new List<int>();
            var cornerVertices = new List<float>();
            for (int corner = 0; corner < OriginalVertexOffset; corner++)
                AddCorner(source, setupSlot, points, boundary, worldVertices, boneOffsets, vertexOffsets,
                    corner, cornerBones, cornerVertices);

            MeshAttachment expanded = source.NewLinkedMesh();
            expanded.ParentMesh = null;
            expanded.Bones = source.Bones == null ? null : Join(cornerBones, source.Bones);
            expanded.Vertices = Join(cornerVertices, source.Vertices);
            expanded.WorldVerticesLength = points.Length * 2;
            expanded.RegionUVs = new float[points.Length * 2];
            for (int i = 0; i < points.Length; i++)
            {
                expanded.RegionUVs[i * 2] = points[i].x;
                expanded.RegionUVs[i * 2 + 1] = points[i].y;
            }
            expanded.Triangles = triangles.ToArray();
            expanded.HullLength = OriginalVertexOffset * 2;
            expanded.Edges = null;
            expanded.UpdateRegion();
            return expanded;
        }

        public static bool Covers(MeshAttachment mesh, Vector2 uv)
        {
            for (int i = 0; i < mesh.Triangles.Length; i += 3)
            {
                Vector2 a = Point(mesh, mesh.Triangles[i]);
                Vector2 b = Point(mesh, mesh.Triangles[i + 1]);
                Vector2 c = Point(mesh, mesh.Triangles[i + 2]);
                if (Cross(a, b, c) == 0) continue;
                double ab = Cross(a, b, uv), bc = Cross(b, c, uv), ca = Cross(c, a, uv);
                if ((ab >= 0 && bc >= 0 && ca >= 0) || (ab <= 0 && bc <= 0 && ca <= 0)) return true;
            }
            return false;
        }

        private static void Admit(MeshAttachment source, Slot slot)
        {
            int vertices = source.WorldVerticesLength / 2, hull = source.HullLength / 2;
            if (vertices < 3 || vertices > 256 || hull < 3 || hull > 64 || hull > vertices ||
                source.Triangles.Length < 3 || source.Triangles.Length > 1536 ||
                (source.Bones?.Length ?? 0) > 4352 || source.Vertices.Length > 12288 ||
                slot.Bone.Skeleton.Bones.Count > 256 || slot.Bone.Skeleton.Data.Animations.Count > 256)
                throw new ArgumentException($"Mesh '{source.Name}' exceeds rectangle expansion geometry limits.");
            if (source.Sequence != null || slot.Deform.Count != 0)
                throw new ArgumentException($"Mesh '{source.Name}' rectangle expansion requires a non-sequence setup pose without vertex deform.");
            for (int i = 0; i < source.RegionUVs.Length; i++)
                if (float.IsNaN(source.RegionUVs[i]) || source.RegionUVs[i] < 0 || source.RegionUVs[i] > 1)
                    throw new ArgumentException($"Mesh '{source.Name}' has UVs outside the finite Sprite rectangle.");
            int timelines = 0;
            foreach (var animation in slot.Bone.Skeleton.Data.Animations) timelines += animation.Timelines.Count;
            if (timelines > 4096)
                throw new ArgumentException($"Mesh '{source.Name}' skeleton exceeds 4096 animation timelines.");
            foreach (var animation in slot.Bone.Skeleton.Data.Animations)
                foreach (var timeline in animation.Timelines)
                    if (timeline is DeformTimeline deform && ReferenceEquals(deform.Attachment, source.TimelineAttachment))
                        throw new ArgumentException($"Mesh '{source.Name}' has authored vertex deform in animation '{animation.Name}'; rectangle expansion supports bone-driven meshes.");
        }

        private static void AddCorner(MeshAttachment source, Slot slot, Vector2[] points, List<int> boundary,
            float[] world, int[] boneOffsets, int[] vertexOffsets, int corner, List<int> bones, List<float> vertices)
        {
            double distance = double.PositiveInfinity, t = 0;
            int a = -1, b = -1;
            for (int i = 0; i < boundary.Count; i++)
            {
                int first = boundary[i], last = boundary[(i + 1) % boundary.Count];
                Vector2 start = points[first], end = points[last], p = points[corner];
                double dx = (double)end.x - start.x, dy = (double)end.y - start.y;
                double fraction = Math.Max(0, Math.Min(1, (((double)p.x - start.x) * dx + ((double)p.y - start.y) * dy) / (dx * dx + dy * dy)));
                double x = start.x + dx * fraction - p.x, y = start.y + dy * fraction - p.y;
                double squared = x * x + y * y;
                if (squared >= distance) continue;
                distance = squared; t = fraction; a = first; b = last;
            }
            int originalA = a - OriginalVertexOffset, originalB = b - OriginalVertexOffset;
            int c = IncidentTriangleVertex(source, originalA, originalB) + OriginalVertexOffset;
            double determinant = Cross(points[a], points[b], points[c]);
            if (determinant == 0)
                throw new ArgumentException($"Mesh '{source.Name}' has a degenerate boundary triangle at edge {originalA}/{originalB}.");
            double wa = Cross(points[b], points[c], points[corner]) / determinant;
            double wb = Cross(points[c], points[a], points[corner]) / determinant;
            double wc = 1 - wa - wb;
            float xWorld = (float)(wa * world[originalA * 2] + wb * world[originalB * 2] + wc * world[(c - OriginalVertexOffset) * 2]);
            float yWorld = (float)(wa * world[originalA * 2 + 1] + wb * world[originalB * 2 + 1] + wc * world[(c - OriginalVertexOffset) * 2 + 1]);
            if (source.Bones == null)
            {
                slot.Bone.WorldToLocal(xWorld, yWorld, out float x, out float y);
                vertices.Add(x); vertices.Add(y);
                return;
            }
            var weights = new SortedDictionary<int, double>();
            AddWeights(source, originalA, 1 - t, boneOffsets, vertexOffsets, weights);
            AddWeights(source, originalB, t, boneOffsets, vertexOffsets, weights);
            double sum = 0;
            foreach (double weight in weights.Values) sum += weight;
            bones.Add(weights.Count);
            foreach (var pair in weights)
            {
                Bone bone = slot.Bone.Skeleton.Bones.Items[pair.Key];
                bone.WorldToLocal(xWorld, yWorld, out float x, out float y);
                bones.Add(pair.Key); vertices.Add(x); vertices.Add(y); vertices.Add((float)(pair.Value / sum));
            }
        }

        private static void AddWeights(MeshAttachment source, int vertex, double fraction, int[] boneOffsets,
            int[] vertexOffsets, SortedDictionary<int, double> weights)
        {
            if (fraction == 0) return;
            int boneOffset = boneOffsets[vertex], vertexOffset = vertexOffsets[vertex];
            int count = source.Bones[boneOffset++];
            for (int i = 0; i < count; i++, vertexOffset += 3)
            {
                int bone = source.Bones[boneOffset++];
                double weight = fraction * source.Vertices[vertexOffset + 2];
                if (weight == 0) continue;
                weights.TryGetValue(bone, out double previous);
                weights[bone] = previous + weight;
            }
        }

        private static int IncidentTriangleVertex(MeshAttachment mesh, int a, int b)
        {
            for (int i = 0; i < mesh.Triangles.Length; i += 3)
            {
                int x = mesh.Triangles[i], y = mesh.Triangles[i + 1], z = mesh.Triangles[i + 2];
                if ((x == a && y == b) || (x == b && y == a)) return z;
                if ((y == a && z == b) || (y == b && z == a)) return x;
                if ((z == a && x == b) || (z == b && x == a)) return y;
            }
            throw new ArgumentException($"Mesh '{mesh.Name}' has no incident triangle for boundary edge {a}/{b}.");
        }

        private static List<int> ConvexHull(Vector2[] points, List<int> candidates)
        {
            var sorted = new List<int>(candidates);
            sorted.Sort((a, b) => points[a].x == points[b].x ? points[a].y.CompareTo(points[b].y) : points[a].x.CompareTo(points[b].x));
            var hull = new List<int>();
            foreach (int point in sorted)
            {
                while (hull.Count >= 2 && Cross(points[hull[hull.Count - 2]], points[hull[hull.Count - 1]], points[point]) <= 0)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(point);
            }
            int lower = hull.Count;
            for (int i = sorted.Count - 2; i >= 0; i--)
            {
                int point = sorted[i];
                while (hull.Count > lower && Cross(points[hull[hull.Count - 2]], points[hull[hull.Count - 1]], points[point]) <= 0)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(point);
            }
            hull.RemoveAt(hull.Count - 1);
            return hull;
        }

        private static void AddTriangle(Vector2[] points, List<int> triangles, int a, int b, int c, double winding)
        {
            double cross = Cross(points[a], points[b], points[c]);
            if (cross == 0) return;
            triangles.Add(a); triangles.Add(cross * winding > 0 ? b : c); triangles.Add(cross * winding > 0 ? c : b);
        }

        private static double SignedArea(Vector2[] points, List<int> polygon)
        {
            double twiceArea = 0;
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 a = points[polygon[i]], b = points[polygon[(i + 1) % polygon.Count]];
                twiceArea += (double)a.x * b.y - (double)a.y * b.x;
            }
            return twiceArea / 2;
        }

        private static Vector2 Point(MeshAttachment mesh, int index) => new(mesh.RegionUVs[index * 2], mesh.RegionUVs[index * 2 + 1]);

        private static double Cross(Vector2 a, Vector2 b, Vector2 c) =>
            ((double)b.x - a.x) * ((double)c.y - a.y) - ((double)b.y - a.y) * ((double)c.x - a.x);

        private static T[] Join<T>(List<T> prefix, T[] suffix)
        {
            var result = new T[prefix.Count + suffix.Length];
            prefix.CopyTo(result); Array.Copy(suffix, 0, result, prefix.Count, suffix.Length);
            return result;
        }
    }
}
