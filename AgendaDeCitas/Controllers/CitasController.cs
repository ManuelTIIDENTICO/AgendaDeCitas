using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AgendaDeCitas.Models;
using System.Collections.Generic;
using System;
using System.Text;
using System.Xml;
using System.IO;

namespace AgendaDeCitas.Controllers
{
    public class CitasController : Controller
    {
        private readonly HttpClient _httpClient;

        public CitasController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [HttpGet]
        public IActionResult Crear()
        {
            string dateString = Request.Query["date"];
            DateTime date;

            if (!string.IsNullOrEmpty(dateString) && DateTime.TryParse(dateString, out DateTime parsedDate))
            {
                date = parsedDate;
            }
            else
            {
                date = DateTime.Now;
            }

            string horaString = Request.Query["time"];
            TimeSpan hora;

            if (!string.IsNullOrEmpty(horaString) && TimeSpan.TryParse(horaString, out hora))
            {
                hora = hora.Subtract(TimeSpan.FromMinutes(30));
            }
            else
            {
                hora = TimeSpan.Zero;
            }

            TimeSpan time;
            if (!string.IsNullOrEmpty(horaString) && TimeSpan.TryParse(horaString, out TimeSpan parsedTime))
            {
                time = parsedTime;
            }
            else
            {
                time = TimeSpan.Zero;
            }

            var cita = new Cita
            {
                ClinicID = "1",
                DiaryDate = date,
                TimeEnd = time,
                TimeStart = hora
            };

            return View(cita);
        }

        [HttpPost]
        public async Task<string> GetClientToken()
        {
            string xmlContent;

            var filePath = Path.GetFullPath("./auth.xml");

            try
            {
                using (var reader = new StreamReader(filePath))
                {
                    xmlContent = await reader.ReadToEndAsync();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error al leer el archivo XML: " + e.Message);
                throw;
            }

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.flowww.ws/app.asp?fwa_sid=4a53abac9c24a923fa8ef0012c9f2cae");
            request.Content = new StringContent(xmlContent, Encoding.UTF8, "application/xml");

            try
            {
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();

                var xmlDoc = new XmlDocument();
                responseContent = responseContent.Replace("xmlns=", "ignore=");
                try
                {
                    xmlDoc.LoadXml(responseContent);
                }
                catch (XmlException e)
                {
                    Console.WriteLine("Error al analizar el XML: " + e.Message);
                    throw;
                }

                var tokenNode = xmlDoc.SelectSingleNode("//ClientToken");
                if (tokenNode != null)
                {
                    return tokenNode.InnerText;
                }
                else
                {
                    Console.WriteLine("ClientToken no encontrado en la respuesta.");
                    throw new Exception("ClientToken no encontrado en la respuesta.");
                }
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("Error al realizar la solicitud HTTP: " + e.Message);
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
                throw;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Crear(Cita cita)
        {
            if (ModelState.IsValid)
            {
                var clientToken = await GetClientToken();

                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.flowww.ws/app.asp?fwa_sid=4a53abac9c24a923fa8ef0012c9f2cae");
                var collection = new List<KeyValuePair<string, string>>
                {
                    new("Cmd", "c1043"),
                    new("SystemKey", "sinvello"),
                    new("Locale", "ES"),
                    new("ClinicID", cita.ClinicID),
                    new("DiaryDate", cita.DiaryDate.ToString("yyyy-MM-dd")),
                    new("TimeStart", cita.TimeStart.ToString(@"hh\:mm")),
                    new("TimeEnd", cita.TimeEnd.ToString(@"hh\:mm")),
                    new("CabineID", cita.CabineID.ToString()),
                    new("Services", cita.Services),
                    new("DiaryComments", cita.DiaryComments),
                    new("ClientToken", clientToken)
                };

                var content = new FormUrlEncodedContent(collection);
                request.Content = content;

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (responseContent.Contains("Success"))
                    {
                        ViewBag.Message = "La cita se ha creado correctamente.";
                        return View("Success");
                    }
                    else
                    {
                        ViewBag.Message = "Hubo un problema al crear la cita: " + responseContent;
                        return View("Error");
                    }
                }
                else
                {
                    ViewBag.Message = "Error al crear la cita: " + response.ReasonPhrase;
                    return View("Error");
                }
            }

            return View(cita);
        }
    }
}
