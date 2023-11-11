using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess
{
	public class Event
	{
		[Key]
		public int Id { get; set; }
		public string? Date { get; set; }
		public int CameraId { get; set; }
		[ForeignKey(nameof(CameraId))]
		public Camera? Camera { get; set; }
		public IEnumerable<BoundingBox> BoundingBoxes { get; set; }
		public string? Path { get; set; }
		public int? EventVideoId { get; set; }
		[ForeignKey(nameof(EventVideoId))]
        public EventVideo? EventVideo { get; set; }
    }
}
