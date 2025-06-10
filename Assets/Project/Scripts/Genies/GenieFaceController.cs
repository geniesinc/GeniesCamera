using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARKit;
using Unity.Collections;

public class GenieFaceController : MonoBehaviour
{
    // Gen13 Genies only have one of these, but our Legacy "offline genies"
    // have multiple skinned meshes, each of which has a subset of blendshapes.
    private SkinnedMeshRenderer[] _skinnedMeshRenderers;
    // A list of dictionaries, one for each skinned mesh renderer.
    private List<Dictionary<ARKitBlendShapeLocation, List<int>>> _blendshapeMappingsPerMesh;
    private ARKitFaceSubsystem _faceSubsystem;
    private float _blendshapeScale = 100f; // default

    // Genies uses these specific blendshapes in a departure from ARKit:
    const string _cheekPuffLeft_Gen13 = "CHEEK_PUFF_L";
    const string _cheekPuffRight_Gen13 = "CHEEK_PUFF_R";
    const string _browInnerUpRight_Gen13 = "INNER_BROW_RAISER_R";
    const string _browInnerUpLeft_Gen13 = "INNER_BROW_RAISER_L";
    const string _mouthPuckerLeft_Gen13 = "MOUTH_PUCKER_L";
    const string _mouthPuckerRight_Gen13 = "MOUTH_PUCKER_R";
    const string _mouthFunnelUpperL_Gen13 = "MOUTH_FUNNELER_LT";
    const string _mouthFunnelUpperR_Gen13 = "MOUTH_FUNNELER_RT";
    const string _mouthFunnelLowerL_Gen13 = "MOUTH_FUNNELER_LB";
    const string _mouthFunnelLowerR_Gen13 = "MOUTH_FUNNELER_RB";
    const string _mouthRollLowerR_Gen13 = "LIP_SUCK_RB";
    const string _mouthRollLowerL_Gen13 = "LIP_SUCK_LB";
    const string _mouthRollUpperR_Gen13 = "LIP_SUCK_RT";
    const string _mouthRollUpperL_Gen13 = "LIP_SUCK_LT";


    // Prev gen (Rumor, Aria, etc)
    const string _cheekPuffLeft_Gen12 = "CheekPuffLeft";
    const string _cheekPuffRight_Gen12 = "CheekPuffRight";
    const string _browInnerUpRight_Gen12 = "BrowInnerUpRight";
    const string _browInnerUpLeft_Gen12 = "BrowInnerUpLeft";

    BlendshapeGaze _gazeCurrent = new BlendshapeGaze();

    private Dictionary<ARKitBlendShapeLocation, float> _maxVals =
                        new Dictionary<ARKitBlendShapeLocation, float>();

    private float _lookInMaxVal = 1f;
    private float _lookOutMaxVal = 1f;
    private float _lookUpMaxVal = 1f;
    private float _lookDownMaxVal = 1f;

    // Absolute value min/max for looking up (-30) and down (30), same as
    // absolute value min/max for looking left (-30) and right (30).
    private const float GAZE_MAX_ROM_IN_DEGREES = 30f;

    public void Initialize(SkinnedMeshRenderer[] skinnedMeshRenderers,
                            ARKitFaceSubsystem faceSubsystem,
                            float blendshapeScale,
                            float lookInMaxVal = 1f,
                            float lookOutMaxVal = 1f,
                            float lookUpMaxVal = 1f,
                            float lookDownMaxVal = 1f)
    {
        _skinnedMeshRenderers = skinnedMeshRenderers;
        _faceSubsystem = faceSubsystem;
        _blendshapeScale = blendshapeScale;

        _lookInMaxVal = lookInMaxVal;
        _lookOutMaxVal = lookOutMaxVal;
        _lookUpMaxVal = lookUpMaxVal;
        _lookDownMaxVal = lookDownMaxVal;

        // Setuup mapping for each Skinned Mesh
        _blendshapeMappingsPerMesh = new List<Dictionary<ARKitBlendShapeLocation, List<int>>>();
        for(int i=0; i < skinnedMeshRenderers.Length; i++)
        {
            var mappings = new Dictionary<ARKitBlendShapeLocation, List<int>>();
            // Iterate over enums in ARKitBlendShapeLocation and add them to the dictionary
            foreach (ARKitBlendShapeLocation location in System.Enum.GetValues(typeof(ARKitBlendShapeLocation)))
            {
                mappings[location] = new List<int>();
            }            
            _blendshapeMappingsPerMesh.Add(mappings);
        }

        MapMaxBlendshapeVals();

        MapBlendshapes();
    }

