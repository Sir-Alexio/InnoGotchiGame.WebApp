﻿using InnoGotchi_backend.Models.Dto;
using InnoGotchi_backend.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace InnoGotchi_frontend.Controllers
{
    [Route("user")]
    public class UserController : Controller
    {
        private readonly HttpClient _httpClient;
        public UserController(IHttpClientFactory factory)
        {
            _httpClient = factory.CreateClient("Client");
        }

        [Route("all-users")]
        public async Task<IActionResult> GetAllUsers()
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Request.Cookies["token"]);

            HttpResponseMessage response = await _httpClient.GetAsync("api/user/all-users");

            if (!response.IsSuccessStatusCode)
            {
                return BadRequest();
            }

            List<UserDto>? users = JsonSerializer.Deserialize<List<UserDto>>(response.Content.ReadAsStringAsync().Result);

            return View("AllUsers", users);
        }
    }
}