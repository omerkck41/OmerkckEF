using OmerkckEF.Biscom.ToolKit;
using static OmerkckEF.Biscom.ToolKit.Enums;

namespace OmerkckEF.Biscom.Interfaces
{
    public interface IORM<T> where T : class, new()
    {
        Result<List<T>> GetAll();
        Result<T> GetById(object id);
        Result<int> Insert(T entity);
        Result<bool> MultiInsert(IEnumerable<T> entityList);
        Result<bool> Update(T entity);
        Result<bool> UpdateCompositeTable(T entity, params object[] args);
        Result<bool> Delete(T entity);

        //Table CRUD
        Result<bool> CreateTable();
        Result<bool> DropTable();
        Result<bool> UpdateTable();
        Result<bool> RemoveTableColumn(string? columnName = null);
        Result<bool> AddAttributeToTableColumn(TableColumnAttribute attribute, string propertyName);
    }
}