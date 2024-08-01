using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Xml.Linq;

namespace VerseLinkWindows
{
    internal class VerseRetrieveInterface
    {
        private string Bible_Dir;
        private string Verse_Version;
        private XElement BibleXML;
        private BibleReferenceVerseFormat VerseReferenceVerseFormat;

        public string LastError { get; set; }
        public bool Error { get; set; }

        public VerseRetrieveInterface(string version, string bibleXmlDirPath, BibleReferenceVerseFormat verseFormat)
        {
            LastError = "";
            BibleXML = null!;
            Verse_Version = version;
            Bible_Dir = bibleXmlDirPath;
            Error = !loadBibleVersion();
            VerseReferenceVerseFormat = verseFormat;
        }

        private bool loadBibleVersion()
        {
            bool parsedBV = true;
            try
            {
                var filename = String.Concat(Verse_Version, ".xml");
                var bibleXMLFilepath = Path.Combine(Bible_Dir,"Bibles", filename);

                BibleXML = XElement.Load(bibleXMLFilepath);
            }
            catch (Exception e)
            {
                parsedBV = false;
                LastError = e.Message;
            }
            return parsedBV;
        }

        public string GetVerseText(BibleReference b)
        {
            XElement? book;
            XElement chapter;
            string chapternumberstr;
            string versenumberstr;
            int startNum;
            int endNum;
            var verseText = String.Empty;

            book = getElementByN(BibleXML, "b", b.getBookName());
            if (book.IsEmpty) { return String.Empty; }

            switch (b.getReferenceType())
            {
                case BibleReferenceType.SingleVerse:
                    chapternumberstr = b.getChapterVerse(0).Chapter;
                    versenumberstr = b.getChapterVerse(0).Verse;
                    chapter = getElementByN(book, "c", chapternumberstr);
                    verseText = getElementByN(chapter, "v",versenumberstr).Value ?? String.Empty;
                    break;
                case BibleReferenceType.VerseRange:
                    chapternumberstr = b.getChapterVerse(0).Chapter;
                    startNum = b.getChapterVerse(0,true).Verse;
                    endNum = b.getChapterVerse(1, true).Verse;
                    chapter = getElementByN(book, "c", chapternumberstr);
                    verseText = formatVerse(getElementByNtoN(chapter,"v",startNum,endNum));
                    break;
                case BibleReferenceType.ChapterRange:
                    startNum = b.getChapterVerse(0, true).Chapter;
                    endNum = b.getChapterVerse(-1, true).Chapter;
                    while (startNum <= endNum)
                    {
                        verseText += formatVerse(getElementByN(book, "c", startNum.ToString()).Descendants(), VerseReferenceVerseFormat.IncludeNewLineBetweenChapters);
                        startNum++;
                    }
                    break;
                case BibleReferenceType.ChapterVerseRange:
                    startNum = b.getChapterVerse(0, true).Chapter;
                    endNum = b.getChapterVerse(-1, true).Chapter;
                    chapter = getElementByN(book, "c", startNum.ToString());
                    verseText = formatVerse(getElementByNtoN(chapter, "v", b.getChapterVerse(0, true).Verse, -1));
                    ++startNum;
                    while (startNum <= endNum)
                    {
                        if (VerseReferenceVerseFormat.IncludeNewLineBetweenChapters) verseText += '\n';
                        chapter = getElementByN(book, "c", startNum.ToString());
                        int endVerse = (startNum == endNum) ? b.getChapterVerse(-1, true).Verse : -1;
                        verseText += formatVerse(getElementByNtoN(chapter, "v", 1, endVerse));
                        startNum++;
                    }
                    break;
                default:
                    verseText = "";
                    break;
            }
            return (VerseReferenceVerseFormat.IncludeReference) ? String.Concat(b.getReference()," ",verseText) : verseText;
        }

        private XElement getElementByN(XElement xe, string descendant, string n)
        {
            if (xe.IsEmpty) return xe;
            XElement? node = xe.Descendants(descendant).FirstOrDefault(x => x.Attribute("n")?.Value == n);
            if (node == null) return XElement.EmptySequence.First();
            return node;
        }

        private IEnumerable<XElement> getElementByNtoN(XElement xe, string descendant, int startN, int endN)
        {
            if (xe.IsEmpty) return XElement.EmptySequence;
            IEnumerable<XElement>? node = (endN == -1) ?
                xe.Descendants(descendant).Where(x => int.TryParse(x.Attribute("n")?.Value, out int num) && num >= startN) :
                xe.Descendants(descendant).Where(x => int.TryParse(x.Attribute("n")?.Value, out int num) && num >= startN && num <= endN);
            if (node == null) return XElement.EmptySequence;
            return node;
        }

        private string formatVerse(IEnumerable<XElement> nodes,bool includeEndingNewLine = false)
        {            
            string v = (VerseReferenceVerseFormat.IncludeVerseNumbers) ? 
                String.Join(" ", nodes.Select(v => String.Concat(v.Attribute("v")," ",v.Value))) : 
                String.Join(" ", nodes.Select(v => v.Value));
            return includeEndingNewLine ? String.Concat(v,'\n') : v.Trim();
        }
    }
}
