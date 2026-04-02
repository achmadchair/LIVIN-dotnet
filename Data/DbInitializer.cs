using Livin.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Livin.Api.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            context.Database.Migrate();

            var requiredSites = new[] { "Batam", "Belawan", "Dumai", "Jetty Tuban", "Lhokseumawe", "Lampung", "Pontianak", "Lhoknga" };
            foreach (var sn in requiredSites)
            {
                var existing = context.Sites.FirstOrDefault(s => s.Name.ToLower() == sn.ToLower() || (sn == "Jetty Tuban" && s.Name == "Jetty_Tuban"));
                if (existing == null)
                {
                    context.Sites.Add(new Site { Name = sn });
                }
                else if (existing.Name == "Jetty_Tuban" && sn == "Jetty Tuban")
                {
                    existing.Name = "Jetty Tuban";
                    context.Sites.Update(existing);
                }
            }
            context.SaveChanges();

            if (context.Users.Any())
            {
                // Ensure khoir exists if DB was already seeded
                if (!context.Users.Any(u => u.Username == "khoir"))
                {
                    var site = context.Sites.FirstOrDefault();
                    if (site != null)
                    {
                        context.Users.Add(new User { Username = "khoir", PasswordHash = "123", Role = UserRole.Leader, SiteId = site.Id });
                        context.SaveChanges();
                    }
                }
                return; // DB has been seeded with data initially
            }

            var sites = context.Sites.ToList();

            var users = new User[]
            {
                new User { Username = "leader_batam", PasswordHash = "password123", Role = UserRole.Leader, SiteId = sites[0].Id },
                new User { Username = "inspector_batam", PasswordHash = "password123", Role = UserRole.Inspector, SiteId = sites[0].Id },
                new User { Username = "inspector_belawan", PasswordHash = "password123", Role = UserRole.Inspector, SiteId = sites[1].Id },
                new User { Username = "khoir", PasswordHash = "123", Role = UserRole.Leader, SiteId = sites[0].Id }
            };

            context.Users.AddRange(users);
            context.SaveChanges();

            var equipment = new Equipment { HACCode = "HAC-BTM-001", Name = "Pump A", SiteId = sites[0].Id };
            context.Equipments.Add(equipment);
            context.SaveChanges();

            var part = new Part { HACCode = "HAC-BTM-001", Name = "Engine", EquipmentId = equipment.Id, SiteId = sites[0].Id };
            context.Parts.Add(part);
            context.SaveChanges();

            var task = new InspectionTask { Description = "Check Oil Level", PartId = part.Id };
            context.InspectionTasks.Add(task);
            context.SaveChanges();

            var standard = new TaskStandard { StandardText = "Oil level must be above 50%", InspectionTaskId = task.Id };
            context.TaskStandards.Add(standard);
            context.SaveChanges();
        }
    }
}
