using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RoomManager : MonoBehaviour
{
    public Canvas welcomeCanvas;
    public Button continueButton;
    public Button newRoomButton;
    public TMP_InputField roomNameInput;
    public TMP_Dropdown roomDropdown;
    public GameObject roomTemplate;

    private List<GameObject> rooms = new List<GameObject>();
    private GameObject activeRoom;

    private const string ROOM_NAMES_KEY = "RoomNames";
    private const string LAST_USED_ROOM_KEY = "LastUsedRoom";

    void Start()
    {
        // Make sure welcome canvas is visible at start
        welcomeCanvas.gameObject.SetActive(true);
        
        // Make sure room template is inactive at start
        if (roomTemplate != null)
        {
            roomTemplate.SetActive(false);
        }

        LoadRoomNames();
        if (HasExistingRooms())
        {
            continueButton.interactable = true;
            PopulateRoomDropdown();
            
            // Don't automatically activate the last used room
            // Just populate the dropdown with the last used room selected
            string lastUsedRoom = PlayerPrefs.GetString(LAST_USED_ROOM_KEY, "");
            if (!string.IsNullOrEmpty(lastUsedRoom) && RoomExists(lastUsedRoom))
            {
                int roomIndex = rooms.FindIndex(r => r.name == lastUsedRoom);
                if (roomIndex >= 0)
                {
                    roomDropdown.value = roomIndex;
                }
            }
            
            // Make sure all rooms are initially inactive
            foreach (GameObject room in rooms)
            {
                room.SetActive(false);
            }
        }
        else
        {
            continueButton.interactable = false;
            Debug.Log("No existing rooms found. Please create a new room.");
        }
    }

    // --- Room Management ---

    public void OnNewRoomButtonClicked()
    {
        Debug.Log("New Room Button Clicked");
        string newRoomName = roomNameInput.text.Trim();
        if (string.IsNullOrEmpty(newRoomName))
        {
            Debug.LogWarning("Room name cannot be empty");
            return;
        }
        
        if (RoomExists(newRoomName))
        {
            Debug.LogWarning("A room with this name already exists");
            return;
        }

        Debug.Log($"Creating new room: {newRoomName}");
        CreateRoom(newRoomName);
        SaveRoomNames();
        ActivateRoom(newRoomName);
        roomNameInput.text = ""; // Clear the input field
        
        Debug.Log($"Room count after creation: {rooms.Count}");
        Debug.Log("Room creation completed");
    }

    public void OnContinueButtonClicked()
    {
        string selectedRoomName = roomDropdown.options[roomDropdown.value].text;
        ActivateRoom(selectedRoomName);
    }

    public void OnDeleteRoomButtonClicked()
    {
        string roomToDelete = roomDropdown.options[roomDropdown.value].text;
        DeleteRoom(roomToDelete);
        SaveRoomNames();
        PopulateRoomDropdown();
    }

    private void CreateRoom(string roomName)
    {
        Debug.Log($"Creating room with name: {roomName}");
        GameObject newRoom = Instantiate(roomTemplate);
        newRoom.name = roomName;
        
        // Find and set up the room name text and buttons in the instantiated room
        TextMeshProUGUI[] texts = newRoom.GetComponentsInChildren<TextMeshProUGUI>();
        Button[] buttons = newRoom.GetComponentsInChildren<Button>();
        
        foreach (TextMeshProUGUI text in texts)
        {
            if (text.gameObject.name == "RoomNameText")
            {
                text.text = roomName;
            }
            else if (text.transform.parent.name == "CaptureButton")  // Check the button's name
            {
                text.text = "Capture Poster";
            }
            else if (text.transform.parent.name == "OpenLibraryButton")  // Check if it's the library button
            {
                text.text = "Open Gallery";
            }
        }

        rooms.Add(newRoom);
        newRoom.transform.SetParent(transform, false);
        Debug.Log($"Room {roomName} created successfully");
    }

    private void DeleteRoom(string roomName)
    {
        GameObject roomToDelete = rooms.Find(r => r.name == roomName);
        if (roomToDelete != null)
        {
            // Disable components before destroying
            CameraCapture cameraCapture = roomToDelete.GetComponent<CameraCapture>();
            if (cameraCapture != null)
            {
                cameraCapture.enabled = false;
            }

            rooms.Remove(roomToDelete);
            Destroy(roomToDelete);

            // Delete the room's saved image paths
            PlayerPrefs.DeleteKey(roomName + "_ImagePaths");
        }
    }

    private void ActivateRoom(string roomName)
    {
        Debug.Log($"Activating room: {roomName}");
        activeRoom = rooms.Find(r => r.name == roomName);
        PlayerPrefs.SetString(LAST_USED_ROOM_KEY, roomName);

        foreach (GameObject room in rooms)
        {
            room.SetActive(room == activeRoom);
            
            // Initialize camera when activating the room
            if (room == activeRoom)
            {
                CameraCapture cameraCapture = room.GetComponent<CameraCapture>();
                if (cameraCapture != null)
                {
                    cameraCapture.InitializeCamera();
                }
            }
        }

        welcomeCanvas.gameObject.SetActive(false);
    }

    // --- Saving and Loading ---

    private void SaveRoomNames()
    {
        string roomNames = string.Join(",", rooms.ConvertAll(r => r.name).ToArray());
        PlayerPrefs.SetString(ROOM_NAMES_KEY, roomNames);
    }

    private void LoadRoomNames()
    {
        string roomNames = PlayerPrefs.GetString(ROOM_NAMES_KEY, "");
        if (!string.IsNullOrEmpty(roomNames))
        {
            string[] roomNameArray = roomNames.Split(',');
            foreach (string roomName in roomNameArray)
            {
                if (!string.IsNullOrEmpty(roomName))
                {
                    CreateRoom(roomName);
                }
            }
        }
    }

    // --- Helper Methods ---

    private bool HasExistingRooms()
    {
        return rooms.Count > 0;
    }

    private bool RoomExists(string roomName)
    {
        return rooms.Find(r => r.name == roomName) != null;
    }

    private void PopulateRoomDropdown()
    {
        roomDropdown.ClearOptions();
        roomDropdown.AddOptions(rooms.ConvertAll(r => r.name));
    }
}