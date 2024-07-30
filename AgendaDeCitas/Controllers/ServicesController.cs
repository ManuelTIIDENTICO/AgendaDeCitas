using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;

namespace AgendaDeCitas.Controllers
{
    public class ServicesController : Controller
    {
        private readonly HttpClient _httpClient;

        public ServicesController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IActionResult> Details(string clinicID)
        {
            if (string.IsNullOrEmpty(clinicID))
            {
                ViewBag.ErrorMessage = "El ID de la clínica no puede estar vacío.";
                return View("Error");
            }


            string xmlFilePath = "./Auth.xml";

            // Lee y parsea el archivo XML
            XDocument xmlDoc = XDocument.Load(xmlFilePath);

            // Extrae los valores de fwa_sid y SystemKey
            string fwaSid = xmlDoc.Root.Element("fwa_sid")?.Value;
            string systemKey = xmlDoc.Root.Element("SystemKey")?.Value;



            string url = $"https://api.flowww.ws/app.asp?fwa_sid={fwaSid}";

            // Crea la solicitud HTTP
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Cmd", "c1031"),
                new KeyValuePair<string, string>("SystemKey", systemKey),
                new KeyValuePair<string, string>("Locale", "ES"),
                new KeyValuePair<string, string>("ClinicID", clinicID)
            });
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            dynamic jsonResponse = JObject.Parse(responseContent);
            string clinicName = jsonResponse.Clinic.ClinicCommercialName;
            string direccion = jsonResponse.Clinic.ClinicAddress1;

            ViewBag.ClinicID = clinicID;
            ViewBag.ClinicName = clinicName;
            ViewBag.direccion = direccion;
            return View();
        }

        [HttpPost]
        public IActionResult NavigateToDetails(string clinicID, string serviceFamilySourceID)
        {
            // Lógica para manejar la navegación a Details con los parámetros recibidos
            return RedirectToAction("Details", "Clinics", new { clinicID, serviceFamilySourceID });
        }
    }
}
