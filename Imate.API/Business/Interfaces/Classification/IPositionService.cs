using Imate.API.Business.Helper;
using Imate.API.Models.Entities;
using Imate.API.Presentation.RequestModels.Classification;
using Imate.API.Presentation.ResponseModels.Classification;

namespace Imate.API.Business.Interfaces.Classification
{
    public interface IPositionService
    {
        Task<List<int>> GetNonExistingPositionIdsAsync(IEnumerable<int> categoryIds);
        Task<PagedList<PositionResponse>> GetAllPositionsAsync(CommonParams positionParams);
        Task<Position> AddPositionAsync(PositionCreateRequest position);
        Task<Position> UpdatePositionAsync(int id, PositionUpdateRequest position);
        Task<List<AffectedQuestionResponseModel>> GetAffectedQuestionsAsync(int categoryId, bool willBeActive);
        Task<Position?> SetPositionStatusAsync(int id, bool isActive);
    }
}
