using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WP25G10.Areas.Admin.Controllers;
using WP25G10.Data;
using WP25G10.Models;
using WP25G10.Models.ViewModels;
using WP25G10.Security;
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
            SeedData();

            var userManager = UserManagerMock.Create();
            _controller = new FlightsController(_context, userManager.Object);

            SetFreshHttpContext();
        }

        private void SetFreshHttpContext()
        {
            var httpContext = new DefaultHttpContext
            {
                Session = new TestSession(),
                User = BuildUserWithPermission(Permissions.Flights.View)
            };

            httpContext.Request.QueryString = QueryString.Empty;

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        private static ClaimsPrincipal BuildUserWithPermission(string permission)
        {
            var claims = new List<Claim>
            {
                new Claim(Permissions.ClaimType, permission),
                new Claim(ClaimTypes.NameIdentifier, "test-user-id")
            };

            return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        }

        private void SeedData()
        {
            // Seed Identity user that matches the test principal NameIdentifier
            var user = new IdentityUser
            {
                Id = "test-user-id",
                UserName = "test@local",
                NormalizedUserName = "TEST@LOCAL",
                Email = "test@local",
                NormalizedEmail = "TEST@LOCAL"
            };
            _context.Users.Add(user);

            var airline = new Airline { Id = 1, Name = "Alpha Air", Code = "AA", IsActive = true };
            _context.Airlines.Add(airline);

            var gateA = new Gate { Id = 1, Terminal = "T1", Code = "A1", IsActive = true, Status = GateStatus.Open };
            var gateB = new Gate { Id = 2, Terminal = "T1", Code = "A2", IsActive = true, Status = GateStatus.Open };
            _context.Gates.AddRange(gateA, gateB);

            var desk1 = new CheckInDesk { Id = 1, Terminal = "T1", DeskNumber = 1, IsActive = true };
            _context.CheckInDesks.Add(desk1);

            var today = DateTime.Today;

            _context.Flights.AddRange(
                new Flight
                {
                    Id = 1,
                    FlightNumber = "AA100",
                    AirlineId = 1,
                    GateId = 1,
                    CheckInDeskId = 1,
                    Type = FlightType.Departure,
                    OriginAirport = "Prishtina",
                    DestinationAirport = "BBB",
                    DepartureTime = today.AddHours(10),
                    ArrivalTime = today.AddHours(12),
                    Status = FlightStatus.Arrived,
                    DelayMinutes = 0,
                    IsActive = true,
                    CreatedByUserId = "test-user-id"
                },
                new Flight
                {
                    Id = 2,
                    FlightNumber = "AA101",
                    AirlineId = 1,
                    GateId = 2,
                    CheckInDeskId = 1,
                    Type = FlightType.Departure,
                    OriginAirport = "Prishtina",
                    DestinationAirport = "CCC",
                    DepartureTime = today.AddHours(8),
                    ArrivalTime = today.AddHours(10),
                    Status = FlightStatus.Delayed,
                    DelayMinutes = 25,
                    IsActive = true,
                    CreatedByUserId = "test-user-id"
                },
                new Flight
                {
                    Id = 3,
                    FlightNumber = "AA200",
                    AirlineId = 1,
                    GateId = 1,
                    CheckInDeskId = 1,
                    Type = FlightType.Arrival,
                    OriginAirport = "AAA",
                    DestinationAirport = "Prishtina",
                    DepartureTime = today.AddHours(6),
                    ArrivalTime = today.AddHours(7),
                    Status = FlightStatus.Arrived,
                    DelayMinutes = 0,
                    IsActive = true,
                    CreatedByUserId = "test-user-id"
                }
            );

            _context.SaveChanges();
        }

        [Fact]
        public async Task Index_DefaultBoardDepartures_OrderedByDepartureTime()
        {
            SetFreshHttpContext();

            Assert.True(_context.Flights.Any());

            var result = await _controller.Index(
                search: null,
                status: null,
                board: null,
                airlineId: null,
                gateId: null,
                terminal: null,
                flightStatus: null,
                date: null,
                delayedOnly: null,
                reset: null
            );

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<FlightsIndexViewModel>(view.Model);

            Assert.Equal("departures", model.Board);
            Assert.All(model.Flights, f => Assert.Equal(FlightType.Departure, f.Type));

            var expectedOrder = model.Flights.OrderBy(f => f.DepartureTime).Select(f => f.Id).ToList();
            var actualOrder = model.Flights.Select(f => f.Id).ToList();
            Assert.Equal(expectedOrder, actualOrder);
        }

        [Fact]
        public async Task Index_SearchByAirportOrFlightNumber_SetsSearchTerm_AndReturnsFlights()
        {
            SetFreshHttpContext();

            Assert.True(_context.Flights.Any(f => f.DestinationAirport == "CCC"));

            var result = await _controller.Index(
                search: "CCC",
                status: "all",
                board: "departures",
                airlineId: null,
                gateId: null,
                terminal: null,
                flightStatus: null,
                date: null,
                delayedOnly: null,
                reset: null
            );

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<FlightsIndexViewModel>(view.Model);

            Assert.Equal("CCC", model.SearchTerm);
            Assert.Equal("departures", model.Board);

            Assert.NotEmpty(model.Flights);
            Assert.Contains(model.Flights, f => f.DestinationAirport == "CCC");
        }

        [Fact]
        public async Task Index_DelayedOnly_ReturnsOnlyDelayedFlights()
        {
            SetFreshHttpContext();

            Assert.True(_context.Flights.Any(f => f.DelayMinutes > 0 || f.Status == FlightStatus.Delayed));

            var result = await _controller.Index(
                search: null,
                status: "all",
                board: "departures",
                airlineId: null,
                gateId: null,
                terminal: null,
                flightStatus: null,
                date: null,
                delayedOnly: true,
                reset: null
            );

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<FlightsIndexViewModel>(view.Model);

            Assert.True(model.DelayedOnly);
            Assert.NotEmpty(model.Flights);

            Assert.All(model.Flights, f =>
                Assert.True(f.DelayMinutes > 0 || f.Status == FlightStatus.Delayed));
        }

        [Fact]
        public async Task Index_ResetTrue_RedirectsToIndex_AndClearsSessionKeys()
        {
            SetFreshHttpContext();

            _controller.HttpContext.Session.SetString("Flights_Search", "AAA");
            _controller.HttpContext.Session.SetString("Flights_Status", "inactive");
            _controller.HttpContext.Session.SetString("Flights_Board", "arrivals");

            var result = await _controller.Index(
                search: null,
                status: null,
                board: null,
                airlineId: null,
                gateId: null,
                terminal: null,
                flightStatus: null,
                date: null,
                delayedOnly: null,
                reset: true
            );

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(FlightsController.Index), redirect.ActionName);

            Assert.Null(_controller.HttpContext.Session.GetString("Flights_Search"));
            Assert.Null(_controller.HttpContext.Session.GetString("Flights_Status"));
            Assert.Null(_controller.HttpContext.Session.GetString("Flights_Board"));
        }

        [Fact]
        public async Task Index_DateFilter_Departures_FiltersByDepartureDate()
        {
            SetFreshHttpContext();

            var targetDate = DateTime.Today;

            var result = await _controller.Index(
                search: null,
                status: "all",
                board: "departures",
                airlineId: null,
                gateId: null,
                terminal: null,
                flightStatus: null,
                date: targetDate,
                delayedOnly: null,
                reset: null
            );

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<FlightsIndexViewModel>(view.Model);

            var start = targetDate.Date;
            var end = start.AddDays(1);

            Assert.NotEmpty(model.Flights);
            Assert.All(model.Flights, f => Assert.True(f.DepartureTime >= start && f.DepartureTime < end));
        }
    }
}