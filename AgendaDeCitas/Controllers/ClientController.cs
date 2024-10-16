using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AgendaDeCitas.Models;
using Newtonsoft.Json.Linq;
using Microsoft.Data.SqlClient;
using System.Xml.Linq;
using System.Configuration;

namespace AgendaDeCitas.Controllers
{
    public class ClientController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _connectionString;

        public ClientController(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
      



        [HttpPost]
        public async Task<IActionResult> UpdateUserDetails(string nombres, string telefono)
        {
            var email = HttpContext.Session.GetString("Email");
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("No email found in session.");
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    string query = "UPDATE [Sinvello].[dbo].[Users] " +
                                   "SET [usr_nombres] = @Nombres, [usr_telefono] = @Telefono " +
                                   "WHERE [usr_email] = @Email";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Nombres", nombres);
                        cmd.Parameters.AddWithValue("@Telefono", telefono);
                        cmd.Parameters.AddWithValue("@Email", email);

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            return Ok("User details updated successfully.");
                        }
                        else
                        {
                            return NotFound("User not found.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }



        [HttpGet]
        public IActionResult Create()
        {
            var query = HttpContext.Request.Query;
            SetSessionValue("clinicid", query["clinicID"].ToString());
            SetSessionValue("serviceID", query["serviceID"].ToString());
            SetSessionValue("date", query["date"].ToString());
            SetSessionValue("timeStart", query["timeStart"].ToString());
            SetSessionValue("timeEnd", query["timeEnd"].ToString());
            var clinicAddress = _httpContextAccessor.HttpContext.Session.GetString("ClinicAddress");
            ViewBag.ClinicAddress = clinicAddress;
            ViewBag.Nombres = HttpContext.Session.GetString("Nombres");
            ViewBag.Apellidos = HttpContext.Session.GetString("Apellidos");
            ViewBag.Email = HttpContext.Session.GetString("Email");
            ViewBag.Telefono = HttpContext.Session.GetString("Telefono");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(ClientViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {

                    string nombre = model.Nombres; // Asumiendo que 'Nombres' contiene el nombre completo
                    string telefono = model.ClientPhone1; // Asumiendo que 'ClientPhone1' contiene el teléfono

                    // Llamar a la función para actualizar los detalles del usuario
                    await UpdateUserDetails(nombre, telefono);

                    // Paso 1: Obtener el token del cliente
                    var clientTokenFromApi = await GetClientTokenAsync(model);

                    // Paso 2: Crear la cita con el token obtenido
                    var appointmentResponse = await CreateAppointmentAsync(clientTokenFromApi);

                    if (appointmentResponse != null && appointmentResponse.Result.ErrorNumber == "0")
                    {
                        // Paso 3: Guardar el token del cliente en sesión
                        _httpContextAccessor.HttpContext.Session.SetString("ClientToken", clientTokenFromApi);

                        // Redireccionar a la URL ./flowww/getFlowwwData si la creación fue exitosa
                        return Redirect("../flowww/getFlowwwData");
                    }
                    else
                    {
                        // Mostrar mensaje de error si la creación de la cita falló
                        ViewBag.ErrorMessage = appointmentResponse?.Result?.ErrorDescription ?? "Error al crear la cita.";
                        return View("Error");
                    }
                }
                catch (HttpRequestException ex)
                {
                    ViewBag.ErrorMessage = "Error al conectar con el servidor: " + ex.Message;
                    return View("Error");
                }
                catch (Exception ex)
                {
                    ViewBag.ErrorMessage = "Error inesperado: " + ex.Message;
                    return View("Error");
                }
            }

            return View(model);
        }

        public IActionResult Success()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }

        private async Task<string> GetClientTokenAsync(ClientViewModel model)
         {

            string xmlFilePath = "./Auth.xml";

            // Lee y parsea el archivo XML
            XDocument xmlDoc = XDocument.Load(xmlFilePath);

            // Extrae los valores de fwa_sid y SystemKey
            string fwaSid = xmlDoc.Root.Element("fwa_sid")?.Value;
            string systemKey = xmlDoc.Root.Element("SystemKey")?.Value;



            string url = $"https://api.flowww.ws/app.asp?fwa_sid={fwaSid}";

            // Crea la solicitud HTTP
            var request = new HttpRequestMessage(HttpMethod.Post, url);
           

            var collection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Cmd", "c1021"),
                new KeyValuePair<string, string>("SystemKey", systemKey),
                new KeyValuePair<string, string>("Locale", "ES"),
                new KeyValuePair<string, string>("ClinicID", _httpContextAccessor.HttpContext.Session.GetString("clinicid")),
                new KeyValuePair<string, string>("ClientName", model.Nombres),
                new KeyValuePair<string, string>("ClientSurname1", model.Primer_Apellido),
                new KeyValuePair<string, string>("ClientSurname2", model.ClientSurname2),
                new KeyValuePair<string, string>("ClientPhone1", model.ClientPhone1),
                new KeyValuePair<string, string>("ClientEmail", model.ClientEmail)
            };

            request.Content = new FormUrlEncodedContent(collection);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseContent);
            Console.WriteLine("EL TOKEN " + apiResponse?.ClientToken);
            return apiResponse?.ClientToken;
        }

