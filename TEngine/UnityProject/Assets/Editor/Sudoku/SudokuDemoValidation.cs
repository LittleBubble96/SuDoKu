#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using GameLogic;
using GameLogic.Sudoku;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SudokuEditor
{
    /// <summary>Batch validation and screenshots of the actual Unity UI, without starting legacy gameplay or SDKs.</summary>
    public static class SudokuDemoValidation
    {
        public static void Run()
        {
#if !TE_RELEASE
            // Resource loading now needs the real async startup and player loop.
            SudokuStartupValidation.Run();
#else
            throw new InvalidOperationException("Run UI interaction validation in a development build.");
#endif
        }
        private static void Capture(Camera camera, RenderTexture target, string path)
        {
            Canvas.ForceUpdateCanvases(); camera.Render();
            var previous = RenderTexture.active; RenderTexture.active = target;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            try { image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply(); File.WriteAllBytes(path, image.EncodeToPNG()); }
            finally { RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(image); }
        }

        public static void CaptureCanvas(GameObject source, string path)
        {
            var pipeline = GraphicsSettings.defaultRenderPipeline;
            var quality = QualitySettings.renderPipeline;
            GameObject clone = null, cameraObject = null;
            RenderTexture target = null;
            try
            {
                GraphicsSettings.defaultRenderPipeline = null; QualitySettings.renderPipeline = null;
                clone = UnityEngine.Object.Instantiate(source); clone.name = "GMPreview"; clone.hideFlags = HideFlags.HideAndDontSave;
                clone.transform.SetParent(null, false); clone.transform.localScale = Vector3.one;
                cameraObject = new GameObject("GMPreviewCamera", typeof(Camera)); cameraObject.hideFlags = HideFlags.HideAndDontSave;
                var camera = cameraObject.GetComponent<Camera>(); camera.orthographic = true; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.white; camera.cullingMask = 1 << 5;
                target = new RenderTexture(720, 1280, 24); target.Create(); camera.targetTexture = target;
                var canvas = clone.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
                // 独立渲染的克隆不再继承 UIRoot，复制原 CanvasScaler，避免截图按 1:1 像素裁掉 1080 布局。
                var sourceScaler = source.GetComponentInParent<CanvasScaler>();
                if (sourceScaler != null)
                {
                    var scaler = clone.GetComponent<CanvasScaler>();
                    if (scaler == null) scaler = clone.AddComponent<CanvasScaler>();
                    EditorUtility.CopySerialized(sourceScaler, scaler);
                    typeof(CanvasScaler).GetMethod("Handle", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(scaler, null);
                }
                Canvas.ForceUpdateCanvases(); Capture(camera, target, path);
            }
            finally
            {
                if (clone != null) UnityEngine.Object.DestroyImmediate(clone);
                if (cameraObject != null) UnityEngine.Object.DestroyImmediate(cameraObject);
                if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
                GraphicsSettings.defaultRenderPipeline = pipeline; QualitySettings.renderPipeline = quality;
            }
        }
    }
}
#endif
