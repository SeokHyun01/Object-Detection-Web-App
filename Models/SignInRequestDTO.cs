using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
	public class SignInRequestDTO
	{
		[Required(ErrorMessage = "Id를 입력해 주세요.")]
		[RegularExpression("^[a-zA-Z0-9_.-]+@[a-zA-Z0-9-]+.[a-zA-Z0-9-.]+$", ErrorMessage = "잘못된 형식의 이메일입니다.")]
		public string? Email { get; set; }

		[Required(ErrorMessage = "비밀번호를 입력해 주세요.")]
		[DataType(DataType.Password)]
		public string? Password { get; set; }
		public string? FCMToken { get; set; }
		public string? FCMDeviceNickname { get; set; }
	}
}
