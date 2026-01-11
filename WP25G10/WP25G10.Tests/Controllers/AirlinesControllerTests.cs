using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using WP25G10.Areas.Admin.Controllers;
using WP25G10.Data;
using WP25G10.Models;
using WP25G10.Models.ViewModels;
using WP25G10.Tests.Helpers;
using Xunit;

namespace WP25G10.Tests.Controllers
{
    public class AirlinesControllerTests
    {
        private readonly ApplicationDbContext _context;
        private readonly AirlinesController _controller;

        public AirlinesControllerTests()
        {
            _context = DbContextFactory.CreateInMemoryContext();
            SeedAirlines();

            var userManager = UserManagerMock.Create();

            var envMock = new Mock<IWebHostEnvironment>();
            envMock.Setup(e => e.WebRootPath).Returns("wwwroot");

            _controller = new AirlinesController(_context, userManager.Object, envMock.Object);

            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        private void SeedAirlines()
        {
            _context.Airlines.AddRange(
                new Airline { Id = 1, Name = "Alpha Air", Code = "AA", Country = "X", IsActive = true },
                new Airline { Id = 2, Name = "Beta Lines", Code = "BL", Country = "Y", IsActive = false },
                new Airline { Id = 3, Name = "Zeta Flights", Code = "ZF", Country = "Z", IsActive = true }
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
                pageSize: 5);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<AirlinesIndexViewModel>(view.Model);

            Assert.Equal(3, model.Airlines.Count());
            Assert.Equal("created_desc", model.SortOrder);
        }

        [Fact]
        public async Task Index_DefaultSort_ByIdDescending()
        {
            var result = await _controller.Index(
                search: null,
                status: "all",
                sort: "created_desc",
                page: 1,
                pageSize: 10);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<AirlinesIndexViewModel>(view.Model);

            var ids = model.Airlines.Select(a => a.Id).ToList();
            Assert.Equal(new[] { 3, 2, 1 }, ids);
        }

        [Fact]
        public async Task Index_SearchFiltersByNameCodeOrCountry()
        {
            var result = await _controller.Index(
                search: "alpha",
                status: "all",
                sort: "created_desc",
                page: 1,
                pageSize: 10);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<AirlinesIndexViewModel>(view.Model);

            Assert.Single(model.Airlines);
            Assert.Equal("Alpha Air", model.Airlines.First().Name);
        }

        [Fact]
        public async Task Index_StatusActiveOnly()
        {
            var result = await _controller.Index(
                search: null,
                status: "active",
                sort: "created_desc",
                page: 1,
                pageSize: 10);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<AirlinesIndexViewModel>(view.Model);

            Assert.All(model.Airlines, a => Assert.True(a.IsActive));
        }
    }
}