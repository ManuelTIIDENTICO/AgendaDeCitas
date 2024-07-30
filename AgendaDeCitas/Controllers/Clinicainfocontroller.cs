using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AgendaDeCitas.Controllers
{
    public class ClinicInfo
    {
        [JsonProperty("ClinicCommercialName")]
        public string ClinicCommercialName { get; set; }

        [JsonProperty("ClinicAddress1")]
        public string ClinicAddress1 { get; set; }

        [JsonProperty("ClinicCity")]
        public string ClinicCity { get; set; }
    }

    public class ClinicaInfoController : Controller
    {
        private readonly HttpClient _httpClient;

        public ClinicaInfoController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IActionResult> Index(string clinicId)
        {
            if (string.IsNullOrEmpty(clinicId))
            {
                ViewBag.ErrorMessage = "El ID de la clínica no puede estar vacío.";
                return View("Error");
            }

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.flowww.ws/app.asp?fwa_sid=4a53abac9c24a923fa8ef0012c9f2cae");
                var collection = new List<KeyValuePair<string, string>>
                {
                    new("Cmd", "c1031"),
                    new("SystemKey", "sinvello"),
                    new("Locale", "ES"),
                    new("ClinicID", clinicId)
                };

                var content = new FormUrlEncodedContent(collection);
                request.Content = content;

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JObject.Parse(responseContent);
                var clinicInfo = jsonResponse["Clinic"].ToObject<ClinicInfo>();

                ViewBag.Response = JsonConvert.SerializeObject(clinicInfo);
                TempData["ClinicID"] = clinicId;

                return View();
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
    }
}
