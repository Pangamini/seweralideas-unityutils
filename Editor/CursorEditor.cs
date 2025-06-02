using UnityEditor;
using UnityEngine;
using Cursor = SeweralIdeas.UnityUtils.Cursor;

namespace SeweralIdeas.UnityUtils.Editor
{
    [CustomEditor(typeof(Cursor))]
    [CanEditMultipleObjects]
    public class CursorEditor : UnityEditor.Editor
    {

        public override bool HasPreviewGUI() => true;

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            var cursor = (Cursor)target;
            if (!cursor.icon)
                return;
            
            var rect = r.GetAspectFittedRect((float)cursor.icon.width / cursor.icon.height);
            GUI.DrawTexture(rect, cursor.icon);

            Vector2 normalizedHotspot = cursor.pivot * cursor.icon.texelSize;
            GUI.DrawTexture(new Rect(rect.xMin, rect.yMin + rect.height * normalizedHotspot.y, rect.width, 1), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMin + rect.width * normalizedHotspot.x, rect.yMin, 1, rect.height), Texture2D.whiteTexture);
        }

        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            var activeRt = RenderTexture.active;
            var cursor = (Cursor)target;

            var texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            var rt = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
            try
            {
                GL.Clear(true, true, Color.clear);
                Graphics.Blit(cursor.icon, rt);
                texture.ReadPixels(new Rect(0,0,width, height), 0, 0);
                texture.Apply();
                return texture;
            }
            catch
            {
                DestroyImmediate(texture);
                return null;
            }
            finally
            {
                RenderTexture.ReleaseTemporary(rt);
                RenderTexture.active = activeRt;
            }
        }
    }
}