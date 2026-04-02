using Microsoft.Extensions.Configuration;
using PlantDecor.BusinessLogicLayer.Interfaces;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class EmbeddingTextPreprocessor : IEmbeddingTextPreprocessor
    {
        /* HtmlTagRegex giải thích:
         <       : bắt đầu thẻ
         [^>]    : bất kỳ ký tự nào KHÔNG phải '>'
         +       : 1 hoặc nhiều lần
         >       : kết thúc thẻ
         */
        private static readonly Regex HtmlTagRegex = new("<[^>]+>", RegexOptions.Compiled);
        /* MultiSpaceRegex giải thích:
         [ ... ] : tập ký tự
         (space) : dấu cách
         \t      : tab
         \f      : form feed
         \v      : vertical tab
         +       : lặp 1 hoặc nhiều lần
         */
        private static readonly Regex MultiSpaceRegex = new("[ \t\f\v]+", RegexOptions.Compiled);
        /* MultiNewLineRegex giải thích:
         \n     : xuống dòng
         {3,}   : ít nhất 3 lần
         */
        private static readonly Regex MultiNewLineRegex = new("\n{3,}", RegexOptions.Compiled);
        private readonly bool _enableCleaning;

        public EmbeddingTextPreprocessor(IConfiguration configuration)
        {
            _enableCleaning = configuration.GetValue<bool>("EmbeddingProcessingSettings:EnableCleaning", true);
        }

        public string Preprocess(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var normalized = text.Replace("\r\n", "\n").Replace("\r", "\n");

            if (!_enableCleaning)
            {
                return normalized.Trim();
            }

            // vd cho Decode: &nbsp; → space
            //&lt; → <
            //&gt; → >
            normalized = WebUtility.HtmlDecode(normalized);
            // Thay thế tất cả thẻ HTML bằng một dấu cách để tránh dính các từ lại với nhau sau khi loại bỏ thẻ
            normalized = HtmlTagRegex.Replace(normalized, " ");
            // Thay thế space đặc biệt của HTML (U+00A0) bằng space thông thường để tránh các vấn đề về khoảng trắng không mong muốn
            normalized = normalized.Replace('\u00A0', ' ');
            // Chuẩn hóa Unicode để đảm bảo các ký tự được biểu diễn nhất quán, tránh trường hợp cùng một ký tự nhưng có nhiều cách biểu diễn khác nhau
            // ví dụ: chữ "é" có thể được biểu diễn dưới dạng một ký tự duy nhất (U+00E9) hoặc kết hợp giữa "e" (U+0065) và dấu sắc (U+0301).
            // Việc chuẩn hóa sẽ giúp đảm bảo rằng cả hai cách biểu diễn này đều được xử lý như nhau trong quá trình tạo embedding.
            normalized = normalized.Normalize(NormalizationForm.FormC);

            // Thay thế nhiều khoảng trắng liên tiếp bằng một khoảng trắng duy nhất, đồng thời loại bỏ khoảng trắng ở đầu và cuối mỗi dòng
            normalized = MultiSpaceRegex.Replace(normalized, " ");
            // Xóa space ở đầu và cuối mỗi dòng, đồng thời chuẩn hóa xuống dòng để tránh trường hợp có nhiều khoảng trắng trước hoặc sau dấu xuống
            normalized = Regex.Replace(normalized, "\n[ \t]+", "\n");
            // Giữ tối đa 2 xuống dòng liên tiếp để tránh tạo ra quá nhiều khoảng trắng dọc trong embedding, điều này có thể ảnh hưởng đến chất lượng embedding và hiệu suất lưu trữ
            normalized = MultiNewLineRegex.Replace(normalized, "\n\n");

            return normalized.Trim();
        }
    }
}
