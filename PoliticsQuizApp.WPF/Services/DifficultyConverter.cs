using System;
using System.Globalization;
using System.Windows.Data;

namespace PoliticsQuizApp.WPF.Services // Chú ý namespace
{
    public class DifficultyConverter : IValueConverter
    {
        // Chuyển từ Số (Database) sang Chữ (Giao diện)
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is byte level) // Vì trong DB Difficulty là tinyint (byte)
            {
                switch (level)
                {
                    case 1: return "Dễ";
                    case 2: return "Trung bình";
                    case 3: return "Khó";
                    default: return "Không xác định";
                }
            }
            return "";
        }

        // Chuyển ngược lại (không dùng trong trường hợp này nhưng bắt buộc phải có)
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}