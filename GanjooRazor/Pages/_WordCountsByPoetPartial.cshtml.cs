﻿using Microsoft.AspNetCore.Mvc.RazorPages;
using RMuseum.Models.Ganjoor.ViewModels;

namespace GanjooRazor.Pages
{
    public class _WordCountsByPoetPartialModel : PageModel
    {
        public string Term { get; set; }
        public PoetOrCatWordStat[]  WordStats { get; set; }
        public bool Whole { get; set; }
        public string SectionName
        {
            get
            {
                return Whole ? "گنجور" : "این بخش";
            }
        }
    }
}
