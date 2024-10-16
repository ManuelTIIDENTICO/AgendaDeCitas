using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using AgendaDeCitas.Models;
using Microsoft.AspNetCore.Http;
using System.Net.Mail;
using System.Net;
using System;
using System.Security.Cryptography;
using System.Text;

namespace AgendaDeCitas.Controllers
{
    public class UserController : Controller
    {
        private readonly string _connectionString;

        public UserController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(User user)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Message = "Por favor, completa todos los campos requeridos.";
                return View(user);
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string checkUserQuery = "SELECT usr_estado, usr_nombres , usr_link FROM Users WHERE usr_email = @Email";
                    SqlCommand checkUserCommand = new SqlCommand(checkUserQuery, connection);
                    checkUserCommand.Parameters.AddWithValue("@Email", user.Email);

                    connection.Open();
                    SqlDataReader reader = await checkUserCommand.ExecuteReaderAsync();

                    if (await reader.ReadAsync())
                    {
                        int usrEstado = reader.GetInt32(0);
                        string nombres = reader.GetString(1);
                        string hash = reader.GetString(2);

                        if (usrEstado == 0)
                        {
                            // Enviar correo de confirmación
                            await EnviarCorreoDeConfirmacionAsync(user.Email, nombres, hash);
                            ViewBag.Message = "El correo electrónico ya está registrado y no ha sido confirmado. Hemos enviado nuevamente el enlace de confirmación.";
                        }
                        else if (usrEstado == 1)
                        {
                            ViewBag.Message = "El correo electrónico ya está registrado.";
                        }

                        await reader.CloseAsync();
                        connection.Close();
                        return View(user);
                    }

                    await reader.CloseAsync();
                    connection.Close();


                    // Insertar nuevo usuario
                    string insertQuery = "INSERT INTO Users (usr_email, usr_pass, usr_nombres, usr_apellidos, usr_link) " +
                                         "VALUES (@Email, @Password, @Nombres, @Apellidos, @link)";

                    string has = GenerateHash(user.Email);

                    SqlCommand insertCommand = new SqlCommand(insertQuery, connection);
                    insertCommand.Parameters.AddWithValue("@Email", user.Email);
                    insertCommand.Parameters.AddWithValue("@Password", user.Password);
                    insertCommand.Parameters.AddWithValue("@Nombres", user.Nombres);
                    insertCommand.Parameters.AddWithValue("@Apellidos", user.Apellidos);
                    insertCommand.Parameters.AddWithValue("@link", has);

                    connection.Open();
                    await insertCommand.ExecuteNonQueryAsync();
                    connection.Close();

                    // Guardar datos en la sesión
                    HttpContext.Session.SetString("Nombres", user.Nombres);
                    HttpContext.Session.SetString("Apellidos", user.Apellidos);
                    HttpContext.Session.SetString("Email", user.Email);

                    // Enviar correo de confirmación
                    await EnviarCorreoDeConfirmacionAsync(user.Email, user.Nombres, has);

                    // Redirigir a la página de confirmación
                    return RedirectToAction("ConfirmRegistration");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = $"Error al registrar el usuario: {ex.Message}";
                return View(user);
            }
        }


        public static string GenerateHash(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                // Convert the input string to a byte array and compute the hash.
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));

                // Convert the byte array to a string.
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private async Task EnviarCorreoDeConfirmacionAsync(string email, string nombres , string hash)
        {
            try
            {
                
                var fromAddress = new MailAddress("notificaciones@sinvello.com", "Sin Vello");
                var toAddress = new MailAddress(email, nombres);
                const string fromPassword = "TiyPhgLK";
                const string subject = "Confirmación de Registro";
                string body = $@"
                    <h1>Hola {nombres},</h1>
                    <p>Gracias por registrarte en Sin vello.</p>
                    <p>Nos complace darte la bienvenida a nuestra comunidad. Si tienes alguna pregunta, no dudes en contactarnos.</p>
                    <p><a href=""http://181.48.173.70:8088/activate/account/{hash}"" target=""_blank"">Activar cuenta</a></p>
                    <br>
                    <p>Saludos cordiales,<br>El equipo de Sin Vello</p>";

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
                    Body = body,
                    IsBodyHtml = true
                })
                {
                    await smtp.SendMailAsync(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al enviar el correo: " + ex.Message);
            }
        }

        public IActionResult ConfirmRegistration()
        {
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("Nombres")) &&
                !string.IsNullOrEmpty(HttpContext.Session.GetString("Apellidos")) &&
                !string.IsNullOrEmpty(HttpContext.Session.GetString("Email")))
            {
                ViewBag.Nombres = HttpContext.Session.GetString("Nombres");
                ViewBag.Apellidos = HttpContext.Session.GetString("Apellidos");
                ViewBag.Email = HttpContext.Session.GetString("Email");
                return View();
            }

            return RedirectToAction("Register");
        }

        public IActionResult Ciudades()
        {
            return View();
        }
    }
}
