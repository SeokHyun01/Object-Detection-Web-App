using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
	/*
	 * 카메라에서 Object Detection한 결과를 저장하는 클래스
	 */
	public class DetectionDTO
	{
		public string? Date { get; set; }
		public string? UserId { get; set; }
		public int CameraId { get; set; }
		public string? Image { get; set; }
		public string? Model { get; set; }
	}
}
