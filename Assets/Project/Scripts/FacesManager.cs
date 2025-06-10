using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;
using Unity.XR.CoreUtils;
using UnityEngine.XR.ARFoundation;


/// Subscribes to <see cref="ARFaceManager.facesChanged"/> event to update face data.
[RequireComponent(typeof(XROrigin))]
[DisallowMultipleComponent]
[DefaultExecutionOrder(ARUpdateOrder.k_FaceManager)]
public sealed class FacesManager : ARTrackableManager<XRFaceSubsystem,
                                                       XRFaceSubsystemDescriptor,
                                                       XRFaceSubsystem.Provider,
                                                       XRFace,
                                                       ARFace>
{
    public delegate void ARFaceUpdateEvent(ARFace face);
    public event ARFaceUpdateEvent OnARFaceUpdated;

    [SerializeField] private ARFace _debugFacePrefab;
    private ARFace _debugFace;

    [SerializeField]
    [Tooltip("The maximum number of faces to track simultaneously.")]
    private int m_MaximumFaceCount = 1;

    /// <summary>
    /// Get or set the requested maximum number of faces to track simultaneously
    /// </summary>
    public int requestedMaximumFaceCount
    {
        get => subsystem?.requestedMaximumFaceCount ?? m_MaximumFaceCount;
        set
        {
            m_MaximumFaceCount = value;
            if (enabled && subsystem != null)
            {
                subsystem.requestedMaximumFaceCount = value;
            }
        }
    }

    /// <summary>
    /// Get or set the maximum number of faces to track simultaneously. This method is obsolete.
    /// Use <see cref="currentMaximumFaceCount"/> or <see cref="requestedMaximumFaceCount"/> instead.
    /// </summary>
    [Obsolete("Use requestedMaximumFaceCount or currentMaximumFaceCount instead. (2020-01-14)")]
    public int maximumFaceCount
    {
        get => subsystem?.currentMaximumFaceCount ?? m_MaximumFaceCount;
        set => requestedMaximumFaceCount = value;
    }

    /// <summary>
    /// Get the maximum number of faces to track simultaneously.
    /// </summary>
    public int currentMaximumFaceCount => subsystem?.currentMaximumFaceCount ?? 0;

    /// <summary>
    /// Get the supported number of faces that can be tracked simultaneously. This value
    /// might change when the configuration changes.
    /// </summary>
    public int supportedFaceCount => subsystem?.supportedFaceCount ?? 0;

    /// <summary>
    /// Raised for each new <see cref="ARFace"/> detected in the environment.
    /// </summary>
    public event Action<ARFacesChangedEventArgs> facesChanged;

    // These are just for editor quality of life, so that the face isn't
    // always at an insane angle.
    private CameraManager _cameraManager;
    private GeniesManager _geniesManager;
    private bool _didInitialize = false;

    public void Initialize(CameraManager cameraManager, GeniesManager geniesManager)
    {
        _cameraManager = cameraManager;
        _geniesManager = geniesManager;

#if UNITY_EDITOR
        CreateDebugARFace();
        _geniesManager.OnFirstGenieInitialized += AlignDebugFaceWithCamera;
#endif
        _didInitialize = true;
    }

    protected override void OnDestroy()
    {
        if (_didInitialize)
        {
#if UNITY_EDITOR
            _geniesManager.OnFirstGenieInitialized -= AlignDebugFaceWithCamera;
#endif
        }
        base.OnDestroy();
    }

    protected override void Update()
    {
        base.Update();

#if UNITY_EDITOR
        OnARFaceUpdated?.Invoke(_debugFace);
        if (Input.GetKeyDown(KeyCode.F))
        {
            AlignDebugFaceWithCamera();
        }
#endif
    }

    private void CreateDebugARFace()
    {
        _debugFace = Instantiate(_debugFacePrefab);
        AlignDebugFaceWithCamera();
    }

    private void AlignDebugFaceWithCamera()
    {
        _debugFace.transform.forward = -_cameraManager.ActiveCamera.transform.forward;
        _debugFace.transform.position = _cameraManager.ActiveCamera.transform.position +
                                        _cameraManager.ActiveCamera.transform.forward;
        _debugFace.transform.LookAt(_cameraManager.ActiveCamera.transform.position, Vector3.up);
    }

    /// <summary>
    /// Attempts to retrieve an <see cref="ARFace"/>.
    /// </summary>
    /// <param name="faceId">The <c>TrackableId</c> associated with the <see cref="ARFace"/>.</param>
    /// <returns>The <see cref="ARFace"/>if found. <c>null</c> otherwise.</returns>
    public ARFace TryGetFace(TrackableId faceId)
    {
        m_Trackables.TryGetValue(faceId, out ARFace face);
        return face;
    }

    /// <summary>
    /// Invoked just before calling `Start` on the Subsystem. Used to set the `requestedMaximumFaceCount`
    /// on the subsystem.
    /// </summary>
    protected override void OnBeforeStart()
    {
        subsystem.requestedMaximumFaceCount = m_MaximumFaceCount;
    }

    /// <summary>
    /// Invoked just after a <see cref="ARFace"/> has been updated.
    /// </summary>
    /// <param name="face"></param>
    /// <param name="sessionRelativeData"></param>
    protected override void OnAfterSetSessionRelativeData(
        ARFace face,
        XRFace sessionRelativeData)
    {
        OnARFaceUpdated?.Invoke(face);
    }

    /// <summary>
    /// Invoked when the base class detects trackable changes.
    /// </summary>
    /// <param name="added">The list of added <see cref="ARFace"/>s.</param>
    /// <param name="updated">The list of updated <see cref="ARFace"/>s.</param>
    /// <param name="removed">The list of removed <see cref="ARFace"/>s.</param>
    protected override void OnTrackablesChanged(
        List<ARFace> added,
        List<ARFace> updated,
        List<ARFace> removed)
    {
        if (facesChanged != null)
        {
            using (new ScopedProfiler("OnFacesChanged"))
                facesChanged(new ARFacesChangedEventArgs(added, updated, removed));
        }
    }

    /// <summary>
    /// The name assigned to each `GameObject` belonging to each <see cref="ARFace"/>.
    /// </summary>
    protected override string gameObjectName => "ARFace";
}