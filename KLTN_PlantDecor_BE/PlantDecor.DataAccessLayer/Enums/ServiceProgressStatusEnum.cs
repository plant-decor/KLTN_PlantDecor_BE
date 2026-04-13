using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantDecor.DataAccessLayer.Enums
{
    public enum ServiceProgressStatusEnum
    {
        Pending = 1, // Công việc đang chờ bắt đầu
        Assigned = 2, // Công việc đã được giao cho người thực hiện
        InProgress = 3, // Công việc đang tiến hành
        Completed = 4, // Công việc đã hoàn thành
        Cancelled = 5 // Công việc đã bị hủy
    }
}
