namespace OmerkckEF.Biscom.ToolKit
{
    public class Result<T>
    {
        public Result() { }

        public bool IsSuccess { get; set; } = false;
        public T? Data { get; set; } = default!;
        public string? Message { get; set; } = "Great, Everything is fine!!!";

    }
}