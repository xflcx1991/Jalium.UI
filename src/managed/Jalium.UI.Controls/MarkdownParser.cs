using System.Text;
using System.Text.RegularExpressions;

namespace Jalium.UI.Controls;

internal abstract record MarkdownBlock;
internal sealed record MarkdownParagraphBlock(IReadOnlyList<MarkdownInline> Inlines) : MarkdownBlock;
internal sealed record MarkdownHeadingBlock(int Level, IReadOnlyList<MarkdownInline> Inlines) : MarkdownBlock;
internal sealed record MarkdownListBlock(bool Ordered, int StartIndex, IReadOnlyList<MarkdownListItemBlock> Items) : MarkdownBlock;
internal sealed record MarkdownListItemBlock(bool? TaskState, IReadOnlyList<MarkdownBlock> Blocks);
internal sealed record MarkdownQuoteBlock(IReadOnlyList<MarkdownBlock> Blocks) : MarkdownBlock;
internal sealed record MarkdownCodeBlock(string Text, string? Language) : MarkdownBlock;
internal sealed record MarkdownRuleBlock() : MarkdownBlock;
internal sealed record MarkdownTableBlock(IReadOnlyList<MarkdownTableRow> HeaderRows, IReadOnlyList<MarkdownTableRow> Rows) : MarkdownBlock;
internal sealed record MarkdownTableRow(IReadOnlyList<IReadOnlyList<MarkdownInline>> Cells);

internal abstract record MarkdownInline;
internal sealed record MarkdownTextInline(string Text) : MarkdownInline;
internal sealed record MarkdownStrongInline(IReadOnlyList<MarkdownInline> Children) : MarkdownInline;
internal sealed record MarkdownEmphasisInline(IReadOnlyList<MarkdownInline> Children) : MarkdownInline;
internal sealed record MarkdownCodeInline(string Text) : MarkdownInline;
internal sealed record MarkdownLinkInline(IReadOnlyList<MarkdownInline> Children, Uri? Uri, string Target) : MarkdownInline;
internal sealed record MarkdownLineBreakInline() : MarkdownInline;

internal static class MarkdownParser
{
    private static readonly Regex s_tableSeparatorRegex =
        new(@"^\s*\|?(?:\s*:?-{3,}:?\s*\|)+(?:\s*:?-{3,}:?\s*)\|?\s*$", RegexOptions.Compiled);

