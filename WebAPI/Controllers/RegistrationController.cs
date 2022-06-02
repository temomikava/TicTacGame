using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Core.Interface;
using WebAPI.Models;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegistrationController : Controller
    {
        private readonly IUserRegistration _registrationService;
        public RegistrationController(IUserRegistration registrationService)
        {
            _registrationService = registrationService;
        }


        [HttpPost("Registration")]
        public void UserRegistration([FromBody] Registration filter)
        {
            _registrationService.Registration(filter);
        }
        [HttpGet("")]
        public async Task<string> GetRegistrationById()
        {
            return "OK";
        }

    }
}
