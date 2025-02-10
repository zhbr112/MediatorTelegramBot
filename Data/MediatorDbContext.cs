using MediatorTelegramBot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediatorTelegramBot.Data;

public class MediatorDbContext:DbContext
{
    public DbSet<Mediator> Mediators { get; set; }

    public DbSet<Admin> Admins { get; set; }

    public DbSet<ProcessAddMediator> ProcessAddMediators { get; set; }

    public MediatorDbContext(DbContextOptions options) : base(options)
    {
        //Database.EnsureCreated();
        //Database.Migrate();
    }

    //protected override void OnModelCreating(ModelBuilder modelBuilder)
    //{
    //    modelBuilder.Entity<Admin>().HasData(
    //        new Admin { TelegramId = 852796737 },
    //        new Admin { TelegramId = 731317190 }
    //        );

    //    modelBuilder.Entity<Mediator>().HasData(
    //        new Mediator
    //        {
    //            Name = "Степанова Наталья",
    //            Description = "Судебный юрист, профессиональный медиатор.\r\n Более 20-ти лет работы по юридической специальности, имеет дополнительное образование в сфере закупок. \r\n\r\nПоможет в решении  конфликтов по следующим видам споров:",
    //            Phone = "+79500904344",
    //            Tags = ["Бизнес споры", "Семейные споры", "Трудовые споры"]
    //        },
    //        new Mediator
    //        {
    //            Name = "Коконова Наталья",
    //            Description = "Юрист , профессиональный медиатор.\r\nОпыт работы более 15 лет в сфере ЖКХ, спорах по защите прав потребителей. \r\nБольшой опыт работы в Управляющих компаниях,  как в штате, так и на аутсорсинге.\r\nПоможет в решении конфликтных ситуации в различных сферах жизни:",
    //            Phone = "+77777777777",
    //            Tags = ["Семейные споры", "Споры в учебных учреждениях", "Трудовые споры"]
    //        },
    //        new Mediator
    //        {
    //            Name = "Мякошина Юлия",
    //            Description = "Судебный юрист, профессиональный медиатор.\r\n Более 20-ти лет работы по юридической специальности, имеет большой опыт работы с органами власти\r\n\r\nПоможет в решении  конфликтов по следующим видам споров:",
    //            Phone = "+79027658729",
    //            Tags = ["Бизнес споры", "Семейные споры", "Трудовые споры"]
    //        },
    //        new Mediator
    //        {
    //            Name = "Евгения Мироманова",
    //            Description = "Юрист, профессиональный медиатор, эксперт по спорам потребителей и предпринимателей.\r\n\r\n17 лет в сфере защиты прав потребителей, из них 12  руководитель консультационного центра в Учреждении Роспотребнадзора. \r\n\r\nПоможет в вашем конфликте с помощью проведения процедуры медиации или проведет подготовку стороны к переговорам по следующим видам споров: ",
    //            Phone = "+79086441041",
    //            Tags = ["Потребительские споры", "Семейные споры", "Бизнес споры"]
    //        }
    //        );
    //}
}
