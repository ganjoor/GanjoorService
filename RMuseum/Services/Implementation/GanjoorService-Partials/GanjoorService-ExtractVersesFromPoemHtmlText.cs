using RMuseum.Models.Ganjoor;
using System;
using System.Collections.Generic;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// IGanjoorService implementation
    /// </summary>
    public partial class GanjoorService : IGanjoorService
    {
        private List<GanjoorVerse> _extractVersesFromPoemHtmlText(int poemId, string poemtext)
        {
            List<GanjoorVerse> verses = new List<GanjoorVerse>();

            //this spagetti code has been imported from my internal utilities:
            while (poemtext.IndexOf("<a href") != -1)
            {
                int ahrefStart = poemtext.IndexOf("<a href");
                string part1 = poemtext.Substring(0, ahrefStart);
                string part2 = poemtext.Substring(poemtext.IndexOf(">", ahrefStart) + 1, poemtext.IndexOf("</a>") - (poemtext.IndexOf(">", ahrefStart) + 1));
                poemtext = part1 + part2 + poemtext.Substring(poemtext.IndexOf("</a>") + 4, poemtext.Length - (poemtext.IndexOf("</a>") + 4));
            }
            while (poemtext.IndexOf("<acronym") != -1)
            {
                int acroStart = poemtext.IndexOf("<acronym");
                string part1 = poemtext.Substring(0, acroStart);
                string part2;
                try
                {
                    part2 = poemtext.Substring(poemtext.IndexOf(">", acroStart) + 1, poemtext.IndexOf("</acronym>") - (poemtext.IndexOf(">", acroStart) + 1));
                    poemtext = part1 + part2 + poemtext.Substring(poemtext.IndexOf("</acronym>") + 10, poemtext.Length - (poemtext.IndexOf("</acronym>") + 10));
                }
                catch
                {
                    part2 = poemtext.Substring(poemtext.IndexOf(">", acroStart) + 1, poemtext.IndexOf("<acronym>") - (poemtext.IndexOf(">", acroStart) + 1));
                    poemtext = part1 + part2 + poemtext.Substring(poemtext.IndexOf("<acronym>") + 10, poemtext.Length - (poemtext.IndexOf("<acronym>") + 10));
                }

            }

            while (poemtext.IndexOf("<sup>") != -1)
            {
                string part1 = poemtext.Substring(0, poemtext.IndexOf("<sup>"));
                try
                {
                    poemtext = part1 + poemtext.Substring(poemtext.IndexOf("</sup>") + 6, poemtext.Length - (poemtext.IndexOf("</sup>") + 6));
                    poemtext = poemtext.Replace("  ", " ");
                }
                catch
                {
                    throw new Exception($"poemtext.IndexOf(\"<sup>\": {poemtext}");
                }

            }

            while (poemtext.IndexOf("id=\"bn") != -1)
            {
                int idxbn1 = poemtext.IndexOf(" id=\"bn");
                int idxbn2 = poemtext.IndexOf("\"", idxbn1 + " id=\"bn".Length);
                
                poemtext = poemtext.Substring(0, idxbn1) + poemtext.Substring(idxbn2+1);
            }


            poemtext = poemtext.Replace("Adaptation du milieu", "یییییییییییییییییییی");
            poemtext = poemtext.Replace("Empirique", "ببببببببب");

            poemtext = poemtext.Replace(" >", ">");
            poemtext = poemtext.Replace("<div class=\"b\" style=\"width:750px\">", "<div class=\"b\">").Replace("<div class=\"b\" style=\"width:660px\">", "<div class=\"b\">").Replace("<div class=\"b\" style=\"width:680px\">", "<div class=\"b\">").Replace("<div class=\"b\" style=\"width:650px\">", "<div class=\"b\">").Replace("<div class=\"b\" style=\"width:690px\">", "<div class=\"b\">").Replace("<p style=\"color:#911\">", "<p>").Replace("<p style=\"color:#191\">", "<p>").Replace("<div class=\"spacer\">", "").Replace("&nbsp;", "").Replace("<div class=\"spacer\" />", "").Replace("<div class=\"b\" style=\"width:700px\">", "<div class=\"b\">");
            poemtext = poemtext.Replace("<em>", "").Replace("</em>", "");
            poemtext = poemtext.Replace("<em>", "").Replace("</em>", "").Replace("<small>", "").Replace("</small>", "");
            poemtext = poemtext.Replace("<b>", "").Replace("</b>", "").Replace("<strong>", "").Replace("</strong>", "");
            poemtext = poemtext.Replace("<p><br style=\"clear:both;\"/></p>", "").Replace("<br style=\"clear:both;\"/>", "");
            if (poemtext.IndexOf("\r\n") == 0)
                poemtext = poemtext.Substring(2);
            poemtext = poemtext.Replace("\r", "").Replace("\n", "");
            poemtext = poemtext.Replace("</div>", "").Replace("</p>", "");
            poemtext = poemtext.Replace("<div class=\"b2\">", "a");
            poemtext = poemtext.Replace("<div class=\"b\">", "b");
            poemtext = poemtext.Replace("<div class=\"m1\">", "m");
            poemtext = poemtext.Replace("<div class=\"m2\">", "n");
            poemtext = poemtext.Replace("<div class=\"n\"><p>", "s");
            poemtext = poemtext.Replace("<div class=\"n\">", "s");
            poemtext = poemtext.Replace("<div class=\"l\"><p>", "l");
            poemtext = poemtext.Replace("<div class=\"l\">", "l");
            poemtext = poemtext.Replace("<div class=\"c\"><p>", "c");
            poemtext = poemtext.Replace("<div class=\"c\">", "c");
            poemtext = poemtext.Replace("<p>", "p");
            poemtext = poemtext.Replace("bmp", "b");
            poemtext = poemtext.Replace("np", "n");
            poemtext = poemtext.Replace("ap", "a");
            poemtext = poemtext.Replace("\"", "").Replace("'", "");
            if (poemtext.IndexOfAny(new char[] { '<', '>' }) != -1)
                throw new Exception($"Invalid Characteres: {poemtext}");
            if (poemtext.IndexOf("mp") != -1)
                throw new Exception($"مصرع اول بدون مصرع دوم: {poemtext}");

            if (poemtext.Length > 0)
            {

                int idx = poemtext.IndexOfAny(new char[] { 'a', 'b', 'm', 'n', 'p', 's', 'l' , 'c' });
                bool preWasBand = false;
                while (idx != -1)
                {
                    GanjoorVerse verse = new GanjoorVerse();
                    verse.PoemId = poemId;
                    verse.VOrder = verses.Count + 1;
                   
                    switch (poemtext[idx])
                    {
                        case 'p':
                            if (preWasBand)
                                verse.VersePosition = VersePosition.CenteredVerse2;
                            else
                                verse.VersePosition = VersePosition.Paragraph;
                            preWasBand = false;
                            break;
                        case 'b':
                            verse.VersePosition = VersePosition.Right;
                            preWasBand = false;
                            break;
                        case 'n':
                            verse.VersePosition = VersePosition.Left;
                            preWasBand = false;
                            break;
                        case 'a':
                            verse.VersePosition = VersePosition.CenteredVerse1;
                            preWasBand = true;
                            break;
                        case 's':
                            verse.VersePosition = VersePosition.Paragraph;
                            preWasBand = false;
                            break;
                        case 'l':
                            verse.VersePosition = VersePosition.Single;
                            preWasBand = false;
                            break;
                        case 'c':
                            verse.VersePosition = VersePosition.Comment;
                            preWasBand = false;
                            break;
                    }
                    int nextIdx = poemtext.IndexOfAny(new char[] { 'a', 'b', 'm', 'n', 'p', 's', 'l' , 'c' }, idx + 1);
                    if (nextIdx == -1)
                    {
                        verse.Text = poemtext.Substring(idx + 1).Replace("یییییییییییییییییییی", "Adaptation du milieu").Replace("ببببببببب", "Empirique");
                    }
                    else
                    {
                        verse.Text = poemtext.Substring(idx + 1, nextIdx - idx - 1).Replace("یییییییییییییییییییی", "Adaptation du milieu").Replace("ببببببببب", "Empirique");
                    }

                    verse.Text = verse.Text.Trim();

                    verses.Add(verse);

                    idx = nextIdx;
                }
            }

            return verses;
        }

    }
}
