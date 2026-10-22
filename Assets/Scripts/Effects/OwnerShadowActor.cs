using UnityEngine;
using UnityEngine.Rendering;

namespace PurrcelainPanic
{
    /// <summary>모습은 그리지 않고 씬 조명으로 실제 그림자만 드리우는 집주인.</summary>
    public class OwnerShadowActor : MonoBehaviour
    {
        Vector3 outside;
        Vector3 doorway;
        Light shadowLight;
        Quaternion roomLightRotation;

        public void Initialize()
        {
            shadowLight = RenderSettings.sun;
            roomLightRotation = shadowLight.transform.rotation;
            var table = FindFirstObjectByType<TableSurface>();
            Bounds bounds = table.GetComponent<Collider>().bounds;
            Material material = table.GetComponent<Renderer>().sharedMaterial;
            doorway = new Vector3(bounds.max.x + 0.35f, 0f, bounds.center.z + 0.1f);
            outside = doorway + Vector3.right * 2.2f;

            var model = Instantiate(Resources.Load<GameObject>("Owner/OwnerWoman"), transform);
            model.name = "Female homeowner";
            model.transform.localRotation = Quaternion.Euler(0, -90, 0) * model.transform.localRotation;
            var renderers = model.GetComponentsInChildren<Renderer>();
            Bounds modelBounds = ModelBounds(renderers);
            model.transform.localScale *= 1.7f / modelBounds.size.y;
            modelBounds = ModelBounds(renderers);
            model.transform.position -= new Vector3(modelBounds.center.x, modelBounds.min.y, modelBounds.center.z);
            foreach (var renderer in renderers)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                renderer.receiveShadows = false;
            }
            SetProgress(0);
        }

        static Bounds ModelBounds(Renderer[] renderers)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        public void SetProgress(float amount)
        {
            shadowLight.transform.rotation = Quaternion.Slerp(roomLightRotation,
                Quaternion.Euler(25f, 270f, 0f), amount);
            transform.position = Vector3.Lerp(outside, doorway, amount);
            gameObject.SetActive(amount > 0);
        }

        void OnDisable()
        {
            if (shadowLight != null)
                shadowLight.transform.rotation = roomLightRotation;
        }
    }
}
