using Microsoft.AspNetCore.Identity;
using MyLocalGov.com.Models;
using MyLocalGov.com.Repositories.Interfaces;

namespace MyLocalGov.com.Data
{
    public static class TestDataSeeder
    {
        public static async Task SeedAsync(
            IUnitOfWork unitOfWork,
            UserManager<IdentityUser> userManager)
        {
            // Seed admin user
            var adminEmail = "admin@test.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail
                };
                await userManager.CreateAsync(adminUser, "Password123!");
                await userManager.AddToRoleAsync(adminUser, "Admin");

                var adminProfile = new UserProfileModel
                {
                    UserID = adminUser.Id,
                    FirstName = "Admin",
                    LastName = "Guy",
                    PreferencesJson = "{}",
                    User = adminUser
                };
                await unitOfWork.UserProfiles.CreateProfileForUserAsync(adminProfile);
                await unitOfWork.SaveAsync();
            }

            // Seed themed citizen user: Hiccup Haddock
            var hiccupEmail = "hiccup@berk.com";
            var hiccupUser = await userManager.FindByEmailAsync(hiccupEmail);
            if (hiccupUser == null)
            {
                hiccupUser = new IdentityUser
                {
                    UserName = hiccupEmail,
                    Email = hiccupEmail
                };
                await userManager.CreateAsync(hiccupUser, "Toothless123!");
                await userManager.AddToRoleAsync(hiccupUser, "Citizen");

                var hiccupProfile = new UserProfileModel
                {
                    UserID = hiccupUser.Id,
                    FirstName = "Hiccup",
                    LastName = "Haddock",
                    DefaultAddressLine = "Chief's House",
                    DefaultSuburb = "Berk Village",
                    DefaultCity = "Berk",
                    DefaultPostalCode = "0001",
                    DefaultLatitude = 64.9631,
                    DefaultLongitude = -19.0208,
                    PreferencesJson = "{\"dragon\":\"Toothless\"}",
                    User = hiccupUser
                };
                await unitOfWork.UserProfiles.CreateProfileForUserAsync(hiccupProfile);
                await unitOfWork.SaveAsync();

                // Seed 3 themed issues for Hiccup
                var issues = new List<IssueModel>
                {
                    new IssueModel
                    {
                        ReporterUserID = hiccupUser.Id,
                        Address = "Dragon Arena",
                        Latitude = 64.9632,
                        Longitude = -19.0210,
                        CategoryID = 1,
                        Description = "Arena gate damaged after Night Fury landing.",
                        StatusID = 1,
                        Priority = 2,
                        DateReported = DateTime.UtcNow.AddDays(-2),
                        LastUpdated = DateTime.UtcNow.AddDays(-1)
                    },
                    new IssueModel
                    {
                        ReporterUserID = hiccupUser.Id,
                        Address = "Village Square",
                        Latitude = 64.9633,
                        Longitude = -19.0212,
                        CategoryID = 2,
                        Description = "Fish supplies running low after dragon feast.",
                        StatusID = 1,
                        Priority = 3,
                        DateReported = DateTime.UtcNow.AddDays(-3),
                        LastUpdated = DateTime.UtcNow.AddDays(-2)
                    },
                    new IssueModel
                    {
                        ReporterUserID = hiccupUser.Id,
                        Address = "Cliffside",
                        Latitude = 64.9634,
                        Longitude = -19.0214,
                        CategoryID = 3,
                        Description = "Broken catapult from last dragon training session.",
                        StatusID = 2,
                        Priority = 1,
                        DateReported = DateTime.UtcNow.AddDays(-1),
                        LastUpdated = DateTime.UtcNow
                    }
                };

                foreach (var issue in issues)
                {
                    await unitOfWork.Issues.AddAsync(issue);
                }
                await unitOfWork.SaveAsync();
            }
        }
    }
}