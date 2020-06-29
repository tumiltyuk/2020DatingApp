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
    [Route("api/users/{userid}/[controller]")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        private readonly IDatingRepository _repo;
        private readonly IMapper _mapper;
        public MessagesController(IDatingRepository repo, IMapper mapper)
        {
            _mapper = mapper;
            _repo = repo;
        }

        // Get individual Message
        [HttpGet("{id}", Name="GetMessage")]
        public async Task<IActionResult> GetMessage(int userid, int id)
        {
            // Check to make sure user is authorised
            if (userid != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)) 
                return Unauthorized();   

            var messageFromRepo = await _repo.GetMessage(id);
            
            if (messageFromRepo == null)
                return NotFound();

            return Ok(messageFromRepo);
        }

        // Get ALL Messages for a logged in User
        [HttpGet]
        public async Task<IActionResult> GetMessagesForUser(int userId, [FromQuery]MessageParams messageParams)
        {
            // Check to make sure user is authorised
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)) 
                return Unauthorized();  

            messageParams.UserId = userId;

            var messagesFromRepo = await _repo.GetMessagesForUser(messageParams);

            var messagesToReturn = _mapper.Map<IEnumerable<MessageToReturnDto>>(messagesFromRepo);

            Response.AddPagination(messagesFromRepo.CurrentPage, messagesFromRepo.PageSize, // included in response header
                messagesFromRepo.TotalCount, messagesFromRepo.TotalPages);

            return Ok(messagesToReturn);
        }

        // Get Message Thread for a logged in User
        [HttpGet("thread/{recipientId}")]
        public async Task<IActionResult> GetMessageThread(int userId, int recipientId)
        {
            // Check to make sure user is authorised
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)) 
                return Unauthorized();  
            
            var messagesFromRepo = await _repo.GetMessageThread(userId, recipientId);

            var messageThread = _mapper.Map<IEnumerable<MessageToReturnDto>>(messagesFromRepo);

            return Ok(messageThread);
        } 


        // Post individual Message
        [HttpPost]
        public async Task<IActionResult> CreateMessage(int userId,
            MessageForCreationDto messageForCreationDto)
        {
            var sender = await _repo.GetUser(userId, true);

            // Check to make sure user is authorised
            if (sender.Id != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)) 
                return Unauthorized();   

            messageForCreationDto.SenderId = userId;

            var recipient = await _repo.GetUser(messageForCreationDto.RecipientId, false);

            if (recipient == null)
                return BadRequest("Could not find user");

            // map messagedto to message
            var savedMessage = _mapper.Map<Message>(messageForCreationDto);

            _repo.Add(savedMessage);

            if (await _repo.SaveAll()) 
            {
                var messageToReturn = _mapper.Map<MessageToReturnDto>(savedMessage);
                return CreatedAtRoute("GetMessage", 
                    new {userId, id = savedMessage.Id}, messageToReturn);
            }

            throw new Exception("Creating the message failed on save");
        }

        [HttpPost("{id}")]
        public async Task<IActionResult> DeleteMessage(int id, int userId)
        {
            // Check to make sure user is authorised
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)) 
                return Unauthorized();
            
            var messageFromRepo = await _repo.GetMessage(id);

            if (messageFromRepo.SenderId == userId) // if user is the sender then set sender deleted
                messageFromRepo.SenderDeleted = true;

            if (messageFromRepo.RecipientId == userId) // if user is the sender then set recipient deleted
                messageFromRepo.RecipientDeleted = true;
            
            if (messageFromRepo.SenderDeleted && messageFromRepo.RecipientDeleted)
                _repo.Delete(messageFromRepo);

            if (await _repo.SaveAll())
                return NoContent();

            throw new Exception("Error deleting the message");
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkMessageAsRead(int userId, int id)
        {
            // Check to make sure user is authorised
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)) 
                return Unauthorized();

            var message = await _repo.GetMessage(id);

            if (message.RecipientId != userId)
                return Unauthorized();

            message.IsRead = true;
            message.DateRead = DateTime.Now;

            await _repo.SaveAll();

            return NoContent();
        }
    }
}