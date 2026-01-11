using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace WP25G10.Tests.Helpers
{
    public static class UserManagerMock
    {
        public static Mock<UserManager<IdentityUser>> Create()
        {
            var store = new Mock<IUserStore<IdentityUser>>();
            return new Mock<UserManager<IdentityUser>>(
                store.Object, null, null, null, null, null, null, null, null);
        }

        public static Mock<UserManager<IdentityUser>> CreateWithUsers(IList<IdentityUser> users)
        {
            var mock = Create();
            mock.Setup(m => m.GetUsersInRoleAsync("Staff"))
                .ReturnsAsync(users);
            return mock;
        }
    }
}
