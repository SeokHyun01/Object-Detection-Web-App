using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
	public class EventViewModel
	{
        public int Id { get; set; }
        public DateTime? Date { get; set; }
		public string? CameraName { get; set; }
		public string? Labels { get; set; }
		public string? Path { get; set; }
    }
}
