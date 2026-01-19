using PoliticsQuizApp.Data;
using PoliticsQuizApp.Data.Models;

namespace PoliticsQuizApp.WPF.Services
{
    public class TopicService
    {
        private readonly PoliticsQuizDbContext _context;

        public TopicService()
        {
            _context = new PoliticsQuizDbContext();
        }

        public List<Topic> GetAllTopics()
        {
            return _context.Topics.ToList();
        }
    }
}