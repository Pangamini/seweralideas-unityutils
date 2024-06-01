using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    public interface IHasPositionAndVelocity
    {
        Vector3 Position { get; }
        Vector3 Velocity { get; }
    }
}
