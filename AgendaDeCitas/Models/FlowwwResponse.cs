namespace AgendaDeCitas.Models
{
    public class FlowwwResponse
    {
        public List<Appointment> AppNext { get; set; }
    }

    public class Appointment
    {
        public string Id { get; set; }
        public string Date { get; set; }
        public string TimeStart { get; set; }
        public string TimeEnd { get; set; }
        public string AppGTags { get; set; }
        public string AppProductZoneCode { get; set; }
        public string AppProductDesc { get; set; }
        public string AppLaserGID { get; set; }
        public string ClinicID { get; set; }
        public string ClinicName { get; set; }
        public string ClinicPhone { get; set; }
        public string ProfessionalName { get; set; }
    }
}
