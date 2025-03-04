using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class CameraCapture : MonoBehaviour
{
    public RawImage cameraFeed;
    public RawImage ThumbnailImage;
    public GameObject LibraryPanel;
    public RectTransform openLibraryButtonRect;
    public RectTransform closeLibraryButtonRect;
    private WebCamTexture webCamTexture;
    private string savePath;
    private List<string> capturedImagePaths = new List<string>();

    void Awake()
    {
        Debug.Log("CameraCaptureTest Awake() called.");
    }

    private void Start()
    {
        InitializeCamera();
    }

    public void InitializeCamera()
    {
        if (webCamTexture == null || !webCamTexture.isPlaying)
        {
            webCamTexture = new WebCamTexture();
            cameraFeed.texture = webCamTexture;
            webCamTexture.Play();
        }

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

    void OnDisable()
    {
        if (webCamTexture != null && webCamTexture.isPlaying)
        {
            webCamTexture.Stop();
        }
    }

    public void CapturePoster()
    {
        Debug.Log("CapturePoster() called.");
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
            capturedImagePaths.Add(filePath);
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

    public void OpenLibrary()
    {
        LibraryPanel.SetActive(true);
        DisplayLibraryImages();
    }

    public void DisplayLibraryImages()
    {
        Debug.Log("DisplayLibraryImages() called. Captured image count: " + capturedImagePaths.Count);

        capturedImagePaths.Clear(); // Clear the list before loading new images

        // Repopulate the capturedImagePaths list
        string savePath = Application.persistentDataPath + "/CapturedImages/";
        if (Directory.Exists(savePath))
        {
            string[] files = Directory.GetFiles(savePath, "*.png");
            foreach (string file in files)
            {
                capturedImagePaths.Add(file);
            }
        }

        // Clear existing images (if any)
        foreach (Transform child in LibraryPanel.transform)
        {
            if (child.name.StartsWith("LibraryImage_"))
            {
                Destroy(child.gameObject);
            }
        }

        // Display new images
        for (int i = 0; i < capturedImagePaths.Count; i++)
        {
            Debug.Log("Loading image: " + capturedImagePaths[i]);

            string filePath = capturedImagePaths[i];
            Texture2D texture = new Texture2D(2, 2);
            byte[] fileData = File.ReadAllBytes(filePath);
            texture.LoadImage(fileData);

            GameObject imageObject = new GameObject("LibraryImage_" + i);
            imageObject.transform.SetParent(LibraryPanel.transform, false);

            RawImage rawImage = imageObject.AddComponent<RawImage>();
            rawImage.texture = texture;

            // Adjust the Rect Transform of the imageObject as needed
            RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(100, 100); // Example size
        }
    }

    public void CloseLibrary()
    {
        LibraryPanel.SetActive(false);
    }

    void OnOpenLibrary(InputAction.CallbackContext context)
    {
        Debug.Log("OnOpenLibrary() called.");
        OpenLibrary();
    }

    void OnCloseLibrary(InputAction.CallbackContext context)
    {
        Debug.Log("OnCloseLibrary() called.");
        CloseLibrary();
    }
}