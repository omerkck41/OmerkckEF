namespace OmerkckEF.Biscom
{
	public class Result<T>
	{
        public Result()
        {
            this.IsSuccess = false;
			this.Data = Activator.CreateInstance<T>();
			this.Message = "Great, Everything is fine!!!";
        }

        public bool IsSuccess { get; set; }
		public T? Data { get; set; }
		public string? Message { get; set; }

	}
}