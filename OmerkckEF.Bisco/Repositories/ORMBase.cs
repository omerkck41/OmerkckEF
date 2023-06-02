using OmerkckEF.Biscom.DBContext;
using OmerkckEF.Biscom.Interfaces;
using OmerkckEF.Biscom.ToolKit;

namespace OmerkckEF.Biscom.Repositories
{
	public class ORMBase<T, OT> : IORM<T>
		where T : class, new()
		where OT : class, new()
	{
		private static OT? _current;
		public static OT Current
		{
			get
			{
				//_current ??= new OT();
				if (_current != null) return _current as OT;
					
				ORMBase<T, OT>._current = ORMBase<T, OT>._current ?? Activator.CreateInstance<OT>();
				return ORMBase<T, OT>._current;
			}
		}


		#region Properties
		private EntityContext DbHelper { get; set; } = new(DBServer.DBServerInfo ?? new());
		#endregion

		//public ORMBase(DbServer DbServer) : base(DbServer) => _dbHelper = this;



		public virtual Result<List<T>> GetAll() => DbHelper.GetMapClass<T>();

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