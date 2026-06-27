using System;

namespace RMuseum.Models.Ganjoor.ViewModels
{
    public class GanjoorUserPrePoemVisitViewModel
    {
        public DateTime? LastVisit { get; set; }
        public int TotalVisits { get; set; }
        public bool KeepTrack { get; set; }
    }
}
