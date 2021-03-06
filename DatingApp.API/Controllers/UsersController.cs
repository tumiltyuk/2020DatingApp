using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DatingApp.API.Controllers
{
    [ServiceFilter(typeof(LogUserActivity))]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IDatingRepository _repo;
        private readonly IMapper _mapper;
        public UsersController(IDatingRepository repo, IMapper mapper)
        {
            _mapper = mapper;
            _repo = repo;
        }


        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery]UserParams userParams)
        {
            var isCurrentUser = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value) == userParams.UserId;
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var userFromRepo = await _repo.GetUser(currentUserId, isCurrentUser);
            userParams.UserId = currentUserId;
            if (string.IsNullOrEmpty(userParams.Gender)) {
                userParams.Gender = userFromRepo.Gender == "male" ? "female" : "male";
            }

            var users = await _repo.GetUsers(userParams);
            
            var usersToReturn = _mapper.Map<IEnumerable<UserForListDto>>(users);

            Response.AddPagination(users.CurrentPage, users.PageSize, 
                users.TotalCount, users.TotalPages);

            return Ok(usersToReturn);
        }
        
        
        [HttpGet("{id}", Name="GetUser")]
        public async Task<IActionResult> GetUser(int id)
        {
            var isCurrentUser = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value) == id;
            var user = await _repo.GetUser(id, isCurrentUser);
            var userToReturn = _mapper.Map<UserForDetailedDto>(user);

            return Ok(userToReturn);
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, UserForUpdateDto userForUpdateDto)
        {
            // Check that user completing update is User associated to update -
            // In other words a User CANNOT update someone elses user details
            if (id != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)) // "User.FindFirst(ClaimTypes.NameIdentifier).Value"  is obtained from token
                return Unauthorized();                                            // NameIdentifier is the id

            // _repo or DatingRepository holds data that Enitiy Framework will use to extract/ place into Database
            var userFromRepo = await _repo.GetUser(id, true);
            _mapper.Map(userForUpdateDto, userFromRepo);    // Source --> Destination

            if (await _repo.SaveAll())
                return NoContent();

            throw new Exception($"Updating user {id} failed on save.");
        }

        [HttpPost("{id}/like/{recipientId}")]
        public async Task<IActionResult> LikeUser(int id, int recipientId)
        {
            // Check to make sure user is authorised
            if (id != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)) 
            return Unauthorized();                                            

            // Check is Like Already exists - >>
            var like = await _repo.GetLike(id, recipientId);
            // If exists then trown error
            if (like != null)
                return BadRequest("You have already liked this user");

            // Check if likee (recipient) exists
            if (await _repo.GetUser(recipientId, false) == null)
                return NotFound();

            // Create NEW Like
            like = new Like
            {
                LikerId = id,
                LikeeId = recipientId
            };

            // Add Like to the repo
            _repo.Add<Like>(like);

            // Save changes
            if (await _repo.SaveAll())
                return Ok();

            // return error if all else fails
            return BadRequest("Failed to like user");

        }

    }
}