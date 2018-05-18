using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OfficeMapper.Lib
{
    public enum StageEnum
    {
        NewRequest,
        SendITRequest,
        WaitITAccept,
        SendOwnerRequest,
        WaitOwnerAccept,
        Accepted,
        Declined
    }
}