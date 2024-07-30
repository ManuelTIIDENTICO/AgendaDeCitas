using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace AgendaDeCitas.Controllers
{
    public class confirmarController : Controller
    {
        private readonly HttpClient _httpClient;

        public confirmarController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [HttpGet]
        public IActionResult SendRequest()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SendRequest(string appId, string clientToken)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.flowww.ws/app.asp?fwa_sid=4a53abac9c24a923fa8ef0012c9f2cae");
     

                var collection = new List<KeyValuePair<string, string>>
                {
                    new("Cmd", "c4002"),
                    new("SystemKey", "sinvello"),
                    new("Locale", "ES"),
                    new("AppId", appId),
                    new("IsConfirmed", "-1"),
                    new("ClientToken", clientToken)
                };

                var content = new FormUrlEncodedContent(collection);
                request.Content = content;
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                ViewBag.Response = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(responseContent), Formatting.Indented);
                return View("Success");
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

        public IActionResult Success()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
