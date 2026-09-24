using UnityEngine;
using UnityEngine.UI;

public class MatchSnapshotUI : MonoBehaviour
{
    public static MatchSnapshotUI Instance { get; private set; }

    [SerializeField] private RawImage snapshotImage;

    private Texture2D snapshotTexture;

    private void Awake()
    {
        Instance = this;

        if (snapshotImage != null)
            snapshotImage.gameObject.SetActive(false);
    }

    public void Capture(System.Action onCaptured = null)
    {
        StartCoroutine(CaptureCoroutine(onCaptured));
    }

    private System.Collections.IEnumerator CaptureCoroutine(
        System.Action onCaptured)
    {
        yield return null;
        yield return null;
        yield return new WaitForEndOfFrame();

        snapshotTexture = new Texture2D(
            Screen.width,
            Screen.height,
            TextureFormat.RGB24,
            false);

        snapshotTexture.ReadPixels(
            new Rect(0, 0, Screen.width, Screen.height),
            0,
            0);

        snapshotTexture.Apply();

        snapshotImage.texture = snapshotTexture;
        snapshotImage.gameObject.SetActive(true);

        Debug.Log("[MATCH] Battlefield snapshot captured.");

        onCaptured?.Invoke();
    }

    private void OnDestroy()
    {
        if (snapshotTexture != null)
            Destroy(snapshotTexture);
    }
}