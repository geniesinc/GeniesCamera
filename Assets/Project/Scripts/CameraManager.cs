using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public enum ActiveCameraType
{
    XRCamera,
    ScreenspaceCamera
}

public class CameraManager : MonoBehaviour
{   
    public event Action<ActiveCameraType> OnActiveCameraTypeChanged;

    // Our default worldspace passthrough 3D AR camera (camera rgb input)
    [SerializeField] public Camera XRCamera;
    // The component it uses for light estimations (light input)
    [SerializeField] public ARCameraManager ARCameraManager;
    // Our orothographic screenspace 2d (sometimes passthrough) camera:
    [SerializeField] public Camera ScreenspaceCamera;
    [SerializeField] public Camera ScreenspaceCamera_forRecording;

    private ActiveCameraType _currentCameraType = ActiveCameraType.XRCamera;

    // The camera we are actively getting camera input from.
    public Camera ActiveCamera
    {
        get
        {
            return _currentCameraType switch
            {
                ActiveCameraType.XRCamera => XRCamera,
                ActiveCameraType.ScreenspaceCamera => ScreenspaceCamera,
                _ => throw new ArgumentOutOfRangeException(nameof(_currentCameraType), _currentCameraType, null),
            };
        }
    }

    // Beceause of how this is set up now, the appropriate camera to record render
    // textures from is not always the activeCamera.
    public Camera RecordingCamera
    {
        get
        {
            return _currentCameraType == ActiveCameraType.ScreenspaceCamera
                ? ScreenspaceCamera_forRecording
                : ActiveCamera;
        }
    }

    // The user can toggle into 2d/Screenspace mode from the main menu 
    public bool IsScreenspace { get { return _currentCameraType == ActiveCameraType.ScreenspaceCamera; } }

    // Toggles between ScreenSpace 2D camera and WorldSpace 3D camera
    public void SetActiveCameraType(ActiveCameraType newCameraType)
    {
        // No change needed
        if (_currentCameraType == newCameraType)
        {
            return;
        }

        // Toggle cameras
        ScreenspaceCamera.gameObject.SetActive(newCameraType == ActiveCameraType.ScreenspaceCamera);
        
        _currentCameraType = newCameraType;

        OnActiveCameraTypeChanged?.Invoke(_currentCameraType);
    }

    public static Vector2 GetIdealScreenPointForGeniePlacement()
    {
        // This tends to put the genie in view a comfortable distance from the user
        // with a good viewing ratio
        return new Vector2(Screen.width / 2f, Screen.height / 3f);
    }

}
