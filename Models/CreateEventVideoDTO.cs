using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
	public class CreateEventVideoDTO
	{
		public IEnumerable<int> EventIds { get; set; }
		public string? UserId { get; set; }
	}
}
