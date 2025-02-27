using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEngine.InputSystem;

public class CameraCapture : MonoBehaviour
{
    public RawImage cameraFeed;
    public RawImage ThumbnailImage;
    private WebCamTexture webCamTexture;
    private string savePath;
    private PlayerInput playerInput; // Reference to the generated input class.

    void Awake()
    {
        playerInput = new PlayerInput(); // Initialize the input class.
    }

    void OnEnable()
    {
        playerInput.UI.Enable(); // Enable the UI action map.
        playerInput.UI.CapturePoster.performed += OnCapturePoster; // Subscribe to the event.
    }

    void OnDisable()
    {
        playerInput.UI.CapturePoster.performed -= OnCapturePoster; // Unsubscribe to the event.
        playerInput.UI.Disable(); // Disable the UI action map.
    }

    void Start()
    {
        webCamTexture = new WebCamTexture();
        cameraFeed.texture = webCamTexture;
        webCamTexture.Play();

        savePath = Application.persistentDataPath + "/CapturedImages/";
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }
    }

    void Update()
    {
        if (webCamTexture.didUpdateThisFrame)
        {
            float rotationAngle = -webCamTexture.videoRotationAngle;
            cameraFeed.rectTransform.localEulerAngles = new Vector3(0, 0, rotationAngle);

            cameraFeed.uvRect = webCamTexture.videoVerticallyMirrored ? new Rect(1, 0, -1, 1) : new Rect(0, 0, 1, 1);
        }
    }

    void OnCapturePoster(InputAction.CallbackContext context)
    {
        Debug.Log("CaptureImage() function called.");
        ThumbnailImage.gameObject.SetActive(false);
        Texture2D photo = new Texture2D(webCamTexture.width, webCamTexture.height);
        photo.SetPixels(webCamTexture.GetPixels());
        photo.Apply();

        byte[] bytes = photo.EncodeToPNG();
        string filePath = savePath + "Poster_" + System.DateTime.Now.ToString("yyyyMMddHHmmss") + ".png";
        try
        {
            File.WriteAllBytes(filePath, bytes);
            Debug.Log("Image saved to: " + filePath);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error saving image: " + ex.Message);
        }

        try
        {
            byte[] fileData = File.ReadAllBytes(filePath);
            Texture2D loadedTexture = new Texture2D(2, 2);
            loadedTexture.LoadImage(fileData);
            ThumbnailImage.texture = loadedTexture;
            ThumbnailImage.gameObject.SetActive(true);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error loading image: " + ex.Message);
        }
    }
}