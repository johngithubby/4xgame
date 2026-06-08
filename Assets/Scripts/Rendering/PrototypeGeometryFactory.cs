using System.Collections.Generic;
using UnityEngine;

namespace LaneSurvivor.Rendering
{
    public static class PrototypeGeometryFactory
    {
        private static Mesh sharedCubeMesh;

        private static Mesh sharedHorizontalPlaneMesh;

        private static Mesh sharedSphereMesh;

        private static Mesh sharedCylinderMesh;

        private const int SphereLongitudeSegments = 16;

        private const int SphereLatitudeSegments = 8;

        private const int CylinderSegments = 16;

        public static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material material)
        {
            // Build the object manually so simulator players do not depend on Unity physics collider modules.
            GameObject gameObject = CreateMeshObject(name, position, Quaternion.identity, scale, material, GetCubeMesh());

            return gameObject;
        }

        public static GameObject CreateSphere(string name, Vector3 position, Vector3 scale, Material material)
        {
            // Spheres let prototype characters read as heads, hands, shoulders, and rounded torsos instead of boxes.
            GameObject gameObject = CreateMeshObject(name, position, Quaternion.identity, scale, material, GetSphereMesh());

            return gameObject;
        }

        public static GameObject CreateCylinder(string name, Vector3 position, Vector3 scale, Material material)
        {
            // Cylinders give limbs and weapons a more natural silhouette while staying collider-free.
            GameObject gameObject = CreateMeshObject(name, position, Quaternion.identity, scale, material, GetCylinderMesh());

            return gameObject;
        }

        private static GameObject CreateMeshObject(string name, Vector3 position, Quaternion rotation, Vector3 scale, Material material, Mesh mesh)
        {
            // Build the object manually so no helper path adds a runtime physics collider.
            GameObject gameObject = new(name);

            // Apply transform data up front so callers can treat the helper like GameObject.CreatePrimitive.
            gameObject.transform.SetPositionAndRotation(position, rotation);
            gameObject.transform.localScale = scale;

            // MeshFilter holds the generated shared mesh for this prototype primitive.
            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            // MeshRenderer draws the primitive with the caller-provided prototype material.
            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;

            return gameObject;
        }

        public static GameObject CreateHorizontalPlane(string name, Vector3 position, Vector2 size, Material material)
        {
            // Build the road as a surface, not a solid slab, so it cannot hide actors that stand above it.
            GameObject gameObject = new(name);

            // The generated mesh is one unit square, so scale supplies the requested road dimensions.
            gameObject.transform.position = position;
            gameObject.transform.localScale = new Vector3(size.x, 1f, size.y);

            // MeshFilter references a shared flat plane mesh with no side faces.
            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = GetHorizontalPlaneMesh();

            // MeshRenderer draws the surface using the track material.
            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;

            return gameObject;
        }

