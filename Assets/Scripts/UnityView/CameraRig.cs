using UnityEngine;

namespace ErkekTavlasi.UnityView
{
    public class CameraRig : MonoBehaviour
    {
        public enum ViewMode
        {
            ThreeD,
            TwoD
        }

        public Camera TargetCamera;
        public Vector3 LookAt = new Vector3(0.25f, 0f, 0f);
        public float Height = 11.5f;
        public float Back = -13.0f;
        public ViewMode CurrentMode = ViewMode.ThreeD;

        public void SetMode(ViewMode mode)
        {
            CurrentMode = mode;
            ApplyCamera();
        }

        private void LateUpdate()
        {
            ApplyCamera();
        }

        private void ApplyCamera()
        {
            if (TargetCamera == null)
            {
                TargetCamera = Camera.main;
            }

            if (TargetCamera == null)
            {
                return;
            }

            float aspect = Mathf.Max(0.5f, TargetCamera.aspect);
            float narrowPenalty = aspect < 1.45f ? (1.45f - aspect) * 2.2f : 0f;

            if (CurrentMode == ViewMode.TwoD)
            {
                TargetCamera.orthographic = true;
                TargetCamera.orthographicSize = Mathf.Max(6.2f, 5.5f / aspect) + narrowPenalty * 0.35f;
                TargetCamera.transform.position = new Vector3(0.22f, 13.6f, 0f);
                TargetCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
            else
            {
                TargetCamera.orthographic = false;
                TargetCamera.transform.position = new Vector3(0.35f, Height + narrowPenalty * 0.35f, Back - narrowPenalty * 0.25f);
                TargetCamera.transform.LookAt(LookAt);
                TargetCamera.fieldOfView = 52f;
            }

            TargetCamera.nearClipPlane = 0.05f;
            TargetCamera.farClipPlane = 100f;
        }
    }
}