    private void MapMaxBlendshapeVals()
    {
        // Calibrate max values against Genies available Range of Motion
        _maxVals[ARKitBlendShapeLocation.BrowDownLeft] = 0.5f;
        _maxVals[ARKitBlendShapeLocation.BrowDownRight] = 0.5f;
        _maxVals[ARKitBlendShapeLocation.BrowInnerUp] = 0.5f;
        _maxVals[ARKitBlendShapeLocation.JawOpen] = 0.5f;
        _maxVals[ARKitBlendShapeLocation.CheekPuff] = 0.5f;
        _maxVals[ARKitBlendShapeLocation.MouthFunnel] = 0.5f;
        _maxVals[ARKitBlendShapeLocation.MouthPucker] = 0.5f;
        // Avoid the "Kubrick stare"
        _maxVals[ARKitBlendShapeLocation.EyeLookUpLeft] = Mathf.Min(0.75f, _lookUpMaxVal);
        _maxVals[ARKitBlendShapeLocation.EyeLookUpRight] = Mathf.Min(0.75f, _lookUpMaxVal);
        if(_lookUpMaxVal > 0.75f && _lookUpMaxVal < 1f) // 1f is the default.
        {
            Debug.LogWarning("Genie's EyeLookUp blendshape is capped at 0.75f. " +
                             "You have specified this Genie should have " + _lookUpMaxVal +
                             " which is currently not supported."); // for no good reason, lol?
        }

        // Bishop Briggs is out of control
        _maxVals[ARKitBlendShapeLocation.EyeLookDownLeft] = _lookDownMaxVal;
        _maxVals[ARKitBlendShapeLocation.EyeLookDownRight] = _lookDownMaxVal;
        _maxVals[ARKitBlendShapeLocation.EyeLookInLeft] = _lookInMaxVal;
        _maxVals[ARKitBlendShapeLocation.EyeLookInRight] = _lookInMaxVal;
        _maxVals[ARKitBlendShapeLocation.EyeLookOutLeft] = _lookOutMaxVal;
        _maxVals[ARKitBlendShapeLocation.EyeLookOutRight] = _lookOutMaxVal;
    }

