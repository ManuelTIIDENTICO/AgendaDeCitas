using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace AgendaDeCitas.Controllers
{
    public class CalendarController : Controller
    {
        private readonly HttpClient _httpClient;

        public CalendarController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [HttpGet]
        public async Task<IActionResult> SelectDate(string clinicID, string serviceIDs)
        {
            ViewData["ClinicID"] = clinicID;
            ViewData["ServiceIDs"] = serviceIDs;

            // Hacer la solicitud para obtener el nombre y la dirección de la clínica

            string xmlFilePath = "./Auth.xml";

            // Lee y parsea el archivo XML
            XDocument xmlDoc = XDocument.Load(xmlFilePath);

            // Extrae los valores de fwa_sid y SystemKey
            string fwaSid = xmlDoc.Root.Element("fwa_sid")?.Value;
            string systemKey = xmlDoc.Root.Element("SystemKey")?.Value;



            string url = $"https://api.flowww.ws/app.asp?fwa_sid={fwaSid}";

            // Crea la solicitud HTTP
            var clinicRequest = new HttpRequestMessage(HttpMethod.Post, url);
                  
            var clinicCollection = new List<KeyValuePair<string, string>>
            {
                new("Cmd", "c1031"),
                new("SystemKey",systemKey),
                new("Locale", "ES"),
                new("ClinicID", clinicID)
            };
            var clinicContent = new FormUrlEncodedContent(clinicCollection);
            clinicRequest.Content = clinicContent;
            var clinicResponse = await _httpClient.SendAsync(clinicRequest);
            clinicResponse.EnsureSuccessStatusCode();
            var clinicResponseBody = await clinicResponse.Content.ReadAsStringAsync();

            var clinicObject = JObject.Parse(clinicResponseBody);
            var clinicName = clinicObject["Clinic"]?["ClinicCommercialName"]?.ToString();
            var clinicAddress = clinicObject["Clinic"]?["ClinicAddress1"]?.ToString();
            var cliniccity = clinicObject["Clinic"]?["ClinicCity"]?.ToString();


            ViewData["ClinicName"] = clinicName;
            ViewData["ClinicAddress"] = clinicAddress;
            ViewData["ClinicCity"] = cliniccity;

            

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SelectDate(DateTime selectedDate, string clinicId, string serviceIDs)
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
                new("Cmd", "c1042"),
                new("SystemKey", systemKey),
                new("Locale", "ES"),
                new("ClinicID", clinicId),
                new("DiaryDate", selectedDate.ToString("yyyy-MM-dd")),
                new("Services", serviceIDs)
            };

            var content = new FormUrlEncodedContent(collection);
            request.Content = content;
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();

            var responseObject = JObject.Parse(responseBody);
            var diaryOptions = responseObject["DiaryOptions"];
            Console.WriteLine("clinic " + clinicId + "services " + serviceIDs + "DiaryDate" + selectedDate.ToString("yyyy-MM-dd"));

            if (responseObject["Result"]?["ErrorDescription"]?.ToString() == "Es necesario pedir cita con 1 horas de antelación.")
            {
                return Json(new { error = responseObject["Result"]["ErrorDescription"].ToString() });
            }

            var result = new
            {
                diaryOptions = diaryOptions.Select(option => new
                {
                    timeStart = (string)option["TimeStart"],
                    timeEnd = (string)option["TimeEnd"]
                }).ToList()
            };

            return Json(result);
        }
    }
}
