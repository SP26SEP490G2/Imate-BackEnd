namespace Imate.AI.Module.Interfaces
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
        /// <param name="accountId">Account ID của người dùng (để validate quyền)</param>
        /// <param name="cvId">CV ID trong database</param>
        /// <returns>CV text content</returns>
        Task<string> GetCvTextAsync(int accountId, int cvId);
    }
}
