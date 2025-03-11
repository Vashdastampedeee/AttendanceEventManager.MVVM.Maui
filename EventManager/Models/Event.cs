using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace EventManager.Models
{
    [Table("event")]
    public class Event
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string EventName { get; set; }
        public string EventCategory { get; set; }
        public byte[] EventImage { get; set; }
        public string EventDate { get; set; }
        public string EventFromTime { get; set; }
        public string EventToTime { get; set; }
        public bool isSelected { get; set; }
        public string FormattedTime => $"{EventFromTime} - {EventToTime}";

        [Ignore]
        public bool IsDefaultVisible { get; set; }
    }
}
