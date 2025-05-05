using Microsoft.AspNetCore.Mvc;
using System;
using System.Data.SqlClient;

namespace WebDubRosh.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        // Изменённая строка подключения с TrustServerCertificate=True для работы через localtunnel
        private readonly string _connectionString =
            "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework;TrustServerCertificate=True";

        // POST: /api/auth/login
        // Ожидает JSON: { "username": "значение", "password": "значение" }
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { success = false, message = "Некорректные данные." });
            }

            try
            {
                using (var con = new SqlConnection(_connectionString))
                {
                    con.Open();

                    // Проверка в таблице ChiefDoctors
                    string queryChief = "SELECT COUNT(*) FROM ChiefDoctors WHERE FullName = @login AND Password = @password";
                    using (var cmd = new SqlCommand(queryChief, con))
                    {
                        cmd.Parameters.AddWithValue("@login", request.Username);
                        cmd.Parameters.AddWithValue("@password", request.Password);
                        int count = (int)cmd.ExecuteScalar();
                        if (count > 0)
                        {
                            return Ok(new { success = true, role = "ChiefDoctor" });
                        }
                    }

                    // Проверка в таблице Doctors
                    string queryDoctor = "SELECT DoctorID FROM Doctors WHERE FullName = @login AND Password = @password";
                    using (var cmd = new SqlCommand(queryDoctor, con))
                    {
                        cmd.Parameters.AddWithValue("@login", request.Username);
                        cmd.Parameters.AddWithValue("@password", request.Password);
                        object result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            int doctorId = Convert.ToInt32(result);
                            return Ok(new { success = true, role = "Doctor", doctorID = doctorId });
                        }
                    }
                }

                return Ok(new { success = false });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }

    // Модель запроса для авторизации
    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}