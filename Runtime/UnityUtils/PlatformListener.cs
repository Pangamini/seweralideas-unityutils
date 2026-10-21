#nullable enable
using SeweralIdeas.UnityUtils.Drawers;
using UnityEngine;
using UnityEngine.Events;

namespace SeweralIdeas.UnityUtils
{
    // Reports once, on Start, whether the platform the game is running on is one of
    // _platforms - e.g. to hide a Quit button on WebGL or show touch hints on mobile.
    // With _invert it reports the opposite: whether the platform is not one of them.
    [DisallowMultipleComponent]
    public class PlatformListener : MonoBehaviour
    {
        [SerializeField] private EnumSet<RuntimePlatform> _platforms = new();
        [SerializeField] private bool _invert;
        [SerializeField] private UnityEvent<bool> _onResolved = null!;

        public bool IsCurrentPlatformIncluded => _platforms.Contains(Application.platform) != _invert;

        protected void Start() => _onResolved.Invoke(IsCurrentPlatformIncluded);
    }
}
