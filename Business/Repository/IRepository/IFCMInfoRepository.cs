using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Repository.IRepository
{
	public interface IFCMInfoRepository
	{
		ValueTask<FCMInfoDTO> Create(FCMInfoDTO objDTO);
		ValueTask<FCMInfoDTO> Update(FCMInfoDTO objDTO);
		ValueTask<int> Delete(int id);
		ValueTask<FCMInfoDTO?> Get(int id);
		ValueTask<IEnumerable<FCMInfoDTO>> GetAllByUserId(string userId);
	}
}
