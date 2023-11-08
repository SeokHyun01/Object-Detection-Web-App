using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Repository.IRepository
{
	public interface ICameraRepository
	{
		ValueTask<CameraDTO> Create(CameraDTO objDTO);
		ValueTask<CameraDTO> Update(CameraDTO objDTO);
		ValueTask<int> Delete(int id);
		ValueTask<CameraDTO?> Get(int id);
		ValueTask<IEnumerable<CameraDTO>> GetAll();
		ValueTask<IEnumerable<CameraDTO>> GetAllByUserId(string userId);
	}
}
