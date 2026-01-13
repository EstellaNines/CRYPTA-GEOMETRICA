using UnityEngine;
using UnityEditor;

namespace CryptaGeometrica.LevelGeneration.SmallRoom
{
    [CustomEditor(typeof(RoomGenerator))]
    public class RoomGeneratorEditor : Editor
    {
        private RoomGenerator generator;
        private Texture2D previewTexture;

        private void OnEnable()
        {
            generator = (RoomGenerator)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Debug Controls", EditorStyles.boldLabel);

            if (GUILayout.Button("Generate Preview"))
            {
                generator.GenerateRoom();
                UpdatePreview();
            }

            if (GUILayout.Button("Bake to Tilemap"))
            {
                generator.BakeToTilemap();
            }

            if (previewTexture != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Preview Map", EditorStyles.boldLabel);
                
                // Draw the texture
                Rect rect = GUILayoutUtility.GetRect(200, 200, GUILayout.ExpandWidth(true));
                float aspect = (float)generator.parameters.roomWidth / generator.parameters.roomHeight;
                
                // Adjust rect height based on aspect ratio to keep pixels square
                float width = rect.width;
                float height = width / aspect;
                rect.height = height;

                EditorGUI.DrawPreviewTexture(rect, previewTexture);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void UpdatePreview()
        {
            RoomData data = generator.CurrentRoom;
            if (data == null) return;

            if (previewTexture == null || previewTexture.width != data.width || previewTexture.height != data.height)
            {
                previewTexture = new Texture2D(data.width, data.height);
                previewTexture.filterMode = FilterMode.Point;
                previewTexture.wrapMode = TextureWrapMode.Clamp;
            }

            Color[] pixels = new Color[data.width * data.height];

            // Colors
            Color wallColor = Color.black;
            Color floorColor = Color.white;
            Color anchorColor = Color.green;
            Color platformColor = new Color(0.5f, 0.5f, 1f);

            for (int y = 0; y < data.height; y++)
            {
                for (int x = 0; x < data.width; x++)
                {
                    TileType type = data.GetTile(x, y);
                    Color c = wallColor;

                    if (type == TileType.Floor) c = floorColor;
                    else if (type == TileType.Platform) c = platformColor;

                    // Highlight Anchors
                    if ((x == data.startPos.x && y == data.startPos.y) ||
                        (x == data.endPos.x && y == data.endPos.y))
                    {
                        c = anchorColor;
                    }

                    pixels[y * data.width + x] = c;
                }
            }

            previewTexture.SetPixels(pixels);
            previewTexture.Apply();
        }
    }
}
