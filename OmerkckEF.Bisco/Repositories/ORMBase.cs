using OmerkckEF.Biscom.Interfaces;
using OmerkckEF.Biscom.ToolKit;

namespace OmerkckEF.Biscom.Repositories
{
	public class ORMBase<T> : IORM<T> where T : class
	{
		

		public Result<List<T>> GetAll()
		{
			throw new NotImplementedException();
		}

		public Result<T> GetById(object id)
		{
			throw new NotImplementedException();
		}

		public Result<int> Insert(T entity)
		{
			throw new NotImplementedException();
		}

		public Result<bool> MultiInsert(IEnumerable<T> entityList)
		{
			throw new NotImplementedException();
		}

		public Result<bool> Update(T entity)
		{
			throw new NotImplementedException();
		}

		public Result<bool> Delete(T entity)
		{
			throw new NotImplementedException();
		}
	}
}