    public static IReadOnlyList<MarkdownBlock> Parse(string? markdown, Uri? baseUri)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return Array.Empty<MarkdownBlock>();
        }

        var normalized = Normalize(markdown);
        var lines = normalized.Split('\n');
        var index = 0;
        return ParseBlocks(lines, ref index, baseUri);
    }

    private static IReadOnlyList<MarkdownBlock> ParseBlocks(string[] lines, ref int index, Uri? baseUri)
    {
        var blocks = new List<MarkdownBlock>();

        while (index < lines.Length)
        {
            if (string.IsNullOrWhiteSpace(lines[index]))
            {
                index++;
                continue;
            }

            var trimmed = lines[index].TrimStart();
            if (TryParseCodeBlock(lines, ref index, out var codeBlock))
            {
                blocks.Add(codeBlock);
                continue;
            }

            if (IsHorizontalRule(trimmed))
            {
                blocks.Add(new MarkdownRuleBlock());
                index++;
                continue;
            }

            if (TryParseHeading(trimmed, out var level, out var headingContent))
            {
                blocks.Add(new MarkdownHeadingBlock(level, ParseInlines(headingContent, baseUri)));
                index++;
                continue;
            }

            if (IsBlockQuoteLine(trimmed))
            {
                blocks.Add(ParseQuote(lines, ref index, baseUri));
                continue;
            }

            if (TryMatchListMarker(lines[index], out _))
            {
                blocks.Add(ParseList(lines, ref index, baseUri));
                continue;
            }

            if (IsTableStart(lines, index))
            {
                blocks.Add(ParseTable(lines, ref index, baseUri));
                continue;
            }

            blocks.Add(ParseParagraph(lines, ref index, baseUri));
        }

        return blocks;
    }

    private static bool TryParseCodeBlock(string[] lines, ref int index, out MarkdownCodeBlock block)
    {
        block = null!;
        var trimmed = lines[index].TrimStart();
        if (!TryGetFence(trimmed, out var openingFence, out var language))
        {
            return false;
        }

        block = ParseCodeBlock(lines, ref index, openingFence, language);
        return true;
    }

    private static MarkdownCodeBlock ParseCodeBlock(string[] lines, ref int index, string openingFence, string? language)
    {
        var fenceChar = openingFence[0];
        var fenceLength = openingFence.Length;
        var builder = new StringBuilder();
        index++;

        while (index < lines.Length)
        {
            var trimmed = lines[index].TrimStart();
            if (trimmed.Length >= fenceLength &&
                trimmed.StartsWith(new string(fenceChar, fenceLength), StringComparison.Ordinal))
            {
                index++;
                break;
            }

            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            builder.Append(lines[index]);
            index++;
        }

        return new MarkdownCodeBlock(builder.ToString(), language);
    }

    private static bool TryGetFence(string line, out string openingFence, out string? language)
    {
        openingFence = string.Empty;
        language = null;

        if (line.Length < 3)
        {
            return false;
        }

        var marker = line[0];
        if (marker != '`' && marker != '~')
        {
            return false;
        }

        var count = 0;
        while (count < line.Length && line[count] == marker)
        {
            count++;
        }

        if (count < 3)
        {
            return false;
        }

        openingFence = new string(marker, count);
        var remainder = line.Substring(count).Trim();
        language = string.IsNullOrWhiteSpace(remainder) ? null : remainder;
        return true;
    }

    private static bool IsHorizontalRule(string trimmedLine)
    {
        var content = trimmedLine.Replace(" ", string.Empty, StringComparison.Ordinal);
        if (content.Length < 3)
        {
            return false;
        }

        var marker = content[0];
        if (marker != '-' && marker != '*' && marker != '_')
        {
            return false;
        }

        for (var index = 1; index < content.Length; index++)
        {
            if (content[index] != marker)
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryParseHeading(string trimmedLine, out int level, out string content)
    {
        level = 0;
        content = string.Empty;

        while (level < trimmedLine.Length && level < 6 && trimmedLine[level] == '#')
        {
            level++;
        }

        if (level == 0 || level >= trimmedLine.Length || trimmedLine[level] != ' ')
        {
            return false;
        }

        content = trimmedLine.Substring(level + 1).Trim();
        return true;
    }

    private static bool IsBlockQuoteLine(string trimmedLine) =>
        trimmedLine.StartsWith(">", StringComparison.Ordinal);

    private static MarkdownQuoteBlock ParseQuote(string[] lines, ref int index, Uri? baseUri)
    {
        var quoteLines = new List<string>();

        while (index < lines.Length)
        {
            if (string.IsNullOrWhiteSpace(lines[index]))
            {
                quoteLines.Add(string.Empty);
                index++;
                continue;
            }

            var trimmed = lines[index].TrimStart();
            if (!trimmed.StartsWith(">", StringComparison.Ordinal))
            {
                break;
            }

            var content = trimmed.Substring(1);
            if (content.StartsWith(" ", StringComparison.Ordinal))
            {
                content = content.Substring(1);
            }

            quoteLines.Add(content);
            index++;
        }

        var nestedIndex = 0;
        var nestedBlocks = ParseBlocks(quoteLines.ToArray(), ref nestedIndex, baseUri);
        return new MarkdownQuoteBlock(nestedBlocks);
    }

    private static MarkdownListBlock ParseList(string[] lines, ref int index, Uri? baseUri)
    {
        TryMatchListMarker(lines[index], out var firstMatch);
        var items = new List<MarkdownListItemBlock>();
        var ordered = firstMatch.Ordered;
        var indent = firstMatch.Indent;
        var startIndex = firstMatch.StartIndex;

        while (index < lines.Length)
        {
            if (!TryMatchListMarker(lines[index], out var match) ||
                match.Indent != indent ||
                match.Ordered != ordered)
            {
                break;
            }

            var itemLines = new List<string> { match.Content };
            var taskState = match.TaskState;
            index++;

            while (index < lines.Length)
            {
                var line = lines[index];
                if (string.IsNullOrWhiteSpace(line))
                {
                    var lookahead = index + 1;
                    while (lookahead < lines.Length && string.IsNullOrWhiteSpace(lines[lookahead]))
                    {
                        lookahead++;
                    }

                    if (lookahead >= lines.Length)
                    {
                        index = lookahead;
                        break;
                    }

                    var nextLine = lines[lookahead];
                    var nextIndent = CountIndent(nextLine);
                    if (nextIndent <= indent)
                    {
                        break;
                    }

                    itemLines.Add(string.Empty);
                    index++;
                    continue;
                }

                if (TryMatchListMarker(line, out var nextMatch) &&
                    nextMatch.Indent == indent &&
                    nextMatch.Ordered == ordered)
                {
                    break;
                }

                var lineIndent = CountIndent(line);
                if (lineIndent < indent)
                {
                    break;
                }

                var trimIndent = Math.Min(lineIndent, indent + 2);
                itemLines.Add(line.Substring(trimIndent));
                index++;
            }

            var nestedIndex = 0;
            var nestedBlocks = ParseBlocks(itemLines.ToArray(), ref nestedIndex, baseUri);
            if (nestedBlocks.Count == 0)
            {
                nestedBlocks = new[] { new MarkdownParagraphBlock(Array.Empty<MarkdownInline>()) };
            }

            items.Add(new MarkdownListItemBlock(taskState, nestedBlocks));
        }

        return new MarkdownListBlock(ordered, startIndex, items);
    }

    private static bool TryMatchListMarker(string line, out ListMarkerMatch match)
    {
        match = default;

        var indent = CountIndent(line);
        var trimmed = line.Substring(indent);
        if (trimmed.Length < 2)
        {
            return false;
        }

        if (char.IsDigit(trimmed[0]))
        {
            var cursor = 0;
            while (cursor < trimmed.Length && char.IsDigit(trimmed[cursor]))
            {
                cursor++;
            }

            if (cursor == 0 || cursor + 1 >= trimmed.Length)
            {
                return false;
            }

            if ((trimmed[cursor] != '.' && trimmed[cursor] != ')') || trimmed[cursor + 1] != ' ')
            {
                return false;
            }

            var number = int.Parse(trimmed.Substring(0, cursor));
            match = new ListMarkerMatch(indent, Ordered: true, StartIndex: number, Content: trimmed.Substring(cursor + 2), TaskState: null);
            return true;
        }

        var marker = trimmed[0];
        if ((marker != '-' && marker != '+' && marker != '*') || trimmed[1] != ' ')
        {
            return false;
        }

        var content = trimmed.Substring(2);
        bool? taskState = null;
        if (content.Length >= 3 &&
            content[0] == '[' &&
            content[2] == ']' &&
            (content[1] == ' ' || content[1] == 'x' || content[1] == 'X'))
        {
            taskState = content[1] == 'x' || content[1] == 'X';
            content = content.Length > 3 && content[3] == ' '
                ? content.Substring(4)
                : content.Substring(3);
        }

        match = new ListMarkerMatch(indent, Ordered: false, StartIndex: 1, Content: content, TaskState: taskState);
        return true;
    }

    private static bool IsTableStart(string[] lines, int index)
    {
        if (index + 1 >= lines.Length)
        {
            return false;
        }

        var header = lines[index].Trim();
        var separator = lines[index + 1].Trim();
        return header.Contains('|', StringComparison.Ordinal) && s_tableSeparatorRegex.IsMatch(separator);
    }

    private static MarkdownTableBlock ParseTable(string[] lines, ref int index, Uri? baseUri)
    {
        var header = ParseTableRow(lines[index], baseUri);
        index += 2;

        var rows = new List<MarkdownTableRow>();
        while (index < lines.Length)
        {
            if (string.IsNullOrWhiteSpace(lines[index]) || !lines[index].Contains('|', StringComparison.Ordinal))
            {
                break;
            }

            rows.Add(ParseTableRow(lines[index], baseUri));
            index++;
        }

        return new MarkdownTableBlock(new[] { header }, rows);
    }

    private static MarkdownTableRow ParseTableRow(string line, Uri? baseUri)
    {
        var trimmed = line.Trim();
        if (trimmed.StartsWith("|", StringComparison.Ordinal))
        {
            trimmed = trimmed.Substring(1);
        }

        if (trimmed.EndsWith("|", StringComparison.Ordinal))
        {
            trimmed = trimmed.Substring(0, trimmed.Length - 1);
        }

        var cells = trimmed.Split('|')
            .Select(static cell => cell.Trim())
            .Select(cell => (IReadOnlyList<MarkdownInline>)ParseInlines(cell, baseUri))
            .ToArray();

        return new MarkdownTableRow(cells);
    }

    private static MarkdownParagraphBlock ParseParagraph(string[] lines, ref int index, Uri? baseUri)
    {
        var paragraphLines = new List<string>();

        while (index < lines.Length)
        {
            if (string.IsNullOrWhiteSpace(lines[index]))
            {
                break;
            }

            if (paragraphLines.Count > 0)
            {
                var trimmed = lines[index].TrimStart();
                if (TryGetFence(trimmed, out _, out _) ||
                    TryParseHeading(trimmed, out _, out _) ||
                    IsHorizontalRule(trimmed) ||
                    IsBlockQuoteLine(trimmed) ||
                    TryMatchListMarker(lines[index], out _) ||
                    IsTableStart(lines, index))
                {
                    break;
                }
            }

            paragraphLines.Add(lines[index].TrimStart());
            index++;
        }

        var inlines = new List<MarkdownInline>();
        for (var lineIndex = 0; lineIndex < paragraphLines.Count; lineIndex++)
        {
            var line = paragraphLines[lineIndex];
            var explicitBreak = line.EndsWith("  ", StringComparison.Ordinal) ||
                                line.EndsWith("\\", StringComparison.Ordinal);
            var content = explicitBreak
                ? line.TrimEnd(' ', '\\')
                : line.Trim();

            AppendInlines(inlines, ParseInlines(content, baseUri));
            if (lineIndex < paragraphLines.Count - 1)
            {
                inlines.Add(explicitBreak ? new MarkdownLineBreakInline() : new MarkdownTextInline(" "));
            }
        }

        return new MarkdownParagraphBlock(MergeTextInlines(inlines));
    }

    internal static IReadOnlyList<MarkdownInline> ParseInlines(string text, Uri? baseUri)
    {
        var result = new List<MarkdownInline>();
        var buffer = new StringBuilder();

        for (var index = 0; index < text.Length;)
        {
            if (text[index] == '\\' && index + 1 < text.Length)
            {
                buffer.Append(text[index + 1]);
                index += 2;
                continue;
            }

            if (text[index] == '`')
            {
                var closing = FindUnescaped(text, index + 1, "`");
                if (closing > index)
                {
                    FlushBuffer(buffer, result);
                    result.Add(new MarkdownCodeInline(text.Substring(index + 1, closing - index - 1)));
                    index = closing + 1;
                    continue;
                }
            }

            if (index + 1 < text.Length &&
                ((text[index] == '*' && text[index + 1] == '*') ||
                 (text[index] == '_' && text[index + 1] == '_')))
            {
                var delimiter = text.Substring(index, 2);
                var closing = FindUnescaped(text, index + 2, delimiter);
                if (closing > index)
                {
                    FlushBuffer(buffer, result);
                    result.Add(new MarkdownStrongInline(ParseInlines(text.Substring(index + 2, closing - index - 2), baseUri)));
                    index = closing + 2;
                    continue;
                }
            }

            if (text[index] == '*' || text[index] == '_')
            {
                var delimiter = text[index].ToString();
                var closing = FindUnescaped(text, index + 1, delimiter);
                if (closing > index)
                {
                    FlushBuffer(buffer, result);
                    result.Add(new MarkdownEmphasisInline(ParseInlines(text.Substring(index + 1, closing - index - 1), baseUri)));
                    index = closing + 1;
                    continue;
                }
            }

            if (text[index] == '[' &&
                TryParseLink(text, index, baseUri, out var link, out var consumed))
            {
                FlushBuffer(buffer, result);
                result.Add(link);
                index += consumed;
                continue;
            }

            buffer.Append(text[index]);
            index++;
        }

        FlushBuffer(buffer, result);
        return MergeTextInlines(result);
    }

    private static bool TryParseLink(string text, int startIndex, Uri? baseUri, out MarkdownLinkInline link, out int consumed)
    {
        link = null!;
        consumed = 0;

        var closeBracket = FindMatching(text, startIndex, '[', ']');
        if (closeBracket < 0 || closeBracket + 1 >= text.Length || text[closeBracket + 1] != '(')
        {
            return false;
        }

        var closeParen = FindMatching(text, closeBracket + 1, '(', ')');
        if (closeParen < 0)
        {
            return false;
        }

        var linkText = text.Substring(startIndex + 1, closeBracket - startIndex - 1);
        var target = text.Substring(closeBracket + 2, closeParen - closeBracket - 2).Trim();
        link = new MarkdownLinkInline(ParseInlines(linkText, baseUri), ResolveUri(target, baseUri), target);
        consumed = closeParen - startIndex + 1;
        return true;
    }

    private static Uri? ResolveUri(string target, Uri? baseUri)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return null;
        }

        if (Uri.TryCreate(target, UriKind.Absolute, out var absolute))
        {
            return absolute;
        }

        if (baseUri != null && Uri.TryCreate(baseUri, target, out var relativeToBase))
        {
            return relativeToBase;
        }

        if (Uri.TryCreate(target, UriKind.Relative, out var relative))
        {
            return relative;
        }

        return null;
    }

    private static int FindMatching(string text, int openIndex, char openChar, char closeChar)
    {
        var depth = 0;
        for (var index = openIndex; index < text.Length; index++)
        {
            if (text[index] == openChar && !IsEscaped(text, index))
            {
                depth++;
            }
            else if (text[index] == closeChar && !IsEscaped(text, index))
            {
                depth--;
                if (depth == 0)
                {
                    return index;
                }
            }
        }

        return -1;
    }

    private static int FindUnescaped(string text, int startIndex, string delimiter)
    {
        for (var index = startIndex; index <= text.Length - delimiter.Length; index++)
        {
            if (text.AsSpan(index, delimiter.Length).SequenceEqual(delimiter) && !IsEscaped(text, index))
            {
                return index;
            }
        }

        return -1;
    }

    private static bool IsEscaped(string text, int index)
    {
        var slashCount = 0;
        for (var cursor = index - 1; cursor >= 0 && text[cursor] == '\\'; cursor--)
        {
            slashCount++;
        }

        return slashCount % 2 == 1;
    }

    private static void FlushBuffer(StringBuilder buffer, List<MarkdownInline> result)
    {
        if (buffer.Length == 0)
        {
            return;
        }

        result.Add(new MarkdownTextInline(buffer.ToString()));
        buffer.Clear();
    }

    private static IReadOnlyList<MarkdownInline> MergeTextInlines(IEnumerable<MarkdownInline> source)
    {
        var merged = new List<MarkdownInline>();
        StringBuilder? textBuffer = null;

        foreach (var inline in source)
        {
            if (inline is MarkdownTextInline textInline)
            {
                textBuffer ??= new StringBuilder();
                textBuffer.Append(textInline.Text);
                continue;
            }

            if (textBuffer is { Length: > 0 })
            {
                merged.Add(new MarkdownTextInline(textBuffer.ToString()));
                textBuffer.Clear();
            }

            merged.Add(inline);
        }

        if (textBuffer is { Length: > 0 })
        {
            merged.Add(new MarkdownTextInline(textBuffer.ToString()));
        }

        return merged;
    }

    private static void AppendInlines(List<MarkdownInline> target, IEnumerable<MarkdownInline> source)
    {
        foreach (var inline in source)
        {
            target.Add(inline);
        }
    }

    private static int CountIndent(string line)
    {
        var count = 0;
        while (count < line.Length && line[count] == ' ')
        {
            count++;
        }

        return count;
    }

    private static string Normalize(string markdown) =>
        markdown
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Replace("\t", "    ", StringComparison.Ordinal);

    private readonly record struct ListMarkerMatch(int Indent, bool Ordered, int StartIndex, string Content, bool? TaskState);
}
