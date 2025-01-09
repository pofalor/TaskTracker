using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Globalization;
using TaskTracker.Core.src.Constants;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.Models.PostRequests;
using TaskTracker.Core.src.Models.ResponseModels;
using TaskTracker.Utils.src.Extensions;

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
            CreateMap<WorkSpaceMember, WorkSpaceModel>()
                .ForMember(dist => dist.Id, opt => opt.MapFrom(x => x.WorkSpace.Id))
                .ForMember(dist => dist.Name, opt => opt.MapFrom(x => x.WorkSpace.Name))
                .ForMember(dist => dist.WorkSpaceType, opt => opt.MapFrom(x => x.WorkSpace.WorkSpaceType))
                .ForMember(dist => dist.DirectorUserId, opt => opt.MapFrom(x => x.WorkSpace.DirectorUserId))
                .ForMember(dist => dist.Country, opt => opt.MapFrom(x => x.WorkSpace.Country))
                .ForMember(dist => dist.RegistrationDate, opt => opt.MapFrom(x => x.WorkSpace.RegistrationDate.HasValue ? x.WorkSpace.RegistrationDate.Value.ToString(DateFormatConstants.FrontInputFormat) : null))
                .ForMember(dist => dist.Address, opt => opt.MapFrom(x => x.WorkSpace.Address))
                .ForMember(dist => dist.INN, opt => opt.MapFrom(x => x.WorkSpace.INN));
            CreateMap<CreateOrEditWorkSpacePostRequest, WorkSpace>()
                .ForMember(dist => dist.RegistrationDate, opt => opt.MapFrom(x => DateTime.ParseExact(x.RegistrationDate, DateFormatConstants.FrontInputFormat, CultureInfo.InvariantCulture)));
        }
    }
}
