using Microsoft.EntityFrameworkCore.Diagnostics;
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
	public class EventVideo
	{
		[Key]
		public int Id { get; set; }
		public string? UserId { get; set; }
		public IEnumerable<Event> Events { get; set; }
		public string? Path { get; set; }
	}
}
