using UnityEngine;

namespace Genies.UIFramework.Widgets {
    public class TweenSpinner : MonoBehaviour {

        [Header("Options")]
        public float degreesPerSecond = -500f;

        private void Update() {
            float angle = Time.time * degreesPerSecond;

            if (angle > 180)
            {
                angle -= 360f;
            }

            if (angle < 180)
            {
                angle += 360f;
            }

            transform.rotation = Quaternion.Euler(0f,0f,angle);
        }
    }
}
