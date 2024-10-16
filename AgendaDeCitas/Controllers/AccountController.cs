using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System;
using NuGet.Protocol;

namespace AgendaDeCitas.Controllers
{
    public class AccountController : Controller
    {
        private readonly string _connectionString;

        public AccountController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public IActionResult Index()
        {
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            var userInfo = ValidateUser(username, password);

            if (userInfo != null)
            {
                if (userInfo.UsrEstado == 0)
                {
                    ViewBag.Message = "La cuenta no ha sido verificada. Por favor verifica tu cuenta antes de ingresar.";
                    return View("Index");
                }
                else if (userInfo.UsrEstado == 1)
                {
                    // Guardar en variables de sesión
                    HttpContext.Session.SetString("Nombres", userInfo.UsrNombres);
                    HttpContext.Session.SetString("Apellidos", userInfo.UsrApellidos);
                    HttpContext.Session.SetString("Email", userInfo.UsrEmail);
                    HttpContext.Session.SetString("Telefono", userInfo.UsrTelefono);

                    // Actualizar usr_lastlogin con la fecha actual
                    UpdateLastLogin(username);

                    ViewBag.Message = "Login exitoso";
                    return RedirectToAction("Index", "Ciudades"); // Redirige a la acción 'Index' del controlador 'Ciudades'
                }
            }

            ViewBag.Message = "Alguno de los datos introducidos es incorrecto, por favor, introdúcelos de nuevo.";
            return View("Index");
        }

        private UserInfo ValidateUser(string username, string password)
        {
            UserInfo userInfo = null;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string query = @"SELECT usr_email, usr_pass, usr_lastlogin, usr_estado, usr_nombres, usr_apellidos ,usr_telefono
                                 FROM Users 
                                 WHERE usr_email = @username AND usr_pass = @password";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password", password);

                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        userInfo = new UserInfo
                        {
                            UsrEmail = reader["usr_email"].ToString(),
                            UsrNombres = reader["usr_nombres"].ToString(),
                            UsrApellidos = reader["usr_apellidos"].ToString(),
                            UsrEstado = Convert.ToInt32(reader["usr_estado"]),
                            UsrLastLogin = Convert.ToDateTime(reader["usr_lastlogin"]),
                            UsrTelefono = reader["usr_telefono"].ToString()

                        };
                    }
                }
            }

            return userInfo;
        }

        private void UpdateLastLogin(string username)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string updateQuery = @"UPDATE Users SET usr_lastlogin = @lastLogin WHERE usr_email = @username";

                using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@lastLogin", DateTime.Now);
                    cmd.Parameters.AddWithValue("@username", username);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        // GET: Account/Register (Página de registro)
        public IActionResult Register()
        {
            return View();
        }
    }

    public class UserInfo
    {
        public string UsrEmail { get; set; }
        public string UsrNombres { get; set; }
        public string UsrApellidos { get; set; }
        public string UsrTelefono { get; set; }
        public int UsrEstado { get; set; }
        public DateTime UsrLastLogin { get; set; }
    }
}