        private static Mesh GetCubeMesh()
        {
            // Reuse one immutable mesh for all placeholder cubes to keep runtime allocations small.
            if (sharedCubeMesh != null)
            {
                return sharedCubeMesh;
            }

            // Each face owns four vertices so flat normals make the cube edges readable.
            Vector3[] vertices =
            {
                new(-0.5f, -0.5f, -0.5f), new(0.5f, -0.5f, -0.5f), new(0.5f, 0.5f, -0.5f), new(-0.5f, 0.5f, -0.5f),
                new(0.5f, -0.5f, 0.5f), new(-0.5f, -0.5f, 0.5f), new(-0.5f, 0.5f, 0.5f), new(0.5f, 0.5f, 0.5f),
                new(-0.5f, -0.5f, 0.5f), new(-0.5f, -0.5f, -0.5f), new(-0.5f, 0.5f, -0.5f), new(-0.5f, 0.5f, 0.5f),
                new(0.5f, -0.5f, -0.5f), new(0.5f, -0.5f, 0.5f), new(0.5f, 0.5f, 0.5f), new(0.5f, 0.5f, -0.5f),
                new(-0.5f, 0.5f, -0.5f), new(0.5f, 0.5f, -0.5f), new(0.5f, 0.5f, 0.5f), new(-0.5f, 0.5f, 0.5f),
                new(-0.5f, -0.5f, 0.5f), new(0.5f, -0.5f, 0.5f), new(0.5f, -0.5f, -0.5f), new(-0.5f, -0.5f, -0.5f)
            };

            // Flat normals match the corresponding face order above.
            Vector3[] normals =
            {
                Vector3.back, Vector3.back, Vector3.back, Vector3.back,
                Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward,
                Vector3.left, Vector3.left, Vector3.left, Vector3.left,
                Vector3.right, Vector3.right, Vector3.right, Vector3.right,
                Vector3.up, Vector3.up, Vector3.up, Vector3.up,
                Vector3.down, Vector3.down, Vector3.down, Vector3.down
            };

            // Simple UVs keep the mesh compatible with any basic material shader.
            Vector2[] uvs =
            {
                Vector2.zero, Vector2.right, Vector2.one, Vector2.up,
                Vector2.zero, Vector2.right, Vector2.one, Vector2.up,
                Vector2.zero, Vector2.right, Vector2.one, Vector2.up,
                Vector2.zero, Vector2.right, Vector2.one, Vector2.up,
                Vector2.zero, Vector2.right, Vector2.one, Vector2.up,
                Vector2.zero, Vector2.right, Vector2.one, Vector2.up
            };

            // Two clockwise triangles draw each cube face.
            int[] triangles =
            {
                0, 2, 1, 0, 3, 2,
                4, 6, 5, 4, 7, 6,
                8, 10, 9, 8, 11, 10,
                12, 14, 13, 12, 15, 14,
                16, 18, 17, 16, 19, 18,
                20, 22, 21, 20, 23, 22
            };

            // HideFlags prevent the generated runtime helper mesh from being saved as a scene asset.
            sharedCubeMesh = new Mesh
            {
                name = "Prototype Shared Cube Mesh",
                hideFlags = HideFlags.HideAndDontSave,
                vertices = vertices,
                normals = normals,
                uv = uvs,
                triangles = triangles
            };

            // Bounds let Unity cull the mesh correctly when scaled into tracks and slabs.
            sharedCubeMesh.RecalculateBounds();
            return sharedCubeMesh;
        }

        private static Mesh GetHorizontalPlaneMesh()
        {
            // Reuse one immutable road plane mesh for every flat prototype surface.
            if (sharedHorizontalPlaneMesh != null)
            {
                return sharedHorizontalPlaneMesh;
            }

            // The road lies on the X/Z plane so actors can stand visibly above it on Y.
            Vector3[] vertices =
            {
                new(-0.5f, 0f, -0.5f),
                new(0.5f, 0f, -0.5f),
                new(0.5f, 0f, 0.5f),
                new(-0.5f, 0f, 0.5f)
            };

            // Up normals keep the surface lit from the gameplay camera.
            Vector3[] normals =
            {
                Vector3.up,
                Vector3.up,
                Vector3.up,
                Vector3.up
            };

            // Basic UVs keep the mesh compatible with simple placeholder materials.
            Vector2[] uvs =
            {
                Vector2.zero,
                Vector2.right,
                Vector2.one,
                Vector2.up
            };

            // Draw both windings so the road remains visible even if a shader culls back faces.
            int[] triangles =
            {
                0, 2, 1,
                0, 3, 2,
                0, 1, 2,
                0, 2, 3
            };

            // HideFlags prevent the generated helper mesh from being saved into scenes.
            sharedHorizontalPlaneMesh = new Mesh
            {
                name = "Prototype Shared Horizontal Plane Mesh",
                hideFlags = HideFlags.HideAndDontSave,
                vertices = vertices,
                normals = normals,
                uv = uvs,
                triangles = triangles
            };

            // Bounds let Unity cull the scaled road surface correctly.
            sharedHorizontalPlaneMesh.RecalculateBounds();
            return sharedHorizontalPlaneMesh;
        }

