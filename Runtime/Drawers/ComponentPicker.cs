using UnityEngine;

namespace SeweralIdeas.UnityUtils.Drawers
{
    /// <summary>
    /// On a field that references a Component: next to the usual object field, a dropdown of the components of the
    /// GameObject the current reference is on, to switch to another one of them.
    /// Handy when dragging a prefab or an object in gives the Transform, and it is another component that is wanted.
    /// </summary>
    public class ComponentPickerAttribute : PropertyAttribute
    {

    }
}
