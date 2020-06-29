using System;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace DatingApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class AuthController : ControllerBase
    {
        // private readonly IAuthRepository _repo;
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AuthController(
            // IAuthRepository repo,
            IConfiguration config,
            IMapper mapper,
            UserManager<User> userManager,
            SignInManager<User> signInManager)
        {
            //_repo = repo; // No longer needed for Identity Management
            _config = config;
            _mapper = mapper;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register(UserForRegisterDto userForRegisterDto)
        {
            // // Previous Version of Login -
            // userForRegisterDto.Username = userForRegisterDto.Username.ToLower();
            // if (await _repo.UserExists(userForRegisterDto.Username))
            // //     return BadRequest("User already exists");
            // var userToCreate = _mapper.Map<User>(userForRegisterDto);
            // var createdUser = await _repo.Register(userToCreate, userForRegisterDto.Password);
            // var userToReturn = _mapper.Map<UserForDetailedDto>(createdUser);

            // return CreatedAtRoute("GetUser", new {controller = "Users", id = createdUser.Id}, userToReturn); 

            var userToCreate = _mapper.Map<User>(userForRegisterDto);
            var result = await _userManager.CreateAsync(userToCreate, userForRegisterDto.Password);

            var userToReturn = _mapper.Map<UserForDetailedDto>(userToCreate);

            if (result.Succeeded)
            {
                return CreatedAtRoute("GetUser", new {controller = "Users", id = userToCreate.Id}, userToReturn); 
            }

            return BadRequest(result.Errors);
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(UserForLoginDto userForLoginDto)
        {
            // // Previous Version of Login -
            // var userFromRepo = await _repo.Login(userForLoginDto.Username.ToLower(), 
            // userForLoginDto.Password);
            // if (userFromRepo == null)
            //     return Unauthorized();
            // // Generate user Object for Client side storage
            // var user = _mapper.Map<UserForClientStorageDto>(userFromRepo);

            // // Return token as an object to the Client
            // return Ok(new {
            //     token = GenerateJwtToken(userFromRepo),
            //     user 
            // });

            // Using .NetCore "Identity" approach
            var userFromRepo = await _userManager.FindByNameAsync(userForLoginDto.Username);
            var result = await _signInManager.CheckPasswordSignInAsync(userFromRepo, userForLoginDto.Password, false);

            if (result.Succeeded)
            {
                // Generate user Object for Client side storage
                var appUser = _mapper.Map<UserForClientStorageDto>(userFromRepo);

                // Return token as an object to the Client
                return Ok(new {
                    token = GenerateJwtToken(userFromRepo).Result,
                    user = appUser 
                });
            }
            return Unauthorized();
        }

        private async Task<string> GenerateJwtToken(User user)
        {
            //Build Token to return to user (Token to contain "Claims": Username and UserId)
            var claims = new List<Claim>
            {   // Claim is a key value pair:  Claim Type & Value
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),   // User Id
                new Claim(ClaimTypes.Name, user.UserName)                   // UserName
            };

            var roles = await _userManager.GetRolesAsync(user);

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Generate "Key" to sign our token - (Hashed)  - Used for Validation check when future calls are made back from the client
            //[Key is stored in our appsettings.json - value in here should be at least 12 char long and be a complicated string or random gen text]
            var key = new SymmetricSecurityKey(Encoding.UTF8
                .GetBytes(_config.GetSection("AppSettings:Token").Value));

            // Generate Signin Credentials - takes security Key and algorithms used to hash our Key 
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            // Generate Token Discriptor that holds the token properties & claims
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(0.5),
                SigningCredentials = creds
            };

            // Generate Token Handler for creating a Token and passing in the Token Descriptor
            var tokenHandler = new JwtSecurityTokenHandler();

            // Generate Token
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

    }
}