        private static Mesh GetSphereMesh()
        {
            // Reuse one immutable rounded mesh for all generated heads, hands, and soft body parts.
            if (sharedSphereMesh != null)
            {
                return sharedSphereMesh;
            }

            // Lists keep the mesh construction readable while the segment counts stay tiny.
            List<Vector3> vertices = new();
            List<Vector3> normals = new();
            List<Vector2> uvs = new();
            List<int> triangles = new();

            // Latitude rings run from the top pole to the bottom pole.
            for (int latitude = 0; latitude <= SphereLatitudeSegments; latitude++)
            {
                // Normalized vertical progress maps directly to the polar angle.
                float v = latitude / (float)SphereLatitudeSegments;

                // Phi sweeps over the half-circle that defines a sphere profile.
                float phi = Mathf.PI * v;

                // Y is the profile height, scaled to fit the same unit bounds as the cube helper.
                float y = Mathf.Cos(phi) * 0.5f;

                // The ring radius expands from the top pole and contracts toward the bottom pole.
                float ringRadius = Mathf.Sin(phi) * 0.5f;

                // Longitude repeats the first vertex at the seam so UVs wrap without special cases.
                for (int longitude = 0; longitude <= SphereLongitudeSegments; longitude++)
                {
                    // Normalized horizontal progress maps to a full circle around the Y axis.
                    float u = longitude / (float)SphereLongitudeSegments;

                    // Theta sweeps the ring around the vertical axis.
                    float theta = u * Mathf.PI * 2f;

                    // The generated sphere uses X/Z as the ground plane and Y as height.
                    Vector3 vertex = new(Mathf.Cos(theta) * ringRadius, y, Mathf.Sin(theta) * ringRadius);

                    // The unit sphere normal points away from the origin; poles fall back to vertical normals.
                    Vector3 normal = vertex.sqrMagnitude > 0.0001f ? vertex.normalized : Vector3.up;

                    // Store geometry and UV data in matching order for every ring vertex.
                    vertices.Add(vertex);
                    normals.Add(normal);
                    uvs.Add(new Vector2(u, v));
                }
            }

            // Quads between adjacent latitude rings become two triangles each.
            for (int latitude = 0; latitude < SphereLatitudeSegments; latitude++)
            {
                // The next ring starts one full seam-inclusive row after the current ring.
                int nextRingStart = (latitude + 1) * (SphereLongitudeSegments + 1);

                // The current ring starts at this row offset.
                int currentRingStart = latitude * (SphereLongitudeSegments + 1);

                // Each longitude segment connects four neighboring vertices.
                for (int longitude = 0; longitude < SphereLongitudeSegments; longitude++)
                {
                    // Current and next indices describe the quad corners in ring order.
                    int current = currentRingStart + longitude;
                    int currentNext = current + 1;
                    int lower = nextRingStart + longitude;
                    int lowerNext = lower + 1;

                    // Add both windings so the lightweight mesh remains visible across shader culling defaults.
                    AddDoubleSidedTriangle(triangles, current, currentNext, lower);
                    AddDoubleSidedTriangle(triangles, currentNext, lowerNext, lower);
                }
            }

            // HideFlags prevent the generated runtime helper mesh from being saved as a scene asset.
            sharedSphereMesh = new Mesh
            {
                name = "Prototype Shared Sphere Mesh",
                hideFlags = HideFlags.HideAndDontSave,
                vertices = vertices.ToArray(),
                normals = normals.ToArray(),
                uv = uvs.ToArray(),
                triangles = triangles.ToArray()
            };

            // Bounds let Unity cull scaled ellipsoid body parts correctly.
            sharedSphereMesh.RecalculateBounds();
            return sharedSphereMesh;
        }

