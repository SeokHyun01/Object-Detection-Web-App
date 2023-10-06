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

		public async ValueTask<int> Delete(int id)
		{
			var video = await _db.EventVideos.FirstOrDefaultAsync(x => x.Id == id);
			if (video == null)
			{
				return 0;
			}
			var path = video.Path;
			if (string.IsNullOrEmpty(path) && File.Exists(path))
			{
				File.Delete(path);
			}
			_db.EventVideos.Remove(video);

			return await _db.SaveChangesAsync();
		}

		public async ValueTask<IEnumerable<EventVideoDTO>> GetAllByUserId(string userId)
		{
			try
			{
                return _mapper.Map<IEnumerable<EventVideo>, IEnumerable<EventVideoDTO>>(_db.EventVideos.Where(x => x.UserId == userId));

            } catch (Exception ex)
			{
				Console.WriteLine(ex.StackTrace);

				throw;
			}
		}
	}
}
