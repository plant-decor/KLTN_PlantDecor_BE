namespace PlantDecor.BusinessLogicLayer.Exceptions
{
    public class BadRequestException : Exception
    {
        public BadRequestException() : base("Invalid Request!") { }
        public BadRequestException(string message) : base(message)
        {
        }
    }
}
