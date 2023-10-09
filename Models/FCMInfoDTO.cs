using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
	public class FCMInfoDTO
	{
		public int Id { get; set; }
		public string? UserId { get; set; }
		public string? DeviceNickname { get; set; }
		public string? Token { get; set; }
	}
}
