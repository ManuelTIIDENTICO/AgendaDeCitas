using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using Microsoft.Data.SqlClient; // Cambiar a Microsoft.Data.SqlClient
using System.Net;
using System.Net.Mail;

namespace AgendaDeCitas.Controllers
{
    public class RecoveryController : Controller
    {
        private readonly string _connectionString;

        public RecoveryController(IConfiguration configuration)
        {
            // Obtén la cadena de conexión desde appsettings.json
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public IActionResult Index()
        {
            return View();
        }

        public ActionResult RecoverPassword()
        {
            return View();
        }

        // POST: Account/RecoverPassword
        [HttpPost]
        public ActionResult RecoverPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Message = "Por favor, ingresa tu correo electrónico.";
                return View();
            }

            // Variable para almacenar la contraseña del usuario
            string userPassword = string.Empty;
            string userName = string.Empty;

            // Busca el usuario en la base de datos usando ADO.NET
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "SELECT usr_email, usr_pass FROM users WHERE usr_email = @Email";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Email", email);

                try
                {
                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            userName = reader["usr_email"].ToString();
                            userPassword = reader["usr_pass"].ToString();
                        }
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.Message = $"Error al buscar el usuario en la base de datos: {ex.Message}";
                    return View();
                }
            }

            if (string.IsNullOrEmpty(userPassword))
            {
                ViewBag.Message = "El correo electrónico ingresado no está registrado.";
                return View();
            }

            try
            {
                // Configura los detalles del correo electrónico
                var fromAddress = new MailAddress("notificaciones@sinvello.com", "Sin Vello");
                var toAddress = new MailAddress(email);
                const string fromPassword = "TiyPhgLK"; // Contraseña de tu cuenta de correo
                const string subject = "Recuperación de Contraseña";
                string body = $"Hola {userName},\n\nTu contraseña es: {userPassword}.\n\n";

                var smtp = new SmtpClient
                {
                    Host = "pro.turbo-smtp.com", 
                    Port = 587,
                    EnableSsl = false,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential("manuelcamargo@identico.com.co", fromPassword)
                };

                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = body
                })
                {
                    smtp.Send(message);
                }

                ViewBag.Message = "Tu contraseña ha sido enviada a tu correo electrónico.";
            }
            catch (Exception ex)
            {
                ViewBag.Message = $"Ocurrió un error al enviar el correo: {ex.Message}";
            }

            return View();
        }
    }
}
