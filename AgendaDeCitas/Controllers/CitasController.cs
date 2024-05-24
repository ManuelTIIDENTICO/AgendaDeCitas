using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AgendaDeCitas.Models;
using System.Collections.Generic;
using System;

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
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Crear(Cita cita)
        {
            if (ModelState.IsValid)
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.flowww.ws/app.asp?fwa_sid=1RAxyrJHyELT0RaTXd4HG7jUIHDnWFiaWza2s5SNzcj4");
                var collection = new List<KeyValuePair<string, string>>
                {
                    new("Cmd", "c1043"),
                    new("SystemKey", "SYSTEM_KEY"),
                    new("Locale", "ES"),
                    new("ClinicID", cita.ClinicID),
                    new("DiaryDate", cita.DiaryDate.ToString("yyyy-MM-dd")),
                    new("TimeStart", cita.TimeStart.ToString(@"hh\:mm")),
                    new("TimeEnd", cita.TimeEnd.ToString(@"hh\:mm")),
                    new("CabineID", cita.CabineID.ToString()),
                    new("ProfessionalID", cita.ProfessionalID.ToString()),
                    new("Services", cita.Services),
                    new("DiaryComments", cita.DiaryComments),
                    new("ClientToken", cita.ClientToken)
                };
                
                var content = new FormUrlEncodedContent(collection);
                request.Content = content;

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                ViewBag.Response = responseContent;
                return View("Success");
            }

            return View(cita);
        }
    }
}