        private async Task<RootObject> CreateAppointmentAsync(string clientToken)
        {
            string xmlFilePath = "./Auth.xml";

            // Lee y parsea el archivo XML
            XDocument xmlDoc = XDocument.Load(xmlFilePath);

            // Extrae los valores de fwa_sid y SystemKey
            string fwaSid = xmlDoc.Root.Element("fwa_sid")?.Value;
            string systemKey = xmlDoc.Root.Element("SystemKey")?.Value;



            string url = $"https://api.flowww.ws/app.asp?fwa_sid={fwaSid}";

            // Crea la solicitud HTTP
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            var collection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Cmd", "c1043"),
                new KeyValuePair<string, string>("SystemKey", systemKey),
                new KeyValuePair<string, string>("Locale", "ES"),
                new KeyValuePair<string, string>("ClinicID", _httpContextAccessor.HttpContext.Session.GetString("clinicid")),
                new KeyValuePair<string, string>("DiaryDate", _httpContextAccessor.HttpContext.Session.GetString("date")),
                new KeyValuePair<string, string>("TimeStart", _httpContextAccessor.HttpContext.Session.GetString("timeStart")),
                new KeyValuePair<string, string>("TimeEnd", _httpContextAccessor.HttpContext.Session.GetString("timeEnd")),
                new KeyValuePair<string, string>("CabineID", "1"),
                new KeyValuePair<string, string>("Services", _httpContextAccessor.HttpContext.Session.GetString("serviceID")),
                new KeyValuePair<string, string>("ClientToken", clientToken)
            };

            request.Content = new FormUrlEncodedContent(collection);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var rootObject = JsonSerializer.Deserialize<RootObject>(responseContent);
            var appId = rootObject?.AppID.ToString();
            var clientTMP = rootObject?.ClientTMP;

            SetSessionValue("AppID", appId);
            SetSessionValue("ClientTMP", clientTMP);


            var request2 = new HttpRequestMessage(HttpMethod.Post, url);

           
            var collection1 = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Cmd", "c4002"),
                new KeyValuePair<string, string>("SystemKey",systemKey),
                new KeyValuePair<string, string>("Locale", "ES"),
                new KeyValuePair<string, string>("AppId", appId),
                new KeyValuePair<string, string>("IsConfirmed", clientTMP),
                new KeyValuePair<string, string>("ClientToken", clientToken)
            };

            var content2 = new FormUrlEncodedContent(collection1);
            request2.Content = content2;
            var response2 = await _httpClient.SendAsync(request2);
            response2.EnsureSuccessStatusCode();

            var responseContent2 = await response2.Content.ReadAsStringAsync();
            Console.WriteLine(responseContent2);

            Console.WriteLine("Response 2" + appId + " - " + clientTMP);

            return rootObject;
        }

        private void SetSessionValue(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _httpContextAccessor.HttpContext.Session.SetString(key, value);
            }
        }

        public class ApiResponse
        {
            public Result Result { get; set; }
            public int ClientID { get; set; }
            public string ClinicID { get; set; }
            public string ClientToken { get; set; }
            public Resources Resources { get; set; }
        }

        public class Result
        {
            public string ErrorNumber { get; set; }
            public string ErrorDescription { get; set; }
        }

        public class Resources
        {
            public string RegionAPIUrl { get; set; }
        }

        public class RootObject
        {
            public Result Result { get; set; }
            public int AppID { get; set; }
            public string ClientTMP { get; set; }
            public Resources Resources { get; set; }
        }
    }
}
