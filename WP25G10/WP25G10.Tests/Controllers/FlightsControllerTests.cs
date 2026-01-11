using System;
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
    public class FlightsControllerTests
    {
        private readonly ApplicationDbContext _context;
        private readonly FlightsController _controller;

        public FlightsControllerTests()
        {
            _context = DbContextFactory.CreateInMemoryContext();
            SeedFlights();

            var userManager = UserManagerMock.Create();

            _controller = new FlightsController(_context, userManager.Object);

            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        private void SeedFlights()
        {
            var airline = new Airline { Id = 1, Name = "Alpha Air", Code = "AA", IsActive = true };
            _context.Airlines.Add(airline);

            _context.Flights.AddRange(
                new Flight
                {
                    Id = 1,
                    FlightNumber = "AA100",
                    AirlineId = 1,
                    OriginAirport = "AAA",
                    DestinationAirport = "BBB",
                    DepartureTime = DateTime.Today.AddHours(10),
                    ArrivalTime = DateTime.Today.AddHours(12),
                    IsActive = true
                },
                new Flight
                {
                    Id = 2,
                    FlightNumber = "AA101",
                    AirlineId = 1,
                    OriginAirport = "AAA",
                    DestinationAirport = "CCC",
                    DepartureTime = DateTime.Today.AddHours(8),
                    ArrivalTime = DateTime.Today.AddHours(10),
                    IsActive = true
                }
            );

            _context.SaveChanges();
        }

        [Fact]
        public async Task Index_DefaultBoardDeparturesOrderedByDepartureTime()
        {
            var result = await _controller.Index(
                search: null,
                status: null,
                board: null,
                airlineId: null,
                gateId: null,
                terminal: null,
                flightStatus: null,
                date: null,
                delayedOnly: null);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<FlightsIndexViewModel>(view.Model);

            var ordered = model.Flights
                .OrderBy(f => f.DepartureTime)
                .Select(f => f.Id)
                .ToList();

            Assert.Equal(ordered, model.Flights.Select(f => f.Id).ToList());
        }

        [Fact]
        public async Task Index_SearchByOriginAirport()
        {
            var result = await _controller.Index(
                search: "AAA",
                status: "all",
                board: "departures",
                airlineId: null,
                gateId: null,
                terminal: null,
                flightStatus: null,
                date: null,
                delayedOnly: null);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<FlightsIndexViewModel>(view.Model);

            Assert.Equal("AAA", model.SearchTerm);
            Assert.NotNull(model.Flights);
        }


    }
}