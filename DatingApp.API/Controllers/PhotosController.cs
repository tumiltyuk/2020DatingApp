using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DatingApp.API.Controllers
{
    [Authorize]
    [Route("api/users/{userId}/photos")]
    [ApiController]
    public class PhotosController: ControllerBase
    {
        private readonly IDatingRepository _repo;
        private readonly IMapper _mapper;
        private readonly IOptions<CloudinarySettings> _cloudinaryConfig;
        private Cloudinary _cloudinary;

        public PhotosController(
            IDatingRepository repo, 
            IMapper mapper, 
            IOptions<CloudinarySettings> cloudinaryConfig
        )
        {
            _repo = repo;
            _mapper = mapper;
            _cloudinaryConfig = cloudinaryConfig;

            Account acc = new Account(
                _cloudinaryConfig.Value.CloudName,
                _cloudinaryConfig.Value.ApiKey,
                _cloudinaryConfig.Value.ApiSecret
            );

            _cloudinary = new Cloudinary(acc);
        }


        [HttpGet("{id}", Name = "GetPhoto")]
        public async Task<IActionResult> GetPhoto(int id)
        {
            var photoFromRepo =  await _repo.GetPhoto(id);
            var photo = _mapper.Map<PhotoForReturnDto>(photoFromRepo);

            return Ok(photo);
        }


        [HttpPost]
        public async Task<IActionResult> AddPhotoForUser(int userId, [FromForm]PhotoForCreationDto photoForCreationDto)
        {
            // 1) Validate that user adding photo is the user for this photo.
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))   // "User.FindFirst(ClaimTypes.NameIdentifier).Value"  is obtained from token
                return Unauthorized();                                                  // NameIdentifier is the UserId

            var userFromRepo = await _repo.GetUser(userId);
            
            // 2) Upload Photo to Cloudinary
            var file = photoForCreationDto.File;        // Contains the file to be uploaded

            var uploadResult = new ImageUploadResult(); // Stores result back from Cloudinary - (deserialized server response)

            if (file.Length > 0)
            {
                using (var stream = file.OpenReadStream()) // Read stream into memory
                {
                    var uploadParams = new ImageUploadParams()  // Sets the image upload parameters 
                    {
                        File = new FileDescription(file.Name, stream),  // file name and file literal (stream)
                        Transformation = new Transformation().Width(500).Height(500).Crop("fill").Gravity("face")
                    };
                    uploadResult = _cloudinary.Upload(uploadParams); // envoke upload to Cloudinary
                }
            }
            
            // 3) Update our Database with Photo details 

            // Set the Photo Dto properties for Url and PublicId - Obtained from Cloudinary Response
            photoForCreationDto.Url = uploadResult.Uri.ToString();
            photoForCreationDto.PublicId = uploadResult.PublicId;

            var photo = _mapper.Map<Photo>(photoForCreationDto);

            // If the user DOES NOT have a Main Photo, then set this photo to Main.
            if (!userFromRepo.Photos.Any(u => u.IsMain))
                photo.IsMain = true;

            userFromRepo.Photos.Add(photo); // Update the user photo in the database

            // Save the changes
            if (await _repo.SaveAll())
            {
                var photoToReturn = _mapper.Map<PhotoForReturnDto>(photo);
                return CreatedAtRoute(
                    "GetPhoto",             // 1) string of "Route Name"
                    new {
                        userId = userId,    // 2) Object "Route Values" 
                        id = photo.Id
                    },
                    photoToReturn           // 3) the photoToReturnDto object
                );
            }

            // Any problems then:
            return BadRequest("Could not add the photo");
        }


        [HttpPost("{id}/setMain")]
        public async Task<IActionResult> SetMainPhoto(int userId, int id)
        {
            // 1) Validate that user adding photo is the user for this photo.
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))   // "User.FindFirst(ClaimTypes.NameIdentifier).Value"  is obtained from token
                return Unauthorized();                                                  // NameIdentifier is the UserId

            var userFromRepo = await _repo.GetUser(userId);

            if (!userFromRepo.Photos.Any(p => p.Id == id))
                return Unauthorized();
            
            // Get NEW photo
            var photoFromRepo = await _repo.GetPhoto(id);

            if(photoFromRepo.IsMain)
                return BadRequest("This is already the main photo.");

            // Set original isMain photo to False
            var currentMainPhoto = await _repo.GetMainPhotoForUser(userId);
            currentMainPhoto.IsMain = false;

            // Set NEW photo to "isMain" is true
            photoFromRepo.IsMain = true;

            // Save Changes
            if (await _repo.SaveAll())
                return NoContent();
            
            return BadRequest("Could not set photo to Main.");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePhoto(int userId, int id) {

        // 1) Validate that user adding photo is the user for this photo.
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))   // "User.FindFirst(ClaimTypes.NameIdentifier).Value"  is obtained from token
                return Unauthorized();                                                  // NameIdentifier is the UserId

            var userFromRepo = await _repo.GetUser(userId);

            if (!userFromRepo.Photos.Any(p => p.Id == id))
                return Unauthorized();

            // Dont want to allow delete of main photo
            var photoFromRepo = await _repo.GetPhoto(id);

            if(photoFromRepo.IsMain)
                return BadRequest("You cannot delete your main photo.");

            if (photoFromRepo.PublicId != null)
            {
                var deleteParams = new DeletionParams(photoFromRepo.PublicId);
                var result = _cloudinary.Destroy(deleteParams);

                if (result.Result == "ok")
                    _repo.Delete(photoFromRepo);
            }

            if (photoFromRepo.PublicId == null)
            {
                _repo.Delete(photoFromRepo);
            }

            if (await _repo.SaveAll())
                return Ok();

            return BadRequest("Failed to delete photo.");
        } 
        
    }
}