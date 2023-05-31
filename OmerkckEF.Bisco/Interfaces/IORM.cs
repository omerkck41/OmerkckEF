using OmerkckEF.Biscom.ToolKit;

namespace OmerkckEF.Biscom.Interfaces
{
	public interface IORM<T> where T : class
	{
		Result<List<T>> GetAll();
		Result<T> GetById(object id);
		Result<int> Insert(T entity);
		Result<bool> MultiInsert(IEnumerable<T> entityList);
		Result<bool> Update(T entity);
		Result<bool> Delete(T entity);
	}
}