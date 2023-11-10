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
				Console.WriteLine(ex.Message);

				throw;
			}
		}

		public async ValueTask Delete(int id)
		{
			var video = await _db.EventVideos.FirstOrDefaultAsync(x => x.Id == id);
			if (video == null)
			{
				return;
			}
			var path = video.Path;
			if (!string.IsNullOrEmpty(path) && File.Exists(path))
			{
				// 이벤트 영상 삭제
				File.Delete(path);
			}
			_db.EventVideos.Remove(video);
			await _db.SaveChangesAsync();

			var objs = await _db.Events.Where(x => x.EventVideoId == id).ToListAsync();
			foreach (var obj in objs)
			{
				if (!string.IsNullOrEmpty(obj.Path) && File.Exists(obj.Path))
				{
					// 이벤트 이미지 삭제
					File.Delete(obj.Path);
				}

				var boundingBoxes = _db.BoundingBoxes.Where(x => x.EventId == obj.Id);
				_db.BoundingBoxes.RemoveRange(boundingBoxes);
				_db.Events.Remove(obj);
				await _db.SaveChangesAsync();
			}
		}

		public async ValueTask<EventVideoDTO> Get(int id)
		{
			try
			{
				return _mapper.Map<EventVideo, EventVideoDTO>(await _db.EventVideos
					.Include(ev => ev.Events)
					.ThenInclude(e => e.Camera)
					.Include(ev => ev.Events)
					.ThenInclude(e => e.BoundingBoxes)
					.FirstOrDefaultAsync(x => x.Id == id));

			} catch(Exception ex)
			{
				Console.WriteLine(ex.StackTrace);
				Console.WriteLine(ex.Message);

				throw;
			}
		}

		public async ValueTask<IEnumerable<EventVideoDTO>> GetAllByUserId(string userId)
		{
			try
			{
                return _mapper.Map<IEnumerable<EventVideo>, IEnumerable<EventVideoDTO>>(_db.EventVideos
					.Include(ev => ev.Events)
					.ThenInclude(e => e.Camera)
					.Include(ev => ev.Events)
					.ThenInclude(e => e.BoundingBoxes)
					.Where(x => x.UserId == userId));

            } catch (Exception ex)
			{
				Console.WriteLine(ex.StackTrace);
				Console.WriteLine(ex.Message);

				throw;
			}
		}
	}
}
