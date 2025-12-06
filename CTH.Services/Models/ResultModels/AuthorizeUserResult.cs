using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTH.Services.Models.ResultModels
{
    public class AuthorizeUserResult
    {
        public long Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int RoleTypeId { get; set; }
    }
}
