using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingGhost : MonoBehaviour
{
    private Transform visual;
    private Transform outline;
    private PlacedObjectTypeSO placedObjectTypeSO;
    [SerializeField] private Material outlineMaterial;

    private void Start()
    {
        RefreshVisual();

        GridBuildingSystem.Instance.OnSelectedChanged += Instance_OnSelectedChanged;
    }

    private void CreateOutline()
    {
        if (outline != null)
        {
            Destroy(outline.gameObject);
        }

        outline = Instantiate(visual, visual);
        outline.localPosition = Vector3.zero;
        outline.localRotation = Quaternion.identity;

        // scale lớn hơn một chút
        Vector3 baseScale = placedObjectTypeSO.visual.localScale;
        outline.localScale = baseScale * 1.01f;

        // gán material outline
        foreach (var r in outline.GetComponentsInChildren<MeshRenderer>())
        {
            r.material = outlineMaterial;
        }
    }

    private void Instance_OnSelectedChanged(object sender, System.EventArgs e)
    {
        RefreshVisual();
    }

    private void LateUpdate()
    {
        Vector3 targetPosition = GridBuildingSystem.Instance.GetMouseWorldSnappedPosition();
        targetPosition.y = 0.1f;
        Quaternion targetRotation = GridBuildingSystem.Instance.GetPlacedObjectRotation();
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 15f);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 15f);
    }

    private void RefreshVisual()
    {
        if (visual != null)
        {
            Destroy(visual.gameObject);
            visual = null;
        }
        placedObjectTypeSO = GridBuildingSystem.Instance.GetPlacedObjectTypeSO();
        if (placedObjectTypeSO != null)
        {
            visual = Instantiate(placedObjectTypeSO.visual, Vector3.zero, Quaternion.identity);
            visual.parent = transform;
            visual.localPosition = Vector3.zero;
            visual.localEulerAngles = Vector3.zero;
            SetLayerRecursive(visual.gameObject, 11);
            CreateOutline();
        }
    }

    private void SetLayerRecursive(GameObject targetGameObject, int layer)
    {
        targetGameObject.layer = layer;
        foreach (Transform child in targetGameObject.transform)
        {
            SetLayerRecursive(child.gameObject, layer);
        }
    }
}
