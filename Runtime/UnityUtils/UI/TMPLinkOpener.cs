#if UNITY_TEXTMESHPRO
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace SeweralIdeas.UnityUtils
{
    [RequireComponent(typeof(TMP_Text))]
    public class TMPLinkOpener : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private UnityEvent<string> m_linkClicked = new();
        private TMP_Text m_text;

        public event UnityAction<string> LinkClicked
        {
            add => m_linkClicked.AddListener(value);
            remove => m_linkClicked.RemoveListener(value);
        }

        void Awake()
        {
            m_text = GetComponent<TMP_Text>();
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(m_text, eventData.position, m_text.canvas.worldCamera);
            if (linkIndex != -1)
            {
                TMP_LinkInfo linkInfo = m_text.textInfo.linkInfo[linkIndex];
                var id = linkInfo.GetLinkID();
                m_linkClicked.Invoke(id);
            }
        }
    }
}
#endif