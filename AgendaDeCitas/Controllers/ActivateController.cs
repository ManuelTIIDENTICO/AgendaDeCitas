using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace AgendaDeCitas.Controllers
{
    public class ActivateController : Controller
    {
        private readonly string _connectionString;

        public ActivateController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpGet("/Activate/Account/{hash?}")]
        public ActionResult Account(string hash)
        {
            if (string.IsNullOrEmpty(hash))
            {
                ViewBag.ErrorMessage = "El enlace de activación no es válido o está vacío.";
                return View("Error");
            }

            // Cortar el hash en el primer '&' si existe
            int ampersandIndex = hash.IndexOf('&');
            if (ampersandIndex != -1)
            {
                hash = hash.Substring(0, ampersandIndex);
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    string query = @"SELECT [usr_email], [usr_nombres], [usr_apellidos], [usr_estado], [usr_link]
                                     FROM [Sinvello].[dbo].[Users]
                                     WHERE usr_link = @hash AND usr_estado = 0";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@hash", hash);

                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.HasRows && reader.Read())
                    {
                        // Guardar datos en variables de sesión
                        HttpContext.Session.SetString("Email", reader["usr_email"].ToString());
                        HttpContext.Session.SetString("Nombres", reader["usr_nombres"].ToString());
                        HttpContext.Session.SetString("Apellidos", reader["usr_apellidos"].ToString());

                        // Cambiar el estado del usuario a 1 (verificado)
                        string updateQuery = @"UPDATE [Sinvello].[dbo].[Users] SET usr_estado = 1 WHERE usr_link = @hash";
                        SqlCommand updateCmd = new SqlCommand(updateQuery, conn);
                        updateCmd.Parameters.AddWithValue("@hash", hash);
                        updateCmd.ExecuteNonQuery();

                        // Pasar datos a la vista
                        ViewBag.UserName = $"{reader["usr_nombres"]} {reader["usr_apellidos"]}";
                        return View("AccountVerified"); // Vista de cuenta verificada
                    }
                    else
                    {
                        return View("AlreadyValidated"); // Vista de enlace ya validado o no encontrado
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                ViewBag.ErrorMessage = $"Error de base de datos: {sqlEx.Message}";
                return View("Error");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Ocurrió un error: {ex.Message}";
                return View("Error");
            }
        }
    }
}
