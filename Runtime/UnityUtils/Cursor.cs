using UnityEngine;
namespace SeweralIdeas.UnityUtils
{
    [CreateAssetMenu(menuName = "SeweralIdeas/Cursor")]
    public class Cursor : ScriptableObject
    {
        [SerializeField] public Texture2D icon;
        [SerializeField] public Vector2 pivot;
    }
}