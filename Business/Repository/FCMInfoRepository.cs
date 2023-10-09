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
	public class FCMInfoRepository : IFCMInfoRepository
	{
		private readonly AppDbContext _db;
		private readonly IMapper _mapper;

		public FCMInfoRepository(AppDbContext db, IMapper mapper)
		{
			_db = db;
			_mapper = mapper;
		}

		public async ValueTask<FCMInfoDTO> Create(FCMInfoDTO objDTO)
		{
			try
			{
				if (string.IsNullOrEmpty(objDTO.DeviceNickname))
				{
					objDTO.DeviceNickname = "untitled";
				}
				var obj = _mapper.Map<FCMInfoDTO, FCMInfo>(objDTO);
				var createdObj = _db.FCMInfos.Add(obj);
				await _db.SaveChangesAsync();

				return _mapper.Map<FCMInfo, FCMInfoDTO>(createdObj.Entity);

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
				var obj = await _db.FCMInfos.FirstOrDefaultAsync(x => x.Id == id);
				if (obj != null)
				{
					_db.FCMInfos.Remove(obj);

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

		public async ValueTask<FCMInfoDTO?> Get(int id)
		{
			try
			{
				var obj = await _db.FCMInfos.FirstOrDefaultAsync(x => x.Id == id);
				if (obj != null)
				{
					return _mapper.Map<FCMInfo, FCMInfoDTO>(obj);
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

		public async ValueTask<IEnumerable<FCMInfoDTO>> GetAllByUserId(string userId)
		{
			return _mapper.Map<IEnumerable<FCMInfo>, IEnumerable<FCMInfoDTO>>(_db.FCMInfos.Where(x => x.UserId == userId));
		}

		public async ValueTask<FCMInfoDTO> Update(FCMInfoDTO objDTO)
		{
			try
			{
				var objFromDb = await _db.FCMInfos.FirstOrDefaultAsync(x => x.Id == objDTO.Id);
				if (objFromDb != null)
				{
					objFromDb.DeviceNickname = objDTO.DeviceNickname;
					objFromDb.Token = objDTO.Token;
					_db.FCMInfos.Update(objFromDb);
					await _db.SaveChangesAsync();

					return _mapper.Map<FCMInfo, FCMInfoDTO>(objFromDb);
				}
				return objDTO;

			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.StackTrace);
				Console.WriteLine(ex.Message);

				throw;
			}
		}
	}
}
