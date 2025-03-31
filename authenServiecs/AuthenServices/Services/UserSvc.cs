using AuthenController.Models;
using AuthenServices.DBContext;
using Microsoft.EntityFrameworkCore;

namespace AuthenServices.Services
{
    public interface IUserServices
    {
        Task<User> GetUserByIdAsync(Guid id);
        Task<User> GetUserByNameAsync(string userName);
        Task<List<User>> GetAllUsersAsync();
        Task<bool> AddUser(User user);
        Task<bool> UpdateUser(User user);
        Task<bool> DeleteUser(User user);
    }

    public class UserServices : IUserServices
    {
        private readonly AuthenDb _authenDb = new AuthenDb("authenDb.db");

        public async Task<bool> AddUser(User user)
        {
            try
            {
                _authenDb.users.Add(user);

                await _authenDb.SaveChangesAsync();

                return true;

            }catch{
                return false;
            }
        }

        public Task<bool> DeleteUser(User user)
        {
            throw new NotImplementedException();
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _authenDb.users.ToListAsync();
        }

        public async Task<User> GetUserByIdAsync(Guid id)
        {
            var user = await _authenDb.users.FirstOrDefaultAsync(x => x.Id == id);
            
            return user;
        }

        public async Task<User> GetUserByNameAsync(string userName)
        {
            var user = await _authenDb.users.FirstOrDefaultAsync(x => x.UserName == userName);
            
            return user;
        }

        public Task<bool> UpdateUser(User user)
        {
            throw new NotImplementedException();
        }
    }
}