    private void MapBlendshapes()
    {
        // There are some blendshapes that won't be matched, so we will track those too and alert the dev.
        List<string> unmappedGeniesShapes = new List<string>();

        // Iterate over list of skinned meshes
        for (int i = 0; i < _skinnedMeshRenderers.Length; i++)
        {
            int blendshapeCount = _skinnedMeshRenderers[i].sharedMesh.blendShapeCount;
            for (int j = 0; j < blendshapeCount; j++)
            {
                string geniesName = _skinnedMeshRenderers[i].sharedMesh.GetBlendShapeName(j);

                // Gen13 Support: Based on AndroidXR names / FACS
                // ex) headOnly_geo_blendShape.LIPS_TOWARD
                string[] namespaceTokens = geniesName.Split('.');
                geniesName = namespaceTokens[namespaceTokens.Length - 1];
                // Gen12 Support: Capitalize the first character
                // ex) headOnly_geo_blendShape.normalArkitName
                geniesName = char.ToUpper(geniesName[0]) + geniesName.Substring(1);

                // Special handling needed for blendshapes that are higher fidelity than
                // what ARKit can offer.
                if (geniesName == _cheekPuffLeft_Gen13 || geniesName == _cheekPuffLeft_Gen12 ||
                    geniesName == _cheekPuffRight_Gen13 || geniesName == _cheekPuffRight_Gen12)
                {
                    _blendshapeMappingsPerMesh[i][ARKitBlendShapeLocation.CheekPuff].Add(j);
                }
                else if (geniesName == _browInnerUpRight_Gen13 || geniesName == _browInnerUpRight_Gen12 ||
                         geniesName == _browInnerUpLeft_Gen13 || geniesName == _browInnerUpLeft_Gen12)
                {
                    _blendshapeMappingsPerMesh[i][ARKitBlendShapeLocation.BrowInnerUp].Add(j);
                }
                else if (geniesName == _mouthPuckerLeft_Gen13 || geniesName == _mouthPuckerRight_Gen13)
                {
                    _blendshapeMappingsPerMesh[i][ARKitBlendShapeLocation.MouthPucker].Add(j);
                }
                else if (geniesName == _mouthFunnelUpperL_Gen13 || geniesName == _mouthFunnelUpperR_Gen13 ||
                         geniesName == _mouthFunnelLowerL_Gen13 || geniesName == _mouthFunnelLowerR_Gen13)
                {
                    _blendshapeMappingsPerMesh[i][ARKitBlendShapeLocation.MouthFunnel].Add(j);
                }
                else if (geniesName == _mouthRollLowerR_Gen13 || geniesName == _mouthRollLowerL_Gen13)
                {
                    _blendshapeMappingsPerMesh[i][ARKitBlendShapeLocation.MouthRollLower].Add(j);
                }
                else if (geniesName == _mouthRollUpperR_Gen13 || geniesName == _mouthRollUpperL_Gen13)
                {
                    _blendshapeMappingsPerMesh[i][ARKitBlendShapeLocation.MouthRollUpper].Add(j);
                }

                // Potentially mappable
                else
                {
                    if (GeniesBlendshapeMapping.Map.TryGetValue(geniesName, out ARKitBlendShapeLocation whichEnum))
                    {
                        _blendshapeMappingsPerMesh[i][whichEnum].Add(j);
                    }
                    else
                    {
                        if (!unmappedGeniesShapes.Contains(geniesName))
                        {
                            unmappedGeniesShapes.Add(geniesName);
                        }
                    }
                }
            }
        }

        // Debugging
        /*int totalMappedBlendshapes = 0;
        foreach (var meshBlendshapeMap in _blendshapeMappingsPerMesh)
        {
            foreach (var entry in meshBlendshapeMap)
            {
                totalMappedBlendshapes += entry.Value.Count;
            }
        }
        Debug.Log($"{transform.name}: Mapped {totalMappedBlendshapes} total blendshapes for " +
                        $"{_blendshapeMappingsPerMesh.Count} meshes.", transform);

        // Did Genies make random extra ones?
        if (unmappedGeniesShapes.Count > 0)
        {
            Debug.Log($"Unmapped Genies Blendshapes " + 
            $"({unmappedGeniesShapes.Count}): {System.String.Join(", ", unmappedGeniesShapes)}");
        }*/
    }

    private float GetCalibratedValue(ARKitBlendShapeLocation blendShapeLocation, float blendshapeValue)
    {
        /*if (_maxVals.ContainsKey(blendShapeLocation))
        {
            return Mathf.Lerp(0f, _maxVals[blendShapeLocation], blendshapeValue);
        }*/
        return blendshapeValue;
    }

