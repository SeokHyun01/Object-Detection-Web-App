using AutoMapper;
using DataAccess;
using DataAccess.Data;
using Microsoft.EntityFrameworkCore;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Repository.IRepository
{
	public class EventRepository : IEventRepository
	{
		private readonly AppDbContext _db;
		private readonly IMapper _mapper;

		public EventRepository(AppDbContext db, IMapper mapper)
		{
			_db = db;
			_mapper = mapper;
		}

		public async ValueTask<EventDTO> Create(EventDTO objDTO)
		{
			var obj = _mapper.Map<EventDTO, Event>(objDTO);
			var createdObj = _db.Events.Add(obj);
			await _db.SaveChangesAsync();

			return _mapper.Map<Event, EventDTO>(createdObj.Entity);
		}

		public ValueTask<int> Delete(int id)
		{
			throw new NotImplementedException();
		}

		public async ValueTask<IEnumerable<EventDTO>> GetAll(string userId)
		{
			return _mapper.Map<IEnumerable<Event>, IEnumerable<EventDTO>>(_db.Events.Include(u => u.BoundingBoxes).Where(x => x.UserId == userId));
		}
	}
}
