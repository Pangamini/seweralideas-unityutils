using UnityEngine;

namespace SeweralIdeas.UnityUtils.Drawers
{
    /// <summary>
    /// Draws a field whose type has a single serialized field as if it were that inner field, under the outer field's name.
    /// Display only: the serialized data (and property paths) keep the wrapper. Does nothing if the type doesn't have exactly
    /// one visible serialized field. On a list or array it applies to every element.
    /// Can be combined with <see cref="ConditionAttribute"/>.
    /// </summary>
    public class FlattenAttribute : PropertyAttribute { }
}
