using DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
	public class CameraDTO
	{
		public int Id { get; set; }
		public string? UserId { get; set; }
		public string? Name { get; set; }
		public int Angle { get; set; }
		public string? Image { get; set; }
		public IEnumerable<EventDTO> Events { get; set; }
	}
}
