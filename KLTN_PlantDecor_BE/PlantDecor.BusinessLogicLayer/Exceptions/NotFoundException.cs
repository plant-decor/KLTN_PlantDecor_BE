using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantDecor.BusinessLogicLayer.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException() : base("Không tìm thấy tài nguyên!") { }
        public NotFoundException(string message) : base(message)
        {
        }
    }
}
