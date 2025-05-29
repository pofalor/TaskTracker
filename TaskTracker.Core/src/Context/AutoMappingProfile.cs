using AutoMapper;
using Microsoft.AspNetCore.Identity;
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
            CreateMap<WorkspaceMember, WorkspaceModel>()
                .ForMember(dist => dist.Id, opt => opt.MapFrom(x => x.Workspace.Id))
                .ForMember(dist => dist.Name, opt => opt.MapFrom(x => x.Workspace.Name))
                .ForMember(dist => dist.WorkspaceType, opt => opt.MapFrom(x => x.Workspace.WorkspaceType))
                .ForMember(dist => dist.DirectorUserId, opt => opt.MapFrom(x => x.Workspace.DirectorUserId))
                .ForMember(dist => dist.Country, opt => opt.MapFrom(x => x.Workspace.Country))
                .ForMember(dist => dist.RegistrationDate, opt => opt.MapFrom(x => x.Workspace.RegistrationDate.HasValue ? x.Workspace.RegistrationDate.Value.ToString(DateFormatConstants.FrontInputFormat) : null))
                .ForMember(dist => dist.Address, opt => opt.MapFrom(x => x.Workspace.Address))
                .ForMember(dist => dist.INN, opt => opt.MapFrom(x => x.Workspace.INN))
                .ForMember(dist => dist.ReviewStatus, opt => opt.MapFrom(x => x.Workspace.ReviewStatus));
            CreateMap<CreateOrEditWorkspacePostRequest, Workspace>()
                .ForMember(dist => dist.RegistrationDate, opt => opt.MapFrom(x => x.RegistrationDateTime));
            CreateMap<WorkspaceInvite, WorkspaceInviteModel>()
                .ForMember(dist => dist.Date, opt => opt.MapFrom(x => x.Date.ToString(DateFormatConstants.FullDateTimeShort)))
                .ForMember(dist => dist.WorkspaceName, opt => opt.MapFrom(x => x.Workspace.Name))
                .ForMember(dist => dist.InviterName, opt => opt.MapFrom(x => x.Inviter.GetUserName(false)))
                .ForMember(dist => dist.DirectorWspName, opt => opt.MapFrom(x => x.Workspace.DirectorUser.GetUserName(false)))
                .ForMember(dist => dist.UserName, opt => opt.MapFrom(x => x.User.GetUserName(false)))
                .ForMember(dist => dist.UserEmail, opt => opt.MapFrom(x => x.User.Email))
                .ForMember(dist => dist.InviterEmail, opt => opt.MapFrom(x => x.Inviter.Email));
            CreateMap<CreateWspInvitePostRequest, WorkspaceInvite>()
               .ForMember(dist => dist.Date, opt => opt.MapFrom(x => DateTime.ParseExact(x.Date, DateFormatConstants.DataBaseTime, CultureInfo.InvariantCulture)));
            CreateMap<Project, ProjectModel>()
                .ForMember(dist => dist.StartDate, opt => opt.MapFrom(x => x.StartDate.ToString(DateFormatConstants.DatewithoutTimeZone)))
                .ForMember(dist => dist.EndDate, opt => opt.MapFrom(x => x.EndDate.HasValue ? x.EndDate.Value.ToString(DateFormatConstants.DatewithoutTimeZone) : null))
                .ForMember(dist => dist.ProjectMgrName, opt => opt.MapFrom(x => x.ProjectMgr.GetUserName(false)));
            CreateMap<CreateOrEditProjectPR, Project>()
                .ForMember(dist => dist.StartDate, opt => opt.MapFrom(x => DateTime.ParseExact(x.StartDate, DateFormatConstants.FrontInputFormat, CultureInfo.InvariantCulture)))
                .ForMember(dist => dist.EndDate, opt => opt.MapFrom(x => x.ConvertEndDate()));
            CreateMap<TimeTrackPR, TimeTracking>()
                .ForMember(dist => dist.DateBegin, opt => opt.MapFrom(x => x.GetBeginDate()))
                .ForMember(dist => dist.TimeSpent, opt => opt.MapFrom(x => x.TimeSpent.ConvertToTimespan()));
            CreateMap<CreateOrEditIssuePR, Issue>()
                .ForMember(dist => dist.Estimate, opt => opt.MapFrom(x => x.Estimate.ConvertToTimespan()));
            CreateMap<TimeTracking, TimeTrackingModel>()
                .ForMember(dist => dist.DateBegin, opt => opt.MapFrom(x => x.DateBegin.ToString(DateFormatConstants.IsoString)))
                .ForMember(dist => dist.TimeSpent, opt => opt.MapFrom(x => x.TimeSpent.ToString()));
            CreateMap<AutoTimeTrackPR, TimeTracking>()
                .ForMember(dist => dist.DateBegin, opt => opt.MapFrom(x => x.GetBeginDate()))
                .ForMember(dist => dist.TimeSpent, opt => opt.MapFrom(x => x.TimeSpent.ConvertToTimespan()));
        }
    }
}
