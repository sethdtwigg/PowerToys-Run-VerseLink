using System.Text.RegularExpressions;

namespace VerseLinkWindows
{
    public class VerseLink
    {
        public string LastError { get; set; }
        public bool Error { get; set; }
        
        private VerseRetrieveInterface vli;

        private const string SINGLE_VERSE_PATTERN = @"^([1?2?] )?[a-z]+ [0-9]{1,3}:[0-9]{1,3}$";
        private const string VERSE_RANGE_PATTERN = @"^([1?2?] )?[a-z]+ [0-9]{1,3}:[0-9]{1,3}-[0-9]{1,3}$";
        private const string CHAPTER_RANGE_PATTERN = @"^([1?2?] )?[a-z]+ [0-9]{1,3}-[0-9]{1,3}$";
        private const string CHAPTER_VERSE_RANGE_PATTERN = @"^([1?2?] )?[a-z]+ [0-9]{1,3}:[0-9]{1,3}-[0-9]{1,3}:[0-9]{1,3}$";

        private Regex singleVerseRegex;
        private Regex verseRangeRegex;
        private Regex chapterRangeRegex;
        private Regex chapterVerseRangeRegex;

        public VerseLink(string version, string bibleXmlDirPath,BibleReferenceVerseFormat verseFormat) 
        {
            LastError = "";
            Error = false;
            vli = new VerseRetrieveInterface(version, bibleXmlDirPath, verseFormat);
            HandleVLIError();

            RegexOptions rOptions = RegexOptions.IgnoreCase | RegexOptions.Compiled;
            singleVerseRegex = new Regex(SINGLE_VERSE_PATTERN, rOptions);
            verseRangeRegex = new Regex(VERSE_RANGE_PATTERN, rOptions);
            chapterRangeRegex = new Regex(CHAPTER_RANGE_PATTERN, rOptions);
            chapterVerseRangeRegex = new Regex(CHAPTER_VERSE_RANGE_PATTERN, rOptions);
        }

        private void HandleVLIError()
        {
            if (vli.Error)
            {
                Error = true;
                LastError = vli.LastError;
            }
        }

        public string VerseLinkRetrieve(string reference)
        {
            Error = false;
            string verseText = "";

            try
            {
                var b = parseBibleReference(reference);
                verseText = vli.GetVerseText(b);
                HandleVLIError();
            }
            catch (Exception ex)
            {
                Error = true;
                LastError = ex.Message;
            }
            return verseText;
        }

        private BibleReference parseBibleReference(string reference)
        {
            if (singleVerseRegex.Match(reference).Success) return new BibleReference(reference,BibleReferenceType.SingleVerse);
            if (verseRangeRegex.Match(reference).Success) return new BibleReference(reference, BibleReferenceType.VerseRange);
            if (chapterRangeRegex.Match(reference).Success) return new BibleReference(reference, BibleReferenceType.ChapterRange);
            if (chapterVerseRangeRegex.Match(reference).Success) return new BibleReference(reference, BibleReferenceType.ChapterVerseRange);

            Error = true;
            LastError = $"Unable to parseBibleReference! {reference}";
            return new BibleReference();
        }
    }
}
