using Microsoft.EntityFrameworkCore;
using Test26.Context;
using Test26.EventS;
using Test26.Models;

namespace Test26.Service;

public class ChangeLogService
{
    private readonly ProjectManagementSystemContext _projectManagementSystemContext;
    private readonly LogService _logService;

    public ChangeLogService(ProjectManagementSystemContext pc, LogService ls)
    {
        _projectManagementSystemContext = pc;
        _logService = ls;
    }

    public async Task<List<ChangeLog>> GetNTop(int n = 30)
    {
        var cl = await _projectManagementSystemContext.ChangeLogs
            .Where(c => !((ISoftDeletable)c).IsDeleted)
            .Take(n)
            .ToListAsync()
            ?? throw new InvalidOperationException("Change Log Empity");

        return cl;
    }

    public async Task<List<ChangeLog>> GetByLogId(Guid id)
    {
        var cl = await _projectManagementSystemContext.ChangeLogs
            .Where(c => !((ISoftDeletable)c).IsDeleted && c.LogId == id)
            .ToListAsync();

        if (cl.Count == 0)
            throw new KeyNotFoundException("There are no Change Log with this ID");

        return cl;
    }

    public async Task<ChangeLog> GetById(Guid id)
    {
        var cl = await _projectManagementSystemContext.ChangeLogs
            .FirstOrDefaultAsync(c => c.ChangeLogId == id && !((ISoftDeletable)c).IsDeleted)
            ?? throw new KeyNotFoundException("There are no Change Log with this ID");

        return cl;
    }

    public async Task<ChangeLog> Delete(Guid id)
    {
        var cl = await _projectManagementSystemContext.ChangeLogs
            .FirstOrDefaultAsync(c => c.ChangeLogId == id && !((ISoftDeletable)c).IsDeleted)
            ?? throw new KeyNotFoundException("There are no Change Log with this ID");

        cl.IsDeleted = true;
        
        await _projectManagementSystemContext.SaveChangesAsync();

        return cl;
    }
}