using GameLibrary;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
       
        // GET: api/<GameController>
       
        [HttpPost]
        public IActionResult CreateBoard(int boarsSize)
        {
            var gamestate = new GameState(boarsSize);
            return Ok(gamestate.GameGrid.GetLength(1));         
        }

       
    }
}
