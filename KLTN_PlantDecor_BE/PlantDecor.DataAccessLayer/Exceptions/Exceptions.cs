namespace PlantDecor.DataAccessLayer.Exceptions
{
    public sealed class ForbiddenAccessException : Exception
    {
        public ForbiddenAccessException() : base("Bạn không đủ thẩm quyền truy cập vào tài nguyên này") { }
        public ForbiddenAccessException(string message) : base(message) { }
    }
}
