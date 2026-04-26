using Imate.AI.Module.Core.Interfaces;

namespace Imate.AI.Module.Core.Services
{
    /// <summary>
    /// Chọn gap cho mỗi session theo rule:
    ///
    /// Ưu tiên hỏi:
    ///   1. jdRequired (JD yêu cầu + CV có) chưa hỏi lần nào → kiểm chứng CV
    ///   2. Unresolved gap chưa hỏi (theo thứ tự list)
    ///   3. Weak gap (trả lời kém, ConsecutiveGoodScore = 0) → random 1 cái
    ///
    /// Nếu thiếu → bù bằng câu hỏi JD (NeedJdFill = true)
    /// Tất cả resolved → AllResolved = true
    /// </summary>
    public class GapSelectionService
    {
        private const int GapsPerSession = 3;
        private readonly Random _rng = new();

        public SessionGapSelection SelectGapsForSession(List<JourneyGapItem> allGaps)
        {
            if (allGaps.Count == 0)
                return new SessionGapSelection { NeedJdFill = true, FillCount = GapsPerSession };

            var unresolved = allGaps.Where(g => g.Status == "Unresolved").ToList();

            if (unresolved.Count == 0)
                return new SessionGapSelection { AllResolved = true };

            var selected = new List<JourneyGapItem>();

            // Slot 1: jdRequired chưa hỏi lần nào (ưu tiên kiểm chứng CV)
            var jdRequiredNew = unresolved
                .Where(g => g.Source == "jdRequired" && g.TimesAsked == 0)
                .FirstOrDefault();
            if (jdRequiredNew != null) selected.Add(jdRequiredNew);

            // Slot 2: Unresolved chưa hỏi (theo thứ tự list, bỏ qua cái đã chọn)
            var neverAsked = unresolved
                .Where(g => g.TimesAsked == 0 && !selected.Contains(g))
                .FirstOrDefault();
            if (neverAsked != null && selected.Count < GapsPerSession)
                selected.Add(neverAsked);

            // Slot 3: Weak gap (ConsecutiveGoodScore = 0 + đã hỏi ít nhất 1 lần) → random
            if (selected.Count < GapsPerSession)
            {
                var weakGaps = unresolved
                    .Where(g => g.TimesAsked > 0 && g.ConsecutiveGoodScore == 0 && !selected.Contains(g))
                    .ToList();
                if (weakGaps.Any())
                    selected.Add(weakGaps[_rng.Next(weakGaps.Count)]);
            }

            // Nếu vẫn thiếu slot → lấy thêm unresolved chưa hỏi bất kỳ
            if (selected.Count < GapsPerSession)
            {
                var remaining = unresolved
                    .Where(g => !selected.Contains(g))
                    .Take(GapsPerSession - selected.Count);
                selected.AddRange(remaining);
            }

            int fillCount = GapsPerSession - selected.Count;

            return new SessionGapSelection
            {
                SelectedGaps = selected,
                AllResolved = false,
                NeedJdFill = fillCount > 0,
                FillCount = fillCount
            };
        }

        /// <summary>
        /// Cuối session: cập nhật trạng thái gap được hỏi.
        /// Score >= 0.7 → ConsecutiveGoodScore++, đạt 2 lần → Resolved.
        /// Score < 0.7  → reset ConsecutiveGoodScore.
        /// </summary>
        public void UpdateGapStatuses(List<JourneyGapItem> allGaps, List<GapScoreResult> sessionScores, int sessionId = 0)
        {
            foreach (var scoreResult in sessionScores)
            {
                var gap = allGaps.FirstOrDefault(g =>
                    g.GapName.Equals(scoreResult.GapName, StringComparison.OrdinalIgnoreCase));
                if (gap == null) continue;

                gap.TimesAsked++;
                gap.LastAskedSessionId = sessionId;

                if (scoreResult.Score >= 0.7)
                {
                    gap.ConsecutiveGoodScore++;
                    if (gap.ConsecutiveGoodScore >= 2)
                        gap.Status = "Resolved";
                }
                else
                {
                    gap.ConsecutiveGoodScore = 0;
                }
            }
        }

        /// <summary>
        /// Build prompt inject vào AI — chỉ 3 gap được chọn.
        /// Nếu thiếu gap → chỉ dẫn bù theo JD.
        /// </summary>
        public string BuildGapPromptSection(List<JourneyGapItem> selectedGaps, int jdFillCount = 0)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("\n=== ĐỊNH HƯỚNG CÂU HỎI SESSION NÀY ===");

            if (selectedGaps.Any())
            {
                sb.AppendLine("ƯU TIÊN hỏi về các kỹ năng sau (theo thứ tự):\n");
                foreach (var gap in selectedGaps)
                {
                    var instruction = gap.TimesAsked == 0
                        ? gap.Source == "jdRequired"
                            ? "(CV có khai — kiểm chứng thực tế) → bắt đầu từ câu hỏi thực tiễn"
                            : "(Lần đầu) → bắt đầu từ cơ bản"
                        : gap.ConsecutiveGoodScore == 0
                            ? "(Trả lời kém lần trước) → hỏi lại góc đơn giản hơn, củng cố nền tảng"
                            : "(Đang tiến bộ) → tăng độ khó nhẹ";

                    sb.AppendLine($"- {gap.GapName}: {instruction}");
                }
            }

            if (jdFillCount > 0)
            {
                sb.AppendLine($"\nBổ sung {jdFillCount} câu hỏi theo JD (chưa đủ gap để hỏi).");
                sb.AppendLine("Chọn chủ đề quan trọng nhất trong JD chưa được đề cập ở trên.");
            }

            if (!selectedGaps.Any() && jdFillCount == 0)
            {
                sb.AppendLine("Gen toàn bộ câu hỏi theo JD và vị trí ứng tuyển.");
            }

            sb.AppendLine("\nSau mỗi câu trả lời: tốt → tăng độ khó | kém → hỏi lại đơn giản hơn.");
            return sb.ToString();
        }
    }

    public class GapScoreResult
    {
        public string GapName { get; set; } = string.Empty;
        /// <summary>0.0 → 1.0</summary>
        public double Score { get; set; }
    }
}