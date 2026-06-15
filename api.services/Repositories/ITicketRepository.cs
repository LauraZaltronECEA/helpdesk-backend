using api.models.DTOs;
using api.models.Entities;
using api.models.Responses;

namespace api.services.Repositories;

public interface ITicketRepository
{
    Task<IEnumerable<Ticket>> GetAll(int currentUserId, string role);
    Task<IEnumerable<TicketInboxResponse>> GetInbox(int currentUserId, string role);
    Task<Ticket?> GetById(int id, int currentUserId, string role);
    Task<TicketDetailResponse?> GetDetail(int id, int currentUserId, string role);
    Task<TicketCreateResponse> Create(TicketCreateDTO dto, int createdById);
    Task<GeneralResponse> Update(int id, TicketUpdateDTO dto, int currentUserId, string role);
    Task<GeneralResponse> SoftDelete(int id, int currentUserId, string role);
    Task<GeneralResponse> HardDelete(int id, int currentUserId, string role);
}
