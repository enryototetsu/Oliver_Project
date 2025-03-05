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

            if (string.IsNullOrEmpty(booking.BookedBy))
            {
                return BadRequest(new { message = "BookedBy field is required." });
            }

            try
            {
                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetBooking), new { id = booking.BookingId },
                    new { message = "Booking created successfully.", data = booking });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "Database error occurred.", error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unexpected error occurred.", error = ex.Message });
            }
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

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
            var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value);

            // Members can only update their own bookings
            if (userRoles.Contains("Member") && existingBooking.BookedBy != userId)
            {
                return StatusCode(403, new
                {
                    message = "You are not authorized to update this booking."
                });
            }

            // Update allowed fields only
            existingBooking.FacilityDescription = updatedBooking.FacilityDescription;
            existingBooking.BookingDateFrom = updatedBooking.BookingDateFrom;
            existingBooking.BookingDateTo = updatedBooking.BookingDateTo;
            existingBooking.BookingStatus = updatedBooking.BookingStatus;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Booking updated successfully.", data = existingBooking });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating booking.", error = ex.Message });
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

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
            var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value);

            // Only Admins or the owner can delete
            if (!userRoles.Contains("Admin") && booking.BookedBy != userId)
            {
                return StatusCode(403, new { message = "You are not authorized to delete this booking." });
            }

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Booking deleted successfully." });
        }
    }
}
