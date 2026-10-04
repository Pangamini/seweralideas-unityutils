using UnityEngine;

namespace SeweralIdeas.UnityUtils.Markdown
{
    /// <summary>
    /// How <see cref="MarkdownToTmp"/> dresses each Markdown element: the name of the TMP style (in the TMP style sheet,
    /// <c>&lt;style=Name&gt;</c>) it is wrapped in. The look itself - size, colour, indentation - is defined there.
    /// An empty name means the element gets no tags at all.
    /// </summary>
    [CreateAssetMenu(menuName = "SeweralIdeas/Markdown/Style Set")]
    public class MarkdownStyleSet : ScriptableObject
    {
        [Tooltip("Styles of the headings, # to ######. A level past the end of the list uses the last one.")]
        [SerializeField] private string[] _headings = { "Heading1", "Heading2", "Heading3", "Heading4", "Heading5", "Heading6" };

        [SerializeField] private string _paragraph     = "Paragraph";
        [SerializeField] private string _bold          = "Bold";
        [SerializeField] private string _italic        = "Italic";

        [Tooltip("For ***both***. If empty, the Bold and Italic styles are nested instead.")]
        [SerializeField] private string _boldItalic    = "BoldItalic";

        [SerializeField] private string _strikethrough = "Strikethrough";
        [SerializeField] private string _inlineCode    = "InlineCode";
        [SerializeField] private string _link          = "Link";
        [SerializeField] private string _quote         = "Quote";
        [SerializeField] private string _rule          = "Rule";

        [Tooltip("Style of a list item per nesting level, the last one repeats. This is where the indentation goes.")]
        [SerializeField] private string[] _listItems = { "ListItem1", "ListItem2", "ListItem3" };

        [Tooltip("The bullet of an item per nesting level, the last one repeats.")]
        [SerializeField] private string[] _bullets = { "•", "◦", "▪" };

        [Tooltip("Format of the number of a numbered item.")]
        [SerializeField] private string _numberFormat = "{0}.";

        [Tooltip("What a horizontal rule is made of.")]
        [SerializeField] private string _ruleText = "――――――――――――";

        [Tooltip("Blank lines between blocks (list items follow each other without).")]
        [SerializeField, Min(0)] private int _blockSpacing = 1;

        public string Heading(int level) => Pick(_headings, level - 1);
        public string Paragraph          => _paragraph;
        public string Bold               => _bold;
        public string Italic             => _italic;
        public string BoldItalic         => _boldItalic;
        public string Strikethrough      => _strikethrough;
        public string InlineCode         => _inlineCode;
        public string Link               => _link;
        public string Quote              => _quote;
        public string Rule               => _rule;
        public string ListItem(int level) => Pick(_listItems, level);
        public string Bullet(int level)   => Pick(_bullets, level);
        public string NumberFormat       => string.IsNullOrEmpty(_numberFormat) ? "{0}." : _numberFormat;
        public string RuleText           => _ruleText;
        public int    BlockSpacing       => _blockSpacing;

        // The element at the index, the last one for an index past the end, nothing for no elements.
        private static string Pick(string[] names, int index)
        {
            if (names == null || names.Length == 0)
                return string.Empty;
            if (index < 0)
                index = 0;
            return names[index < names.Length ? index : names.Length - 1] ?? string.Empty;
        }
    }
}
