using AutoMapper;
using Business.Repository.IRepository;
using DataAccess;
using DataAccess.Data;
using Microsoft.EntityFrameworkCore;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Repository
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
			try
			{
				var obj = _mapper.Map<EventDTO, Event>(objDTO);
				var createdObj = _db.Events.Add(obj);
				await _db.SaveChangesAsync();

				return _mapper.Map<Event, EventDTO>(createdObj.Entity);

			} catch (Exception ex)
			{
				Console.WriteLine(ex.StackTrace);

				throw;
			}
		}

		public async ValueTask<IEnumerable<EventDTO>> GetAll(IEnumerable<int> ids)
		{
			if (ids.Any())
			{
				return _mapper.Map<IEnumerable<Event>, IEnumerable<EventDTO>>(_db.Events.Where(x => ids.Contains(x.Id)));
			}
			else
			{
				return Enumerable.Empty<EventDTO>();
			}
		}

		public async ValueTask<IEnumerable<EventDTO>> GetAllByUserId(string userId)
		{
			return _mapper.Map<IEnumerable<Event>, IEnumerable<EventDTO>>(_db.Events.Include(u => u.BoundingBoxes).Where(x => x.UserId == userId));
		}

		public async ValueTask<EventDTO> Update(EventDTO objDTO)
		{
			var objFromDb = await _db.Events.FirstOrDefaultAsync(u => u.Id == objDTO.Id);
			if (objFromDb != null)
			{
				objFromDb.EventVideoId = objDTO.EventVideoId;
				_db.Events.Update(objFromDb);
				await _db.SaveChangesAsync();
				return _mapper.Map<Event, EventDTO>(objFromDb);
			}
			return objDTO;
		}

		public async ValueTask<IEnumerable<EventDTO>> GetAllByVideoId(int videoId)
		{
			return _mapper.Map<IEnumerable<Event>, IEnumerable<EventDTO>>(_db.Events.Where(x => x.EventVideoId == videoId));
		}
	}
}
