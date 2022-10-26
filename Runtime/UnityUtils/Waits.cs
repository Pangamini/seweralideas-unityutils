using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    public static class Waits
    {
        /// <summary>
        /// WaitForSeconds(0)
        /// </summary>
        public static readonly WaitForSeconds waitForNextFrame = new WaitForSeconds(0);
        public static readonly WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();
        public static readonly WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();
    }
}