    // CALCULTE BLENDSHAPE VALUES FOR EYECONTACT WITH CAMERA
    private BlendshapeGaze GetBlendshapeGazeAtCamera(Transform eyesMidPoint)
    {
        // What is the rotation associated with the look vector from the avatar's eyes to the camera?
        Vector3 lookAtCamDirWorld = 
                Vector3.Normalize(AppManager.ActiveCamera.transform.position -
                                  eyesMidPoint.position);

        // Using the headTransform's world position, get the local direction to the camera.
        // This is the direction the eyes should be looking in local space.
        Vector3 lookAtCamDirLocal = eyesMidPoint.InverseTransformDirection(lookAtCamDirWorld);
        Vector3 lookAtCamEulerLocal = Quaternion.LookRotation(lookAtCamDirLocal, Vector3.up).eulerAngles;

        float newEyePitch = lookAtCamEulerLocal.x.GetSmallestEulerValue();
        float newEyeYaw = lookAtCamEulerLocal.y.GetSmallestEulerValue();

        // Get values for looking into the cam and store them in this object
        BlendshapeGaze gazeLookAtCam = new BlendshapeGaze();

        // If you wanna be looking UP (or perfectly straight)
        if (newEyePitch <= 0)
        {
            float eyeLookUp = Mathf.Clamp01(Mathf.Abs(newEyePitch / GAZE_MAX_ROM_IN_DEGREES));
            eyeLookUp = Mathf.Min(eyeLookUp, _lookUpMaxVal);
            gazeLookAtCam.blendshapeValues[ARKitBlendShapeLocation.EyeLookUpLeft] = eyeLookUp;
            gazeLookAtCam.blendshapeValues[ARKitBlendShapeLocation.EyeLookUpRight] = eyeLookUp;
            gazeLookAtCam.blendshapeValues[ARKitBlendShapeLocation.EyeLookDownLeft] = 0;
            gazeLookAtCam.blendshapeValues[ARKitBlendShapeLocation.EyeLookDownRight] = 0;
        }
        // If you are looking DOWN
        else
        {
            float eyeLookDown = Mathf.Clamp01(Mathf.Abs(newEyePitch / GAZE_MAX_ROM_IN_DEGREES));
            eyeLookDown = Mathf.Min(eyeLookDown, _lookDownMaxVal);
            gazeLookAtCam.blendshapeValues[ARKitBlendShapeLocation.EyeLookDownLeft] = eyeLookDown;
            gazeLookAtCam.blendshapeValues[ARKitBlendShapeLocation.EyeLookDownRight] = eyeLookDown;
            gazeLookAtCam.blendshapeValues[ARKitBlendShapeLocation.EyeLookUpLeft] = 0;
            gazeLookAtCam.blendshapeValues[ARKitBlendShapeLocation.EyeLookUpRight] = 0;
        }
        // If you wanna look LEFT
        if(newEyeYaw <= 0)
        {
            float eyeLookOut = Mathf.Clamp01(Mathf.Abs(newEyeYaw / GAZE_MAX_ROM_IN_DEGREES));
            eyeLookOut = Mathf.Min(eyeLookOut, _lookOutMaxVal);
            gazeLookAtCam.blendshapeValues[ARKitBlendShapeLocation.EyeLookOutLeft] = eyeLookOut;
            gazeLookAtCam.blendshapeValues[ARKitBlendShapeLocation.EyeLookInRight] = eyeLookOut;
            gazeLookAtCam.blendshapeValues[ARKitBlendShapeLocation.EyeLookInLeft] = 0f;
            gazeLookAtCam.blendshapeValues[ARKitBlendShapeLocation.EyeLookOutRight] = 0;
        }
        // If you wanna look RIGHT
        else
        {
            float eyeLookIn = Mathf.Clamp01(Mathf.Abs(newEyeYaw / GAZE_MAX_ROM_IN_DEGREES));
            eyeLookIn = Mathf.Min(eyeLookIn, _lookInMaxVal);
            gazeLookAtCam.blendshapeValues[ARKitBlendShapeLocation.EyeLookOutRight] = eyeLookIn;
            gazeLookAtCam.blendshapeValues[ARKitBlendShapeLocation.EyeLookInLeft] = eyeLookIn;
            gazeLookAtCam.blendshapeValues[ARKitBlendShapeLocation.EyeLookInRight] = 0;
            gazeLookAtCam.blendshapeValues[ARKitBlendShapeLocation.EyeLookOutLeft] = 0;
        }

        // Remap blendshapes
        for (int i = 0; i < gazeLookAtCam.blendshapeValues.Count; i++)
        {
            KeyValuePair<ARKitBlendShapeLocation, float> entry = gazeLookAtCam.blendshapeValues.ElementAt(i);
            gazeLookAtCam.blendshapeValues[entry.Key] = GetCalibratedValue(entry.Key, entry.Value);
        }   

        return gazeLookAtCam;
    }

    public void ApplyLookAtCameraGaze(Transform eyesMidPoint)
    {
        var gazeAtCamera = GetBlendshapeGazeAtCamera(eyesMidPoint);
        ApplyBlendshapeGaze(gazeAtCamera);
    }

    public void ApplyCurrentGaze()
    {
        ApplyBlendshapeGaze(_gazeCurrent);
    }

