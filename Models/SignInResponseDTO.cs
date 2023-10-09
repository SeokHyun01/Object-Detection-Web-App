using DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
	public class SignInResponseDTO
	{
		public bool IsSucceeded { get; set; }
		public IEnumerable<string> Errors { get; set; }
		public string? Token { get; set; }
		public AppUser? User { get; set; }
	}
}
