using AutoMapper;
using DataAccess;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Mapper
{
	public class MappingProfile : Profile
	{
		public MappingProfile()
		{
			CreateMap<Camera, CameraDTO>().ReverseMap();
			CreateMap<Event, EventDTO>().ReverseMap();
			CreateMap<BoundingBox, BoundingBoxDTO>().ReverseMap();
			CreateMap<EventVideo, EventVideoDTO>().ReverseMap();
			CreateMap<FCMInfo, FCMInfoDTO>().ReverseMap();
		}
	}
}
