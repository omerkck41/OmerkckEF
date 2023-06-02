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
	}
}
