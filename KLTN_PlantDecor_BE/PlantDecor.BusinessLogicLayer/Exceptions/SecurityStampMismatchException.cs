namespace PlantDecor.BusinessLogicLayer.Exceptions
{
    public class SecurityStampMismatchException : Exception
    {
        public SecurityStampMismatchException() : base("Session has expired. Please log in again.") { }
        public SecurityStampMismatchException(string message) : base(message)
        {
        }
    }
}
