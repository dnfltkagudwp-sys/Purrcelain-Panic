using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PurrcelainPanic.Editor
{
    public static class KitchenBackgroundBuilder
    {
        [MenuItem("Purrcelain Panic/Build Kitchen Background")]
        public static void Build()
        {
            if (Application.isPlaying || GameObject.Find("KitchenBackground") != null)
                throw new System.InvalidOperationException("Build in Edit Mode, with no existing KitchenBackground.");

            var root = new GameObject("KitchenBackground");
            Undo.RegisterCreatedObjectUndo(root, "Build kitchen background");
            var cream = Material("Kitchen_Cream", new Color(0.86f, 0.80f, 0.68f));
            var tile = Material("Kitchen_Tile", new Color(0.72f, 0.65f, 0.53f));
            var sage = Material("Kitchen_Sage", new Color(0.33f, 0.48f, 0.38f));
            var inset = Material("Kitchen_Inset", new Color(0.39f, 0.55f, 0.44f));
            var wood = Material("Kitchen_Wood", new Color(0.48f, 0.28f, 0.14f));
            var brass = Material("Kitchen_Brass", new Color(0.68f, 0.48f, 0.20f));
            var glass = Material("Kitchen_Glass", new Color(0.55f, 0.76f, 0.79f));
            var leaf = Material("Kitchen_Leaf", new Color(0.20f, 0.36f, 0.19f));
            var terra = Material("Kitchen_Terracotta", new Color(0.63f, 0.30f, 0.18f));

            // Decorative meshes have no colliders: vase flight and floor impacts stay unchanged.
            for (int x = -5; x <= 5; x++)
                for (int z = -3; z <= 6; z++)
                    Shape("FloorTile", PrimitiveType.Cube, new Vector3(x * 0.6f, 0.014f, z * 0.6f),
                        new Vector3(0.592f, 0.02f, 0.592f), (x + z) % 2 == 0 ? cream : tile, root.transform);

            Shape("BackWall", PrimitiveType.Cube, new Vector3(0, 1.3f, 3.25f),
                new Vector3(6.6f, 2.6f, 0.12f), cream, root.transform);
            Shape("Skirting", PrimitiveType.Cube, new Vector3(0, 0.07f, 3.16f),
                new Vector3(6.6f, 0.14f, 0.06f), wood, root.transform);
            for (int i = -2; i <= 2; i++)
            {
                float x = i * 0.78f;
                Shape("Cabinet", PrimitiveType.Cube, new Vector3(x, 0.4f, 2.7f),
                    new Vector3(0.76f, 0.78f, 0.65f), sage, root.transform);
                Shape("DoorPanel", PrimitiveType.Cube, new Vector3(x, 0.42f, 2.36f),
                    new Vector3(0.65f, 0.62f, 0.035f), inset, root.transform);
                Shape("Handle", PrimitiveType.Cube, new Vector3(x + 0.22f, 0.62f, 2.33f),
                    new Vector3(0.035f, 0.13f, 0.035f), brass, root.transform);
            }
            Shape("Countertop", PrimitiveType.Cube, new Vector3(0, 0.82f, 2.67f),
                new Vector3(4.05f, 0.09f, 0.78f), wood, root.transform);
            Shape("WindowFrame", PrimitiveType.Cube, new Vector3(0, 1.7f, 3.15f),
                new Vector3(1.7f, 1.15f, 0.1f), wood, root.transform);
            Shape("WindowGlass", PrimitiveType.Cube, new Vector3(0, 1.7f, 3.08f),
                new Vector3(1.56f, 1.01f, 0.025f), glass, root.transform);
            Shape("WindowMullion", PrimitiveType.Cube, new Vector3(0, 1.7f, 3.05f),
                new Vector3(0.05f, 1.02f, 0.03f), cream, root.transform);
            Shape("WindowCrossbar", PrimitiveType.Cube, new Vector3(0, 1.7f, 3.05f),
                new Vector3(1.56f, 0.05f, 0.03f), cream, root.transform);
            Shape("PlantPot", PrimitiveType.Cylinder, new Vector3(1.55f, 0.96f, 2.65f),
                new Vector3(0.23f, 0.1f, 0.23f), terra, root.transform);
            for (int i = 0; i < 5; i++)
            {
                float angle = i * Mathf.PI * 2f / 5f;
                Shape("PlantLeaf", PrimitiveType.Sphere,
                    new Vector3(1.55f + Mathf.Cos(angle) * 0.09f, 1.15f, 2.65f + Mathf.Sin(angle) * 0.09f),
                    new Vector3(0.13f, 0.3f, 0.13f), leaf, root.transform);
            }
            Shape("ChoppingBoard", PrimitiveType.Cube, new Vector3(-1.2f, 0.88f, 2.65f),
                new Vector3(0.42f, 0.025f, 0.28f), cream, root.transform);
            EditorSceneManager.MarkSceneDirty(root.scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = root;
        }

        static Material Material(string name, Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.name = name;
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.2f);
            AssetDatabase.CreateAsset(material, $"Assets/Data/{name}.mat");
            return material;
        }

        static void Shape(string name, PrimitiveType type, Vector3 position, Vector3 scale,
            Material material, Transform parent)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            if (name != "FloorTile")
                position.z -= 1.3f;
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
