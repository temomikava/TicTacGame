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
        private readonly IUserRegistration _registrationService;
        private readonly IUserAuthorization _authorizationService;
        public ApiController(IUserRegistration registrationService, IUserAuthorization userAuthorization)
        {
            _registrationService = registrationService;
            _authorizationService = userAuthorization;
        }


        [HttpPost("Registration")]
        public async Task<IActionResult> UserRegistration([FromBody] Registration filter)
        {         
            var data =  _registrationService.Registration(filter);
            return Ok(data.ErrorMessage);
        }

        [HttpPost("Authorization")]
        public async Task<IActionResult> UserAuthorization([FromBody]Authorization filter)
        {           
            var data=_authorizationService.Authorization(filter);
            return Ok(data.ErrorMessage);
        }

    }
}
