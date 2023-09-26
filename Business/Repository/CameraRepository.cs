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
	public class CameraRepository : ICameraRepository
	{
		private readonly AppDbContext _db;
		private readonly IMapper _mapper;

		public CameraRepository(AppDbContext db, IMapper mapper)
		{
			_db = db;
			_mapper = mapper;
		}

		public async ValueTask<CameraDTO> Create(CameraDTO objDTO)
		{
			var obj = _mapper.Map<CameraDTO, Camera>(objDTO);
			var createdObj = _db.Cameras.Add(obj);
			await _db.SaveChangesAsync();

			return _mapper.Map<Camera, CameraDTO>(createdObj.Entity);
		}

		public async ValueTask<int> Delete(int id)
		{
			var obj = await _db.Cameras.FirstOrDefaultAsync(x => x.Id == id);
			if (obj != null)
			{
				_db.Cameras.Remove(obj);
				return await _db.SaveChangesAsync();
			}

			return 0;
		}

		public async ValueTask<CameraDTO> Get(int id)
		{
			var obj = await _db.Cameras.FirstOrDefaultAsync(x => x.Id == id);
			if (obj != null)
			{
				return _mapper.Map<Camera, CameraDTO>(obj);
			}

			return new CameraDTO();
		}

		public async ValueTask<IEnumerable<CameraDTO>> GetAll()
		{
			return _mapper.Map<IEnumerable<Camera>, IEnumerable<CameraDTO>>(_db.Cameras);
		}

		public async ValueTask<IEnumerable<CameraDTO>> GetAllByUserId(string userId)
		{
			return _mapper.Map<IEnumerable<Camera>, IEnumerable<CameraDTO>>(_db.Cameras.Where(x => x.UserId == userId));
		}

		public ValueTask<CameraDTO> Update(CameraDTO objDTO)
		{
			throw new NotImplementedException();
		}
	}
}
