using GrammarNazi.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace GrammarNazi.Domain.Services
{
    public interface IChatConfigurationService
    {
        Task AddConfiguration(ChatConfiguration chatConfiguration);

        Task Delete(ChatConfiguration chatConfiguration);

        Task Update(ChatConfiguration chatConfiguration);

        Task<ChatConfiguration> GetConfigurationByChatId(long chatId);
    }
}
