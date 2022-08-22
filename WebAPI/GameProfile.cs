using AutoMapper;
using GameLibrary;

namespace WebAPI
{
    public class GameProfile:Profile
    {
        public GameProfile()
        {
            CreateMap<Game, GameDTO>().ReverseMap();
        }

    }
}
