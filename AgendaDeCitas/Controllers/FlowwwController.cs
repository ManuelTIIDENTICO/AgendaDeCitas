using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using AgendaDeCitas.Models;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;

namespace YourNamespace.Controllers
{
    public class FlowwwController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public FlowwwController(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpGet]
        public async Task<IActionResult> GetFlowwwData()
        {
            try
            {
                var token = _httpContextAccessor.HttpContext.Session.GetString("ClientToken");

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
                    new("Cmd", "c4001"),
                    new("SystemKey", systemKey),
                    new("Locale", "ES"),
                    new("AppLastDays", "30"),
                    new("ClientToken", token)
                };

                var content = new FormUrlEncodedContent(collection);
                request.Content = content;

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var responseData = await response.Content.ReadAsStringAsync();

                var flowwwResponse = JsonConvert.DeserializeObject<FlowwwResponse>(responseData);
                return View(flowwwResponse);
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
