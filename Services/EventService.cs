
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Test26.Data;
using Test26.Models;

namespace Test26.EventS;

public class EventService
{
    private readonly ProjectManagementSystemContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public EventService(ProjectManagementSystemContext pmsc, IHttpContextAccessor IHTTP)
    {
        _context = pmsc;
        _httpContextAccessor = IHTTP;
    }

    public async Task Log(Guid eventTypeId, Guid tableId, Guid relatedId, string description)
    {
        var et = await _context.EventTypes
            .FindAsync(eventTypeId)
            
            ?? throw new KeyNotFoundException("Event Type Not Found");

        var tb = await _context.RelatedTables
            .FindAsync(tableId)

            ?? throw new KeyNotFoundException("Table ID Not Found");

        /*
        var userIdClaim = _httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;
        */
        
        var ev = new Event
        {
            EventTypeId = eventTypeId,
            RelatedTableId = tableId,
            RelatedId = relatedId,
            Description = description,
            Date = DateTime.Now
        };

        await _context.Events.AddAsync(ev);
        await _context.SaveChangesAsync();
    }
}