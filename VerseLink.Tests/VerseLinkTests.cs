using VerseLinkWindows;

namespace VerseLink.Tests;

/// <summary>
/// Exercises the public retrieval path against <c>Bibles\TEST.xml</c>. Each group below
/// pins down a bug that shipped at some point, so a regression fails loudly rather than
/// returning empty text the way the plugin silently used to.
/// </summary>
[TestClass]
public class VerseLinkTests
{
    private const string Version = "TEST";

    private static string Dir => AppContext.BaseDirectory;

    private static BibleReferenceVerseFormat Format(
        bool includeReference = false,
        bool includeVerseNumbers = false,
        bool includeNewLineBetweenChapters = false) =>
        new()
        {
            IncludeReference = includeReference,
            IncludeVerseNumbers = includeVerseNumbers,
            IncludeNewLineBetweenChapters = includeNewLineBetweenChapters,
        };

    private static string Retrieve(string reference, BibleReferenceVerseFormat? format = null) =>
        new VerseLinkWindows.VerseLink(Version, Dir, format ?? Format()).VerseLinkRetrieve(reference);

    [TestMethod]
    public void LoadsTheConfiguredVersion()
    {
        var vl = new VerseLinkWindows.VerseLink(Version, Dir, Format());
        Assert.IsFalse(vl.Error, vl.LastError);
    }

    [TestMethod]
    public void MissingVersionFileReportsErrorRatherThanThrowing()
    {
        var vl = new VerseLinkWindows.VerseLink("NOPE", Dir, Format());
        Assert.IsTrue(vl.Error);
        Assert.AreEqual(string.Empty, vl.VerseLinkRetrieve("Genesis 1:1"));
    }

    // reference[0].Equals(2) boxed an int, so the test was always false and every book
    // with a numeric prefix fell through to a parse that threw and was swallowed.
    [TestMethod]
    [DataRow("1 Samuel 1:1", "first samuel text")]
    [DataRow("2 Timothy 3:16", "second timothy text")]
    [DataRow("3 John 1:4", "third john text")]
    public void NumberedBookPrefixesResolve(string reference, string expected)
        => Assert.AreEqual(expected, Retrieve(reference));

    // The book pattern was [a-z]+, which could not span a space.
    [TestMethod]
    public void MultiWordBookNamesResolve()
        => Assert.AreEqual("song of solomon text", Retrieve("Song of Solomon 1:1"));

    [TestMethod]
    public void BookLookupIsCaseInsensitive()
        => Assert.AreEqual("alpha one", Retrieve("genesis 1:1"));

    [TestMethod]
    public void SingleVerseResolves()
        => Assert.AreEqual("alpha one", Retrieve("Genesis 1:1"));

    // Single verses bypassed formatVerse, so this setting never applied to them.
    [TestMethod]
    public void SingleVerseHonoursIncludeVerseNumbers()
        => Assert.AreEqual("1 alpha one", Retrieve("Genesis 1:1", Format(includeVerseNumbers: true)));

    // Verse numbers live on attribute "n"; the code read "v" and concatenated the
    // XAttribute itself, which stringifies as markup.
    [TestMethod]
    public void VerseNumbersUseTheNumberNotTheMarkup()
    {
        var actual = Retrieve("Genesis 1:1-2", Format(includeVerseNumbers: true));
        Assert.AreEqual("1 alpha one 2 alpha two", actual);
        StringAssert.DoesNotMatch(actual, new System.Text.RegularExpressions.Regex("[nv]=\""));
    }

    [TestMethod]
    public void IncludeReferencePrefixesTheReference()
        => Assert.AreEqual("Genesis 1:1 alpha one", Retrieve("Genesis 1:1", Format(includeReference: true)));

    [TestMethod]
    public void VerseRangeResolves()
        => Assert.AreEqual("alpha one alpha two alpha three", Retrieve("Genesis 1:1-3"));

    [TestMethod]
    public void ChapterVerseRangeResolves()
        => Assert.AreEqual("alpha two alpha three beta one", Retrieve("Genesis 1:2-2:1"));

    [TestMethod]
    public void ChapterVerseRangeSeparatesChaptersWithANewLineWhenRequested()
        => Assert.AreEqual(
            "alpha two alpha three\nbeta one",
            Retrieve("Genesis 1:2-2:1", Format(includeNewLineBetweenChapters: true)));

    [TestMethod]
    public void ChapterVerseRangeSpanningThreeChapters()
        => Assert.AreEqual("alpha three beta one beta two gamma one", Retrieve("Genesis 1:3-3:1"));

    // Chapters were concatenated with no separator at all, running the last verse of
    // one chapter into the first verse of the next.
    [TestMethod]
    public void ChapterRangeSeparatesChaptersWithASpace()
        => Assert.AreEqual("alpha one alpha two alpha three beta one beta two", Retrieve("Genesis 1-2"));

    [TestMethod]
    public void ChapterRangeSeparatesChaptersWithANewLineWhenRequested()
        => Assert.AreEqual(
            "alpha one alpha two alpha three\nbeta one beta two",
            Retrieve("Genesis 1-2", Format(includeNewLineBetweenChapters: true)));

    // getElementByN called XElement.EmptySequence.First(), which throws on an empty
    // sequence, so anything not found blew up instead of returning nothing.
    [TestMethod]
    [DataRow("Habakkuk 1:1")]
    [DataRow("Genesis 99:1")]
    [DataRow("Genesis 1:99")]
    [DataRow("not a reference")]
    [DataRow("")]
    [DataRow("Genesis")]
    public void UnresolvableReferencesReturnEmptyAndDoNotThrow(string reference)
        => Assert.AreEqual(string.Empty, Retrieve(reference));
}
