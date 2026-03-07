using Jalium.UI.Controls.Editor;

namespace Jalium.UI.Tests;

public class JalxamlSyntaxHighlighterRazorTests
{
    [Fact]
    public void HighlightLine_RazorExpressionInAttribute_SplitsExpressionTokens()
    {
        const string line = """<TextBlock Text='@(Count > 0 ? "A(1)" : "B")' />""";

        var tokens = Highlight(line);

        Assert.Contains(tokens, token => token.Text == "@" && token.Classification == TokenClassification.Operator);
        Assert.Contains(tokens, token => token.Text == "(" && token.Classification == TokenClassification.Punctuation);
        Assert.Contains(tokens, token => token.Text == "Count" && token.Classification == TokenClassification.BindingPath);
        Assert.Contains(tokens, token => token.Text == ">" && token.Classification == TokenClassification.Operator);
        Assert.Contains(tokens, token => token.Text == "0" && token.Classification == TokenClassification.Number);
        Assert.Contains(tokens, token => token.Text == "\"A(1)\"" && token.Classification == TokenClassification.String);
        Assert.Contains(tokens, token => token.Text == "\"B\"" && token.Classification == TokenClassification.String);
    }

    [Fact]
    public void HighlightLine_RazorEscapesInAttribute_OnlyHighlightsDynamicSegment()
    {
        const string line = """<TextBlock Text="@@prefix \@escaped @Name" />""";

        var tokens = Highlight(line);

        Assert.Equal(1, tokens.Count(token => token.Text == "@" && token.Classification == TokenClassification.Operator));
        Assert.Equal(1, tokens.Count(token => token.Text == "Name" && token.Classification == TokenClassification.BindingPath));
        Assert.DoesNotContain(tokens, token => token.Text == "prefix" && token.Classification == TokenClassification.BindingPath);
        Assert.DoesNotContain(tokens, token => token.Text == "escaped" && token.Classification == TokenClassification.BindingPath);
    }

    [Fact]
    public void HighlightLine_RazorIfTextNode_HighlightsDirectiveConditionAndBraces()
    {
        const string line = """<StackPanel>@if(IsOnline){<Border x:Name="OnlineBorder" />}</StackPanel>""";

        var tokens = Highlight(line);

        Assert.Contains(tokens, token => token.Text == "@" && token.Classification == TokenClassification.Operator);
        Assert.Contains(tokens, token => token.Text == "if" && token.Classification == TokenClassification.ControlKeyword);
        Assert.Contains(tokens, token => token.Text == "IsOnline" && token.Classification == TokenClassification.BindingPath);
        Assert.Contains(tokens, token => token.Text == "{" && token.Classification == TokenClassification.Operator);
        Assert.Contains(tokens, token => token.Text == "}" && token.Classification == TokenClassification.Operator);
    }

    [Fact]
    public void HighlightLine_RazorMultipleAtEscapes_OnlyHighlightsDynamicTail()
    {
        const string line = """<TextBlock Text="@@@Name" />""";

        var tokens = Highlight(line);

        Assert.Equal(1, tokens.Count(token => token.Text == "@" && token.Classification == TokenClassification.Operator));
        Assert.Equal(1, tokens.Count(token => token.Text == "Name" && token.Classification == TokenClassification.BindingPath));
    }

    [Fact]
    public void HighlightLine_RazorBackslashEscapeBeforeDynamicAt_HighlightsOnlyUnescapedSegment()
    {
        const string line = """<TextBlock Text="\@@Name" />""";

        var tokens = Highlight(line);

        Assert.Equal(1, tokens.Count(token => token.Text == "@" && token.Classification == TokenClassification.Operator));
        Assert.Equal(1, tokens.Count(token => token.Text == "Name" && token.Classification == TokenClassification.BindingPath));
    }

    [Fact]
    public void HighlightLine_RazorEscapedAtWithoutDynamicTail_DoesNotEmitBindingPath()
    {
        const string line = """<TextBlock Text="@@\@Name" />""";

        var tokens = Highlight(line);

        Assert.DoesNotContain(tokens, token => token.Classification == TokenClassification.BindingPath);
        Assert.DoesNotContain(tokens, token => token.Text == "@" && token.Classification == TokenClassification.Operator);
    }

    [Fact]
    public void HighlightLine_RazorCodeBlock_HighlightsKeywordsAndMethodName()
    {
        const string line = """@{ void test(){} }""";

        var tokens = Highlight(line);

        Assert.Contains(tokens, token => token.Text == "@" && token.Classification == TokenClassification.Operator);
        Assert.Contains(tokens, token => token.Text == "void" && token.Classification == TokenClassification.Keyword);
        Assert.Contains(tokens, token => token.Text == "test" && token.Classification == TokenClassification.Method);
    }

    private static IReadOnlyList<(string Text, TokenClassification Classification)> Highlight(string line)
    {
        var highlighter = JalxamlSyntaxHighlighter.Create();
        var (tokens, _) = highlighter.HighlightLine(1, line, highlighter.GetInitialState());

        return tokens
            .Where(token => token.Length > 0 && token.Classification != TokenClassification.PlainText)
            .Select(token => (line.Substring(token.StartOffset, token.Length), token.Classification))
            .ToArray();
    }
}
