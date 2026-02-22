using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantDecor.BusinessLogicLayer.Exceptions
{
    public class ConflictException : Exception
    {
        public ConflictException() : base("Xung đột dữ liệu!") { }
        public ConflictException(string message) : base(message)
        {
        }
    }
}
