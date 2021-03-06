using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Extentions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace API.Controllers
{
    [Authorize]
    public class UsersController : BaseApiController
    {
        private readonly IMapper _mapper;
        private readonly IPhotoService _photoService;
        private readonly IUnitOfWork _unitOfWork;

        public UsersController(IUnitOfWork unitOfWork, IMapper mapper, 
                                IPhotoService photoService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _photoService = photoService;
        }
        
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MemberDto>>> GetUsers(UserParams userParams)
        {
            var gender = await _unitOfWork.UserRepository.GetUserGender(User.getUserName());
            userParams.CurrentUsername = User.getUserName();

            if(string.IsNullOrEmpty(userParams.Gender))
                userParams.Gender = gender == "male" ? "female" : "male";

            var users = await _unitOfWork.UserRepository.GetMembersAsync(userParams);

            Response.AddPaginationHeader(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages);
            
            return Ok(users);
        }

        [HttpGet("{username}", Name="GetUser")]
        public async Task<ActionResult<MemberDto>> GetUser(string username)
        {
            return await _unitOfWork.UserRepository.GetMemberByUsernameAsync(username);
        }

        [HttpPut]
        public async Task<ActionResult> updateUser([FromBody] MemberUpdateDto memberUpdateDto)
        {
            var user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(User.getUserName());

            _mapper.Map(memberUpdateDto, user);

            _unitOfWork.UserRepository.Update(user);

            if(await _unitOfWork.Completed()) return NoContent();

            return BadRequest("Failed to update user");
        }

        [HttpPost("add-photo")]
        public async Task<ActionResult<PhotoDto>> AddPhoto(IFormFile file)
        {
            var user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(User.getUserName());

            var result = await _photoService.AddPhotoAsync(file);

            if (result.Error != null) return BadRequest(result.Error.Message);

            var photo = new Photo
            {
                Url = result.SecureUrl.AbsoluteUri,
                PublicId = result.PublicId
            };

            if (user.Photos.Count == 0)
            {
                photo.IsMain = true;
            }

            user.Photos.Add(photo);

            if (await _unitOfWork.Completed())
            {
                return CreatedAtRoute("GetUser", new {username = user.UserName}, _mapper.Map<PhotoDto>(photo));
            }


            return BadRequest("Problem addding photo");
        }

        [HttpPut("set-main-photo/{photoId}")]
        public async Task<ActionResult> SetMainPhoto(int photoId)
        {
            var user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(User.getUserName());

            var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);

            if(photo.IsMain) return BadRequest("This photo is already your main photo");

            var currentMain = user.Photos.FirstOrDefault(a => a.IsMain);
            if(currentMain != null) currentMain.IsMain= false;
            photo.IsMain = true;

            if(await _unitOfWork.Completed()) return NoContent();

            return BadRequest("Failed to set main photo");

        }

        [HttpDelete("delete-photo/{photoId}")]
        public async Task<ActionResult> DeletePhoto(int photoId)
        {
            var user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(User.getUserName());

            var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);

            if(photo == null) return NotFound();

            if(photo.IsMain) return BadRequest("You can not delete your main photo");

            if(photo.PublicId != null)
            {
                var result = _photoService.DeletePhotoAsync(photo.PublicId);
                if(result.Result.Error != null) return BadRequest(result.Result.Error.Message);
            }

            user.Photos.Remove(photo);

            if(await _unitOfWork.Completed()) return Ok();

            return BadRequest("Failed to delete the photo.");
        }
    }
}