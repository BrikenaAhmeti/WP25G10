using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WP25G10.Areas.Admin.Controllers;
using WP25G10.Models.ViewModels;
using WP25G10.Tests.Helpers;
using Xunit;

namespace WP25G10.Tests.Controllers
{
    public class StaffControllerTests
    {
        private readonly StaffController _controller;

        public StaffControllerTests()
        {
            var staffUsers = new List<IdentityUser>
            {
                new IdentityUser { Id = "1", UserName = "staff1", Email = "s1@test.com" },
                new IdentityUser { Id = "2", UserName = "staff2", Email = "s2@test.com" }
            };

            var userManagerMock = UserManagerMock.CreateWithUsers(staffUsers);
            var roleManagerMock = RoleManagerMock.Create(); // your existing helper

            _controller = new StaffController(userManagerMock.Object, roleManagerMock.Object);

            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public async Task Index_ReturnsViewWithStaffList()
        {
            var result = await _controller.Index(
                search: null,
                StatusFilter: "all",
                page: 1,
                pageSize: 10,
                reset: true);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<StaffIndexViewModel>(view.Model);

            Assert.Equal(2, model.Staff.Count);
            Assert.Contains(model.Staff, s => s.UserName == "staff1");
        }
    }
}