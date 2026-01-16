using PoliticsQuizApp.Data.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PoliticsQuizApp.WPF.ViewModels
{
    public class QuestionViewModel : INotifyPropertyChanged
    {
        public Question QuestionData { get; set; }
        public int Index { get; set; }
        public List<Answer> Answers { get; set; }

        private bool _isFlagged;
        public bool IsFlagged
        {
            get => _isFlagged;
            set { _isFlagged = value; OnPropertyChanged(); }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        private long? _userSelectedAnswerId;
        public long? UserSelectedAnswerId
        {
            get => _userSelectedAnswerId;
            set
            {
                _userSelectedAnswerId = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsAnswered)); // Cập nhật trạng thái IsAnswered
            }
        }

        // --- KHẮC PHỤC LỖI CS0200 TẠI ĐÂY ---

        // Biến này dùng để "ép" trạng thái (Dùng cho chế độ Xem lại bài thi)
        private bool? _forceAnsweredStatus;

        public bool IsAnswered
        {
            get
            {
                // 1. Ưu tiên: Nếu đang ở chế độ Review (đã bị set giá trị), trả về giá trị đó
                if (_forceAnsweredStatus.HasValue)
                    return _forceAnsweredStatus.Value;

                // 2. Mặc định: Chế độ làm bài thi -> Tự động tính dựa trên việc đã chọn đáp án chưa
                return UserSelectedAnswerId.HasValue;
            }
            set
            {
                // Cho phép gán giá trị từ bên ngoài (Dùng trong ReportService)
                if (_forceAnsweredStatus != value)
                {
                    _forceAnsweredStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}