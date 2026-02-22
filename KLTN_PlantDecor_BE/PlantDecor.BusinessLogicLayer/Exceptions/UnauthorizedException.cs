using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantDecor.BusinessLogicLayer.Exceptions
{
    public class UnauthorizedException : Exception
    {
        public UnauthorizedException() : base("Không có quyền truy cập!") { }
        public UnauthorizedException(string message) : base(message)
        {
        }
    }
}
