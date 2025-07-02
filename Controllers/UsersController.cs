using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.IdentityModel.Tokens;
using Project.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Project.Controllers
{
    public class UsersController : Controller
    {
        private readonly Project2Context _context;
        private readonly IConfiguration _configuration;
        public UsersController(Project2Context context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[] {
                new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),
                new Claim(ClaimTypes.Name,user.Email),
                new Claim(ClaimTypes.Role,user.Role)
            };

            var token = new JwtSecurityToken(
                    issuer: jwtSettings["Issuer"],
                    audience: jwtSettings["Audience"],
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(Convert.ToDouble(jwtSettings["DurationInMinutes"])),
                    signingCredentials: creds
             );
            return new JwtSecurityTokenHandler().WriteToken(token);

        }

        [AllowAnonymous]
        //Get:Register
        public IActionResult Register()
        {
            return View();
        }

        //Post:Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Register([Bind("Name,Email,Password,Role")] User user) {
            if (ModelState.IsValid) {
                var exist = await _context.Users.AnyAsync(u=>u.Email==user.Email);
                if (exist) {
                    TempData["Error"] = "Email already exists";
                    return View(user);
                }

                var hasher =new PasswordHasher<User>();
                user.Password = hasher.HashPassword(user, user.Password);
                try
                {
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Registration successful. Please login.";
                    return RedirectToAction("Login");
                }
                catch (DbUpdateException ex)
                {
                    Console.WriteLine(ex.InnerException?.Message);
                }



            }
            return View(user);
        }

        //GET:Login
        public IActionResult Login() { 
            return View();
        }

        [HttpPost]
        [Route("/")]
        public async Task<IActionResult> Login([Bind("Email,Password")] Login model){
            var user= await _context.Users.FirstOrDefaultAsync(u=>u.Email== model.Email);
            if (user == null)
            {
                TempData["Error"] = "Invalid Email";
                return View();
            }

            var hasher= new PasswordHasher<User>();
            var result= hasher.VerifyHashedPassword(user,user.Password,model.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                TempData["Error"] = "Invalid Password";
                return View();
            }

            var token = GenerateJwtToken(user);
            Response.Cookies.Append("jwt", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                Expires = DateTime.UtcNow.AddMinutes(60)
            });
            if (user.Role == "Admin")
                return RedirectToAction("Index", "Users");
            else if (user.Role == "Manager")
                return RedirectToAction("Index", "Events");
            else if (user.Role == "User")
                return RedirectToAction("Create", "Events");
            else
                return RedirectToAction("Login");
        }
        //User user
        public IActionResult RoleRedirect() {
            var role = HttpContext.Session.GetString("UserRole");
            if (role == "Admin")
                return RedirectToAction("Index", "Users");
            else if (role == "Manager")
                return RedirectToAction("Index", "Events");
            else if (role == "User")
                return RedirectToAction("Index", "Events");
            else
                return Unauthorized("Invalid Role");
        }

        [Authorize]
        // GET: Users
        public async Task<IActionResult> Index()
        {
            Console.WriteLine("inside index");
            return View(await _context.Users.ToListAsync());
        }

        [Authorize]
        // GET: Users/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Users/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Email,Password,Role")] User user)
        {
            if (ModelState.IsValid)
            {
                var hasher= new PasswordHasher<User>();
                user.Password = hasher.HashPassword(user, user.Password);
                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }
        [Authorize]
        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }
        // POST: Users/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Email,Password,Role")] User user)
        {
            if (id != user.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Users/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}
