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
			try
			{
				if (string.IsNullOrEmpty(objDTO.Name))
				{
					objDTO.Name = "untitled";
				}
				var obj = _mapper.Map<CameraDTO, Camera>(objDTO);
				var createdObj = _db.Cameras.Add(obj);
				await _db.SaveChangesAsync();

				return _mapper.Map<Camera, CameraDTO>(createdObj.Entity);

			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.StackTrace);
				Console.WriteLine(ex.Message);

				throw;
			}
		}

		public async ValueTask<int> Delete(int id)
		{
			try
			{
				var obj = await _db.Cameras.FirstOrDefaultAsync(x => x.Id == id);
				if (obj != null)
				{
					_db.Cameras.Remove(obj);

					return await _db.SaveChangesAsync();
				}
				return 0;

			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.StackTrace);
				Console.WriteLine(ex.Message);

				throw;
			}
		}

		public async ValueTask<CameraDTO?> Get(int id)
		{
			try
			{
				var obj = await _db.Cameras.FirstOrDefaultAsync(x => x.Id == id);
				if (obj != null)
				{
					return _mapper.Map<Camera, CameraDTO>(obj);
				}
				return null;

			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.StackTrace);
				Console.WriteLine(ex.Message);

				throw;
			}
		}

		public async ValueTask<IEnumerable<CameraDTO>> GetAll()
		{
			return _mapper.Map<IEnumerable<Camera>, IEnumerable<CameraDTO>>(_db.Cameras);
		}

		public async ValueTask<IEnumerable<CameraDTO>> GetAllByUserId(string id)
		{
			return _mapper.Map<IEnumerable<Camera>, IEnumerable<CameraDTO>>(_db.Cameras.Where(x => x.UserId == id));
		}

		public async ValueTask<CameraDTO> Update(CameraDTO objDTO)
		{
			try
			{
				var objFromDb = await _db.Cameras.FirstOrDefaultAsync(x => x.Id == objDTO.Id);
				if (objFromDb != null)
				{
					objFromDb.Angle = objDTO.Angle;
					_db.Cameras.Update(objFromDb);
					await _db.SaveChangesAsync();

					return _mapper.Map<Camera, CameraDTO>(objFromDb);
				}
				return objDTO;

			} catch (Exception ex)
			{
				Console.WriteLine(ex.StackTrace);
				Console.WriteLine(ex.Message);

				throw;
			}
		}
	}
}
