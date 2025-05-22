namespace KontorService.Models.Reporting.ViewModels
{
    public class PageVisitsViewModel
    {
        /// <summary>
        /// url
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// title
        /// </summary>
        public string? Title { get; set; }
        /// <summary>
        /// visits
        /// </summary>
        public int Visits { get; set; }
    }
}
