using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IJPSystem.Platform.Domain.Enums
{
    public enum UserRole
    {
        Operator = 0,   // 기본 작업자 (조회 위주)
        Engineer = 1,   // 엔지니어 (설정 및 유지보수)
        Admin = 2       // 관리자 (모든 권한)
    }
}
