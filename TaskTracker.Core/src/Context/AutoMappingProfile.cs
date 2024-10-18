using AutoMapper;
using Microsoft.AspNetCore.Identity;
using System;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.Models.PostRequests;

namespace TaskTracker.Core.src.Context
{
    public class AutoMappingProfile : Profile
    {
        public AutoMappingProfile()
        {
            CreateMap<CreateUserPostRequest, IdentityUser>()
                .ForMember(dist => dist.UserName, opt => opt.MapFrom(x => x.Email));
            CreateMap<CreateUserPostRequest, User>();
        }
    }
}
