using System;
using UnityEngine;
using UnityEngine.UI;

namespace TimeControl.Unity
{
    [RequireComponent(typeof(RectTransform))]
    public class WarpSpeedEditor : MonoBehaviour
    {
        [SerializeField]
        private Toggle m_OrbitToggle = null;
        [SerializeField]
        private Toggle m_OrbitDragToggle = null;
        [SerializeField]
        private Toggle m_OrbitSettingsToggle = null;
        [SerializeField]
        private Text m_VersionText = null;
    }
}
