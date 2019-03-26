using System;
using UnityEngine;

namespace GPUInstancer
{
    [Serializable]
    public class GPUInstancerCameraData
    {
        public Camera mainCamera;
        public float frustumHeight;
        public bool cameraChanged;
#if !UNITY_2017_3_OR_NEWER
        public float[] mvpMatrixFloats;
#endif

        private Vector3 _cameraPosition;
        private Quaternion _cameraRotation;
        private float _cameraFieldOfView;
        private Vector2 _screenSize;

        public GPUInstancerCameraData(Camera mainCamera)
        {
            this.mainCamera = mainCamera;
            cameraChanged = true;
            _screenSize = Vector2.zero;
#if !UNITY_2017_3_OR_NEWER
            mvpMatrixFloats = new float[16];
#endif
        }

        public void CalculateCameraData()
        {
            CheckCameraChanges();
            if (!cameraChanged)
                return;

#if !UNITY_2017_3_OR_NEWER
            Matrix4x4 mvpMatrix = mainCamera.projectionMatrix * mainCamera.worldToCameraMatrix;
            mvpMatrixFloats[0] = mvpMatrix[0,0];
            mvpMatrixFloats[1] = mvpMatrix[1,0];
            mvpMatrixFloats[2] = mvpMatrix[2,0];
            mvpMatrixFloats[3] = mvpMatrix[3,0];
            mvpMatrixFloats[4] = mvpMatrix[0,1];
            mvpMatrixFloats[5] = mvpMatrix[1,1];
            mvpMatrixFloats[6] = mvpMatrix[2,1];
            mvpMatrixFloats[7] = mvpMatrix[3,1];
            mvpMatrixFloats[8] = mvpMatrix[0,2];
            mvpMatrixFloats[9] = mvpMatrix[1,2];
            mvpMatrixFloats[10] = mvpMatrix[2,2];
            mvpMatrixFloats[11] = mvpMatrix[3,2];
            mvpMatrixFloats[12] = mvpMatrix[0,3];
            mvpMatrixFloats[13] = mvpMatrix[1,3];
            mvpMatrixFloats[14] = mvpMatrix[2,3];
            mvpMatrixFloats[15] = mvpMatrix[3,3];
#endif

            // https://docs.unity3d.com/Manual/FrustumSizeAtDistance.html
            frustumHeight = 2.0f * Mathf.Tan(mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        }

        private void CheckCameraChanges()
        {
            if (mainCamera == null)
                return;
            if(_cameraPosition != mainCamera.transform.position)
            {
                _cameraPosition = mainCamera.transform.position;
                cameraChanged = true;
            }
            if (_cameraRotation != mainCamera.transform.rotation)
            {
                _cameraRotation = mainCamera.transform.rotation;
                cameraChanged = true;
            }
            if (_cameraFieldOfView != mainCamera.fieldOfView)
            {
                _cameraFieldOfView = mainCamera.fieldOfView;
                cameraChanged = true;
            }
            if(_screenSize.x != Screen.width || _screenSize.y != Screen.height)
            {
                _screenSize.x = Screen.width;
                _screenSize.y = Screen.height;
                cameraChanged = true;
            }
        }
    }
}