using Microsoft.Extensions.Configuration;
using PlantDecor.BusinessLogicLayer.Interfaces;
using System.Text;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class EmbeddingChunker : IEmbeddingChunker
    {
        private readonly bool _enableChunking;
        private readonly int _maxChunkChars;
        private readonly int _chunkOverlapChars;

        public EmbeddingChunker(IConfiguration configuration)
        {
            _enableChunking = configuration.GetValue<bool>("EmbeddingProcessingSettings:EnableChunking", false);
            _maxChunkChars = configuration.GetValue<int>("EmbeddingProcessingSettings:MaxChunkChars", 1200);
            _chunkOverlapChars = configuration.GetValue<int>("EmbeddingProcessingSettings:ChunkOverlapChars", 150);
        }

        public IReadOnlyList<string> Chunk(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return Array.Empty<string>();
            }

            var normalized = text.Trim();
            // Đảm bảo chunk tối thiểu 100 ký tự — tránh trường hợp config set giá trị quá nhỏ hoặc = 0
            var maxChunkChars = Math.Max(100, _maxChunkChars);
            // đảm bảo rằng overlap không lớn hơn maxChunkChars - 1 để tránh tình trạng chunk bị trùng lặp hoàn toàn
            // 0 <= overlapChars <= maxChunkChars
            var overlapChars = Math.Clamp(_chunkOverlapChars, 0, Math.Max(0, maxChunkChars - 1));

            if (!_enableChunking || normalized.Length <= maxChunkChars)
            {
                return new List<string> { normalized };
            }

            var baseChunks = BuildParagraphChunks(normalized, maxChunkChars, overlapChars);
            // Nếu chỉ có 1 chunk hoặc không có overlap, chúng ta sẽ trả về kết quả ngay mà không cần xử lý thêm
            if (baseChunks.Count <= 1 || overlapChars == 0)
            {
                return baseChunks;
            }

            // Thêm phần overlap vào đầu mỗi chunk (trừ chunk đầu tiên)
            var result = new List<string> { baseChunks[0] };
            for (var i = 1; i < baseChunks.Count; i++)
            {
                var current = baseChunks[i];
                var previous = baseChunks[i - 1];

                // ví dụ maxchunk là 10 còn current là 5 thì
                // availablePrefixLength sẽ là 10 -5 -1 =4 (vì chúng ta sẽ thêm một ký tự xuống dòng giữa phần overlap và nội dung chính của chunk)
                var availablePrefixLength = Math.Max(0, maxChunkChars - current.Length - 1);
                // phần overlap thực tế được tính Min vì khi overlapchars vượt quá availablePrefixLength,
                // chúng ta chỉ có thể lấy availablePrefixLength vì đây là ngưỡng tối đa mà chunk hiện tại có thể chứa (bao gồm cả phần overlap và nội dung chính)
                var actualPrefixLength = Math.Min(overlapChars, availablePrefixLength);
                // nếu <= 0 nghĩa là chunk hiện tại đã gần đạt đến giới hạn maxChunkChars,
                // chúng ta sẽ không thể thêm phần overlap nào nữa
                if (actualPrefixLength <= 0)
                {
                    result.Add(current);
                    continue;
                }

                var prefix = Tail(previous, actualPrefixLength);
                result.Add(string.IsNullOrWhiteSpace(prefix) ? current : $"{prefix}\n{current}");
            }

            return result;
        }

        private static List<string> BuildParagraphChunks(string text, int maxChunkChars, int overlapChars)
        {
            var chunks = new List<string>();
            // Tách văn bản thành các đoạn dựa trên khoảng trắng kép (paragraphs)
            // Sử dụng StringSplitOptions.TrimEntries để loại bỏ khoảng trắng thừa ở đầu và cuối mỗi đoạn,
            // và RemoveEmptyEntries để loại bỏ các đoạn rỗng
            // \n\n là dấu hiệu phân tách đoạn, phù hợp với nhiều định dạng văn bản (Markdown, plain text, v.v.)
            var paragraphs = text
                .Split("\n\n", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            var current = new StringBuilder();
            foreach (var paragraph in paragraphs)
            {
                // Nếu đoạn văn bản dài hơn maxChunkChars,
                // chúng ta sẽ cắt nó thành các phần nhỏ hơn
                if (paragraph.Length > maxChunkChars)
                {
                    FlushCurrent(chunks, current);
                    chunks.AddRange(SplitLongSegment(paragraph, maxChunkChars, overlapChars));
                    continue;
                }

                // Nếu chưa có nội dung nào trong current, chúng ta sẽ thêm đoạn văn bản vào current mà không cần kiểm tra độ dài
                if (current.Length == 0)
                {
                    current.Append(paragraph);
                    continue;
                }

                // Nếu thêm đoạn văn bản nữa vào current mà không vượt quá maxChunkChars, chúng ta sẽ thêm nó vào current
                // +2 ở đây là 2 kí tự (\n\n)
                if (current.Length + 2 + paragraph.Length <= maxChunkChars)
                {
                    current.Append("\n\n");
                    current.Append(paragraph);
                    continue;
                }

                // Nếu thêm đoạn văn bản vào current mà vượt quá maxChunkChars,
                // chúng ta sẽ đẩy current vào chunks và bắt đầu một current mới với đoạn văn bản hiện tại
                FlushCurrent(chunks, current);
                current.Append(paragraph);
            }

            FlushCurrent(chunks, current);
            return chunks;
        }

        // Nếu một đoạn văn bản dài hơn maxChunkChars, chúng ta sẽ cắt nó thành các phần nhỏ hơn
        private static IEnumerable<string> SplitLongSegment(string segment, int maxChunkChars, int overlapChars)
        {
            var parts = new List<string>();
            // Đảm bảo rằng bước nhảy không nhỏ hơn 1 để tránh vòng lặp vô hạn
            // step = chunk_size - overlap : số ký tự dịch sang phải để tạo chunk tiếp theo,
            // đảm bảo rằng mỗi chunk mới sẽ có một phần nội dung trùng lặp với chunk trước đó (nếu overlapChars > 0)
            var step = Math.Max(1, maxChunkChars - Math.Max(0, overlapChars));

            for (var start = 0; start < segment.Length; start += step)
            {
                var remaining = segment.Length - start;
                // Lấy đến maxChunkChars từ start hoặc remaining nếu remaining < maxChunkChars
                var length = Math.Min(maxChunkChars, remaining);
                var slice = segment.Substring(start, length).Trim();
                if (slice.Length > 0)
                {
                    parts.Add(slice);
                }

                // Nếu chunk hiện tại đã lấy đến cuối đoạn văn bản, chúng ta sẽ dừng lại để tránh tạo thêm chunk rỗng
                if (start + length >= segment.Length)
                {
                    break;
                }
            }

            return parts;
        }

        // 
        private static string Tail(string value, int length)
        {
            // nếu length <= 0 hoặc value là null/empty/whitespace,
            // chúng ta sẽ trả về chuỗi rỗng vì không có gì để lấy hoặc không có ý nghĩa khi lấy phần đuôi của một chuỗi
            if (length <= 0 || string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            // nếu độ dài của value <= length, chúng ta sẽ trả về toàn bộ value đã được trim,
            if (value.Length <= length)
            {
                return value.Trim();
            }

            return value.Substring(value.Length - length, length).Trim();
        }

        // Đẩy nội dung hiện tại vào danh sách chunks nếu có, sau đó xóa nội dung hiện tại
        // “Hoàn tất chunk hiện tại → lưu vào kết quả → reset để bắt đầu chunk mới”
        private static void FlushCurrent(ICollection<string> chunks, StringBuilder current)
        {
            if (current.Length == 0)
            {
                return;
            }

            var chunk = current.ToString().Trim();
            if (chunk.Length > 0)
            {
                chunks.Add(chunk);
            }

            current.Clear();
        }
    }
}
