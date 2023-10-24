using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Repository.IRepository
{
	public interface IEventRepository
	{
		ValueTask<EventDTO> Create(EventDTO objDTO);
		ValueTask<IEnumerable<EventDTO>> GetAll(IEnumerable<int> ids);
		ValueTask<IEnumerable<EventDTO>> GetAllByVideoId(int videoId);
		ValueTask<EventDTO> Update(EventDTO objDTO);
	}
}
