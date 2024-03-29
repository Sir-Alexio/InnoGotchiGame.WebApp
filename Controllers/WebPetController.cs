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
        private readonly ILoggerService _logger;

        private static PetDto _pet;
        private static string _farmName;

        public WebPetController(IHttpClientFactory httpClient,ITokenService tokenSevice, ILoggerService logger)
        {
            _httpClient = httpClient.CreateClient("Client");
            _tokenService = tokenSevice;
            _logger = logger;

            if(_pet == null)
                _pet = new PetDto(); 
        }

        [Route("current-pet/{petName}")]
        public async Task<ActionResult> GetCurrentPetView(string petName)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Request.Cookies["token"]);

            HttpResponseMessage response = await _httpClient.GetAsync($"api/pet/current-pet/{petName}");

            if (!response.IsSuccessStatusCode)
            {
                CustomExeption? errorMessage = JsonSerializer.Deserialize<CustomExeption>(await response.Content.ReadAsStringAsync());

                _logger.LogError(errorMessage.Message);

                ViewBag.Message = errorMessage.Message;

                return View("GetPetListPage");
            }

            return View("CurrentPetOverview", JsonSerializer.Deserialize<PetDto>(await response.Content.ReadAsStringAsync()));
        }

        [Route("feed-current-pet/{petName}")]
        public async Task<IActionResult> FeedMyPet(string petName)
        {
            await FeedPet(petName);
            return RedirectToAction("pet-list", "pet");
        }

        [Route("feed-foreign-pet/{petName}")]
        public async Task<IActionResult> FeedForeignPet(string petName)
        {
            await FeedPet(petName);
            string encodedFarmName = Uri.EscapeDataString(_farmName);
            return Redirect($"/pet/foreign-pet-list/{encodedFarmName}");
        }

        [Route("feed-current-pet/{petName}")]
        private async Task<IActionResult> FeedPet(string petName)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Request.Cookies["token"]);

            HttpResponseMessage response = await _httpClient.GetAsync($"api/pet/current-pet/{petName}");

            PetDto? pet = JsonSerializer.Deserialize<PetDto>(await response.Content.ReadAsStringAsync());

            JsonContent content = JsonContent.Create(pet);

            response = await _httpClient.PatchAsync("api/pet/feed-current-pet", content);

            if (!response.IsSuccessStatusCode)
            {
                CustomExeption? errorMessage = JsonSerializer.Deserialize<CustomExeption>(await response.Content.ReadAsStringAsync());

                _logger.LogError(errorMessage.Message);

                ViewBag.Message = errorMessage.Message;

                return RedirectToAction("pet-list", "pet");
            }

            return Ok();

        }

        [Route("give-drink-to-my-pet/{petName}")]
        public async Task<IActionResult> GiveDrinkToMyPet(string petName)
        {
            await GiveDrink(petName);
            return RedirectToAction("pet-list", "pet");
        }

        [Route("give-drink-to-foreign-pet/{petName}")]
        public async Task<IActionResult> GiveDrinkToForeignPet(string petName)
        {
            await GiveDrink(petName);
            string encodedFarmName = Uri.EscapeDataString(_farmName);
            return Redirect($"/pet/foreign-pet-list/{encodedFarmName}");
        }

        private async Task<IActionResult> GiveDrink(string petName)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Request.Cookies["token"]);

            HttpResponseMessage response = await _httpClient.GetAsync($"api/pet/current-pet/{petName}");

            PetDto? pet = JsonSerializer.Deserialize<PetDto>(await response.Content.ReadAsStringAsync());

            JsonContent content = JsonContent.Create(pet);

            response = await _httpClient.PatchAsync("api/pet/give-drink", content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Can not give a drink.");
                return BadRequest();
            }

            return Ok();
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
            ViewBag.petType = "current";

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Request.Cookies["token"]);

            HttpResponseMessage response = await _httpClient.GetAsync("api/pet/all-pets");

            if (!response.IsSuccessStatusCode)
            {
                CustomExeption? errorMessage = JsonSerializer.Deserialize<CustomExeption>(await response.Content.ReadAsStringAsync());

                _logger.LogError(errorMessage.Message);

                ViewBag.Message = errorMessage.Message;

                return View("FarmInfo","farm");
            }

            List<PetDto>? pets = JsonSerializer.Deserialize<List<PetDto>>(await response.Content.ReadAsStringAsync());

            ViewBag.type = "current";
            
            return View("GetPetListPage",pets);
        }

        [Route("foreign-pet-list/{farmName}")]
        public async Task<IActionResult> GetForeignPetListPage(string farmName)
        {
            ViewBag.petType = "foreign";

            _farmName = farmName;

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Request.Cookies["token"]);

            HttpResponseMessage response = await _httpClient.GetAsync($"api/pet/foreign-all-pets/{farmName}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Can not get foreign pet list");
                return BadRequest();
            }

            List<PetDto>? pets = JsonSerializer.Deserialize<List<PetDto>>(await response.Content.ReadAsStringAsync());

            ViewBag.type = "foreign";
            
            return View("GetPetListPage", pets);
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

            _pet.PetName = dto.PetName;

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Request.Cookies["token"]);

            JsonContent content = JsonContent.Create(_pet);

            HttpResponseMessage response = await _httpClient.PostAsync($"api/pet/new-pet", content);

            if (!response.IsSuccessStatusCode)
            {
                CustomExeption? errorMessage = JsonSerializer.Deserialize<CustomExeption>(await response.Content.ReadAsStringAsync());

                _logger.LogError(errorMessage.Message);

                ViewBag.Message = errorMessage.Message;

                return View("PetOverview", _pet);
            }

            return RedirectToAction("my-own-farm", "farm");
        }

        [Route("back-to-foreign-pet-list")]
        public IActionResult BackToForeign()
        {
            string encodedFarmName = Uri.EscapeDataString(_farmName);
            return Redirect($"/pet/foreign-pet-list/{encodedFarmName}");
        }
    }
}
