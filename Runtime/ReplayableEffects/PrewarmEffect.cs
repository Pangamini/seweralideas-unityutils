using System.Collections;
using SeweralIdeas.ObjectPooling;
using UnityEngine;

namespace SeweralIdeas.ReplayableEffects
{
    public class PrewarmEffect : MonoBehaviour
    {
        [SerializeField]
        private ReplayableEffect _effect;

        protected void Start()
        {
            StartCoroutine(Render());
        }

        private IEnumerator Render()
        {
            ReplayableEffect instance = _effect.Spawn(gameObject.scene, transform, _effect.Duration * 0.5f);
            yield return new WaitForEndOfFrame();
            instance.Stop();
        }
    }
}
