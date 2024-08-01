namespace VerseLinkWindows
{
    internal enum BibleReferenceType
    {
        SingleVerse, VerseRange, ChapterRange, ChapterVerseRange
    }
    internal class BibleReference
    {
        private string Reference;
        private string BookName;
        private List<KeyValuePair<string,string>> ChapterVerse;
        private BibleReferenceType ReferenceType;

        public BibleReference()
        {
            BookName = String.Empty;
            Reference = String.Empty;
            ChapterVerse = [];
            ReferenceType = new BibleReferenceType();
        }

        public BibleReference(string reference, BibleReferenceType bibleReferenceType) 
        {
            BookName = String.Empty;
            Reference = reference;
            if (!String.IsNullOrEmpty(Reference))
            {
                var r = reference.Split(' ');
                bool hasBookPrefix = (reference[0].Equals('1') || reference[0].Equals(2));
                BookName = hasBookPrefix ? String.Join(' ', r.Take(2)) : r.ElementAt(0);
                reference =  hasBookPrefix ? r.ElementAt(2) : r.ElementAt(1);
            }

            ChapterVerse = [];
            ReferenceType = bibleReferenceType;

            parseChapterVerse(reference);
        }

        private void parseChapterVerse(string reference)
        {
            switch (ReferenceType)
            {
                case BibleReferenceType.SingleVerse:
                    var sv = reference.Split(':');
                    ChapterVerse.Add(new KeyValuePair<string,string>(sv.ElementAt(0), sv.ElementAt(1)));
                    break;
                case BibleReferenceType.ChapterVerseRange:
                    var cvr = reference.Split('-');
                    var c = cvr.ElementAt(0).Split(':');
                    ChapterVerse.Add(new KeyValuePair<string, string>(c.ElementAt(0), c.ElementAt(1)));
                    c = cvr.ElementAt(1).Split(':');
                    ChapterVerse.Add(new KeyValuePair<string, string>(c.ElementAt(0), c.ElementAt(1)));
                    break;
                case BibleReferenceType.VerseRange:
                    var vr = reference.Split('-');
                    var v = vr.ElementAt(0).Split(':');
                    ChapterVerse.Add(new KeyValuePair<string, string>(v.ElementAt(0), v.ElementAt(1)));
                    ChapterVerse.Add(new KeyValuePair<string, string>(v.ElementAt(0), vr.ElementAt(1)));
                    break;
                case BibleReferenceType.ChapterRange:
                    var cr = reference.Split('-');
                    ChapterVerse.Add(new KeyValuePair<string, string>(cr.ElementAt(0), ""));
                    ChapterVerse.Add(new KeyValuePair<string, string>(cr.ElementAt(1), ""));
                    break;
                default:
                    ChapterVerse.Add(new KeyValuePair<string, string>(reference, ""));
                    break;
            }
        }

        public string getReference()
        {
            return Reference;
        }

        public string getBookName()
        {
            return BookName;
        }

        public List<KeyValuePair<string, string>> getChapterVerse()
        {
            return ChapterVerse;
        }
        public (string Chapter, string Verse) getChapterVerse(int index)
        {
            var cv = (index == -1) ? ChapterVerse.LastOrDefault() : ChapterVerse.ElementAtOrDefault(index);
            string Chapter = cv.Key;
            string Verse = cv.Value;
            return (Chapter,Verse);
        }

        public (int Chapter, int Verse) getChapterVerse(int index, bool asInt)
        {
            var cv = (index == -1) ? ChapterVerse.LastOrDefault() : ChapterVerse.ElementAtOrDefault(index);
            int Chapter = int.TryParse(cv.Key, out int ch) ? ch : 0;
            int Verse = int.TryParse(cv.Value, out int vs) ? vs : 0;
            return (Chapter, Verse);
        }

        public BibleReferenceType getReferenceType()
        {
            return ReferenceType;
        }
    }
}
