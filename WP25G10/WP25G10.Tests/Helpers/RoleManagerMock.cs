using Microsoft.AspNetCore.Identity;
using Moq;

namespace WP25G10.Tests.Helpers
{
    public static class RoleManagerMock
    {
        public static Mock<RoleManager<IdentityRole>> Create()
        {
            var store = new Mock<IRoleStore<IdentityRole>>();

            var manager = new Mock<RoleManager<IdentityRole>>(
                store.Object,
                null, null, null, null);

            return manager;
        }
    }
}