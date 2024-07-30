using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;

namespace AgendaDeCitas.Controllers
{
    public class ClinicsController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public ClinicsController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task<IActionResult> Details(string clinicID, string serviceFamilySourceID)
        {
            using var client = _clientFactory.CreateClient();

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
                new KeyValuePair<string, string>("Cmd", "c1041"),
                new KeyValuePair<string, string>("SystemKey", systemKey),
                new KeyValuePair<string, string>("Locale", "ES"),
                new KeyValuePair<string, string>("ClinicID", clinicID),
            };

            var content = new FormUrlEncodedContent(collection);
            request.Content = content;

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();

            var services = new Dictionary<string, string>(); // ServiceDescription -> ServiceID

            if (!string.IsNullOrEmpty(responseContent))
            {
                var jsonResponse = JObject.Parse(responseContent);
                if (jsonResponse["Services"] != null)
                {
                    foreach (var service in jsonResponse["Services"])
                    {
                        var serviceDescription = service["ServiceDescription"].ToString();
                        var serviceFamilyID = service["ServiceFamilySourceID"].ToString();
                        var serviceId = service["ServiceId"].ToString();

                        // Verificar si el serviceFamilySourceID del servicio coincide con el proporcionado
                        if (serviceFamilyID == serviceFamilySourceID)
                        {
                            try
                            {
                                services.Add(serviceDescription, serviceId);
                            }catch (Exception ex)

                            {

                            }
                        }
                    }
                }
            }

            ViewBag.ClinicID = clinicID;
            // Pasar la lista de servicios al modelo para ser utilizada en la vista
            return View(services);
        }
    }
}
