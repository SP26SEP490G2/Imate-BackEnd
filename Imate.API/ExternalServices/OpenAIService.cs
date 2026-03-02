namespace Imate.API.ExternalServices
{
    public class OpenAIService
    {
        // Add OpenAI processing logic here
        public async Task<string> GenerateTextAsync(string prompt)
        {
            await Task.Delay(100);
            return "Generated text";
        }
    }
}
