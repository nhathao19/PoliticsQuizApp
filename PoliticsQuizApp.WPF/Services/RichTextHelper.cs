using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace PoliticsQuizApp.WPF
{
    // Class này giúp RichTextBox hiểu được các thẻ <b>, <i>, <u>, <br>
    public static class RichTextHelper
    {
        public static readonly DependencyProperty FormattedTextProperty =
            DependencyProperty.RegisterAttached(
                "FormattedText",
                typeof(string),
                typeof(RichTextHelper),
                new FrameworkPropertyMetadata(string.Empty, OnFormattedTextChanged));

        public static string GetFormattedText(DependencyObject obj)
        {
            return (string)obj.GetValue(FormattedTextProperty);
        }

        public static void SetFormattedText(DependencyObject obj, string value)
        {
            obj.SetValue(FormattedTextProperty, value);
        }

        private static void OnFormattedTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RichTextBox rtb)
            {
                string text = (string)e.NewValue;
                rtb.Document = ConvertToFlowDocument(text);
            }
        }

        // Hàm phân tích cú pháp đơn giản (Hỗ trợ <b>, <i>, <u>, <br>)
        private static FlowDocument ConvertToFlowDocument(string text)
        {
            var doc = new FlowDocument();
            var para = new Paragraph(); // Paragraph mặc định có Margin, ta sẽ chỉnh trong XAML

            if (string.IsNullOrEmpty(text))
            {
                doc.Blocks.Add(para);
                return doc;
            }

            // Tách chuỗi dựa trên các thẻ
            // Regex này sẽ tách ra: "Text trước", "<b>", "Text đậm", "</b>", "Text sau"...
            string pattern = @"(<b>|</b>|<i>|</i>|<u>|</u>|<br>|<br/>)";
            var parts = Regex.Split(text, pattern, RegexOptions.IgnoreCase);

            // Trạng thái hiện tại
            bool isBold = false;
            bool isItalic = false;
            bool isUnderline = false;

            foreach (var part in parts)
            {
                string lowerPart = part.ToLower();

                switch (lowerPart)
                {
                    case "<b>": isBold = true; break;
                    case "</b>": isBold = false; break;
                    case "<i>": isItalic = true; break;
                    case "</i>": isItalic = false; break;
                    case "<u>": isUnderline = true; break;
                    case "</u>": isUnderline = false; break;
                    case "<br>":
                    case "<br/>":
                        para.Inlines.Add(new LineBreak());
                        break;
                    default:
                        // Đây là nội dung văn bản
                        if (!string.IsNullOrEmpty(part))
                        {
                            Run run = new Run(part);
                            if (isBold) run.FontWeight = FontWeights.Bold;
                            if (isItalic) run.FontStyle = FontStyles.Italic;
                            if (isUnderline) run.TextDecorations = TextDecorations.Underline;

                            para.Inlines.Add(run);
                        }
                        break;
                }
            }

            doc.Blocks.Add(para);

            // Xóa padding mặc định của FlowDocument để nó gọn gàng như TextBlock
            doc.PagePadding = new Thickness(0);
            return doc;
        }
    }
}