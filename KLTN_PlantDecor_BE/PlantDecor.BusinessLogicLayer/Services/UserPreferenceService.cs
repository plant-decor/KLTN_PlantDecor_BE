using Microsoft.EntityFrameworkCore;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using System.Globalization;
using System.Text;

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

        public async Task CalculatedAllUserPreferenceAsync()
        {
            var users = await _context.Users
                .Include(u => u.UserProfile)
                .Where(u => u.Status == (int)UserStatusEnum.Active && u.RoleId == (int)RoleEnum.Customer)
                .ToListAsync();

            await CalculatePreferencesForUsersAsync(users);
        }

        public async Task CalculateUserPreferenceForUserAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.UserProfile)
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

            // TEST DỮ LIỆU HARDCODE  ******************************************************
            //var completedOrderPlants = await _context.OrderItems
            //    .Where(oi => oi.Order != null && targetUserIds.Contains(oi.Order.UserId) && oi.Order.Status == (int)OrderStatusEnum.Completed)
            //    .Select(oi => new
            //    {
            //        UserId = oi.Order.UserId,
            //        PlantId = oi.CommonPlant != null
            //            ? oi.CommonPlant.PlantId
            //            : (oi.PlantInstance != null ? oi.PlantInstance.PlantId : (int?)null)
            //    })
            //    .Where(x => x.PlantId != null)
            //    .Select(x => new { x.UserId, PlantId = x.PlantId!.Value })
            //    .Distinct()
            //    .ToListAsync();
            var completedOrderPlants = 1;

            var existingPreferences = await _context.UserPreferences
                .Where(p => p.UserId != null && targetUserIds.Contains(p.UserId.Value))
                .ToListAsync();

            var behaviorScoreByUserPlant = behaviorLogs
                .GroupBy(log => (UserId: log.UserId!.Value, PlantId: log.PlantId!.Value))
                .ToDictionary(
                    g => g.Key,
                    g => Math.Min(10m, g.Sum(log => ActionScoreMap.TryGetValue(log.ActionType!.Value, out var score) ? score : 0m))
                );

            // TEST DỮ LIỆU HARDCODE  ************************************************
            //var purchasedPairs = completedOrderPlants
            //    .Select(x => (x.UserId, x.PlantId))
            //    .ToHashSet();
            var purchasedPairs = 1;

            var preferenceMap = existingPreferences
                .Where(p => p.UserId != null && p.PlantId != null)
                .ToDictionary(p => (p.UserId!.Value, p.PlantId!.Value));

            var toInsert = new List<UserPreference>();
            var toRemove = new List<UserPreference>();

            foreach (var user in users)
            {
                string userElement = "None";
                if (user.UserProfile?.BirthYear != null)
                {
                    userElement = GetFengShuiElement(user.UserProfile.BirthYear.Value);
                }

                foreach (var plant in plants)
                {
                    // 1. Profile Match Score
                    decimal profileScore = 0;
                    if (IsEasyCare(plant.CareLevelType)) profileScore += 3;
                    if (plant.ChildSafe == true || plant.PetSafe == true) profileScore += 2;
                    profileScore += CalculateFengShuiScore(userElement, plant.FengShuiElement);

                    if (profileScore > 10) profileScore = 10;
                    if (profileScore < 0) profileScore = 0;

                    // 2. Behavior Score
                    behaviorScoreByUserPlant.TryGetValue((user.Id, plant.Id), out var behaviorScore);

                    // 3. Purchase History Score (Test dữ liệu hardcode) ************************
                    //decimal purchaseScore = purchasedPairs.Contains((user.Id, plant.Id)) ? 10 : 0;
                    var purchaseScore = 0m;

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
                .Select(p => NormalizeElement(p.FengShuiElement))
                .Where(x => !string.IsNullOrEmpty(x))
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

                    if (seedElements.Contains(NormalizeElement(plant.FengShuiElement)))
                    {
                        contextualScore += 3m;
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

        private string GetFengShuiElement(int birthYear)
        {
            int canValue = birthYear % 10 switch
            {
                0 or 1 => 4,
                2 or 3 => 5,
                4 or 5 => 1,
                6 or 7 => 2,
                8 or 9 => 3,
                _ => 0
            };

            int chiValue = (birthYear % 12) switch
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
                1 => "Kim",
                2 => "Thủy",
                3 => "Hỏa",
                4 => "Thổ",
                5 => "Mộc",
                _ => "None"
            };
        }

        private int CalculateFengShuiScore(string userElement, string plantElement)
        {
            if (string.IsNullOrEmpty(userElement) || string.IsNullOrEmpty(plantElement) || userElement == "None")
                return 0;

            var normalizedUserElement = NormalizeElement(userElement);
            var normalizedPlantElement = NormalizeElement(plantElement);

            if (string.IsNullOrEmpty(normalizedUserElement) || string.IsNullOrEmpty(normalizedPlantElement))
            {
                return 0;
            }

            if (normalizedUserElement == normalizedPlantElement)
                return 5;

            bool isTuongSinh =
                (normalizedUserElement == "thuy" && normalizedPlantElement == "moc") ||
                (normalizedUserElement == "moc" && normalizedPlantElement == "hoa") ||
                (normalizedUserElement == "hoa" && normalizedPlantElement == "tho") ||
                (normalizedUserElement == "tho" && normalizedPlantElement == "kim") ||
                (normalizedUserElement == "kim" && normalizedPlantElement == "thuy") ||
                (normalizedPlantElement == "thuy" && normalizedUserElement == "moc") ||
                (normalizedPlantElement == "moc" && normalizedUserElement == "hoa") ||
                (normalizedPlantElement == "hoa" && normalizedUserElement == "tho") ||
                (normalizedPlantElement == "tho" && normalizedUserElement == "kim") ||
                (normalizedPlantElement == "kim" && normalizedUserElement == "thuy");

            if (isTuongSinh)
                return 5;

            bool isTuongKhac =
                (normalizedUserElement == "thuy" && normalizedPlantElement == "hoa") ||
                (normalizedUserElement == "hoa" && normalizedPlantElement == "kim") ||
                (normalizedUserElement == "kim" && normalizedPlantElement == "moc") ||
                (normalizedUserElement == "moc" && normalizedPlantElement == "tho") ||
                (normalizedUserElement == "tho" && normalizedPlantElement == "thuy") ||
                (normalizedPlantElement == "thuy" && normalizedUserElement == "hoa") ||
                (normalizedPlantElement == "hoa" && normalizedUserElement == "kim") ||
                (normalizedPlantElement == "kim" && normalizedUserElement == "moc") ||
                (normalizedPlantElement == "moc" && normalizedUserElement == "tho") ||
                (normalizedPlantElement == "tho" && normalizedUserElement == "thuy");

            if (isTuongKhac)
                return -2;

            return 0;
        }

        private static bool IsEasyCare(int? careLevelType)
        {
            return careLevelType == (int)CareLevelTypeEnum.Easy;
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

        private static string NormalizeElement(string? element)
        {
            var normalized = NormalizeText(element);
            return normalized switch
            {
                "kim" => "kim",
                "thuy" => "thuy",
                "moc" => "moc",
                "hoa" => "hoa",
                "tho" => "tho",
                _ => string.Empty
            };
        }

        private static string NormalizeText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = value.Trim().Normalize(NormalizationForm.FormD);
            var chars = normalized
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .ToArray();

            return new string(chars).Normalize(NormalizationForm.FormC).ToLowerInvariant();
        }
    }
}
