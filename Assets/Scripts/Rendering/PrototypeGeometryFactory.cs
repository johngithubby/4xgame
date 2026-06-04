using UnityEngine;

namespace LaneSurvivor.Rendering
{
    public static class PrototypeGeometryFactory
    {
        private static Mesh sharedCubeMesh;

        private static Mesh sharedHorizontalPlaneMesh;

        public static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material material)
        {
            // Build the object manually so simulator players do not depend on Unity physics collider modules.
            GameObject gameObject = new(name);

            // Apply transform data up front so callers can treat the helper like GameObject.CreatePrimitive.
            gameObject.transform.position = position;
            gameObject.transform.localScale = scale;

            // MeshFilter holds the generated placeholder cube mesh.
            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = GetCubeMesh();

            // MeshRenderer draws the cube with the caller-provided prototype material.
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
    }
}
