using System;

namespace AvitoParser
{
    public class Candidate
    {
        public int Id { get; set; }
        public int AvitoId { get; set; }
        public int Salary { get; set; }
        public int Experience { get; set; }
        public int Age { get; set; }
        public string Url { get; set; }
        public string Address { get; set; }
        public string Education { get; set; }
        public string Position { get; set; }
        public string Sex { get; set; }
        public string ActionSphere { get; set; }
        public string WorkingSchedule { get; set; }
        public string Description { get; set; }
        public string Citizenship { get; set; }
        public string BusinessTripReady { get; set; }
        public bool RemovalReady { get; set; }
        public DateTime? CreatingDate { get; set; }
    }
}
