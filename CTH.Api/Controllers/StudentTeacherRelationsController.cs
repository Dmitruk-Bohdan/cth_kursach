using CTH.Api.Infrastructure;
using CTH.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;

namespace CTH.Api.Controllers
{
    [Route("auth")]
    [ApiController]
    public class StudentTeacherRelationsController
    {
    }
}
