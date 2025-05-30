﻿using Domain.Entities.Identity.Enums;
using MongoDB.Entities;

namespace Domain.Entities.Identity
{
    [Collection(nameof(User) + "s")]
    public class User
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string PasswordHash { get; set; }
        public string Roles { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Photo {  get; set; }
        public bool IsEmailVerified { get; set; }
    }
}
