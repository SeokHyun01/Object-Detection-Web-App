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
			try
			{
				var obj = _mapper.Map<EventVideoDTO, EventVideo>(objDTO);
				var createdObj = _db.EventVideos.Add(obj);
				await _db.SaveChangesAsync();

				return _mapper.Map<EventVideo, EventVideoDTO>(createdObj.Entity);

			} catch (Exception ex)
			{
				Console.WriteLine(ex.StackTrace);

				throw;
			}
		}

		public ValueTask<int> Delete(int id)
		{
			throw new NotImplementedException();
		}

		public async ValueTask<IEnumerable<EventVideoDTO>> GetAllByUserId(string userId)
		{
			try
			{
				var events = await _db.Events.Where(x => x.UserId == userId).ToListAsync();
				var eventVideoIds = events.Select(e => e.EventVideoId).Distinct().ToList();
				var eventVideos = await _db.EventVideos.Where(e => eventVideoIds.Contains(e.Id)).ToListAsync();

				return _mapper.Map<IEnumerable<EventVideo>, IEnumerable<EventVideoDTO>>(eventVideos);

			} catch (Exception ex)
			{
				Console.WriteLine(ex.StackTrace);

				throw;
			}
		}
	}
}
