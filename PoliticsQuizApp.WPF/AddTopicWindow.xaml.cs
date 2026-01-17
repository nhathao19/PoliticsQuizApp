using System.Windows;

namespace PoliticsQuizApp.WPF
{
    public partial class AddTopicWindow : Window
    {
        public string NewTopicName { get; private set; }
        public AddTopicWindow() { InitializeComponent(); txtTopicName.Focus(); }
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtTopicName.Text))
            {
                NewTopicName = txtTopicName.Text.Trim();
                DialogResult = true;
            }
        }
    }
}