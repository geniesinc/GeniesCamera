using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Genies.CreatorTools.utils
{
    public class CameraOrbiter : MonoBehaviour
    {
        [FormerlySerializedAs("camera")] public Camera _camera;
        public List<CameraTarget> targets;
        public float rotationSpeed = 1f;
        public float scrollSpeed = 10f;
        public float smoothness = 5f;

        private int currentIndex = 0;
        private Transform currentTarget;
        [HideInInspector]
        public Vector3 offset;
        private float distance;

        private void Start()
        {
            currentTarget = targets[currentIndex].transform;
            distance = targets[currentIndex].distance;
            offset = _camera.transform.position - currentTarget.position;
        }

        private void Update()
        {
            // Switch targets when the user presses the Tab key
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                CameraSwtich(0);
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                CameraSwtich(1);
            }

            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                CameraSwtich(2);
            }

            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                CameraSwtich(3);
            }


            // Orbit around the target when the user drags the mouse

            // Zoom in/out when the user scrolls the mouse wheel
            //float t = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0, distance, Vector3.Distance(camera.transform.position, currentTarget.position)));

            //camera.transform.position = Vector3.Lerp(camera.transform.position, currentTarget.position + offset.normalized * distance, Time.deltaTime * smoothness);

            //camera.transform.position = Vector3.Lerp(camera.transform.position, currentTarget.position + offset.normalized * distance, t * Time.deltaTime *smoothness);
            //camera.transform.LookAt(currentTarget);
            //offset = camera.transform.position - currentTarget.position;

            _camera.transform.position = currentTarget.position + offset.normalized * distance;
            _camera.transform.LookAt(currentTarget);
        }

        private void CameraSwtich(int index){
            currentIndex = index;
            currentTarget = targets[currentIndex].transform;
            distance = targets[currentIndex].distance;
            offset = _camera.transform.position - currentTarget.position;
        }
    }
}
