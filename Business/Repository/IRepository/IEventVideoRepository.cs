using DataAccess;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Repository.IRepository
{
	public interface IEventVideoRepository
	{
		ValueTask<EventVideoDTO> Create(EventVideoDTO objDTO);
		ValueTask<int> Delete(int id);
	}
}
