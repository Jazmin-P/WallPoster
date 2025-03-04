using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;

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
    [SerializeField] private GameObject libraryContent; // Drag the Content object here
    public Button captureButton; // Add this at the top with your other public variables
    public Button OpenLibraryButton;

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
        captureButton.gameObject.SetActive(false);
        OpenLibraryButton.gameObject.SetActive(false);
        DisplayLibraryImages();
    }

    public void DisplayLibraryImages()
    {
        Debug.Log("DisplayLibraryImages() called");
        
        // Clear existing images first
        foreach (Transform child in libraryContent.transform)
        {
            Destroy(child.gameObject);
        }

        // Get all PNG files from the directory
        string savePath = Application.persistentDataPath + "/CapturedImages/";
        if (!Directory.Exists(savePath))
        {
            Debug.LogWarning("Save directory doesn't exist!");
            return;
        }

        string[] files = Directory.GetFiles(savePath, "*.png");
        Debug.Log($"Found {files.Length} images in directory");

        foreach (string filePath in files)
        {
            try
            {
                // Load image
                byte[] fileData = File.ReadAllBytes(filePath);
                Texture2D texture = new Texture2D(2, 2);
                if (!texture.LoadImage(fileData))
                {
                    Debug.LogError($"Failed to load image: {filePath}");
                    continue;
                }

                // Create container
                GameObject container = new GameObject("LibraryImage");
                container.transform.SetParent(libraryContent.transform, false);
                
                RectTransform containerRect = container.AddComponent<RectTransform>();
                containerRect.sizeDelta = new Vector2(600, 700);

                // Create image
                GameObject imageObj = new GameObject("Image");
                imageObj.transform.SetParent(container.transform, false);
                
                RawImage image = imageObj.AddComponent<RawImage>();
                image.texture = texture;
                
                RectTransform imageRect = imageObj.GetComponent<RectTransform>();
                imageRect.anchorMin = Vector2.zero;
                imageRect.anchorMax = Vector2.one;
                imageRect.offsetMin = Vector2.zero;
                imageRect.offsetMax = Vector2.zero;

                // Add delete button
                GameObject buttonObj = new GameObject("DeleteButton");
                buttonObj.transform.SetParent(container.transform, false);
                
                Button deleteButton = buttonObj.AddComponent<Button>();
                Image buttonImage = buttonObj.AddComponent<Image>();
                buttonImage.color = Color.red;
                
                RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
                buttonRect.sizeDelta = new Vector2(100, 30);
                buttonRect.anchorMin = new Vector2(0.5f, 0);
                buttonRect.anchorMax = new Vector2(0.5f, 0);
                buttonRect.anchoredPosition = new Vector2(0, 15);

                // Add button text
                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(buttonObj.transform, false);
                
                TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
                text.text = "Delete";
                text.color = Color.white;
                text.alignment = TextAlignmentOptions.Center;
                
                RectTransform textRect = textObj.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;

                // Add delete functionality
                string currentPath = filePath;
                deleteButton.onClick.AddListener(() => DeleteImage(currentPath, container));

                Debug.Log($"Added image from: {filePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error processing image {filePath}: {e.Message}");
            }
        }

        // Update grid layout
        GridLayoutGroup grid = libraryContent.GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            grid.cellSize = new Vector2(600, 700);
            grid.spacing = new Vector2(20, 20);
            grid.padding = new RectOffset(20, 20, 20, 20);
        }
    }

    private void DeleteImage(string filePath, GameObject imageContainer)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            capturedImagePaths.Remove(filePath);
            Destroy(imageContainer);
        }
    }

    public void CloseLibrary()
    {
        LibraryPanel.SetActive(false);
        captureButton.gameObject.SetActive(true);
        OpenLibraryButton.gameObject.SetActive(true);
    }
}