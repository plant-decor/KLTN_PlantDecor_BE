namespace PlantDecor.BusinessLogicLayer.Exceptions
{
    public class SecurityStampMismatchException : Exception
    {
        public SecurityStampMismatchException() : base("Phiên đăng nhập đã hết hiệu lực. Vui lòng đăng nhập lại.") { }
        public SecurityStampMismatchException(string message) : base(message)
        {
        }
    }
}
