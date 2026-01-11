using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WP25G10.Areas.Admin.Controllers;
using WP25G10.Data;
using WP25G10.Models;
using WP25G10.Models.ViewModels;
using WP25G10.Tests.Helpers;
using Xunit;

namespace WP25G10.Tests.Controllers
{
    public class CheckInDesksControllerTests
    {
        private readonly ApplicationDbContext _context;
        private readonly CheckInDesksController _controller;

        public CheckInDesksControllerTests()
        {
            _context = DbContextFactory.CreateInMemoryContext();
            SeedDesks();

            var userManager = UserManagerMock.Create();

            _controller = new CheckInDesksController(_context, userManager.Object);

            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        private void SeedDesks()
        {
            _context.CheckInDesks.AddRange(
                new CheckInDesk { Id = 1, Terminal = "A", DeskNumber = 1, IsActive = true },
                new CheckInDesk { Id = 2, Terminal = "A", DeskNumber = 2, IsActive = false },
                new CheckInDesk { Id = 3, Terminal = "B", DeskNumber = 1, IsActive = true }
            );
            _context.SaveChanges();
        }

        [Fact]
        public async Task Index_ReturnsViewWithModel()
        {
            var result = await _controller.Index(
                search: null,
                status: "all",
                sort: "created_desc",
                page: 1,
                pageSize: 10);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<CheckInDesksIndexViewModel>(view.Model);

            Assert.Equal(3, model.Desks.Count);
        }

        [Fact]
        public async Task Index_SortByTerminalAsc()
        {
            var result = await _controller.Index(
                search: null,
                status: "all",
                sort: "terminal_asc",
                page: 1,
                pageSize: 10);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<CheckInDesksIndexViewModel>(view.Model);

            var ordered = model.Desks
                .OrderBy(d => d.Terminal)
                .ThenBy(d => d.DeskNumber)
                .Select(d => d.Id)
                .ToList();

            Assert.Equal(ordered, model.Desks.Select(d => d.Id).ToList());
        }

        [Fact]
        public async Task Index_StatusInactiveOnly()
        {
            var result = await _controller.Index(
                search: null,
                status: "inactive",
                sort: "created_desc",
                page: 1,
                pageSize: 10);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<CheckInDesksIndexViewModel>(view.Model);

            Assert.All(model.Desks, d => Assert.False(d.IsActive));
        }
    }
}