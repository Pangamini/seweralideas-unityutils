using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    /// <summary>
    /// Adds comments to GameObjects in the Inspector.
    /// </summary>
    /// 
    public class Comments : MonoBehaviour
    {
        [Multiline]
        public string text;
    }
}