        private static Mesh GetCylinderMesh()
        {
            // Reuse one immutable cylinder mesh for generated arms, legs, props, and armor straps.
            if (sharedCylinderMesh != null)
            {
                return sharedCylinderMesh;
            }

            // Lists keep the side, top, and bottom construction straightforward.
            List<Vector3> vertices = new();
            List<Vector3> normals = new();
            List<Vector2> uvs = new();
            List<int> triangles = new();

            // The first two vertices are cap centers in the same unit-height bounds as the sphere and cube.
            int topCenterIndex = vertices.Count;
            vertices.Add(new Vector3(0f, 0.5f, 0f));
            normals.Add(Vector3.up);
            uvs.Add(new Vector2(0.5f, 0.5f));

            // Bottom center follows top center so cap triangles can reference stable indices.
            int bottomCenterIndex = vertices.Count;
            vertices.Add(new Vector3(0f, -0.5f, 0f));
            normals.Add(Vector3.down);
            uvs.Add(new Vector2(0.5f, 0.5f));

            // Ring vertices are duplicated for side and cap normals so lighting remains stable.
            int sideStartIndex = vertices.Count;
            for (int segment = 0; segment <= CylinderSegments; segment++)
            {
                // Normalized segment progress maps around the cylinder seam.
                float u = segment / (float)CylinderSegments;

                // The angle circles around Y, matching the sphere mesh's axis convention.
                float angle = u * Mathf.PI * 2f;

                // X/Z ring coordinates sit on a half-unit radius.
                float x = Mathf.Cos(angle) * 0.5f;
                float z = Mathf.Sin(angle) * 0.5f;

                // The side normal points outward in the ground plane.
                Vector3 normal = new Vector3(x, 0f, z).normalized;

                // Store top then bottom side vertices for each seam-inclusive segment.
                vertices.Add(new Vector3(x, 0.5f, z));
                normals.Add(normal);
                uvs.Add(new Vector2(u, 1f));
                vertices.Add(new Vector3(x, -0.5f, z));
                normals.Add(normal);
                uvs.Add(new Vector2(u, 0f));
            }

            // Cap rings use separate normals so top and bottom surfaces shade cleanly.
            int topCapStartIndex = vertices.Count;
            for (int segment = 0; segment <= CylinderSegments; segment++)
            {
                // Normalized segment progress maps the cap ring and UV circle together.
                float u = segment / (float)CylinderSegments;

                // The angle circles around the cap center.
                float angle = u * Mathf.PI * 2f;

                // X/Z ring coordinates sit on a half-unit radius.
                float x = Mathf.Cos(angle) * 0.5f;
                float z = Mathf.Sin(angle) * 0.5f;

                // Top cap vertices all share an upward normal.
                vertices.Add(new Vector3(x, 0.5f, z));
                normals.Add(Vector3.up);
                uvs.Add(new Vector2(x + 0.5f, z + 0.5f));
            }

            // Bottom cap has its own ring so normals point down.
            int bottomCapStartIndex = vertices.Count;
            for (int segment = 0; segment <= CylinderSegments; segment++)
            {
                // Normalized segment progress maps the cap ring and UV circle together.
                float u = segment / (float)CylinderSegments;

                // The angle circles around the cap center.
                float angle = u * Mathf.PI * 2f;

                // X/Z ring coordinates sit on a half-unit radius.
                float x = Mathf.Cos(angle) * 0.5f;
                float z = Mathf.Sin(angle) * 0.5f;

                // Bottom cap vertices all share a downward normal.
                vertices.Add(new Vector3(x, -0.5f, z));
                normals.Add(Vector3.down);
                uvs.Add(new Vector2(x + 0.5f, z + 0.5f));
            }

            // Build side quads and cap fans for every cylinder segment.
            for (int segment = 0; segment < CylinderSegments; segment++)
            {
                // Side ring vertices are stored as top/bottom pairs.
                int sideTop = sideStartIndex + segment * 2;
                int sideBottom = sideTop + 1;
                int nextSideTop = sideTop + 2;
                int nextSideBottom = sideTop + 3;

                // Two side triangles form the rectangular strip between this segment and the next.
                AddDoubleSidedTriangle(triangles, sideTop, nextSideTop, sideBottom);
                AddDoubleSidedTriangle(triangles, nextSideTop, nextSideBottom, sideBottom);

                // Cap ring indices use one vertex per segment.
                int topCap = topCapStartIndex + segment;
                int nextTopCap = topCap + 1;
                int bottomCap = bottomCapStartIndex + segment;
                int nextBottomCap = bottomCap + 1;

                // Top and bottom fans close the cylinder without adding collider dependencies.
                AddDoubleSidedTriangle(triangles, topCenterIndex, topCap, nextTopCap);
                AddDoubleSidedTriangle(triangles, bottomCenterIndex, nextBottomCap, bottomCap);
            }

            // HideFlags prevent the generated helper mesh from being saved into scenes.
            sharedCylinderMesh = new Mesh
            {
                name = "Prototype Shared Cylinder Mesh",
                hideFlags = HideFlags.HideAndDontSave,
                vertices = vertices.ToArray(),
                normals = normals.ToArray(),
                uv = uvs.ToArray(),
                triangles = triangles.ToArray()
            };

            // Bounds let Unity cull scaled limbs and props correctly.
            sharedCylinderMesh.RecalculateBounds();
            return sharedCylinderMesh;
        }

        private static void AddDoubleSidedTriangle(List<int> triangles, int first, int second, int third)
        {
            // The primary winding draws on shaders with normal back-face culling.
            triangles.Add(first);
            triangles.Add(second);
            triangles.Add(third);

            // The reverse winding keeps tiny limbs visible if a material or platform flips culling behavior.
            triangles.Add(first);
            triangles.Add(third);
            triangles.Add(second);
        }
    }
}
