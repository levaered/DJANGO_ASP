using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MySqlConnector;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BCrypt.Net;
using Dapper;

namespace WebApiJWT.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;

        public AuthController(IConfiguration config)
        {
            _config = config;
            _connectionString = _config.GetConnectionString("MySQLConnection");
        }

        [HttpPost("SignUp")]
        public async Task<IActionResult> SignUp([FromBody] LoginRequest loginRequest)
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    var existingUser = await connection.QueryFirstOrDefaultAsync<User>(
                        "SELECT * FROM users_user WHERE email = @Email", new { Email = loginRequest.Email });

                    if (existingUser != null)
                    {
                        return Conflict("Пользователь с таким email уже существует");
                    }

                    var newUser = new User
                    {
                        Email = loginRequest.Email,
                        Password = loginRequest.Password // Сохраняем пароль как есть
                    };

                    var insertQuery = "INSERT INTO users_user (email, password) VALUES (@Email, @Password)";
                    await connection.ExecuteAsync(insertQuery, new { newUser.Email, newUser.Password });

                    return Ok("Пользователь успешно зарегистрирован");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during SignUp: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, $"Произошла ошибка при регистрации пользователя: {ex.Message}");
            }
        }

        [HttpPost("LogIn")]
        public async Task<IActionResult> LogIn([FromBody] LoginRequest loginRequest)
        {
            try
            {
                if (string.IsNullOrEmpty(loginRequest.Email) || string.IsNullOrEmpty(loginRequest.Password))
                {
                    Console.WriteLine("Email or password is null or empty");
                    return BadRequest("Email и пароль должны быть заполнены");
                }

                using (var connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var user = await connection.QueryFirstOrDefaultAsync<User>(
                        "SELECT * FROM users_user WHERE email = @Email AND password = @Password",
                        new { Email = loginRequest.Email, Password = loginRequest.Password });

                    if (user == null)
                    {
                        Console.WriteLine("User not found or invalid credentials");
                        return Unauthorized("Неверный email или пароль");
                    }

                    var token = GenerateToken(loginRequest.Email);
                    return Ok(token);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during LogIn: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, $"Произошла ошибка при входе пользователя: {ex.Message}");
            }
        }

        private string GenerateToken(string email)
        {
            var jwtKey = _config["Jwt:Key"];
            Console.WriteLine($"JWT Key: {jwtKey}");
            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new ArgumentNullException("Jwt:Key", "JWT key is null or empty");
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var userId = Guid.NewGuid().ToString(); // Генерируем случайную строку для user_id

            var claims = new[]
            {
                new Claim("id", userId), // Используем случайную строку в качестве user_id
                new Claim(JwtRegisteredClaimNames.Sub, email),
                new Claim(JwtRegisteredClaimNames.NameId, email),
                new Claim(JwtRegisteredClaimNames.Sid, email),
                new Claim("email", email),// Используем email в качестве идентификатора пользователя
                // Другие необходимые клеймы
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }

    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}