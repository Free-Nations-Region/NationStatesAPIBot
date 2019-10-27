using CyborgianStates.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CyborgianStates.Repositories
{
    public class UserRepository : IUserRepository
    {
        public Task AddUserToDbAsync(ulong userId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> IsUserInDbAsync(ulong userId)
        {
            throw new NotImplementedException();
        }

        public Task RemoveUserFromDbAsync(ulong userId)
        {
            throw new NotImplementedException();
        }
    }
}
