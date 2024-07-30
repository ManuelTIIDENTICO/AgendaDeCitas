using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;

namespace AgendaDeCitas.Controllers
{
    public class CiudadesController : Controller
    {
        public async Task<IActionResult> Index()
        {
            using var client = new HttpClient();

            string xmlFilePath = "./Auth.xml";

            // Lee y parsea el archivo XML
            XDocument xmlDoc = XDocument.Load(xmlFilePath);

            // Extrae los valores de fwa_sid y SystemKey
            string fwaSid = xmlDoc.Root.Element("fwa_sid")?.Value;
            string systemKey = xmlDoc.Root.Element("SystemKey")?.Value;



            string url = $"https://api.flowww.ws/app.asp?fwa_sid={fwaSid}";

            // Crea la solicitud HTTP
            var request = new HttpRequestMessage(HttpMethod.Post, url);

            // Crea el contenido de la solicitud
            var collection = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("Cmd", "c1011"),
            new KeyValuePair<string, string>("SystemKey", systemKey),
            new KeyValuePair<string, string>("Locale", "ES")
        };


            var content = new FormUrlEncodedContent(collection);
            request.Content = content;

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();

            var clinics = new List<ClinicViewModel>();

            if (!string.IsNullOrEmpty(responseContent))
            {
                var jsonResponse = JObject.Parse(responseContent);
                if (jsonResponse["Clinics"] != null)
                {
                    foreach (var clinic in jsonResponse["Clinics"])
                    {
                        var clinicModel = new ClinicViewModel
                        {
                            ClinicID = clinic["ClinicID"].ToString(),
                            ClinicName = clinic["ClinicName"].ToString(),
                            ClinicCity = clinic["ClinicCity"].ToString(),
                            ClinicAppAddress1 = clinic["ClinicAppAddress1"].ToString()
                        };
                        clinics.Add(clinicModel);
                    }
                }
            }

            // Agrupar las clínicas por ciudad
            var groupedClinics = clinics
                .GroupBy(c => c.ClinicCity)
                .Select(g => new ClinicsByCityViewModel
                {
                    ClinicCity = g.Key,
                    Clinics = g.ToList()
                })
                .ToList();

            // Pasar la lista agrupada al modelo para ser utilizada en la vista
            return View(groupedClinics);
        }
    }

    public class ClinicViewModel
    {
        public string ClinicID { get; set; }
        public string ClinicName { get; set; }
        public string ClinicCity { get; set; }
        public string ClinicAppAddress1 { get; set; }
    }

    public class ClinicsByCityViewModel
    {
        public string ClinicCity { get; set; }
        public List<ClinicViewModel> Clinics { get; set; }
    }
}
