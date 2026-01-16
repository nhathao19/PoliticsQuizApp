using PoliticsQuizApp.Data;
using PoliticsQuizApp.Data.Models;
using System.Collections.Generic;
using System.Linq;

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