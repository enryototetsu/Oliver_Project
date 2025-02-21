using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oliver_Project.Data;
using Oliver_Project.Models;
using System.Security.Claims;

namespace Oliver_Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BookingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 🔍 GET: api/bookings - View all bookings (Admins & Members)
        [HttpGet]
        [Authorize(Roles = "Admin,Member")]
        public async Task<IActionResult> GetBookings()
        {
            var bookings = await _context.Bookings.ToListAsync();
            if (bookings == null || !bookings.Any())
            {
                return NotFound(new { message = "No bookings found." });
            }
            return Ok(new { message = "Bookings retrieved successfully.", data = bookings });
        }

        // 🔍 GET: api/bookings/{id} - View specific booking
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Member")]
        public async Task<IActionResult> GetBooking(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound(new { message = $"Booking with ID {id} not found." });
            }
            return Ok(new { message = "Booking retrieved successfully.", data = booking });
        }

        // ➕ POST: api/bookings - Create a new booking
        [HttpPost]
        [Authorize(Roles = "Admin,Member")]
        public async Task<IActionResult> CreateBooking([FromBody] Booking booking)
        {
            if (booking == null)
            {
                return BadRequest(new { message = "Booking data is null." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            booking.BookedBy = userId;

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBooking), new { id = booking.BookingId }, new { message = "Booking created successfully.", data = booking });
        }

        // ✏️ PUT: api/bookings/{id} - Update booking
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Member")]
        public async Task<IActionResult> UpdateBooking(int id, [FromBody] Booking updatedBooking)
        {
            if (updatedBooking == null)
            {
                return BadRequest(new { message = "Booking data is null." });
            }

            var existingBooking = await _context.Bookings.FindAsync(id);
            if (existingBooking == null)
            {
                return NotFound(new { message = $"Booking with ID {id} not found." });
            }

            // Get the user ID of the logged-in member
            var userId = User.Identity?.Name;

            // Check if the user is a Member and trying to update someone else's booking
            if (User.IsInRole("Member") && existingBooking.BookedBy != userId)
            {
                return StatusCode(403, new
                {
                    message = "You are not authorized to update this booking. Members can only update their own bookings."
                });
            }

            // Update booking details
            existingBooking.FacilityDescription = updatedBooking.FacilityDescription;
            existingBooking.BookingDateFrom = updatedBooking.BookingDateFrom;
            existingBooking.BookingDateTo = updatedBooking.BookingDateTo;
            existingBooking.BookedBy = updatedBooking.BookedBy;
            existingBooking.BookingStatus = updatedBooking.BookingStatus;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Booking updated successfully.", data = existingBooking });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the booking.", error = ex.Message });
            }
        }

        // ❌ DELETE: api/bookings/{id} - Delete booking
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Member")]
        public async Task<IActionResult> DeleteBooking(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound(new { message = $"Booking with ID {id} not found." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value);

            // ✅ Only Admins or the owner can delete
            if (!userRoles.Contains("Admin") && booking.BookedBy != userId)
            {
                return Forbid("You can only delete your own bookings.");
            }

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Booking deleted successfully." });
        }
    }
}
