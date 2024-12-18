using AutoMapper;
using Microsoft.AspNetCore.Identity;
using System;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.Models.PostRequests;
using TaskTracker.Core.src.Models.ResponseModels;

namespace TaskTracker.Core.src.Context
{
    public class AutoMappingProfile : Profile
    {
        public AutoMappingProfile()
        {
            CreateMap<CreateUserPostRequest, IdentityUser>()
                .ForMember(dist => dist.UserName, opt => opt.MapFrom(x => x.Email));
            CreateMap<CreateUserPostRequest, User>();
            CreateMap<User, UserModel>()
                .ForMember(dist => dist.Name, opt => opt.MapFrom(x => x.GetUserName(false)));
            CreateMap<WorkSpace, WorkSpaceModel>();
            CreateMap<CreateOrEditWorkSpacePostRequest, WorkSpace>();
        }
    }
}
