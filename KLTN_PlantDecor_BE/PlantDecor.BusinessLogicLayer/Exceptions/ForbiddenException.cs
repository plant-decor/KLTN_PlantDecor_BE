using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantDecor.BusinessLogicLayer.Exceptions
{
    public class ForbiddenException : Exception
    {
        public ForbiddenException() : base("You do not have permission to access this resource!") { }
        public ForbiddenException(string message) : base(message)
        {
        }
    }
}
