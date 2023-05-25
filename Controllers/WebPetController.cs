﻿using InnoGotchi_backend.Models.DTOs;
using InnoGotchi_backend.Models.Entity;
using InnoGotchi_frontend.Services.Abstract;
using InnoGotchi_frontend.Validation;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace InnoGotchi_frontend.Controllers
{
    [Route("pet")]
    public class WebPetController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenService _tokenService;

        private static PetDto _pet;

        public WebPetController(IHttpClientFactory httpClient,ITokenService tokenSevice)
        {
            _httpClient = httpClient.CreateClient("Client");
            _tokenService = tokenSevice;

            if(_pet == null)
                _pet = new PetDto(); 
        }

        [Route("current-pet/{petName}")]
        public async Task<ActionResult> GetCurrentPetView(string petName)
        {
            //refresh token
            if (!_tokenService.IsTokenValid(context: HttpContext)) { _tokenService.AddTokenToCookie(await _tokenService.RefreshTokenAsync(HttpContext), HttpContext, "token", 1); }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Request.Cookies["token"]);

            HttpResponseMessage response = await _httpClient.GetAsync($"api/pet/current-pet/{petName}");

            if (!response.IsSuccessStatusCode)
            {
                CustomExeption? errorMessage = JsonSerializer.Deserialize<CustomExeption>(response.Content.ReadAsStringAsync().Result);

                ViewBag.Message = errorMessage.Message;

                return View("GetPetListPage");
            }

            return View("CurrentPetOverview", JsonSerializer.Deserialize<PetDto>(response.Content.ReadAsStringAsync().Result));
        }

        [Route("feed-current-pet/{petName}")]
        public async Task<IActionResult> FeedCurrentPet(string petName)
        {
            //refresh token
            if (!_tokenService.IsTokenValid(context: HttpContext)) { _tokenService.AddTokenToCookie(await _tokenService.RefreshTokenAsync(HttpContext), HttpContext, "token", 1); }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Request.Cookies["token"]);

            HttpResponseMessage response = await _httpClient.GetAsync($"api/pet/current-pet/{petName}");

            PetDto? pet = JsonSerializer.Deserialize<PetDto>(response.Content.ReadAsStringAsync().Result);

            JsonContent content = JsonContent.Create(pet);

            response = await _httpClient.PatchAsync("api/pet/feed-current-pet", content);

            if (!response.IsSuccessStatusCode)
            {
                CustomExeption? errorMessage = JsonSerializer.Deserialize<CustomExeption>(response.Content.ReadAsStringAsync().Result);

                ViewBag.Message = errorMessage.Message;

                return RedirectToAction("pet-list", "pet");
            }

            return RedirectToAction("pet-list", "pet");
        }

        [Route("give-drink/{petName}")]
        public async Task<IActionResult> GiveDrink(string petName)
        {
            //refresh token
            if (!_tokenService.IsTokenValid(context: HttpContext)) { _tokenService.AddTokenToCookie(await _tokenService.RefreshTokenAsync(HttpContext), HttpContext, "token", 1); }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Request.Cookies["token"]);

            HttpResponseMessage response = await _httpClient.GetAsync($"api/pet/current-pet/{petName}");

            PetDto? pet = JsonSerializer.Deserialize<PetDto>(response.Content.ReadAsStringAsync().Result);

            JsonContent content = JsonContent.Create(pet);

            response = await _httpClient.PatchAsync("api/pet/give-drink", content);

            if (!response.IsSuccessStatusCode)
            {
                CustomExeption? errorMessage = JsonSerializer.Deserialize<CustomExeption>(response.Content.ReadAsStringAsync().Result);

                ViewBag.Message = errorMessage.Message;

                return RedirectToAction("pet-list", "pet");
            }

            return RedirectToAction("pet-list", "pet");
        }

        [Route("constractor")]
        public IActionResult PetConstrator()
        {
            return View();
        }

        [Route("pet-overview")]
        public IActionResult GetPetOverview(PetDto pet)
        {
            return View("PetOverview",pet);
        }

        [Route("pet-list")]
        public async Task<IActionResult> GetPetListPage()
        {
            //refresh token
            if (!_tokenService.IsTokenValid(context: HttpContext)) { _tokenService.AddTokenToCookie(await _tokenService.RefreshTokenAsync(HttpContext), HttpContext, "token", 1); }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Request.Cookies["token"]);

            HttpResponseMessage response = await _httpClient.GetAsync("api/pet/all-pets");

            if (!response.IsSuccessStatusCode)
            {
                CustomExeption? errorMessage = JsonSerializer.Deserialize<CustomExeption>(response.Content.ReadAsStringAsync().Result);

                ViewBag.Message = errorMessage.Message;

                return View("FarmInfo","farm");
            }

            List<PetDto>? pets = JsonSerializer.Deserialize<List<PetDto>>(response.Content.ReadAsStringAsync().Result);

            return View("GetPetListPage",pets);
        }

        public IActionResult CheckRadio(IFormCollection form)
        {
            _pet.Body = form["body"].ToString();
            _pet.Nose = form["nose"].ToString();
            _pet.Eyes = form["eye"].ToString();
            _pet.Mouth = form["mouth"].ToString();

            return RedirectToAction("pet-overview","pet",_pet);
        }

        [Route("new-pet")]
        public async Task<IActionResult> CreateNewPet(PetDto dto)
        {
            PetValidator validator = new PetValidator();

            if (!validator.Validate(dto).IsValid)
            {
                return View("PetOverview", _pet);
            }

            //refresh token
            if (!_tokenService.IsTokenValid(context: HttpContext)) { _tokenService.AddTokenToCookie(await _tokenService.RefreshTokenAsync(HttpContext), HttpContext, "token", 1); }

            _pet.PetName = dto.PetName;

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Request.Cookies["token"]);

            JsonContent content = JsonContent.Create(_pet);

            HttpResponseMessage response = await _httpClient.PostAsync($"api/pet/new-pet", content);

            if (!response.IsSuccessStatusCode)
            {
                CustomExeption? errorMessage = JsonSerializer.Deserialize<CustomExeption>(response.Content.ReadAsStringAsync().Result);

                ViewBag.Message = errorMessage.Message;

                return View("PetOverview", _pet);
            }

            return RedirectToAction("my-own-farm", "farm");
        }
    }
}
