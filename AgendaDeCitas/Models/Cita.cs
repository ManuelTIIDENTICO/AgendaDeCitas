using System;
using System.ComponentModel.DataAnnotations;

namespace AgendaDeCitas.Models
{
    public class Cita
    {
        [Required]
        public string ClinicID { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DiaryDate { get; set; }

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan TimeStart { get; set; }

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan TimeEnd { get; set; }

        [Required]
        public int CabineID { get; set; }

        [Required]
        public int ProfessionalID { get; set; }

        [Required]
        public string Services { get; set; }

        [Required]
        public string DiaryComments { get; set; }

        [Required]
        public string ClientToken { get; set; }
    }
}

