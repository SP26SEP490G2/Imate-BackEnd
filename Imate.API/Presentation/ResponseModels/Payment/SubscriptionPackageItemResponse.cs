namespace Imate.API.Presentation.ResponseModels.Payment
{
    public record SubscriptionPackageItemResponse(
        int Id,
        string Name,
        decimal Price,
        string Duration,
        List<string> Benefits,
        bool IsRecommended,
        int Rank
    );
}
