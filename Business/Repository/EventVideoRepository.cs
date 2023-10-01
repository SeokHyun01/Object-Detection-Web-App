using AutoMapper;
using Business.Repository.IRepository;
using DataAccess;
using DataAccess.Data;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Repository
{
	public class EventVideoRepository : IEventVideoRepository
	{
		private readonly AppDbContext _db;
		private readonly IMapper _mapper;

		public EventVideoRepository(AppDbContext db, IMapper mapper)
		{
			_db = db;
			_mapper = mapper;
		}

		public async ValueTask<EventVideoDTO> Create(EventVideoDTO objDTO)
		{
			var obj = _mapper.Map<EventVideoDTO, EventVideo>(objDTO);
			var createdObj = _db.EventVideos.Add(obj);
			await _db.SaveChangesAsync();

			return _mapper.Map<EventVideo, EventVideoDTO>(createdObj.Entity);
		}

		public ValueTask<int> Delete(int id)
		{
			throw new NotImplementedException();
		}
	}
}
