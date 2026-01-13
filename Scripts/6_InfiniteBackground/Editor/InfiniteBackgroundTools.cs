using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using CryptaGeometrica.InfiniteBackground.Core;
using CryptaGeometrica.InfiniteBackground.Data;

namespace CryptaGeometrica.InfiniteBackground.Editor
{
    public class InfiniteBackgroundTools
    {
        [MenuItem("Tools/Infinite Background/Setup Test Scene")]
        public static void SetupTestScene()
        {
            // 1. Create new scene
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            
            // 2. Setup Camera
            GameObject cameraObj = new GameObject("Main Camera");
            Camera camera = cameraObj.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            cameraObj.transform.position = new Vector3(0, 0, -10);

            // 3. Create Manager
            GameObject managerObj = new GameObject("InfiniteBackgroundManager");
            InfiniteBackgroundManager manager = managerObj.AddComponent<InfiniteBackgroundManager>();
            
            // 4. Create a test Theme SO if it doesn't exist
            string folderPath = "Assets/Resources/InfiniteBackground";
            if (!System.IO.Directory.Exists(folderPath))
            {
                System.IO.Directory.CreateDirectory(folderPath);
            }
            
            string themePath = $"{folderPath}/TestTheme.asset";
            BackgroundThemeSO theme = AssetDatabase.LoadAssetAtPath<BackgroundThemeSO>(themePath);
            
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<BackgroundThemeSO>();
                theme.themeName = "Test Theme";
                theme.layers = new LayerConfig[1];
                
                // Create a placeholder sprite texture
                Texture2D texture = new Texture2D(512, 512);
                Color[] pixels = new Color[512 * 512];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = (i % 2 == 0) ? Color.gray : Color.white;
                texture.SetPixels(pixels);
                texture.Apply();
                
                // NOTE: We can't easily save a procedural texture as asset without saving it as PNG first.
                // Instead, let's try to find an existing sprite or just warn user.
                Debug.LogWarning("Created Test Theme SO but Sprite is missing. Please assign a sprite in the Inspector.");
                
                AssetDatabase.CreateAsset(theme, themePath);
                AssetDatabase.SaveAssets();
            }

            // Assign theme via serialized object to simulate Inspector assignment
            SerializedObject so = new SerializedObject(manager);
            so.FindProperty("defaultTheme").objectReferenceValue = theme;
            so.FindProperty("targetCamera").objectReferenceValue = camera;
            so.ApplyModifiedProperties();

            // 5. Add a simple camera mover for testing
            GameObject moverObj = new GameObject("CameraMover");
            CameraMover mover = moverObj.AddComponent<CameraMover>();
            mover.cam = cameraObj.transform;
            
            Debug.Log("Infinite Background Test Scene Setup Complete!");
        }
    }

    // Simple script to move camera with arrow keys for testing
    public class CameraMover : MonoBehaviour
    {
        public Transform cam;
        public float speed = 10f;

        void Update()
        {
            if (cam == null) return;
            
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            
            cam.position += new Vector3(h, v, 0) * speed * Time.deltaTime;
        }
    }
}
