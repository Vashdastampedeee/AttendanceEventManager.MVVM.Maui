using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace EventManager.Models
{
    [Table("attendancelog")]
    public class AttendanceLog
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string IdNumber { get; set; }
        public string Name { get; set; }
        public string BusinessUnit { get; set; }
        public string Status { get; set; }
        public string Timestamp { get; set; }

    }
}
