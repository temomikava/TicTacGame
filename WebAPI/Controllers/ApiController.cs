using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Core.Interface;
using WebAPI.Models;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApiController : Controller
    {
        private readonly IDatabaseConnection _connection;
        public ApiController(IDatabaseConnection connection )
        {
            _connection = connection;
        }

        [HttpGet("get")]
        public async Task<IActionResult> Get()
        {
            return Ok("ok");
        }
        [HttpPost("Registration")]
        public async Task<IActionResult> UserRegistration([FromBody] RegistrationModel filter)
        {         
            var data =  _connection.Registration(filter);
            return Ok(data.ErrorMessage);
        }

        [HttpPost("Authorization")]
        public async Task<IActionResult> UserAuthorization([FromBody]AuthorizationModel filter)
        {           
            var data=_connection.Authorization(filter);
            return Ok(data.ErrorMessage);
        }
        [HttpPost("Create_match")]
        public async Task<IActionResult> CreateMatchup([FromBody] Matchup filter)
        {
            var data = _connection.CreateMatch(filter);
            return Ok(data.ErrorMessage);
        }


    }
}
