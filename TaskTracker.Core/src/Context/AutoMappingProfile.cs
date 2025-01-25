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
                .ForMember(dist => dist.RegistrationDate, opt => opt.MapFrom(x => x.RegistrationDateTime));
            CreateMap<UserWorkspaceStatusChangeRequest, UserWspStatusChangeModel>()
                .ForMember(dist => dist.Date, opt => opt.MapFrom(x => x.Date.ToString(DateFormatConstants.FullDateTimeShort)))
                .ForMember(dist => dist.WorkSpaceName, opt => opt.MapFrom(x => x.WorkSpace.Name))
                .ForMember(dist => dist.InviterName, opt => opt.MapFrom(x => x.Inviter.GetUserName(false)))
                .ForMember(dist => dist.DirectorWspName, opt => opt.MapFrom(x => x.WorkSpace.DirectorUser.GetUserName(false)))
                .ForMember(dist => dist.UserName, opt => opt.MapFrom(x => x.User.GetUserName(false)))
                .ForMember(dist => dist.UserEmail, opt => opt.MapFrom(x => x.User.Email))
                .ForMember(dist => dist.InviterEmail, opt => opt.MapFrom(x => x.Inviter.Email));
            CreateMap<CreateWspInvitePostRequest, UserWorkspaceStatusChangeRequest>()
               .ForMember(dist => dist.Date, opt => opt.MapFrom(x => DateTime.ParseExact(x.Date, DateFormatConstants.DataBaseTime, CultureInfo.InvariantCulture)));
            CreateMap<Project, ProjectModel>()
                .ForMember(dist => dist.StartDate, opt => opt.MapFrom(x => x.StartDate.ToString(DateFormatConstants.DatewithoutTimeZone)))
                .ForMember(dist => dist.EndDate, opt => opt.MapFrom(x => x.EndDate.HasValue ? x.EndDate.Value.ToString(DateFormatConstants.DatewithoutTimeZone) : null))
                .ForMember(dist => dist.ProjectMgrName, opt => opt.MapFrom(x => x.ProjectMgr.GetUserName(false)));
            CreateMap<CreateOrEditProjectPR, Project>()
                .ForMember(dist => dist.StartDate, opt => opt.MapFrom(x => DateTime.ParseExact(x.StartDate, DateFormatConstants.FrontInputFormat, CultureInfo.InvariantCulture)))
                .ForMember(dist => dist.EndDate, opt => opt.MapFrom(x => x.ConvertEndDate()));
            CreateMap<TimeTrackPR, TimeTracking>()
                .ForMember(dist => dist.DateBegin, opt => opt.MapFrom(x => DateTime.ParseExact(x.DateBegin, DateFormatConstants.FrontInputFormat, CultureInfo.InvariantCulture)))
                .ForMember(dist => dist.TimeSpent, opt => opt.MapFrom(x => x.TimeSpent.ConvertToTimespan()));
            CreateMap<CreateOrEditIssuePR, Issue>()
                //костыль, чтобы не отваливался бек
                .ForMember(dist => dist.Estimate, opt => opt.MapFrom(x => new TimeSpan()));
        }
    }
}
