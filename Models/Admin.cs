using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediatorTelegramBot.Models;

public class Admin
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public long TelegramId { get; set; }
}
