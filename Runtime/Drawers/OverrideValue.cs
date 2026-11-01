using UnityEngine;

namespace SeweralIdeas.UnityUtils.Drawers
{
    /// <summary>
    /// On an <c>Override&lt;T&gt;</c> field: draws the override toggle and applies the field's other attributes
    /// (<c>[Euler]</c>, <c>[Range]</c>, ...) to the value inside, instead of to the Override itself. Without it, Unity runs
    /// the other attribute's drawer on the whole Override and the toggle is lost.
    /// The attributes that come after it in <see cref="PropertyAttribute.order"/> apply to the value, so it is first by default.
    /// </summary>
    public class OverrideValueAttribute : PropertyAttribute
    {
        public OverrideValueAttribute() => order = int.MinValue;
    }
}
