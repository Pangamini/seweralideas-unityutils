using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SeweralIdeas.UnityUtils.Markdown
{
    /// <summary>
    /// Converts Markdown to TextMeshPro rich text, dressing each element in the TMP style a
    /// <see cref="MarkdownStyleSet"/> names for it.
    ///
    /// Handled: headings (# and underlined), paragraphs and hard line breaks, *italic*, **bold**, ***both***,
    /// ~~strikethrough~~, `inline code`, [links](url), nested bullet and numbered lists, &gt; quotes and horizontal rules.
    /// Fenced code blocks are shown as they are. Images become their alt text. Tables, footnotes and HTML are not
    /// handled: they show up as the text they are made of. Every literal "&lt;" is escaped, so the text can never be
    /// read as a TMP tag.
    /// </summary>
    public static class MarkdownToTmp
    {
        public static string Convert(string markdown, MarkdownStyleSet styles)
        {
            if (styles == null)
                throw new ArgumentNullException(nameof(styles));
            if (string.IsNullOrEmpty(markdown))
                return string.Empty;

            string normalized = markdown.Replace("\r\n", "\n").Replace('\r', '\n');
            return new Converter(styles).ConvertBlocks(normalized.Split('\n'));
        }

        // ----- Patterns -----

        private static readonly Regex AtxHeading   = new(@"^ {0,3}(#{1,6})(?:[ \t]+(.*?))?(?:[ \t]+#+)?[ \t]*$", RegexOptions.CultureInvariant);
        private static readonly Regex Rule         = new(@"^ {0,3}([-*_])(?:[ \t]*\1){2,}[ \t]*$", RegexOptions.CultureInvariant);
        private static readonly Regex SetextOne    = new(@"^ {0,3}=+[ \t]*$", RegexOptions.CultureInvariant);
        private static readonly Regex SetextTwo    = new(@"^ {0,3}-+[ \t]*$", RegexOptions.CultureInvariant);
        private static readonly Regex Fence        = new(@"^ {0,3}(`{3,}|~{3,})(.*)$", RegexOptions.CultureInvariant);
        private static readonly Regex Quote        = new(@"^ {0,3}>[ ]?(.*)$", RegexOptions.CultureInvariant);
        private static readonly Regex ListItem     = new(@"^([ \t]*)([-+*]|\d{1,9}[.)])(?:[ \t]+(.*))?$", RegexOptions.CultureInvariant);
        private static readonly Regex Autolink     = new(@"\G<([A-Za-z][A-Za-z0-9+.\-]{1,31}:[^\s<>]*)>", RegexOptions.CultureInvariant);

        private const string Escapable = "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~";

        private enum BlockKind { None, Paragraph, Heading, ListItem, Quote, Rule, Code }

        private sealed class Node
        {
            public string Text;          // ready TMP text (not for a delimiter run)
            public char   Delim;         // '*', '_' or '~' for a run of delimiters
            public int    Count;         // delimiters of the run still unused
            public int    OriginalCount;
            public bool   CanOpen;
            public bool   CanClose;
            public bool   Active = true; // a run that was passed over by a match stays as the text it is, but takes no part
        }

        private sealed class Converter
        {
            private readonly MarkdownStyleSet _styles;

            public Converter(MarkdownStyleSet styles) => _styles = styles;

            // ----- Style tags -----

            public static string Open(string style)
            {
                if (string.IsNullOrEmpty(style))
                    return string.Empty;
                return style.IndexOf(' ') >= 0 ? $"<style=\"{style}\">" : $"<style={style}>";
            }

            public static string Close(string style) => string.IsNullOrEmpty(style) ? string.Empty : "</style>";

            // ----- Escaping -----

            private static void AppendEscaped(StringBuilder sb, char c)
            {
                if (c == '<')
                    sb.Append("<noparse><</noparse>");
                else
                    sb.Append(c);
            }

            private static string EscapeText(string text)
            {
                var sb = new StringBuilder(text.Length);
                foreach (char c in text)
                    AppendEscaped(sb, c);
                return sb.ToString();
            }

            private static string LinkOpen(string url)
            {
                string id = url.Replace("\"", "%22").Replace("<", "%3C").Replace(">", "%3E");
                return $"<link=\"{id}\">";
            }

            // ----- Blocks -----

            public string ConvertBlocks(IList<string> lines)
            {
                var blocks = new BlockConverter(this);
                blocks.Run(lines);
                return blocks.Result;
            }

            private sealed class BlockConverter
            {
                private readonly Converter     _owner;
                private readonly StringBuilder _out       = new();
                private readonly List<string>  _paragraph = new();
                private readonly List<int>     _indents   = new(); // indentation of every open list level
                private readonly List<int>     _numbers   = new(); // running number of every open list level, -1 for bullets
                private BlockKind              _last      = BlockKind.None;

                public BlockConverter(Converter owner) => _owner = owner;

                public string Result => _out.ToString();

                public void Run(IList<string> lines)
                {
                    for (int i = 0; i < lines.Count; i++)
                    {
                        string line = lines[i];

                        if (IsBlank(line))
                        {
                            FlushParagraph();
                            continue;
                        }

                        Match fence = Fence.Match(line);
                        if (fence.Success)
                        {
                            FlushParagraph();
                            EndList();
                            string marker = fence.Groups[1].Value;
                            var code = new List<string>();
                            for (i++; i < lines.Count && !IsClosingFence(lines[i], marker); i++)
                                code.Add(lines[i]);
                            EmitCode(code);
                            continue;
                        }

                        if (_paragraph.Count > 0)
                        {
                            // underlined heading
                            int setextLevel = SetextOne.IsMatch(line) ? 1 : SetextTwo.IsMatch(line) ? 2 : 0;
                            if (setextLevel > 0)
                            {
                                string heading = JoinLines(_paragraph);
                                _paragraph.Clear();
                                EndList();
                                EmitHeading(setextLevel, heading);
                                continue;
                            }
                        }

                        Match atx = AtxHeading.Match(line);
                        if (atx.Success)
                        {
                            FlushParagraph();
                            EndList();
                            EmitHeading(atx.Groups[1].Length, atx.Groups[2].Value);
                            continue;
                        }

                        if (Rule.IsMatch(line))
                        {
                            FlushParagraph();
                            EndList();
                            EmitRule();
                            continue;
                        }

                        if (Quote.IsMatch(line))
                        {
                            FlushParagraph();
                            EndList();
                            var content = new List<string>();
                            for (; i < lines.Count && Quote.IsMatch(lines[i]); i++)
                                content.Add(Quote.Match(lines[i]).Groups[1].Value);
                            i--;
                            EmitQuote(content);
                            continue;
                        }

                        Match item = ListItem.Match(line);
                        if (item.Success)
                        {
                            FlushParagraph();

                            // The following lines belong to the item as long as they don't start something of their own
                            var itemLines = new List<string> { item.Groups[3].Value };
                            while (i + 1 < lines.Count && !IsBlank(lines[i + 1]) && !StartsBlock(lines[i + 1]))
                            {
                                itemLines.Add(lines[i + 1]);
                                i++;
                            }

                            EmitListItem(IndentWidth(item.Groups[1].Value), item.Groups[2].Value, itemLines);
                            continue;
                        }

                        EndList();
                        _paragraph.Add(line);
                    }

                    FlushParagraph();
                }

                private static bool IsBlank(string line) => line.Trim().Length == 0;

                private static bool IsClosingFence(string line, string openingFence)
                {
                    string trimmed = line.Trim();
                    if (trimmed.Length < openingFence.Length)
                        return false;
                    foreach (char c in trimmed)
                    {
                        if (c != openingFence[0])
                            return false;
                    }
                    return true;
                }

                private static bool StartsBlock(string line) =>
                    Fence.IsMatch(line) || AtxHeading.IsMatch(line) || Rule.IsMatch(line) || Quote.IsMatch(line) || ListItem.IsMatch(line);

                private static int IndentWidth(string whitespace)
                {
                    int width = 0;
                    foreach (char c in whitespace)
                        width += c == '\t' ? 4 : 1;
                    return width;
                }

                // Lines of one paragraph (or item) as one text: a soft break is a space, two spaces or a backslash
                // before the end of a line make it a hard one.
                private static string JoinLines(IList<string> lines)
                {
                    var sb = new StringBuilder();
                    for (int i = 0; i < lines.Count; i++)
                    {
                        string line = lines[i].TrimStart();
                        bool last = i == lines.Count - 1;
                        if (last)
                        {
                            sb.Append(line.TrimEnd());
                            break;
                        }

                        bool hardBreak = line.EndsWith("  ", StringComparison.Ordinal) || line.EndsWith("\\", StringComparison.Ordinal);
                        string text = line.TrimEnd();
                        if (text.EndsWith("\\", StringComparison.Ordinal))
                            text = text.Substring(0, text.Length - 1);
                        sb.Append(text);
                        sb.Append(hardBreak ? '\n' : ' ');
                    }
                    return sb.ToString();
                }

                private void EndList()
                {
                    _indents.Clear();
                    _numbers.Clear();
                }

                private void FlushParagraph()
                {
                    if (_paragraph.Count == 0)
                        return;

                    string text = JoinLines(_paragraph);
                    _paragraph.Clear();
                    string style = _owner._styles.Paragraph;
                    Emit(BlockKind.Paragraph, Converter.Open(style) + _owner.Inline(text) + Converter.Close(style));
                }

                private void Emit(BlockKind kind, string text)
                {
                    if (_out.Length > 0)
                    {
                        _out.Append('\n');
                        if (!(kind == BlockKind.ListItem && _last == BlockKind.ListItem))
                            _out.Append('\n', _owner._styles.BlockSpacing);
                    }
                    _out.Append(text);
                    _last = kind;
                }

                private void EmitHeading(int level, string text)
                {
                    string style = _owner._styles.Heading(level);
                    Emit(BlockKind.Heading, Converter.Open(style) + _owner.Inline(text.Trim()) + Converter.Close(style));
                }

                private void EmitRule()
                {
                    string style = _owner._styles.Rule;
                    Emit(BlockKind.Rule, Converter.Open(style) + EscapeText(_owner._styles.RuleText ?? string.Empty) + Converter.Close(style));
                }

                private void EmitCode(List<string> code)
                {
                    string style = _owner._styles.InlineCode;
                    var sb = new StringBuilder();
                    for (int i = 0; i < code.Count; i++)
                    {
                        if (i > 0)
                            sb.Append('\n');
                        sb.Append(EscapeText(code[i]));
                    }
                    Emit(BlockKind.Code, Converter.Open(style) + sb + Converter.Close(style));
                }

                private void EmitQuote(List<string> content)
                {
                    string style = _owner._styles.Quote;
                    string inner = _owner.ConvertBlocks(content);
                    Emit(BlockKind.Quote, Converter.Open(style) + inner + Converter.Close(style));
                }

                private void EmitListItem(int indent, string marker, IList<string> itemLines)
                {
                    int level = FindLevel(indent);

                    // Number of this item at its level (-1 for a bullet)
                    bool ordered = char.IsDigit(marker[0]);
                    while (_numbers.Count <= level)
                        _numbers.Add(-1);
                    if (!ordered)
                        _numbers[level] = -1;
                    else if (_numbers[level] < 0)
                        _numbers[level] = int.Parse(marker.Substring(0, marker.Length - 1), System.Globalization.CultureInfo.InvariantCulture);
                    else
                        _numbers[level]++;
                    // a deeper list that was open is over
                    for (int k = level + 1; k < _numbers.Count; k++)
                        _numbers[k] = -1;

                    string prefix = ordered ? FormatNumber(_numbers[level]) : EscapeText(_owner._styles.Bullet(level));
                    string style = _owner._styles.ListItem(level);
                    string text = Converter.Open(style) + prefix + " " + _owner.Inline(JoinLines(itemLines)) + Converter.Close(style);
                    Emit(BlockKind.ListItem, text);
                }

                private string FormatNumber(int number)
                {
                    try
                    {
                        return EscapeText(string.Format(_owner._styles.NumberFormat, number));
                    }
                    catch (FormatException)
                    {
                        return number + ".";
                    }
                }

                // The nesting level of an item with this indentation, keeping track of the open levels.
                private int FindLevel(int indent)
                {
                    if (_indents.Count == 0)
                    {
                        _indents.Add(indent);
                        return 0;
                    }

                    // within a column of an open level: that level, and whatever was deeper is over
                    for (int level = _indents.Count - 1; level >= 0; level--)
                    {
                        if (Math.Abs(indent - _indents[level]) <= 1)
                        {
                            _indents.RemoveRange(level + 1, _indents.Count - level - 1);
                            return level;
                        }
                    }

                    if (indent > _indents[_indents.Count - 1])
                    {
                        _indents.Add(indent);
                        return _indents.Count - 1;
                    }

                    // in between two levels: the shallower one
                    while (_indents.Count > 1 && indent < _indents[_indents.Count - 1])
                        _indents.RemoveAt(_indents.Count - 1);
                    return _indents.Count - 1;
                }
            }

            // ----- Inline -----

            public string Inline(string s)
            {
                var nodes = new List<Node>();
                var text = new StringBuilder();

                void Flush()
                {
                    if (text.Length == 0)
                        return;
                    nodes.Add(new Node { Text = text.ToString() });
                    text.Clear();
                }

                int i = 0;
                while (i < s.Length)
                {
                    char c = s[i];

                    if (c == '\\' && i + 1 < s.Length && Escapable.IndexOf(s[i + 1]) >= 0)
                    {
                        AppendEscaped(text, s[i + 1]);
                        i += 2;
                        continue;
                    }

                    if (c == '`')
                    {
                        if (TryCodeSpan(s, i, out string code, out int runLength, out int codeEnd))
                        {
                            Flush();
                            nodes.Add(new Node { Text = code });
                            i = codeEnd;
                        }
                        else
                        {
                            text.Append('`', runLength); // an unmatched run of backticks is just backticks
                            i += runLength;
                        }
                        continue;
                    }

                    if (c == '!' && i + 1 < s.Length && s[i + 1] == '[' && TryLinkLike(s, i + 1, out string alt, out _, out int imageEnd))
                    {
                        Flush();
                        nodes.Add(new Node { Text = Inline(alt) }); // an image is its alt text
                        i = imageEnd;
                        continue;
                    }

                    if (c == '[' && TryLinkLike(s, i, out string label, out string url, out int linkEnd))
                    {
                        Flush();
                        string linkStyle = _styles.Link;
                        nodes.Add(new Node { Text = LinkOpen(url) + Open(linkStyle) + Inline(label) + Close(linkStyle) + "</link>" });
                        i = linkEnd;
                        continue;
                    }

                    if (c == '<')
                    {
                        Match auto = Autolink.Match(s, i);
                        if (auto.Success)
                        {
                            Flush();
                            string autoUrl = auto.Groups[1].Value;
                            string linkStyle = _styles.Link;
                            nodes.Add(new Node { Text = LinkOpen(autoUrl) + Open(linkStyle) + EscapeText(autoUrl) + Close(linkStyle) + "</link>" });
                            i += auto.Length;
                            continue;
                        }
                    }

                    if (c == '*' || c == '_' || c == '~')
                    {
                        int run = 1;
                        while (i + run < s.Length && s[i + run] == c)
                            run++;

                        if (c == '~' && run != 2)
                        {
                            text.Append(c, run); // only a pair of tildes is strikethrough
                            i += run;
                            continue;
                        }

                        Flush();
                        nodes.Add(MakeDelimiter(s, i, run));
                        i += run;
                        continue;
                    }

                    AppendEscaped(text, c);
                    i++;
                }

                Flush();
                ProcessEmphasis(nodes);

                var result = new StringBuilder();
                foreach (Node node in nodes)
                    result.Append(node.Delim == 0 ? node.Text : new string(node.Delim, node.Count));
                return result.ToString();
            }

            private static bool IsWhitespace(char c) => char.IsWhiteSpace(c);
            private static bool IsPunctuation(char c) => char.IsPunctuation(c) || char.IsSymbol(c);

            // A run of delimiters, and whether it can open and/or close emphasis (the CommonMark flanking rules).
            private static Node MakeDelimiter(string s, int start, int run)
            {
                char c = s[start];
                char before = start == 0 ? ' ' : s[start - 1];
                char after = start + run >= s.Length ? ' ' : s[start + run];

                bool leftFlanking = !IsWhitespace(after) && (!IsPunctuation(after) || IsWhitespace(before) || IsPunctuation(before));
                bool rightFlanking = !IsWhitespace(before) && (!IsPunctuation(before) || IsWhitespace(after) || IsPunctuation(after));

                bool canOpen, canClose;
                if (c == '_')
                {
                    // not inside a word: snake_case is not emphasis
                    canOpen = leftFlanking && (!rightFlanking || IsPunctuation(before));
                    canClose = rightFlanking && (!leftFlanking || IsPunctuation(after));
                }
                else
                {
                    canOpen = leftFlanking;
                    canClose = rightFlanking;
                }

                return new Node { Delim = c, Count = run, OriginalCount = run, CanOpen = canOpen, CanClose = canClose };
            }

            private void ProcessEmphasis(List<Node> nodes)
            {
                for (int closerIndex = 0; closerIndex < nodes.Count; closerIndex++)
                {
                    Node closer = nodes[closerIndex];
                    if (closer.Delim == 0 || !closer.CanClose || !closer.Active)
                        continue;

                    while (closer.Count > 0)
                    {
                        int openerIndex = FindOpener(nodes, closerIndex, closer);
                        if (openerIndex < 0)
                            break;

                        Node opener = nodes[openerIndex];
                        int use = ChooseUse(opener, closer);
                        GetTags(closer.Delim, use, out string openTag, out string closeTag);

                        opener.Count -= use;
                        closer.Count -= use;

                        // what is left between the two can no longer open or close anything (and stays as text)
                        for (int k = openerIndex + 1; k < closerIndex; k++)
                            nodes[k].Active = false;

                        nodes.Insert(closerIndex, new Node { Text = closeTag });   // the closer is one further now
                        nodes.Insert(openerIndex + 1, new Node { Text = openTag }); // and one further again
                        closerIndex += 2;
                    }
                }
            }

            private static int FindOpener(List<Node> nodes, int closerIndex, Node closer)
            {
                for (int i = closerIndex - 1; i >= 0; i--)
                {
                    Node opener = nodes[i];
                    if (opener.Delim != closer.Delim || !opener.CanOpen || !opener.Active || opener.Count == 0)
                        continue;

                    if (closer.Delim == '~')
                    {
                        if (opener.Count < 2 || closer.Count < 2)
                            continue;
                        return i;
                    }

                    // CommonMark's "multiple of 3" rule, for runs that could both open and close
                    if ((opener.CanClose || closer.CanOpen)
                        && (opener.OriginalCount + closer.OriginalCount) % 3 == 0
                        && !(opener.OriginalCount % 3 == 0 && closer.OriginalCount % 3 == 0))
                        continue;

                    return i;
                }
                return -1;
            }

            private int ChooseUse(Node opener, Node closer)
            {
                if (closer.Delim == '~')
                    return 2;
                if (opener.Count >= 3 && closer.Count >= 3 && !string.IsNullOrEmpty(_styles.BoldItalic))
                    return 3;
                return opener.Count >= 2 && closer.Count >= 2 ? 2 : 1;
            }

            private void GetTags(char delimiter, int use, out string open, out string close)
            {
                if (delimiter == '~')
                {
                    open = Open(_styles.Strikethrough);
                    close = Close(_styles.Strikethrough);
                }
                else if (use == 3)
                {
                    if (!string.IsNullOrEmpty(_styles.BoldItalic))
                    {
                        open = Open(_styles.BoldItalic);
                        close = Close(_styles.BoldItalic);
                    }
                    else
                    {
                        open = Open(_styles.Bold) + Open(_styles.Italic);
                        close = Close(_styles.Italic) + Close(_styles.Bold);
                    }
                }
                else if (use == 2)
                {
                    open = Open(_styles.Bold);
                    close = Close(_styles.Bold);
                }
                else
                {
                    open = Open(_styles.Italic);
                    close = Close(_styles.Italic);
                }
            }

            // `code`: the text between two runs of backticks of the same length, as it is.
            private bool TryCodeSpan(string s, int start, out string result, out int runLength, out int end)
            {
                runLength = 1;
                while (start + runLength < s.Length && s[start + runLength] == '`')
                    runLength++;

                result = null;
                end = start + runLength;

                int j = start + runLength;
                while (j < s.Length)
                {
                    if (s[j] != '`')
                    {
                        j++;
                        continue;
                    }

                    int closingRun = 1;
                    while (j + closingRun < s.Length && s[j + closingRun] == '`')
                        closingRun++;

                    if (closingRun == runLength)
                    {
                        string content = s.Substring(start + runLength, j - start - runLength).Replace('\n', ' ');
                        if (content.Length >= 2 && content[0] == ' ' && content[content.Length - 1] == ' ' && content.Trim(' ').Length > 0)
                            content = content.Substring(1, content.Length - 2);

                        string style = _styles.InlineCode;
                        result = Open(style) + EscapeText(content) + Close(style);
                        end = j + closingRun;
                        return true;
                    }

                    j += closingRun;
                }

                return false;
            }

            // [label](destination "title") starting at the bracket
            private static bool TryLinkLike(string s, int open, out string label, out string url, out int end)
            {
                label = null;
                url = null;
                end = open;

                int depth = 0;
                int close = -1;
                for (int j = open; j < s.Length; j++)
                {
                    char c = s[j];
                    if (c == '\\')
                    {
                        j++;
                        continue;
                    }
                    if (c == '[')
                    {
                        depth++;
                    }
                    else if (c == ']')
                    {
                        depth--;
                        if (depth == 0)
                        {
                            close = j;
                            break;
                        }
                    }
                }

                if (close < 0 || close + 1 >= s.Length || s[close + 1] != '(')
                    return false;

                int p = close + 2;
                SkipSpaces(s, ref p);

                var destination = new StringBuilder();
                if (p < s.Length && s[p] == '<')
                {
                    p++;
                    while (p < s.Length && s[p] != '>' && s[p] != '\n')
                    {
                        destination.Append(s[p]);
                        p++;
                    }
                    if (p >= s.Length || s[p] != '>')
                        return false;
                    p++;
                }
                else
                {
                    int parentheses = 0;
                    while (p < s.Length)
                    {
                        char c = s[p];
                        if (c == '\\' && p + 1 < s.Length)
                        {
                            destination.Append(s[p + 1]);
                            p += 2;
                            continue;
                        }
                        if (c == ' ' || c == '\t' || c == '\n')
                            break;
                        if (c == '(')
                        {
                            parentheses++;
                        }
                        else if (c == ')')
                        {
                            if (parentheses == 0)
                                break;
                            parentheses--;
                        }
                        destination.Append(c);
                        p++;
                    }
                }

                SkipSpaces(s, ref p);

                if (p < s.Length && (s[p] == '"' || s[p] == '\'' || s[p] == '('))
                {
                    char closing = s[p] == '(' ? ')' : s[p];
                    p++;
                    while (p < s.Length && s[p] != closing)
                    {
                        if (s[p] == '\\')
                            p++;
                        p++;
                    }
                    if (p >= s.Length)
                        return false;
                    p++;
                    SkipSpaces(s, ref p);
                }

                if (p >= s.Length || s[p] != ')')
                    return false;

                label = s.Substring(open + 1, close - open - 1);
                url = destination.ToString();
                end = p + 1;
                return true;
            }

            private static void SkipSpaces(string s, ref int p)
            {
                while (p < s.Length && (s[p] == ' ' || s[p] == '\t' || s[p] == '\n'))
                    p++;
            }
        }
    }
}
