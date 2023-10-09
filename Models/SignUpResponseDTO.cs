using DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
	public class SignUpResponseDTO
	{
		public bool IsSucceeded { get; set; }
		public AppUser? User { get; set; }
		public FCMInfoDTO? FCMInfo { get; set; }
		public IEnumerable<string> Errors { get; set; }
	}
}
