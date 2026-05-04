using DoTrack.GitProviders.Abstractions;
using Shouldly;

namespace DoTrack.GitProviders.Tests;

public class IssueKeyDetectorTests
{
    [Fact]
    public void Extract_SingleKey_Found()
    {
        IssueKeyDetector.Extract("Fix PROJ-42 and ship").ShouldBe(["PROJ-42"]);
    }

    [Fact]
    public void Extract_MultipleKeys_AllFoundInOrder()
    {
        IssueKeyDetector.Extract("Closes PROJ-42 and PROJ-43, related to FOO-7")
            .ShouldBe(["PROJ-42", "PROJ-43", "FOO-7"]);
    }

    [Fact]
    public void Extract_DuplicateKeys_Deduplicated()
    {
        IssueKeyDetector.Extract("PROJ-42 again PROJ-42 and PROJ-42").ShouldBe(["PROJ-42"]);
    }

    [Fact]
    public void Extract_KeyAtStart_Found()
    {
        IssueKeyDetector.Extract("PROJ-1 first").ShouldBe(["PROJ-1"]);
    }

    [Fact]
    public void Extract_KeyAtEnd_Found()
    {
        IssueKeyDetector.Extract("last PROJ-99").ShouldBe(["PROJ-99"]);
    }

    [Theory]
    [InlineData("PROJ-42.")]
    [InlineData("PROJ-42,")]
    [InlineData("PROJ-42;")]
    [InlineData("PROJ-42!")]
    [InlineData("PROJ-42?")]
    [InlineData("PROJ-42)")]
    [InlineData("(PROJ-42)")]
    [InlineData("[PROJ-42]")]
    [InlineData("\"PROJ-42\"")]
    [InlineData("'PROJ-42'")]
    public void Extract_SurroundingPunctuation_StillMatches(string input)
    {
        IssueKeyDetector.Extract(input).ShouldBe(["PROJ-42"]);
    }

    [Theory]
    [InlineData("proj-42")]
    [InlineData("Proj-42")]
    [InlineData("pROJ-42")]
    public void Extract_LowercaseOrMixedCase_DoesNotMatch(string input)
    {
        IssueKeyDetector.Extract(input).ShouldBeEmpty();
    }

    [Fact]
    public void Extract_DigitsOnlyPrefix_DoesNotMatch()
    {
        IssueKeyDetector.Extract("123-42 nope").ShouldBeEmpty();
    }

    [Fact]
    public void Extract_NoNumber_DoesNotMatch()
    {
        IssueKeyDetector.Extract("PROJECT- nope").ShouldBeEmpty();
    }

    [Fact]
    public void Extract_NumberFollowedByLetter_DoesNotMatchInWord()
    {
        IssueKeyDetector.Extract("PROJ-42abc").ShouldBeEmpty();
    }

    [Fact]
    public void Extract_LetterDigitMixedPrefix_Allowed()
    {
        IssueKeyDetector.Extract("AB12-7").ShouldBe(["AB12-7"]);
    }

    [Fact]
    public void Extract_UnderscorePrefix_Allowed()
    {
        IssueKeyDetector.Extract("PROJ_BETA-12").ShouldBe(["PROJ_BETA-12"]);
    }

    [Fact]
    public void Extract_EmbeddedInUrlPath_StillMatches()
    {
        IssueKeyDetector.Extract("see https://issues.example.com/PROJ-42/details").ShouldBe(["PROJ-42"]);
    }

    [Fact]
    public void Extract_MultiLineText_ScansAcrossNewlines()
    {
        var text = "first line PROJ-1\nsecond line PROJ-2\rthird PROJ-3";
        IssueKeyDetector.Extract(text).ShouldBe(["PROJ-1", "PROJ-2", "PROJ-3"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Extract_NullOrEmpty_ReturnsEmpty(string? input)
    {
        IssueKeyDetector.Extract(input).ShouldBeEmpty();
    }

    [Fact]
    public void Extract_WhitespaceOnly_ReturnsEmpty()
    {
        IssueKeyDetector.Extract("   \t\n  ").ShouldBeEmpty();
    }

    [Fact]
    public void Extract_VeryLongPrefix_StillMatches()
    {
        var longPrefix = new string('Z', 200);
        var input = $"see {longPrefix}-1 here";
        IssueKeyDetector.Extract(input).ShouldBe([$"{longPrefix}-1"]);
    }

    [Fact]
    public void Extract_VeryLongNumber_StillMatches()
    {
        IssueKeyDetector.Extract("PROJ-9999999999").ShouldBe(["PROJ-9999999999"]);
    }

    [Fact]
    public void Extract_PrefixIsLongerWord_FullPrefixMatched()
    {
        // "XPROJ" is the project key here, not "PROJ" — uppercase letters are
        // contiguous so the regex consumes the whole prefix.
        IssueKeyDetector.Extract("XPROJ-42").ShouldBe(["XPROJ-42"]);
    }

    [Fact]
    public void Extract_LowercaseLetterBeforeKey_BlocksWordBoundary()
    {
        // No word boundary between 'x' and 'P' (both word chars), and the regex
        // requires uppercase at the start of the prefix.
        IssueKeyDetector.Extract("xPROJ-42").ShouldBeEmpty();
    }

    [Fact]
    public void Extract_LetterPrefixNoSpace_NoMatch()
    {
        IssueKeyDetector.Extract("see issuePROJ-42 yo").ShouldBeEmpty();
    }

    [Fact]
    public void Extract_UnicodeLettersInPrefix_DoNotMatch()
    {
        IssueKeyDetector.Extract("ПРОЕ-42 русский").ShouldBeEmpty();
        IssueKeyDetector.Extract("プロジ-42").ShouldBeEmpty();
    }

    [Fact]
    public void Extract_KeyInCommitMessage_Found()
    {
        var message = """
            PROJ-42 #fixed: redirect handling for trailing slash

            Also touches PROJ-43 in passing.
            """;
        IssueKeyDetector.Extract(message).ShouldBe(["PROJ-42", "PROJ-43"]);
    }

    [Fact]
    public void Extract_KeyInBranchName_Found()
    {
        IssueKeyDetector.Extract("feature/PROJ-42-redirect-fix").ShouldBe(["PROJ-42"]);
    }

    [Fact]
    public void Extract_TwoLetterPrefix_Allowed()
    {
        IssueKeyDetector.Extract("OK go AB-1 here").ShouldBe(["AB-1"]);
    }

    [Fact]
    public void Extract_SingleLetterPrefix_NotAllowed()
    {
        IssueKeyDetector.Extract("X-1 single letter").ShouldBeEmpty();
    }
}
