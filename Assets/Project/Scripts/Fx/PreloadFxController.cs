using System.Collections;
using UnityEngine;

public class PreloadFxController : MonoBehaviour
{
    private Transform[] _scalableTransforms;
    private Vector3[] _localScalesAtStart;
    private bool _doFollowCamera = true;    
    private GenieController _genieController;
    private CameraManager _cameraManager;
    private bool _didInitialize;

    public void Initialize(GenieController genieController, CameraManager cameraManager)
    {
        _genieController = genieController;
        _cameraManager = cameraManager;

        _scalableTransforms = GetComponentsInChildren<Transform>(true);
        _localScalesAtStart = new Vector3[_scalableTransforms.Length];
        for (int i = 0; i < _localScalesAtStart.Length; i++)
        {
            _localScalesAtStart[i] = _scalableTransforms[i].localScale;
        }

        _didInitialize = true;
    }

    public void SetFxVisibility(bool isVisible)
    {
        for (int i = 0; i < _scalableTransforms.Length; i++)
        {
            _scalableTransforms[i].gameObject.SetActive(isVisible);
        }
    }

    private void Update()
    {
        if (!_didInitialize)
        {
            return;
        }

        if (_doFollowCamera)
        {
            if (_cameraManager.IsScreenspace)
            {
                transform.position = Vector3.up;
                for (int i = 0; i < _scalableTransforms.Length; i++)
                {
                    _scalableTransforms[i].transform.localScale = _localScalesAtStart[i];
                }
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position,
                                            _genieController.GetPreloadFxPosition(),
                                            Time.deltaTime);

                for (int i = 0; i < _scalableTransforms.Length; i++)
                {
                    _scalableTransforms[i].transform.localScale = _localScalesAtStart[i] * _genieController.CurrScale;
                }
            }
        }
    }

    public void DestroyPretty()
    {
        if (gameObject.activeInHierarchy)
        {
            _doFollowCamera = false;
            StartCoroutine(ShrinkAndDestroy());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator ShrinkAndDestroy()
    {
        if(_scalableTransforms.Length == 0)
        {
            yield break;
        }

        float t = 0;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            for (int i = 0; i < _scalableTransforms.Length; i++)
            {
                _scalableTransforms[i].transform.localScale = Vector3.Lerp(_localScalesAtStart[i], Vector3.zero, t);
            }
            yield return null;
        }
        Destroy(gameObject);
    }
}
