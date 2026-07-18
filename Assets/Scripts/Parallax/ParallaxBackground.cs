using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    [SerializeField]
    private ParallaxLayer[] backgroundLayers;
    private float cameraHalfWidth;

    private Camera mainCamera;
    private float lastCameraPositionX;

    private void Awake()
    {
        mainCamera = Camera.main;
        cameraHalfWidth = mainCamera.orthographicSize * mainCamera.aspect;
    }

    private void Start()
    {
        InitializeLayers();
        lastCameraPositionX = mainCamera.transform.position.x;
    }

    // ”√FixedUpdate±‹√‚ª≠√Ê∂∂∂Ø
    private void FixedUpdate()
    {
        float currentCameraPositonX = mainCamera.transform.position.x;
        float distanceToMove = currentCameraPositonX - lastCameraPositionX;
        lastCameraPositionX = currentCameraPositonX;

        float cameraLeftEdge = currentCameraPositonX - cameraHalfWidth;
        float cameraRightEdge = currentCameraPositonX + cameraHalfWidth;

        foreach(ParallaxLayer layer in backgroundLayers)
        {
            layer.Move(distanceToMove);
            layer.LoopBackground(cameraLeftEdge, cameraRightEdge);
        }
    }

    private void InitializeLayers()
    {
        foreach (ParallaxLayer layer in backgroundLayers) layer.CalculateImageWidth();
    }
}
