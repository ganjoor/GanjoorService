﻿using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace RMuseum.Models.Ganjoor
{
    public class GanjoorTajikPoet
    {
        /// <summary>
        /// id
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        /// <summary>
        /// tajik nick name
        /// </summary>
        public string TajikNickname { get; set; }

        /// <summary>
        /// additional description or note in Tajik
        /// </summary>
        public string TajikDescription { get; set; }

        /// <summary>
        /// birth year in lunar hijri
        /// </summary>
        public int BirthYearInLHijri { get; set; }
    }
}
