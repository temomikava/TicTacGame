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
        public async Task<IActionResult> UserRegistration([FromBody] Registration filter)
        {
            //try


            var data =  _registrationService.Registration(filter);
            return Ok(data.ErrorMessage);

            //return Ok(data);


            //catch (Exception ex)
            //{
            //    throw new Exception("Error Message ", ex);
            //}
        }
        
        [HttpGet("")]
        public async Task<string> GetRegistrationById()
        {
            return "OK";
        }

    }
}
