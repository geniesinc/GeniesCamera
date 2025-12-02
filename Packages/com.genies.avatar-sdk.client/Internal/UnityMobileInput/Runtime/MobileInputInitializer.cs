using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UMI
{
    public class MobileInputInitializer : MonoBehaviour
    {
        private void Awake()
        {
            MobileInput.Init();
        }
    }
}
