using Microsoft.EntityFrameworkCore;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class UserPreferenceService : IUserPreferenceService
    {
        private readonly PlantDecorContext _context;
        private static readonly Dictionary<int, decimal> ActionScoreMap = new()
        {
            [(int)UserActionTypeEnum.ViewDetails] = 1m,
            [(int)UserActionTypeEnum.Search] = 2m,
            [(int)UserActionTypeEnum.AddToWishlist] = 4m,
            [(int)UserActionTypeEnum.AddToCart] = 6m
        };

        public UserPreferenceService(PlantDecorContext context)
        {
            _context = context;
        }

        Task<CustomerSurveyResponseDto?> IUserPreferenceService.GetCustomerSurveyAsync(int userId)
            => GetCustomerSurveyAsync(userId);

        Task<CustomerSurveyResponseDto> IUserPreferenceService.UpsertCustomerSurveyAsync(int userId, CustomerSurveyUpsertRequestDto request)
            => UpsertCustomerSurveyAsync(userId, request);

        public async Task CalculatedAllUserPreferenceAsync()
        {
            var users = await _context.Users
                .Include(u => u.UserProfile)
                .Include(u => u.CustomerSurvey)
                .Where(u => u.Status == (int)UserStatusEnum.Active && u.RoleId == (int)RoleEnum.Customer)
                .ToListAsync();

            await CalculatePreferencesForUsersAsync(users);
        }

        public async Task CalculateUserPreferenceForUserAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.UserProfile)
                .Include(u => u.CustomerSurvey)
                .FirstOrDefaultAsync(u => u.Id == userId && u.Status == (int)UserStatusEnum.Active && u.RoleId == (int)RoleEnum.Customer);

            if (user == null)
            {
                throw new NotFoundException($"Active customer with id {userId} was not found");
            }

            await CalculatePreferencesForUsersAsync(new List<User> { user });
        }

        private async Task CalculatePreferencesForUsersAsync(List<User> users)
        {
            if (users.Count == 0)
            {
                return;
            }

            var now = DateTime.UtcNow;
            var thirtyDaysAgo = now.AddDays(-30);
            var targetUserIds = users.Select(u => u.Id).ToHashSet();

            var plants = await _context.Plants
                .Where(p => p.IsActive == true)
                .ToListAsync();

            var behaviorLogs = await _context.UserBehaviorLogs
                .Where(log => log.CreatedAt >= thirtyDaysAgo && log.UserId != null && targetUserIds.Contains(log.UserId.Value) && log.PlantId != null && log.ActionType != null)
                .ToListAsync();

            var completedOrderPlants = await _context.NurseryOrderDetails
                .Where(od => od.NurseryOrder != null && od.NurseryOrder.Order != null && targetUserIds.Contains(od.NurseryOrder.Order.UserId) && od.NurseryOrder.Order.Status == (int)OrderStatusEnum.Completed)
                .Select(od => new
                {
                    UserId = od.NurseryOrder.Order.UserId,
                    PlantId = od.CommonPlant != null
                        ? od.CommonPlant.PlantId
                        : (od.PlantInstance != null ? od.PlantInstance.PlantId : (int?)null)
                })
                .Where(x => x.PlantId != null)
                .Select(x => new { x.UserId, PlantId = x.PlantId!.Value })
                .Distinct()
                .ToListAsync();

            var existingPreferences = await _context.UserPreferences
                .Where(p => p.UserId != null && targetUserIds.Contains(p.UserId.Value))
                .ToListAsync();

            var behaviorScoreByUserPlant = behaviorLogs
                .GroupBy(log => (UserId: log.UserId!.Value, PlantId: log.PlantId!.Value))
                .ToDictionary(
                    g => g.Key,
                    g => Math.Min(10m, g.Sum(log => ActionScoreMap.TryGetValue(log.ActionType!.Value, out var score) ? score : 0m))
                );

            var purchasedPairs = completedOrderPlants
                .Select(x => (x.UserId, x.PlantId))
                .ToHashSet();

            var preferenceMap = existingPreferences
                .Where(p => p.UserId != null && p.PlantId != null)
                .ToDictionary(p => (p.UserId!.Value, p.PlantId!.Value));

            var surveyByUserId = users
                .Where(u => u.CustomerSurvey != null)
                .ToDictionary(u => u.Id, u => u.CustomerSurvey!);

            var toInsert = new List<UserPreference>();
            var toRemove = new List<UserPreference>();

            foreach (var user in users)
            {
                int? userElement = null;
                if (user.UserProfile?.BirthYear != null)
                {
                    try
                    {
                        userElement = GetFengShuiElement(user.UserProfile.BirthYear.Value);
                    }
                    catch
                    {
                        userElement = null;
                    }
                }

                foreach (var plant in plants)
                {
                    // 1. Profile Match Score
                    decimal profileScore = 0;
                    surveyByUserId.TryGetValue(user.Id, out var survey);

                    if (survey != null)
                    {
                        profileScore += ScoreBySurveyExperience(survey.ExperienceLevel, plant.CareLevelType);
                        profileScore += ScoreBySurveyPlacement(survey.PreferredPlacement, plant.PlacementType);
                        profileScore += ScoreBySurveyBudget(survey.MaxBudget, plant.BasePrice);

                        if (survey.HasPets == true)
                        {
                            profileScore += plant.PetSafe == true ? 2m : -2m;
                        }

                        if (survey.HasChildren == true)
                        {
                            profileScore += plant.ChildSafe == true ? 2m : -2m;
                        }
                    }
                    else
                    {
                        if (IsEasyCare(plant.CareLevelType)) profileScore += 3;
                        if (plant.ChildSafe == true || plant.PetSafe == true) profileScore += 2;
                    }

                    profileScore += CalculateFengShuiScore(userElement, plant.FengShuiElement);

                    if (profileScore > 10) profileScore = 10;
                    if (profileScore < 0) profileScore = 0;

                    // 2. Behavior Score
                    behaviorScoreByUserPlant.TryGetValue((user.Id, plant.Id), out var behaviorScore);

                    // 3. Purchase History Score
                    decimal purchaseScore = purchasedPairs.Contains((user.Id, plant.Id)) ? 10 : 0;

                    // 4. Preference Score
                    decimal preferenceScore = (profileScore * 0.2m) + (behaviorScore * 0.4m) + (purchaseScore * 0.4m);
                    var key = (user.Id, plant.Id);
                    preferenceMap.TryGetValue(key, out var existingPref);

                    bool hasInteraction = behaviorScore > 0 || purchaseScore > 0;
                    bool isHighProfileMatch = profileScore >= 6;

                    if (hasInteraction || isHighProfileMatch)
                    {
                        if (existingPref != null)
                        {
                            existingPref.ProfileMatchScore = profileScore;
                            existingPref.BehaviorScore = behaviorScore;
                            existingPref.PurchaseHistoryScore = purchaseScore;
                            existingPref.PreferenceScore = preferenceScore;
                            existingPref.LastCalculated = now;
                        }
                        else
                        {
                            toInsert.Add(new UserPreference
                            {
                                UserId = user.Id,
                                PlantId = plant.Id,
                                ProfileMatchScore = profileScore,
                                BehaviorScore = behaviorScore,
                                PurchaseHistoryScore = purchaseScore,
                                PreferenceScore = preferenceScore,
                                LastCalculated = now
                            });
                        }
                    }
                    else if (existingPref != null)
                    {
                        toRemove.Add(existingPref);
                    }
                }
            }

            if (toRemove.Count > 0)
            {
                _context.UserPreferences.RemoveRange(toRemove);
            }

            if (toInsert.Count > 0)
            {
                await _context.UserPreferences.AddRangeAsync(toInsert);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<UserPreferenceRecommendationResponseDto>> GetTopRecommendationsAsync(int userId, int limit)
        {
            limit = Math.Clamp(limit, 1, 50);

            return await _context.UserPreferences
                .AsNoTracking()
                .Where(p => p.UserId == userId && p.PreferenceScore > 0 && p.Plant != null && p.Plant.IsActive == true)
                .OrderByDescending(p => p.PreferenceScore)
                .ThenByDescending(p => p.BehaviorScore)
                .ThenByDescending(p => p.PurchaseHistoryScore)
                .Take(limit)
                .Select(p => new UserPreferenceRecommendationResponseDto
                {
                    PlantId = p.PlantId ?? 0,
                    PlantName = p.Plant!.Name,
                    PrimaryImageUrl = p.Plant.PlantImages
                        .OrderByDescending(img => img.IsPrimary == true)
                        .ThenBy(img => img.Id)
                        .Select(img => img.ImageUrl)
                        .FirstOrDefault(),
                    BasePrice = p.Plant.BasePrice,
                    CareLevelTypeName = GetCareLevelName(p.Plant.CareLevelType),
                    FengShuiElement = p.Plant.FengShuiElement,
                    PreferenceScore = p.PreferenceScore ?? 0,
                    ProfileMatchScore = p.ProfileMatchScore ?? 0,
                    BehaviorScore = p.BehaviorScore ?? 0,
                    PurchaseHistoryScore = p.PurchaseHistoryScore ?? 0
                })
                .ToListAsync();
        }

        public async Task<List<UserPreferenceRecommendationResponseDto>> GetContextualRecommendationsAsync(int userId, int limit, int? seedPlantId = null)
        {
            limit = Math.Clamp(limit, 1, 50);

            var now = DateTime.UtcNow;
            var thirtyDaysAgo = now.AddDays(-30);

            var recentBehavior = await _context.UserBehaviorLogs
                .AsNoTracking()
                .Where(log => log.UserId == userId && log.CreatedAt >= thirtyDaysAgo && log.PlantId != null)
                .ToListAsync();

            var behaviorSeedIds = recentBehavior
                .GroupBy(log => log.PlantId!.Value)
                .Select(g => new
                {
                    PlantId = g.Key,
                    Score = g.Sum(x => ActionScoreMap.TryGetValue(x.ActionType ?? 0, out var actionScore) ? actionScore : 0m),
                    LastSeenAt = g.Max(x => x.CreatedAt)
                })
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.LastSeenAt)
                .Take(5)
                .Select(x => x.PlantId);

            var seedPlantIds = behaviorSeedIds.ToHashSet();
            if (seedPlantId.HasValue && seedPlantId.Value > 0)
            {
                seedPlantIds.Add(seedPlantId.Value);
            }

            if (seedPlantIds.Count == 0)
            {
                return await GetTopRecommendationsAsync(userId, limit);
            }

            var seedPlants = await _context.Plants
                .AsNoTracking()
                .Include(p => p.Categories)
                .Include(p => p.Tags)
                .Where(p => seedPlantIds.Contains(p.Id) && p.IsActive == true)
                .ToListAsync();

            if (seedPlants.Count == 0)
            {
                return await GetTopRecommendationsAsync(userId, limit);
            }

            var seedCareLevels = seedPlants
                .Where(p => p.CareLevelType.HasValue)
                .Select(p => p.CareLevelType!.Value)
                .ToHashSet();

            var seedElements = seedPlants
                .Where(p => p.FengShuiElement.HasValue)
                .Select(p => p.FengShuiElement!.Value)
                .ToHashSet();

            var seedCategoryIds = seedPlants
                .SelectMany(p => p.Categories.Select(c => c.Id))
                .ToHashSet();

            var seedTagIds = seedPlants
                .SelectMany(p => p.Tags.Select(t => t.Id))
                .ToHashSet();

            var preferenceByPlant = await _context.UserPreferences
                .AsNoTracking()
                .Where(p => p.UserId == userId && p.PlantId != null)
                .ToDictionaryAsync(p => p.PlantId!.Value, p => p);

            var survey = await _context.CustomerSurveys
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == userId);

            var candidates = await _context.Plants
                .AsNoTracking()
                .Include(p => p.Categories)
                .Include(p => p.Tags)
                .Include(p => p.PlantImages)
                .Where(p => p.IsActive == true && !seedPlantIds.Contains(p.Id))
                .ToListAsync();

            var ranked = candidates
                .Select(plant =>
                {
                    decimal contextualScore = 0m;

                    if (plant.CareLevelType.HasValue && seedCareLevels.Contains(plant.CareLevelType.Value))
                    {
                        contextualScore += 3m;
                    }

                    if (plant.FengShuiElement.HasValue && seedElements.Contains(plant.FengShuiElement.Value))
                    {
                        contextualScore += 3m;
                    }

                    if (survey != null)
                    {
                        contextualScore += ScoreBySurveyPlacement(survey.PreferredPlacement, plant.PlacementType) * 0.5m;
                        contextualScore += ScoreBySurveyBudget(survey.MaxBudget, plant.BasePrice) * 0.5m;

                        if (survey.HasPets == true && plant.PetSafe != true)
                        {
                            contextualScore -= 1m;
                        }

                        if (survey.HasChildren == true && plant.ChildSafe != true)
                        {
                            contextualScore -= 1m;
                        }
                    }

                    if (plant.Categories.Any(c => seedCategoryIds.Contains(c.Id)))
                    {
                        contextualScore += 2m;
                    }

                    if (plant.Tags.Any(t => seedTagIds.Contains(t.Id)))
                    {
                        contextualScore += 2m;
                    }

                    preferenceByPlant.TryGetValue(plant.Id, out var pref);
                    var preferenceBoost = Math.Min(2m, (pref?.PreferenceScore ?? 0m) * 0.2m);
                    contextualScore += preferenceBoost;

                    return new
                    {
                        Plant = plant,
                        ContextualScore = contextualScore,
                        Preference = pref
                    };
                })
                .Where(x => x.ContextualScore > 0)
                .OrderByDescending(x => x.ContextualScore)
                .ThenByDescending(x => x.Preference?.PreferenceScore ?? 0)
                .Take(limit)
                .ToList();

            if (ranked.Count == 0)
            {
                return await GetTopRecommendationsAsync(userId, limit);
            }

            return ranked
                .Select(x => new UserPreferenceRecommendationResponseDto
                {
                    PlantId = x.Plant.Id,
                    PlantName = x.Plant.Name,
                    PrimaryImageUrl = x.Plant.PlantImages
                        .OrderByDescending(img => img.IsPrimary == true)
                        .ThenBy(img => img.Id)
                        .Select(img => img.ImageUrl)
                        .FirstOrDefault(),
                    BasePrice = x.Plant.BasePrice,
                    CareLevelTypeName = GetCareLevelName(x.Plant.CareLevelType),
                    FengShuiElement = x.Plant.FengShuiElement,
                    PreferenceScore = Math.Round(x.ContextualScore, 2),
                    ProfileMatchScore = x.Preference?.ProfileMatchScore ?? 0,
                    BehaviorScore = x.Preference?.BehaviorScore ?? 0,
                    PurchaseHistoryScore = x.Preference?.PurchaseHistoryScore ?? 0
                })
                .ToList();
        }

        public async Task<CustomerSurveyResponseDto?> GetCustomerSurveyAsync(int userId)
        {
            var survey = await _context.CustomerSurveys
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (survey == null)
            {
                return null;
            }

            return new CustomerSurveyResponseDto
            {
                Id = survey.Id,
                UserId = survey.UserId,
                HasPets = survey.HasPets,
                HasChildren = survey.HasChildren,
                MaxBudget = survey.MaxBudget,
                ExperienceLevel = survey.ExperienceLevel,
                ExperienceLevelName = ((DataAccessLayer.Enums.CareLevelTypeEnum)survey.ExperienceLevel).ToString(),
                PreferredPlacement = survey.PreferredPlacement,
                PreferredPlacementName = ((DataAccessLayer.Enums.PlacementTypeEnum)survey.PreferredPlacement).ToString(),
                CreatedAt = survey.CreatedAt,
                UpdatedAt = survey.UpdatedAt
            };
        }

        public async Task<CustomerSurveyResponseDto> UpsertCustomerSurveyAsync(int userId, CustomerSurveyUpsertRequestDto request)
        {
            var userExists = await _context.Users
                .AnyAsync(u => u.Id == userId && u.Status == (int)UserStatusEnum.Active && u.RoleId == (int)RoleEnum.Customer);

            if (!userExists)
            {
                throw new NotFoundException($"Active customer with id {userId} was not found");
            }

            var survey = await _context.CustomerSurveys.FirstOrDefaultAsync(s => s.UserId == userId);
            var now = DateTime.UtcNow;

            if (survey == null)
            {
                survey = new CustomerSurvey
                {
                    UserId = userId,
                    HasPets = request.HasPets,
                    HasChildren = request.HasChildren,
                    MaxBudget = request.MaxBudget,
                    ExperienceLevel = request.ExperienceLevel,
                    PreferredPlacement = request.PreferredPlacement,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await _context.CustomerSurveys.AddAsync(survey);
            }
            else
            {
                survey.HasPets = request.HasPets;
                survey.HasChildren = request.HasChildren;
                survey.MaxBudget = request.MaxBudget;
                survey.ExperienceLevel = request.ExperienceLevel;
                survey.PreferredPlacement = request.PreferredPlacement;
                survey.UpdatedAt = now;
            }

            await _context.SaveChangesAsync();
            await CalculateUserPreferenceForUserAsync(userId);

            return new CustomerSurveyResponseDto
            {
                Id = survey.Id,
                UserId = survey.UserId,
                HasPets = survey.HasPets,
                HasChildren = survey.HasChildren,
                MaxBudget = survey.MaxBudget,
                ExperienceLevel = survey.ExperienceLevel,
                ExperienceLevelName = ((DataAccessLayer.Enums.CareLevelTypeEnum)survey.ExperienceLevel).ToString(),
                PreferredPlacement = survey.PreferredPlacement,
                PreferredPlacementName = ((DataAccessLayer.Enums.PlacementTypeEnum)survey.PreferredPlacement).ToString(),
                CreatedAt = survey.CreatedAt,
                UpdatedAt = survey.UpdatedAt
            };
        }

        private static int? GetFengShuiElement(int birthYear)
        {
            if (birthYear <= 0)
            {
                return null;
            }

            try
            {
                var mod10 = PositiveModNoDivision(birthYear, 10);
                var mod12 = PositiveModNoDivision(birthYear, 12);

                int canValue = mod10 switch
                {
                    0 or 1 => 4,
                    2 or 3 => 5,
                    4 or 5 => 1,
                    6 or 7 => 2,
                    8 or 9 => 3,
                    _ => 0
                };

                int chiValue = mod12 switch
                {
                    4 or 5 or 10 or 11 => 0,
                    6 or 7 or 0 or 1 => 1,
                    8 or 9 or 2 or 3 => 2,
                    _ => 0
                };

                int result = canValue + chiValue;
                if (result > 5) result -= 5;

                return result switch
                {
                    1 => (int)FengShuiElementTypeEnum.Kim,
                    2 => (int)FengShuiElementTypeEnum.Thuy,
                    3 => (int)FengShuiElementTypeEnum.Hoa,
                    4 => (int)FengShuiElementTypeEnum.Tho,
                    5 => (int)FengShuiElementTypeEnum.Moc,
                    _ => null
                };
            }
            catch (Exception)
            {
                // Prevent a single malformed profile from breaking preference recalculation.
                return null;
            }
        }

        private static int PositiveModNoDivision(int value, int modulus)
        {
            if (modulus <= 0)
            {
                return 0;
            }

            var remainder = Math.Abs(value);
            while (remainder >= modulus)
            {
                remainder -= modulus;
            }

            return remainder;
        }

        private static int CalculateFengShuiScore(int? userElement, int? plantElement)
        {
            if (!userElement.HasValue || !plantElement.HasValue)
                return 0;

            var userValue = userElement.Value;
            var plantValue = plantElement.Value;

            if (userValue == plantValue)
                return 5;

            var kim = (int)FengShuiElementTypeEnum.Kim;
            var moc = (int)FengShuiElementTypeEnum.Moc;
            var thuy = (int)FengShuiElementTypeEnum.Thuy;
            var hoa = (int)FengShuiElementTypeEnum.Hoa;
            var tho = (int)FengShuiElementTypeEnum.Tho;

            bool isTuongSinh =
                (userValue == thuy && plantValue == moc) ||
                (userValue == moc && plantValue == hoa) ||
                (userValue == hoa && plantValue == tho) ||
                (userValue == tho && plantValue == kim) ||
                (userValue == kim && plantValue == thuy) ||
                (plantValue == thuy && userValue == moc) ||
                (plantValue == moc && userValue == hoa) ||
                (plantValue == hoa && userValue == tho) ||
                (plantValue == tho && userValue == kim) ||
                (plantValue == kim && userValue == thuy);

            if (isTuongSinh)
                return 5;

            bool isTuongKhac =
                (userValue == thuy && plantValue == hoa) ||
                (userValue == hoa && plantValue == kim) ||
                (userValue == kim && plantValue == moc) ||
                (userValue == moc && plantValue == tho) ||
                (userValue == tho && plantValue == thuy) ||
                (plantValue == thuy && userValue == hoa) ||
                (plantValue == hoa && userValue == kim) ||
                (plantValue == kim && userValue == moc) ||
                (plantValue == moc && userValue == tho) ||
                (plantValue == tho && userValue == thuy);

            if (isTuongKhac)
                return -2;

            return 0;
        }

        private static bool IsEasyCare(int? careLevelType)
        {
            return careLevelType == (int)CareLevelTypeEnum.Easy;
        }

        private static decimal ScoreBySurveyExperience(int experienceLevel, int? careLevelType)
        {
            if (!careLevelType.HasValue)
            {
                return 0m;
            }

            return experienceLevel switch
            {
                1 => careLevelType == (int)CareLevelTypeEnum.Easy ? 3m : -1m,
                2 => careLevelType == (int)CareLevelTypeEnum.Easy || careLevelType == (int)CareLevelTypeEnum.Medium ? 2m : 0m,
                3 => careLevelType == (int)CareLevelTypeEnum.Hard || careLevelType == (int)CareLevelTypeEnum.Expert ? 2m : 0m,
                4 => 1m,
                _ => 0m
            };
        }

        private static decimal ScoreBySurveyPlacement(int preferredPlacement, int? plantPlacement)
        {
            if (!plantPlacement.HasValue)
            {
                return 0m;
            }

            return preferredPlacement == plantPlacement.Value ? 2m : 0m;
        }

        private static decimal ScoreBySurveyBudget(decimal? maxBudget, decimal? basePrice)
        {
            if (!maxBudget.HasValue || !basePrice.HasValue)
            {
                return 0m;
            }

            if (basePrice.Value <= maxBudget.Value)
            {
                return 2m;
            }

            var overByRatio = (basePrice.Value - maxBudget.Value) / Math.Max(1m, maxBudget.Value);
            return overByRatio <= 0.2m ? -0.5m : -2m;
        }

        private static string? GetCareLevelName(int? careLevelType)
        {
            if (!careLevelType.HasValue)
            {
                return null;
            }

            return Enum.IsDefined(typeof(CareLevelTypeEnum), careLevelType.Value)
                ? ((CareLevelTypeEnum)careLevelType.Value).ToString()
                : null;
        }
    }
}
