#nullable enable
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SeweralIdeas.UnityUtils
{
    public class SimplePrewarmScene : MonoBehaviour
    {
        [SerializeField] private SceneReference _loadAfter;

        IEnumerator Start()
        {
            yield return new WaitForEndOfFrame();
            SceneManager.LoadScene(_loadAfter.Path);
        }
    }
}
