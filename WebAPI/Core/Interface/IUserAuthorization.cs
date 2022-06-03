using WebAPI.Models;

namespace WebAPI.Core.Interface
{
    public interface IUserAuthorization
    {
        public (int Error, string ErrorMessage) Authorization(Authorization registration);
    }
}