    private void ApplyBlendshapeGaze(BlendshapeGaze blendshapeGaze)
    {
        // Apply value to each skinnedmesh renderer
        for (int i = 0; i < _skinnedMeshRenderers.Length; i++)
        {
            // Iterate over EyeLookInLeft, EyeLookInRight, EyeLookOutLeft, ...
            foreach(var gazeBlendshapeLocation in BlendshapeGaze.locations)
            {
                // For each instance of that blendshape on the SkinnedMeshRenderer (because Genies are
                // a runtime composite of several SkinnedMeshRenderers, it migrates all corresponding blendshapes
                // onto a single composite skinned mesh renderer...)
                for (int j = 0; j < _blendshapeMappingsPerMesh[i][gazeBlendshapeLocation].Count; j++)
                {
                    // Apply value to blendshape
                    _skinnedMeshRenderers[i].SetBlendShapeWeight(
                                    _blendshapeMappingsPerMesh[i][gazeBlendshapeLocation][j],
                                    blendshapeGaze.blendshapeValues[gazeBlendshapeLocation] * _blendshapeScale);
                    //Debug.Log($"Set Blendshape of index ({i}, {j}) {genieBlendshapeMaps[i].arkitEnumToGeniesShapeIdx[arkitGazeBlendShapeLocation][j]} with value {blendshapeGaze.blendshapeValues[arkitGazeBlendShapeLocation] * blendshapeScale}");
                }
            }
        }
    }

    // APPLY FACIAL BLENDSHAPE DATA
    public void ApplyMocapData(ARFace arFace)
    {
        // Grab the latest face blendshape data and apply it to our skinned mesh renderer!
#if !UNITY_EDITOR

        // Saw repeated Null References in the build while I left my phone camera-down
        // on my desk. So, arFace or faceSubsystem I suppose can be null!
        if (arFace == null || _faceSubsystem == null)
        {
            return;
        }

        string debugString = "Blendshape Coefficients:\n";
        using (NativeArray<ARKitBlendShapeCoefficient> blendShapeCoeffs =
            _faceSubsystem.GetBlendShapeCoefficients(arFace.trackableId, Allocator.Temp))
        {
            foreach (var blendShapeCoeff in blendShapeCoeffs)
            {
                float blendshapeValue =
                    GetCalibratedValue(blendShapeCoeff.blendShapeLocation, blendShapeCoeff.coefficient);

                // Unexpected, but ToString(#.##) yields an empty string if value is zero.
                string a = blendShapeCoeff.coefficient == 0 ? "0" : blendShapeCoeff.coefficient.ToString("#.##");
                string b = blendshapeValue == 0 ? "0" : blendshapeValue.ToString("#.##");
                debugString +=
                    $"{blendShapeCoeff.blendShapeLocation}: {a} -> {b}\n";

                // Store gaze vars so as to handle offset gaze in the same way we did the offset head rotation.
                if(blendShapeCoeff.blendShapeLocation.IsGazeBlendshape())
                {
                    _gazeCurrent.blendshapeValues[blendShapeCoeff.blendShapeLocation] = blendshapeValue;
                }
                // Blendshape is not Gaze related
                else
                {
                    // Apply value to each skinnedmesh renderer
                    for (int i = 0; i < _skinnedMeshRenderers.Length; i++)
                    {
                        for(int j=0; j < _blendshapeMappingsPerMesh[i][blendShapeCoeff.blendShapeLocation].Count; j++)
                            {
                                _skinnedMeshRenderers[i].SetBlendShapeWeight(
                                    _blendshapeMappingsPerMesh[i][blendShapeCoeff.blendShapeLocation][j],
                                                                    blendshapeValue * _blendshapeScale);
                            }
                    }
                }
            }
        }

        //AppManager.Instance.mainMenuController.debugDisplayController.SetText(debugString);

#else
        // Apply random facial anim to help notice if stuff is working
        if(Input.GetKey(KeyCode.B))
        {
            for (int i = 0; i < _skinnedMeshRenderers.Length; i++)
            {
                //Debug.Log($"This mesh has {genieBlendshapeMaps[i].arkitEnumToGeniesShapeIdx.Count} blendshapes.");
                foreach (ARKitBlendShapeLocation location in System.Enum.GetValues(typeof(ARKitBlendShapeLocation)))
                {
                    List<int> blendshapeIndices = _blendshapeMappingsPerMesh[i][location];
                    //Debug.Log($"Looking through {value.Count} indices mapped to {key} key on {skinnedMeshRenderers[i].name}");
                    for (int j = 0; j < blendshapeIndices.Count; j++)
                    {
                        //Debug.Log($"Set weight: {skinnedMeshRenderers[i].name}, {value[j]}, {(Time.time % 1f) * blendshapeScale}");
                        float val = GetCalibratedValue(location, Mathf.PingPong(Time.time, 1f)) * _blendshapeScale;
                        _skinnedMeshRenderers[i].SetBlendShapeWeight(blendshapeIndices[j], val);
                    } 
                }  
            }    
        }
#endif
    }
}

