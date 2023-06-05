using OmerkckEF.Biscom.DBContext;

namespace TestLab
{
	public static class Function
	{
		public static EntityContext EntityContext { get; } = new(DBServerInfo);

		public static DBServer DBServerInfo => new()
		{
			DbSchema = "sys",
			DbUser = "root",
			DbPassword = "1q2w3e4r",
		};


		///example usage
		//var rInsert = sys_configORM.Current.Insert(current);
		//var rUpdate = sys_configORM.Current.Update(current);
		//var rDelete = sys_configORM.Current.Delete(current);

		//var result = sys_configORM.Current.GetAll(x => x.sys_id == 5);
		//var result1 = sys_configORM.Current.GetAll();
		//	if(result.IsSuccess)
		//		DataGridView.dataSource = result.Data;
	}
}
