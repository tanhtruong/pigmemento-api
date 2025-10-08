using Pigmemento.Api.Models;

namespace Pigmemento.Api.Data
{
    public static class AppSeeder
    {
        public static void Seed(AppDbContext context)
        {
            // ✅ Only seed if database is empty
            if (!context.Cases.Any())
            {
                var cases = new List<Case>
                {
                    new Case
                    {
                        ImageUrl = "https://example.com/case1.jpg",
                        Label = "benign",
                        Difficulty = "easy",
                        Patient = new Patient
                        {
                            Age = 45,
                            Site = "Back"
                        },
                        Metadata = "HAM10000"
                    },
                    new Case
                    {
                        ImageUrl = "https://example.com/case2.jpg",
                        Label = "malignant",
                        Difficulty = "medium",
                        Patient = new Patient
                        {
                            Age = 60,
                            Site = "Leg"
                        },
                        Metadata = "ISIC2020"
                    }
                };

                context.Cases.AddRange(cases);
                context.SaveChanges();
            }

            // Example: seed an admin user
            if (!context.Users.Any())
            {
                var admin = new User
                {
                    Email = "admin@pigmemento.app",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                    Role = "admin"
                };

                context.Users.Add(admin);
                context.SaveChanges();
            }
        }
    }
}