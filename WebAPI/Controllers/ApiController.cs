using GameLibrary;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WebAPI.Core.Interface;
using WebAPI.Models;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApiController : Controller
    {
        private readonly IDatabaseConnection _connection;
        public ApiController(IDatabaseConnection connection)
        {
            _connection = connection;
        }

        [HttpPost("Registration")]
        public async Task<IActionResult> UserRegistration([FromBody] RegistrationModel filter)
        {
            var data = _connection.Registration(filter);
            return Ok(data.ErrorMessage);
        }

        [HttpPost("Authorization")]
        public async  Task<IActionResult> UserAuthorization([FromBody] AuthorizationModel filter)
        {
            var data = _connection.Authorization(filter);

            return Ok(new
            {
                SessionId = data.SessionId,
                ErrorCode = data.ErrorCode,
                ErrorMessage = data.ErrorMessage
            });
        }
    }
}
