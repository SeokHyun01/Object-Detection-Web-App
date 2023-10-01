using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
	public class EventDTO
	{
		public int Id { get; set; }
		public string? Date { get; set; }
		public string? UserId { get; set; }
		public int CameraId { get; set; }
		public string? Path { get; set; }
		public IEnumerable<BoundingBoxDTO> BoundingBoxes { get; set; }
		public int EventVideoId { get; set; }
	}
}
