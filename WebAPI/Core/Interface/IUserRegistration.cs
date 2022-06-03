using WebAPI.Models;

namespace WebAPI.Core.Interface
{
    public interface IUserRegistration
    {
        public (int Error, string ErrorMessage) Registration(Registration registration);
    }
}
