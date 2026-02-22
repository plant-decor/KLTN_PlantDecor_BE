using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantDecor.BusinessLogicLayer.Exceptions
{
    public class ForbiddenException : Exception
    {
        public ForbiddenException() : base("Bạn không có đủ thẩm quyền để truy cập vào tài nguyên này!") { }
        public ForbiddenException(string message) : base(message)
        {
        }
    }
}
