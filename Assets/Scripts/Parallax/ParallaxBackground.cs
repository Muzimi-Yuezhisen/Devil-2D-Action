using UnityEngine;

/// <summary>
/// 视差背景管理：按相机移动推动各图层并循环。
/// </summary>
public class ParallaxBackground : MonoBehaviour
{
    /// <summary>参与视差的图层列表。</summary>
    [SerializeField]
    private ParallaxLayer[] backgroundLayers;
    /// <summary>相机半宽（世界单位）。</summary>
    private float cameraHalfWidth;
    /// <summary>主相机。</summary>
    private Camera mainCamera;
    /// <summary>上一帧相机位置，用于计算完整的二维位移差。</summary>
    private Vector2 lastCameraPosition;

    /// <summary>缓存相机并计算半宽。</summary>
    private void Awake()
    {
        mainCamera = Camera.main;
        if (mainCamera != null)
            cameraHalfWidth = mainCamera.orthographicSize * mainCamera.aspect;
    }

    /// <summary>初始化图层宽度并记录相机位置。</summary>
    private void Start()
    {
        InitializeLayers();
        if (mainCamera != null)
            lastCameraPosition = mainCamera.transform.position;
    }

    /// <summary>在相机更新后同步视差，避免跳跃时相机顶部离开背景范围。</summary>
    private void LateUpdate()
    {
        SyncToCameraNow();
    }

    /// <summary>立即把所有视差层同步到当前相机位置，供运行时与回归测试共同使用。</summary>
    public void SyncToCameraNow()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null || backgroundLayers == null) return;

        Vector2 currentCameraPosition = mainCamera.transform.position;
        Vector2 cameraDelta = currentCameraPosition - lastCameraPosition;
        lastCameraPosition = currentCameraPosition;
        cameraHalfWidth = mainCamera.orthographicSize * mainCamera.aspect;

        float cameraLeftEdge = currentCameraPosition.x - cameraHalfWidth;
        float cameraRightEdge = currentCameraPosition.x + cameraHalfWidth;

        foreach (ParallaxLayer layer in backgroundLayers)
        {
            layer.Move(cameraDelta);
            layer.LoopBackground(cameraLeftEdge, cameraRightEdge);
        }
    }

    /// <summary>最远景层应始终覆盖相机纵向画面，防止出现纯色空带。</summary>
    public bool PrimaryLayerCoversCameraVertically(float margin = 0.1f)
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null || backgroundLayers == null || backgroundLayers.Length == 0) return false;
        float halfHeight = mainCamera.orthographicSize;
        float centerY = mainCamera.transform.position.y;
        return backgroundLayers[0].CoversVerticalRange(centerY - halfHeight, centerY + halfHeight, margin);
    }

    /// <summary>预计算每层图片宽度。</summary>
    private void InitializeLayers()
    {
        if (backgroundLayers == null) return;
        foreach (ParallaxLayer layer in backgroundLayers) layer.CalculateImageWidth();
    }
}
