using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using AgendaDeCitas.Models;

namespace AgendaDeCitas.Controllers
{
    public class ApiResponse
    {
        [JsonProperty("AppID")]
        public string AppID { get; set; }

        [JsonProperty("ClientTMP")]
        public string ClientTMP { get; set; }

        // Agrega otras propiedades si es necesario
    }

    public class Result
    {
        public string ErrorNumber { get; set; }
        public string ErrorDescription { get; set; }
    }

    public class RootObject
    {
        public Result Result { get; set; }
        public int AppID { get; set; }
        public string ClientTMP { get; set; }
        public Resources Resources { get; set; }
    }

    public class Resources
    {
        public string RegionAPIUrl { get; set; }
    }

    public class AppointmentController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AppointmentController(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
        }

        public IActionResult Index(string clinicid, string serviceID, string date, string timeStart, string timeEnd)
        {
            SetSessionValue("clinicid", clinicid);
            SetSessionValue("serviceID", serviceID);
            SetSessionValue("date", date);
            SetSessionValue("timeStart", timeStart);
            SetSessionValue("timeEnd", timeEnd);

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateAppointment()
        {
            var viewModel = new AppointmentViewModel();

            try
            {
                var clientToken = _httpContextAccessor.HttpContext.Session.GetString("ClientToken");
                var clinicId = _httpContextAccessor.HttpContext.Session.GetString("clinicid");
                var servicesId = _httpContextAccessor.HttpContext.Session.GetString("serviceID");
                var fecha = _httpContextAccessor.HttpContext.Session.GetString("date");
                var tiempoInicio = _httpContextAccessor.HttpContext.Session.GetString("timeStart");
                var tiempoFin = _httpContextAccessor.HttpContext.Session.GetString("timeEnd");

                if (string.IsNullOrEmpty(clientToken) || string.IsNullOrEmpty(clinicId) ||
                    string.IsNullOrEmpty(servicesId) || string.IsNullOrEmpty(fecha) ||
                    string.IsNullOrEmpty(tiempoInicio) || string.IsNullOrEmpty(tiempoFin))
                {
                    viewModel.ErrorMessage = "Faltan datos necesarios para crear la cita.";
                    return View("Error", viewModel);
                }

                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.flowww.ws/app.asp?fwa_sid=4a53abac9c24a923fa8ef0012c9f2cae");
                var collection = new List<KeyValuePair<string, string>>
                {
                    new("Cmd", "c1043"),
                    new("SystemKey", "sinvello"),
                    new("Locale", "ES"),
                    new("ClinicID", clinicId),
                    new("DiaryDate", fecha),
                    new("TimeStart", tiempoInicio),
                    new("TimeEnd", tiempoFin),
                    new("CabineID", "1"),
                    new("Services", servicesId),
                    new("ClientToken", clientToken)
                };

                var content = new FormUrlEncodedContent(collection);
                request.Content = content;
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var rootObject = JsonConvert.DeserializeObject<RootObject>(responseContent);
                var errorDescription = rootObject?.Result?.ErrorDescription;
                Console.WriteLine(responseContent);

                
                viewModel.ResponseContent = rootObject?.Result?.ErrorDescription;

                var appId = rootObject?.AppID.ToString();
                var clientTMP = rootObject?.ClientTMP;

                SetSessionValue("AppID", appId);
                SetSessionValue("ClientTMP", clientTMP);

                var request2 = new HttpRequestMessage(HttpMethod.Post, "https://api.flowww.ws/app.asp?fwa_sid=4a53abac9c24a923fa8ef0012c9f2cae");
                var collection1 = new List<KeyValuePair<string, string>>
                {
                    new("Cmd", "c4002"),
                    new("SystemKey", "sinvello"),
                    new("Locale", "ES"),
                    new("AppId", appId),
                    new("IsConfirmed", clientTMP),
                    new("ClientToken", clientToken)
                };

                var content2 = new FormUrlEncodedContent(collection1);
                request2.Content = content2;
                var response2 = await _httpClient.SendAsync(request2);
                response2.EnsureSuccessStatusCode();

                var responseContent2 = await response2.Content.ReadAsStringAsync();
                Console.WriteLine(responseContent2);
                //                viewModel.ResponseContent = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(responseContent2), Formatting.Indented);
                return View("CreateAppointment", viewModel);
            }
            catch (HttpRequestException ex)
            {
                viewModel.ErrorMessage = "Error al conectar con el servidor: " + ex.Message;
                return View("Error", viewModel);
            }
            catch (Exception ex)
            {
                viewModel.ErrorMessage = "Error inesperado: " + ex.Message;
                return View("Error", viewModel);
            }
        }
        
        private void SetSessionValue(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _httpContextAccessor.HttpContext.Session.SetString(key, value);
            }
        }
    }
}
