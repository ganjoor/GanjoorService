using RMuseum.Models.MusicCatalogue;
using RSecurityBackend.Models.Auth.Db;
using System;

namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// poem related track
    /// </summary>
    public class PoemMusicTrack
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// poem id
        /// </summary>
        public int PoemId { get; set; }

        /// <summary>
        /// poem
        /// </summary>
        public GanjoorPoem Poem { get; set; }

        /// <summary>
        /// track type
        /// </summary>
        public PoemMusicTrackType TrackType { get; set; }

        /// <summary>
        /// artist name
        /// </summary>
        public string ArtistName { get; set; }

        /// <summary>
        /// artist url
        /// </summary>
        public string ArtistUrl { get; set; }

        /// <summary>
        /// album name
        /// </summary>
        public string AlbumName { get; set; }

        /// <summary>
        /// album url
        /// </summary>
        public string AlbumUrl { get; set; }

        /// <summary>
        /// track name
        /// </summary>
        public string TrackName { get; set; }

        /// <summary>
        /// track url
        /// </summary>
        public string TrackUrl { get; set; }

        /// <summary>
        /// GanjoorTrack Id
        /// </summary>
        public int? GanjoorTrackId { get; set; }

        /// <summary>
        /// singer
        /// </summary>
        public virtual GanjoorSinger Singer { get; set; }

        /// <summary>
        /// singer id
        /// </summary>
        public int? SingerId { get; set; }

        /// <summary>
        /// GanjoorTrack
        /// </summary>
        public virtual GanjoorTrack GanjoorTrack { get; set; }

        /// <summary>
        /// GolhaTrack Id
        /// </summary>
        public int? GolhaTrackId { get; set; }

        /// <summary>
        /// GolhaTrack
        /// </summary>
        public virtual GolhaTrack GolhaTrack { get; set; }

        /// <summary>
        /// description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// approved
        /// </summary>
        public bool Approved { get; set; }

        /// <summary>
        /// Suggested by user id
        /// </summary>
        public Guid? SuggestedById { get; set; }

        /// <summary>
        /// Suggested by user
        /// </summary>
        /// <remarks>
        /// approval user would be extractable from AuditLogs
        /// </remarks>
        public virtual RAppUser SuggestedBy { get; set; }

        /// <summary>
        /// approval date
        /// </summary>
        public DateTime ApprovalDate { get; set; }

        /// <summary>
        /// broken link
        /// </summary>
        public bool BrokenLink { get; set; }

        /// <summary>
        /// instead of deleting rejected songs keep track of them
        /// </summary>
        public bool Rejected { get; set; }

        /// <summary>
        /// rejection cause
        /// </summary>
        public string RejectionCause { get; set; }

        /// <summary>
        /// Song Order
        /// </summary>
        public int SongOrder { get; set; }
    }
}
