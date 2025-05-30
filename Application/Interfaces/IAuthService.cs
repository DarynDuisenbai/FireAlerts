﻿using Application.DTOs.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto model);
        Task<AuthResponseDto> LoginAsync(LoginDto model);
        Task<bool> ChangeUserRoleAsync(ChangeRole model);
        Task<bool> UploadProfilePhoto(EditProfilePhotoDto req);
        Task<bool> SendVerificationCodeAsync(string email);
        Task<bool> VerifyEmailAsync(string email, string code);
        Task<UserProfileDto> GetProfileAsync(string userId);
        Task<List<Domain.Entities.Identity.User>> GetAllUsers();

    }
}
