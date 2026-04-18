
namespace Imate.AI.Module.Core.Interfaces
{
    /// <summary>
    /// Interface cho host project implement để cung cấp CV data cho AI Module
    /// Giúp AI Module không phụ thuộc trực tiếp vào database/repository của Imate.API
    /// </summary>
    public interface ICvDataProvider
    {
        /// <summary>
        /// Lấy CV text content theo accountId và cvId
        /// </summary>
        Task<string> GetCvTextAsync(int accountId, int cvId);

        /// <summary>
        /// Lấy kết quả phân tích đã cache (nếu có)
        /// </summary>
        /// <returns>JSON string kết quả phân tích, hoặc null nếu chưa có</returns>
        Task<string?> GetCachedAnalysisAsync(int accountId, int cvId);

        /// <summary>
        /// Lưu kết quả phân tích vào database để cache cho lần sau
        /// </summary>
        Task SaveAnalysisResultAsync(int accountId, int cvId, string analysisJson);

        /// <summary>
        /// Xóa ScannedData đã cache để buộc re-extract từ file gốc
        /// </summary>
        Task ClearScannedDataAsync(int accountId, int cvId);
    }
}