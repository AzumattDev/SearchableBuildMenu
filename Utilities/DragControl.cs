using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SearchableBuildMenu.Utilities;

public class DragWindowCntrl : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private float size;

    /// <summary>
    /// Current scale which is used to keep track whether it is within boundaries
    /// </summary>
    private float _scale = 1f;

    public static void ApplyDragWindowCntrl(GameObject go)
    {
        go.AddComponent<DragWindowCntrl>();
    }

    private RectTransform _window;

    //delta drag
    private Vector2 _delta;

    private Image draggingImage;

    private void Awake()
    {
        _window = (RectTransform)transform;

        // Add an image that blocks for dragging
        draggingImage = Utils.FindChild(transform, "DraggingImage")?.GetComponent<Image>();
        if (draggingImage == null) return;
        draggingImage.enabled = false;
    }

    private void Start()
    {
        Vector2 loadedPosition;
        if (PlayerPrefs.HasKey("SearchableBuildMenuSearchBoxPosition"))
        {
            string savedPosition = PlayerPrefs.GetString("SearchableBuildMenuSearchBoxPosition");
            loadedPosition = HudAwakePatch.StringToVector2(savedPosition);
        }
        else
        {
            loadedPosition = new Vector2(-10.0f, 35.0f); // Default position
        }

        // Adjust if loaded position is off-screen
        loadedPosition = GetCorrectedPositionIfOffScreen(loadedPosition);
        _window.anchoredPosition = loadedPosition;
        size = _window.sizeDelta.x;
        _scale = _window.localScale.x;
    }

    private void Update()
    {
        SearchableBuildMenuPlugin.BuildSearchInputField.interactable = !Input.GetKey(KeyCode.LeftControl);
        if (draggingImage != null)
            draggingImage.enabled = Input.GetKey(KeyCode.LeftControl);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_window.parent as RectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localMousePosition);
        _delta = localMousePosition - _window.anchoredPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Calculate the new position in screen space, but convert it to local space for the RectTransform
        Vector2 localPoint;
        RectTransform? canvasRectTransform = _window.parent as RectTransform; // Assuming the direct parent is the canvas or a container within it.
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            Vector2 adjustedLocalPoint = localPoint - _delta;
            _window.anchoredPosition = adjustedLocalPoint;
        }
    }


    public void OnEndDrag(PointerEventData eventData)
    {
        Vector2 correctedPosition = GetCorrectedPositionIfOffScreen(_window.anchoredPosition);
        _window.anchoredPosition = correctedPosition;
        PlayerPrefs.SetString("SearchableBuildMenuSearchBoxPosition", correctedPosition.ToString());
        PlayerPrefs.Save();
    }


    // Method to determine if the position is off-screen and return a corrected position
    private Vector2 GetCorrectedPositionIfOffScreen(Vector2 position)
    {
        // Assuming the canvas is using Screen Space - Overlay and has a CanvasScaler
        CanvasScaler canvasScaler = FindObjectOfType<CanvasScaler>();
        float screenWidth = Screen.width / canvasScaler.scaleFactor;
        float screenHeight = Screen.height / canvasScaler.scaleFactor;

        // Calculate half sizes for easier boundary checks
        var rect = _window.rect;
        float halfWidth = rect.width / 2;
        float halfHeight = rect.height / 2;

        // Default position reset (center or any preferred default location)
        Vector2 defaultPosition = new Vector2(-10.0f, 35.0f);

        // Check if the UI element is entirely off any screen edge
        bool isOffScreen = position.x + halfWidth < 0 || position.x - halfWidth > screenWidth ||
                           position.y + halfHeight < 0 || position.y - halfHeight > screenHeight;

        // Return default position if off-screen, otherwise return the original position
        return isOffScreen ? defaultPosition : position;
    }
}