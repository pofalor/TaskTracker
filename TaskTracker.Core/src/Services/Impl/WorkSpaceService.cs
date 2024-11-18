using Microsoft.Extensions.Logging;
using TaskTracker.Core.src.DataAccess;
using TaskTracker.Core.src.Entities;
using TaskTracker.Core.src.Models.Filters;

namespace TaskTracker.Core.src.Services.Impl
{
    public class WorkSpaceService : BaseService<WorkSpace, WorkSpaceFilter>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<WorkSpaceService> _logger;

        public WorkSpaceService(ApplicationDbContext dbContext, ILogger<WorkSpaceService> logger) :
            base(dbContext, logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }


    }
}
