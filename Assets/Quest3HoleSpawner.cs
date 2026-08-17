using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Fait apparaître un prefab d'effet de trou lorsque le joueur appuie sur A
/// sur la manette droite du Meta Quest 3.
///
/// Le script lance un rayon depuis la manette. Si une surface est touchée,
/// le prefab est placé sur cette surface et orienté selon sa normale.
/// </summary>
public class Quest3HoleSpawner : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Le prefab 3D qui contient les deux textures / le matériau de l'effet de trou.")]
    [SerializeField] private GameObject holePrefab;

    [Tooltip("Origine du rayon, normalement le Transform de la manette droite.")]
    [SerializeField] private Transform rightControllerRayOrigin;

    [Header("Placement")]
    [Tooltip("Coché : place le trou sur un mur/sol détecté. Décoché : le place dans le vide devant la manette.")]
    [SerializeField] private bool requireDetectedSurface = true;

    [SerializeField, Min(0.1f)] private float maxPlacementDistance = 10f;

    [Tooltip("Distance devant la manette utilisée quand la détection de surface est désactivée.")]
    [SerializeField, Min(0.1f)] private float placementDistanceWithoutSurface = 2f;

    [SerializeField] private LayerMask placementLayers = ~0;

    [Tooltip("Petit décalage pour éviter que le prefab clignote dans la surface.")]
    [SerializeField, Min(0f)] private float surfaceOffset = 0.002f;

    [Tooltip("Rotation supplémentaire si l'axe avant du prefab n'est pas correctement aligné.")]
    [SerializeField] private Vector3 rotationOffsetEuler;

    [Header("Options")]
    [Tooltip("Détruit le trou précédent avant d'en créer un nouveau.")]
    [SerializeField] private bool keepOnlyOneHole = false;

    private InputDevice rightController;
    private bool wasAButtonPressed;
    private GameObject lastSpawnedHole;

    private void OnEnable()
    {
        TryFindRightController();
    }

    private void Update()
    {
        if (!rightController.isValid)
            TryFindRightController();

        bool isAButtonPressed = false;
        rightController.TryGetFeatureValue(CommonUsages.primaryButton, out isAButtonPressed);

        // Déclenche une seule fois au moment où le bouton vient d'être pressé.
        if (isAButtonPressed && !wasAButtonPressed)
            SpawnHoleOnSurface();

        wasAButtonPressed = isAButtonPressed;
    }

    private void TryFindRightController()
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Controller |
            InputDeviceCharacteristics.HeldInHand |
            InputDeviceCharacteristics.Right,
            devices);

        if (devices.Count > 0)
            rightController = devices[0];
    }

    private void SpawnHoleOnSurface()
    {
        if (holePrefab == null || rightControllerRayOrigin == null)
        {
            Debug.LogWarning("Quest3HoleSpawner : assigne le prefab et l'origine de la manette droite.");
            return;
        }

        Ray ray = new Ray(rightControllerRayOrigin.position, rightControllerRayOrigin.forward);

        if (!requireDetectedSurface)
        {
            RemovePreviousHoleIfNeeded();
            Vector3 freePosition = ray.GetPoint(placementDistanceWithoutSurface);
            Quaternion freeRotation = rightControllerRayOrigin.rotation * Quaternion.Euler(rotationOffsetEuler);
            lastSpawnedHole = Instantiate(holePrefab, freePosition, freeRotation);
            return;
        }

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                maxPlacementDistance,
                placementLayers,
                QueryTriggerInteraction.Ignore))
            return;

        if (keepOnlyOneHole && lastSpawnedHole != null)
            Destroy(lastSpawnedHole);

        RemovePreviousHoleIfNeeded();
        Vector3 position = hit.point + hit.normal * surfaceOffset;

        // L'axe avant (+Z) du prefab regarde hors de la surface. Sur un sol ou
        // un plafond, on emploie l'axe avant du contrôleur comme repère afin
        // d'éviter une rotation indéfinie.
        Vector3 referenceUp = Mathf.Abs(Vector3.Dot(hit.normal, Vector3.up)) > 0.98f
            ? Vector3.ProjectOnPlane(rightControllerRayOrigin.forward, hit.normal).normalized
            : Vector3.up;

        if (referenceUp.sqrMagnitude < 0.001f)
            referenceUp = Vector3.forward;

        Quaternion surfaceRotation = Quaternion.LookRotation(hit.normal, referenceUp);
        Quaternion finalRotation = surfaceRotation * Quaternion.Euler(rotationOffsetEuler);

        lastSpawnedHole = Instantiate(holePrefab, position, finalRotation);
    }

    private void RemovePreviousHoleIfNeeded()
    {
        if (keepOnlyOneHole && lastSpawnedHole != null)
            Destroy(lastSpawnedHole);
    }
}