public class BlendshapeGaze
{
    public static ARKitBlendShapeLocation[] locations = new ARKitBlendShapeLocation[]
    {
        ARKitBlendShapeLocation.EyeLookDownLeft,
        ARKitBlendShapeLocation.EyeLookDownRight,
        ARKitBlendShapeLocation.EyeLookInLeft,
        ARKitBlendShapeLocation.EyeLookInRight,
        ARKitBlendShapeLocation.EyeLookOutLeft,
        ARKitBlendShapeLocation.EyeLookOutRight,
        ARKitBlendShapeLocation.EyeLookUpLeft,
        ARKitBlendShapeLocation.EyeLookUpRight
    };

    public Dictionary<ARKitBlendShapeLocation, float> blendshapeValues;

    public BlendshapeGaze()
    {
        this.blendshapeValues = new Dictionary<ARKitBlendShapeLocation, float>();
        foreach (var location in locations)
        {
            this.blendshapeValues[location] = 0f;
        }
    }
}

public static class GeniesBlendshapeMapping
{
    public static readonly IReadOnlyDictionary<string, ARKitBlendShapeLocation> Map =
            new Dictionary<string, ARKitBlendShapeLocation>
    {
        // Gen 13
        { "EYES_CLOSED_L", ARKitBlendShapeLocation.EyeBlinkLeft },
        { "EYES_CLOSED_R", ARKitBlendShapeLocation.EyeBlinkRight },
        { "EYES_LOOK_DOWN_L", ARKitBlendShapeLocation.EyeLookDownLeft },
        { "EYES_LOOK_DOWN_R", ARKitBlendShapeLocation.EyeLookDownRight },
        { "EYES_LOOK_LEFT_L", ARKitBlendShapeLocation.EyeLookOutLeft },
        { "EYES_LOOK_LEFT_R", ARKitBlendShapeLocation.EyeLookInRight },
        { "EYES_LOOK_RIGHT_L", ARKitBlendShapeLocation.EyeLookInLeft },
        { "EYES_LOOK_RIGHT_R", ARKitBlendShapeLocation.EyeLookOutRight },
        { "EYES_LOOK_UP_L", ARKitBlendShapeLocation.EyeLookUpLeft },
        { "EYES_LOOK_UP_R", ARKitBlendShapeLocation.EyeLookUpRight },
        { "LID_TIGHTENER_L", ARKitBlendShapeLocation.EyeSquintLeft },
        { "LID_TIGHTENER_R", ARKitBlendShapeLocation.EyeSquintRight },
        { "UPPER_LID_RAISER_L", ARKitBlendShapeLocation.EyeWideLeft },
        { "UPPER_LID_RAISER_R", ARKitBlendShapeLocation.EyeWideRight },
        { "BROW_LOWERER_L", ARKitBlendShapeLocation.BrowDownLeft },
        { "BROW_LOWERER_R", ARKitBlendShapeLocation.BrowDownRight },
        { "INNER_BROW_RAISER_L", ARKitBlendShapeLocation.BrowInnerUp },
        { "OUTER_BROW_RAISER_L", ARKitBlendShapeLocation.BrowOuterUpLeft },
        { "OUTER_BROW_RAISER_R", ARKitBlendShapeLocation.BrowOuterUpRight },
        { "NOSE_WRINKLER_L", ARKitBlendShapeLocation.NoseSneerLeft },
        { "NOSE_WRINKLER_R", ARKitBlendShapeLocation.NoseSneerRight },
        { "CHEEK_PUFF_L", ARKitBlendShapeLocation.CheekPuff },
        { "CHEEK_RAISER_L", ARKitBlendShapeLocation.CheekSquintLeft },
        { "CHEEK_RAISER_R", ARKitBlendShapeLocation.CheekSquintRight },
        { "JAW_DROP", ARKitBlendShapeLocation.JawOpen },
        { "JAW_SIDEWAYS_LEFT", ARKitBlendShapeLocation.JawLeft },
        { "JAW_SIDEWAYS_RIGHT", ARKitBlendShapeLocation.JawRight },
        { "JAW_THRUST", ARKitBlendShapeLocation.JawForward },
        { "CHIN_RAISER_B", ARKitBlendShapeLocation.MouthShrugLower },
        { "CHIN_RAISER_T", ARKitBlendShapeLocation.MouthShrugUpper },
        { "MOUTH_LEFT", ARKitBlendShapeLocation.MouthLeft },
        { "MOUTH_RIGHT", ARKitBlendShapeLocation.MouthRight },
        { "DIMPLER_L", ARKitBlendShapeLocation.MouthDimpleLeft },
        { "DIMPLER_R", ARKitBlendShapeLocation.MouthDimpleRight },
        { "LIP_FUNNELER_LB", ARKitBlendShapeLocation.MouthFunnel },
        { "LIP_FUNNELER_LT", ARKitBlendShapeLocation.MouthFunnel },
        { "LIP_FUNNELER_RB", ARKitBlendShapeLocation.MouthFunnel },
        { "LIP_FUNNELER_RT", ARKitBlendShapeLocation.MouthFunnel },
        { "LIP_PRESSOR_L", ARKitBlendShapeLocation.MouthPressRight },
        { "LIP_PRESSOR_R", ARKitBlendShapeLocation.MouthPressLeft },
        { "LIP_PUCKER_L", ARKitBlendShapeLocation.MouthPucker },
        { "LIP_PUCKER_R", ARKitBlendShapeLocation.MouthPucker },
        { "LIP_STRETCHER_L", ARKitBlendShapeLocation.MouthStretchLeft },
        { "LIP_STRETCHER_R", ARKitBlendShapeLocation.MouthStretchRight },
        { "LIPS_TOWARD", ARKitBlendShapeLocation.MouthClose },
        { "LOWER_LIP_DEPRESSOR_L", ARKitBlendShapeLocation.MouthLowerDownLeft },
        { "LOWER_LIP_DEPRESSOR_R", ARKitBlendShapeLocation.MouthLowerDownRight },
        { "UPPER_LIP_RAISER_L", ARKitBlendShapeLocation.MouthUpperUpLeft },
        { "UPPER_LIP_RAISER_R", ARKitBlendShapeLocation.MouthUpperUpRight },
        { "LIP_CORNER_DEPRESSOR_L", ARKitBlendShapeLocation.MouthFrownLeft },
        { "LIP_CORNER_DEPRESSOR_R", ARKitBlendShapeLocation.MouthFrownRight },
        { "LIP_CORNER_PULLER_L", ARKitBlendShapeLocation.MouthSmileLeft },
        { "LIP_CORNER_PULLER_R", ARKitBlendShapeLocation.MouthSmileRight },
        { "LIP_SUCK_LT", ARKitBlendShapeLocation.MouthRollUpper },
        { "LIP_SUCK_RT", ARKitBlendShapeLocation.MouthRollUpper },
        { "LIP_SUCK_LB", ARKitBlendShapeLocation.MouthRollLower },
        { "LIP_SUCK_RB", ARKitBlendShapeLocation.MouthRollLower },
        { "TONGUE_OUT", ARKitBlendShapeLocation.TongueOut },
        // Gen 12
        { "EyeBlinkLeft", ARKitBlendShapeLocation.EyeBlinkLeft },
        { "EyeBlinkRight", ARKitBlendShapeLocation.EyeBlinkRight },
        { "EyeLookDownLeft", ARKitBlendShapeLocation.EyeLookDownLeft },
        { "EyeLookDownRight", ARKitBlendShapeLocation.EyeLookDownRight },
        { "EyeLookOutLeft", ARKitBlendShapeLocation.EyeLookOutLeft },
        { "EyeLookInRight", ARKitBlendShapeLocation.EyeLookInRight },
        { "EyeLookInLeft", ARKitBlendShapeLocation.EyeLookInLeft },
        { "EyeLookOutRight", ARKitBlendShapeLocation.EyeLookOutRight },
        { "EyeLookUpLeft", ARKitBlendShapeLocation.EyeLookUpLeft },
        { "EyeLookUpRight", ARKitBlendShapeLocation.EyeLookUpRight },
        { "EyeSquintLeft", ARKitBlendShapeLocation.EyeSquintLeft },
        { "EyeSquintRight", ARKitBlendShapeLocation.EyeSquintRight },
        { "EyeWideLeft", ARKitBlendShapeLocation.EyeWideLeft },
        { "EyeWideRight", ARKitBlendShapeLocation.EyeWideRight },
        { "BrowDownLeft", ARKitBlendShapeLocation.BrowDownLeft },
        { "BrowDownRight", ARKitBlendShapeLocation.BrowDownRight },
        { "BrowInnerUp", ARKitBlendShapeLocation.BrowInnerUp },
        { "BrowOuterUpLeft", ARKitBlendShapeLocation.BrowOuterUpLeft },
        { "BrowOuterUpRight", ARKitBlendShapeLocation.BrowOuterUpRight },
        { "NoseSneerLeft", ARKitBlendShapeLocation.NoseSneerLeft },
        { "NoseSneerRight", ARKitBlendShapeLocation.NoseSneerRight },
        { "CheekPuff", ARKitBlendShapeLocation.CheekPuff },
        { "CheekSquintLeft", ARKitBlendShapeLocation.CheekSquintLeft },
        { "CheekSquintRight", ARKitBlendShapeLocation.CheekSquintRight },
        { "JawOpen", ARKitBlendShapeLocation.JawOpen },
        { "JawLeft", ARKitBlendShapeLocation.JawLeft },
        { "JawRight", ARKitBlendShapeLocation.JawRight },
        { "JawForward", ARKitBlendShapeLocation.JawForward },
        { "MouthShrugLower", ARKitBlendShapeLocation.MouthShrugLower },
        { "MouthShrugUpper", ARKitBlendShapeLocation.MouthShrugUpper },
        { "MouthLeft", ARKitBlendShapeLocation.MouthLeft },
        { "MouthRight", ARKitBlendShapeLocation.MouthRight },
        { "MouthDimpleLeft", ARKitBlendShapeLocation.MouthDimpleLeft },
        { "MouthDimpleRight", ARKitBlendShapeLocation.MouthDimpleRight },
        { "MouthFunnel", ARKitBlendShapeLocation.MouthFunnel },
        { "MouthPressRight", ARKitBlendShapeLocation.MouthPressRight },
        { "MouthPressLeft", ARKitBlendShapeLocation.MouthPressLeft },
        { "MouthPucker", ARKitBlendShapeLocation.MouthPucker },
        { "MouthStretchLeft", ARKitBlendShapeLocation.MouthStretchLeft },
        { "MouthStretchRight", ARKitBlendShapeLocation.MouthStretchRight },
        { "MouthClose", ARKitBlendShapeLocation.MouthClose },
        { "MouthLowerDownLeft", ARKitBlendShapeLocation.MouthLowerDownLeft },
        { "MouthLowerDownRight", ARKitBlendShapeLocation.MouthLowerDownRight },
        { "MouthUpperUpLeft", ARKitBlendShapeLocation.MouthUpperUpLeft },
        { "MouthUpperUpRight", ARKitBlendShapeLocation.MouthUpperUpRight },
        { "MouthFrownLeft", ARKitBlendShapeLocation.MouthFrownLeft },
        { "MouthFrownRight", ARKitBlendShapeLocation.MouthFrownRight },
        { "MouthSmileLeft", ARKitBlendShapeLocation.MouthSmileLeft },
        { "MouthSmileRight", ARKitBlendShapeLocation.MouthSmileRight },
        { "MouthRollUpper", ARKitBlendShapeLocation.MouthRollUpper },
        { "MouthRollLower", ARKitBlendShapeLocation.MouthRollLower },
        { "TongueOut", ARKitBlendShapeLocation.TongueOut }
    };
}
