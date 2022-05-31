using WebAPI.Models;

namespace WebAPI.Core.Interface
{
    public interface IUserRegistration
    {
        public Task Registration(Registration registration);
    }
}
