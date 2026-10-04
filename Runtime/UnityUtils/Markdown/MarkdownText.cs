#if UNITY_TEXTMESHPRO
using TMPro;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Markdown
{
    /// <summary>
    /// Shows a Markdown text (a TextAsset such as a CHANGELOG.md, or any string) in the TMP_Text next to it, converted
    /// by <see cref="MarkdownToTmp"/> with the styles of a <see cref="MarkdownStyleSet"/>.
    /// Links get the URL as their link id, so a <see cref="TMPLinkOpener"/> next to it hears about their clicks.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    [AddComponentMenu("SeweralIdeas/Markdown/MarkdownText")]
    public class MarkdownText : MonoBehaviour
    {
        [SerializeField] private TextAsset         _markdown;
        [SerializeField] private MarkdownStyleSet  _styles;

        private TMP_Text _text;

        private TMP_Text Text => _text != null ? _text : _text = GetComponent<TMP_Text>();

        public TextAsset Markdown
        {
            get => _markdown;
            set
            {
                _markdown = value;
                Refresh();
            }
        }

        protected void OnEnable() => Refresh();

        /// <summary>Converts the Markdown asset again.</summary>
        [ContextMenu("Refresh")]
        public void Refresh()
        {
            if (_markdown != null)
                SetMarkdown(_markdown.text);
        }

        /// <summary>Shows the Markdown string. Shows it as it is if there is no style set.</summary>
        public void SetMarkdown(string markdown)
        {
            if (_styles == null)
            {
                Debug.LogWarning($"{name}: no Markdown style set, showing the text as it is.", this);
                Text.text = markdown;
                return;
            }

            Text.text = MarkdownToTmp.Convert(markdown, _styles);
        }
    }
}
#endif
