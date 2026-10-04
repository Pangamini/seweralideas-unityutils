using UnityEngine;
using UnityEngine.Serialization;

namespace SeweralIdeas.UnityUtils
{
    /// <summary>
    /// Adds comments to GameObjects in the Inspector.
    /// </summary>
    public class Comments : MonoBehaviour
    {
        [Multiline]
        [FormerlySerializedAs("text")]
        [SerializeField] 
        private string _text;

        public const string PropertyName = nameof(_text);
    }
}