using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WP25G10.Areas.Admin.Controllers;
using WP25G10.Data;
using WP25G10.Models;
using WP25G10.Models.ViewModels;
using WP25G10.Tests.Helpers;
using Xunit;

namespace WP25G10.Tests.Controllers
{
    public class GatesControllerTests
    {
        private GatesController CreateController(ApplicationDbContext context)
        {
            var userManagerMock = UserManagerMock.Create();

            var controller = new GatesController(context, userManagerMock.Object);

            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            return controller;
        }

        private void SeedGates(ApplicationDbContext context)
        {
            context.Gates.AddRange(
                new Gate { Id = 1, Terminal = "A", Code = "01", IsActive = true, Status = GateStatus.Open },
                new Gate { Id = 2, Terminal = "B", Code = "02", IsActive = false, Status = GateStatus.Closed },
                new Gate { Id = 3, Terminal = "C", Code = "03", IsActive = true, Status = GateStatus.Open }
            );
            context.SaveChanges();
        }

        [Fact]
        public async Task Index_DefaultParameters_ReturnsGatesOrderedByCreatedDesc()
        {
            using var context = DbContextFactory.CreateInMemoryContext();
            SeedGates(context);
            var controller = CreateController(context);

            var result = await controller.Index(
                search: null,
                status: "all",
                gateState: "all",
                sort: "created_desc",
                page: 1,
                pageSize: 10
            );

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<GatesIndexViewModel>(view.Model);

            Assert.Equal(new[] { 3, 2, 1 }, model.Gates.Select(g => g.Id).ToArray());
        }

        [Fact]
        public async Task Index_SearchFiltersByTerminalOrCode()
        {
            using var context = DbContextFactory.CreateInMemoryContext();
            SeedGates(context);
            var controller = CreateController(context);

            var result = await controller.Index(
                search: "A",
                status: "all",
                gateState: "all",
                sort: "created_desc",
                page: 1,
                pageSize: 10
            );

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<GatesIndexViewModel>(view.Model);

            Assert.Single(model.Gates);
            Assert.Equal("A", model.Gates.First().Terminal);
        }

        [Fact]
        public async Task Index_FiltersByGateStateOpen()
        {
            using var context = DbContextFactory.CreateInMemoryContext();
            SeedGates(context);
            var controller = CreateController(context);

            var result = await controller.Index(
                search: null,
                status: "all",
                gateState: "open",
                sort: "created_desc",
                page: 1,
                pageSize: 10
            );

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<GatesIndexViewModel>(view.Model);

            Assert.All(model.Gates, g => Assert.Equal(GateStatus.Open, g.Status));
        }

    }
}