using AutoMapper;
using LECOMS.Data.DTOs.Course;
using LECOMS.Data.DTOs.Product;
using LECOMS.Data.DTOs.Recombee;
using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using LECOMS.RepositoryContract.Interfaces;
using Microsoft.EntityFrameworkCore;
using Recombee.ApiClient;
using Recombee.ApiClient.ApiRequests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LECOMS.Service.Services
{
    public class RecombeeService
    {
        private readonly RecombeeClient _client;
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public RecombeeService(RecombeeClient client, IUnitOfWork uow, IMapper mapper)
        {
            _client = client;
            _uow = uow;
            _mapper = mapper;
        }
        private static ProductDTO MapProduct(Product p)
        {
            return new ProductDTO
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                Description = p.Description,

                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name,

                Price = p.Price,
                Stock = p.Stock,
                Status = p.Status,
                LastUpdatedAt = p.LastUpdatedAt,

                Images = p.Images.Select(i => new ProductImageDTO
                {
                    Url = i.Url,
                    OrderIndex = i.OrderIndex,
                    IsPrimary = i.IsPrimary
                }).ToList(),

                ThumbnailUrl = p.Images
                    .OrderBy(i => i.OrderIndex)
                    .Select(i => i.Url)
                    .FirstOrDefault(),

                ShopId = p.ShopId,
                ShopName = p.Shop?.Name,
                ShopAvatar = p.Shop?.ShopAvatar,
                ShopDescription = p.Shop?.Description,

                ApprovalStatus = p.ApprovalStatus,
                ModeratorNote = p.ModeratorNote,

                // ⭐ RATING
                RatingCount = p.Feedbacks.Count,
                AverageRating = p.Feedbacks.Any()
                    ? Math.Round(p.Feedbacks.Average(f => f.Rating), 1)
                    : 0
            };
        }

        // ===========================================================================
        // 1️⃣ SYNC PRODUCTS TO RECOMBEE
        // ===========================================================================
        public async Task<int> SyncProductsAsync()
        {
            var products = await _uow.Products.Query()
                .Include(p => p.Category)
                .Include(p => p.Shop)
                .Include(p => p.Images)
                .Where(p =>
                    p.Active == 1 &&
                    p.ApprovalStatus == ApprovalStatus.Approved &&
                    p.Status == ProductStatus.Published
                )
                .ToListAsync();
            int synced = 0;

            foreach (var p in products)
            {
                var itemValues = new Dictionary<string, object>
                {
                    ["name"] = p.Name,
                    ["slug"] = p.Slug,
                    ["categoryId"] = p.CategoryId,
                    ["categoryName"] = p.Category?.Name,
                    ["price"] = Convert.ToDouble(p.Price),
                    ["thumbnailUrl"] = p.Images.FirstOrDefault(i => i.IsPrimary)?.Url,
                    ["shopId"] = p.ShopId,
                    ["shopName"] = p.Shop?.Name,
                    ["status"] = p.Status.ToString()
                };

                await _client.SendAsync(new SetItemValues(p.Id, itemValues, cascadeCreate: true));
                synced++;
            }

            return synced;
        }
        public async Task<int> SyncCoursesAsync()
        {
            var courses = await _uow.Courses.Query()
                .Include(c => c.Category)
                .Include(c => c.Shop)
                .Where(c =>
                    c.Active == 1 &&
                    c.ApprovalStatus == ApprovalStatus.Approved
                )
                .ToListAsync();

            int synced = 0;

            foreach (var c in courses)
            {
                var itemValues = new Dictionary<string, object>
                {
                    ["type"] = "course",
                    ["title"] = c.Title,
                    ["slug"] = c.Slug,
                    ["categoryId"] = c.CategoryId,
                    ["categoryName"] = c.Category?.Name,
                    ["shopId"] = c.ShopId,
                    ["shopName"] = c.Shop?.Name,
                    ["thumbnailUrl"] = c.CourseThumbnail
                };

                await _client.SendAsync(
                    new SetItemValues(c.Id, itemValues, cascadeCreate: true)
                );

                synced++;
            }

            return synced;
        }

        // ===========================================================================
        // 2️⃣ HOMEPAGE BROWSE → RECOMMENDED + CATEGORY + BEST SELLER
        // ===========================================================================
        public async Task<BrowseResultDTO> GetBrowseDataAsync(string userId)
        {
            // Recommend items from Recombee
            var rec = await _client.SendAsync(
                new RecommendItemsToUser(userId, 20, cascadeCreate: true)
            );

            var recIds = rec.Recomms.Select(r => r.Id).ToList();

            var recommendedProducts = await _uow.Products.Query()
                .Include(p => p.Images)
                .Include(p => p.Category)
                .Include(p => p.Shop)
                .Include(p => p.Feedbacks)
                .Where(p => recIds.Contains(p.Id))
                .ToListAsync();

            var recommendedCategories = recommendedProducts
                .GroupBy(p => p.CategoryId)
                .Select(g => new
                {
                    id = g.Key,
                    name = g.First().Category.Name,
                    slug = g.First().Category.Slug,
                    products = _mapper.Map<IEnumerable<ProductDTO>>(g.Take(4))
                })
                .ToList();

            var bestIds = await _uow.OrderDetails.Query()
                .GroupBy(o => o.ProductId)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => g.Key)
                .ToListAsync();

            var bestSellerProducts = await _uow.Products.Query()
                .Include(p => p.Images)
                .Include(p => p.Category)
                .Include(p => p.Shop)
                .Include(p => p.Feedbacks)
                .Where(p => bestIds.Contains(p.Id))
                .ToListAsync();

            return new BrowseResultDTO
            {
                RecommendedProducts = recommendedProducts.Select(MapProduct).ToList(),
                RecommendedCategories = recommendedCategories,
                BestSellerProducts = bestSellerProducts.Select(MapProduct).ToList()
            };
        }


        // ===========================================================================
        // 3️⃣ SIMILAR PRODUCTS (ITEM → ITEM)
        // ===========================================================================
        public async Task<IEnumerable<ProductDTO>> GetSimilarProductsFullAsync(string productId, string userId)
        {
            var rec = await _client.SendAsync(
                new RecommendItemsToItem(productId, userId, 20, cascadeCreate: true)
            );

            var ids = rec.Recomms.Select(r => r.Id).ToList();

            var products = await _uow.Products.Query()
                .Include(p => p.Images)
                .Include(p => p.Category)
                .Include(p => p.Shop)
                .Include(p => p.Feedbacks)
                .Where(p =>
                    ids.Contains(p.Id) &&
                    p.Active == 1 &&
                    p.Status == ProductStatus.Published &&
                    p.ApprovalStatus == ApprovalStatus.Approved
                )
                .ToListAsync();

            return products.Select(MapProduct).ToList();
        }

        // ===========================================================================
        // 4️⃣ SIMILAR COURSES (PRODUCT → COURSE)
        // ===========================================================================
        public async Task<IEnumerable<CourseDTO>> GetSimilarCoursesFullAsync(string itemId, string userId)
        {
            var rec = await _client.SendAsync(
                new RecommendItemsToItem(itemId, userId, 20, cascadeCreate: true)
            );

            var ids = rec.Recomms.Select(r => r.Id).ToList();

            var courses = await _uow.Courses.Query()
                .Include(c => c.Category)
                .Include(c => c.Shop)
                .Where(c => ids.Contains(c.Id))
                .ToListAsync();

            return _mapper.Map<IEnumerable<CourseDTO>>(courses);
        }

        // ===========================================================================
        // 5️⃣ RECOMMEND PRODUCTS FOR USER (FULL DTO)
        // ===========================================================================
        public async Task<IEnumerable<ProductDTO>> RecommendProductsForUserAsync(string userId)
        {
            var rec = await _client.SendAsync(
                new RecommendItemsToUser(userId, 20, cascadeCreate: true)
            );

            var ids = rec.Recomms.Select(r => r.Id).ToList();

            var products = await _uow.Products.Query()
                .Include(p => p.Images)
                .Include(p => p.Category)
                .Include(p => p.Shop)
                .Include(p => p.Feedbacks)
                .Where(p => ids.Contains(p.Id))
                .ToListAsync();

            return products.Select(MapProduct).ToList();
        }

        // ===========================================================================
        // 6️⃣ RECOMMEND COURSES FOR USER (FULL DTO)
        // ===========================================================================
        public async Task<IEnumerable<CourseDTO>> RecommendCoursesForUserAsync(string userId)
        {
            var rec = await _client.SendAsync(
                new RecommendItemsToUser(userId, 20, cascadeCreate: true)
            );

            var ids = rec.Recomms.Select(r => r.Id).ToList();

            var courses = await _uow.Courses.Query()
                .Include(c => c.Category)
                .Include(c => c.Shop)
                .Where(c =>
                ids.Contains(c.Id) &&
                c.Active == 1 &&
                c.ApprovalStatus == ApprovalStatus.Approved
                )

                .ToListAsync();

            return _mapper.Map<IEnumerable<CourseDTO>>(courses);
        }
        public async Task<object> GetBrowseFeedAsync(string userId)
        {
            // --------------------------------------------------
            // 1) Recommended items (Recombee)
            // --------------------------------------------------
            var rec = await _client.SendAsync(
                new RecommendItemsToUser(userId, 20, cascadeCreate: true)
            );

            var recIds = rec.Recomms.Select(r => r.Id).ToList();

            // Nếu Recombee rỗng → fallback mặc định
            var recommendedProducts = await _uow.Products.Query()
                .Include(p => p.Images)
                .Include(p => p.Category)
                .Include(p => p.Shop)
                .Include(p => p.Feedbacks)
                .Where(p => recIds.Contains(p.Id))
                .ToListAsync();

            if (!recommendedProducts.Any())
            {
                recommendedProducts = await _uow.Products.Query()
                    .Include(p => p.Images)
                    .Include(p => p.Category)
                    .Include(p => p.Shop)
                    .Include(p => p.Feedbacks)
                    .OrderByDescending(p => p.LastUpdatedAt)
                    .Take(20)
                    .ToListAsync();
            }

            var recommendedProductsDto =
                recommendedProducts.Select(MapProduct).ToList();

            // --------------------------------------------------
            // 2) Recommended categories (group theo category)
            // --------------------------------------------------
            var recommendedCategories = recommendedProducts
                .GroupBy(p => p.CategoryId)
                .Select(g => new
                {
                    id = g.Key,
                    name = g.First().Category.Name,
                    slug = g.First().Category.Slug,
                    products = g.Take(8).Select(MapProduct).ToList()
                })
                .ToList();

            // --------------------------------------------------
            // 3) Best Sellers
            // --------------------------------------------------
            var bestIds = await _uow.OrderDetails.Query()
                .GroupBy(o => o.ProductId)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => g.Key)
                .ToListAsync();

            var bestSeller = await _uow.Products.Query()
                .Include(p => p.Images)
                .Include(p => p.Category)
                .Include(p => p.Shop)
                .Include(p => p.Feedbacks)
                .Where(p => bestIds.Contains(p.Id))
                .ToListAsync();

            var bestSellerDto =
                bestSeller.Select(MapProduct).ToList();

            // --------------------------------------------------
            // 4) Trending (7 ngày gần nhất)
            // --------------------------------------------------
            var trendingIds = await _uow.OrderDetails.Query()
                .Include(od => od.Order)
                .Where(od => od.Order.CreatedAt >= DateTime.UtcNow.AddDays(-7))
                .GroupBy(od => od.ProductId)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => g.Key)
                .ToListAsync();

            var trendingProducts = await _uow.Products.Query()
                .Include(p => p.Images)
                .Include(p => p.Category)
                .Include(p => p.Shop)
                .Include(p => p.Feedbacks)
                .Where(p => trendingIds.Contains(p.Id))
                .ToListAsync();

            var trendingDto =
                trendingProducts.Select(MapProduct).ToList();

            // --------------------------------------------------
            // 5) New Arrivals (sản phẩm mới nhất)
            // --------------------------------------------------
            var newArrival = await _uow.Products.Query()
                .Include(p => p.Images)
                .Include(p => p.Category)
                .Include(p => p.Shop)
                .Include(p => p.Feedbacks)
                .OrderByDescending(p => p.LastUpdatedAt)
                .Take(12)
                .ToListAsync();

            var newArrivalDto =
                newArrival.Select(MapProduct).ToList();

            // --------------------------------------------------
            // FINAL RESPONSE
            // --------------------------------------------------
            return new
            {
                recommendedProducts = recommendedProductsDto,
                recommendedCategories = recommendedCategories,
                trendingProducts = trendingDto,
                bestSellerProducts = bestSellerDto,
                newArrivalProducts = newArrivalDto
            };
        }
        public async Task<object> GetBrowseCoursesFeedAsync(string userId)
        {
            // --------------------------------------------------
            // 1) Recommended courses từ Recombee
            // --------------------------------------------------
            var rec = await _client.SendAsync(
                new RecommendItemsToUser(userId, 20, cascadeCreate: true)
            );

            var recIds = rec.Recomms.Select(r => r.Id).ToList();

            List<Course> recommendedCourses;

            // Nếu Recombee rỗng → fallback
            if (!recIds.Any())
            {
                recommendedCourses = await _uow.Courses.Query()
                    .Include(c => c.Category)
                    .Include(c => c.Shop)
                    .OrderByDescending(c => c.Id)   // fallback theo tạo mới
                    .Take(20)
                    .ToListAsync();
            }
            else
            {
                recommendedCourses = await _uow.Courses.Query()
                    .Include(c => c.Category)
                    .Include(c => c.Shop)
                    .Where(c => recIds.Contains(c.Id))
                    .ToListAsync();
            }

            var recommendedCoursesDto = _mapper.Map<IEnumerable<CourseDTO>>(recommendedCourses);


            // --------------------------------------------------
            // 2) Recommended categories (không bao giờ rỗng)
            // --------------------------------------------------
            List<object> recommendedCategories = new List<object>();

            if (recommendedCourses.Any())
            {
                // Lấy categories từ recommended
                recommendedCategories = recommendedCourses
                    .GroupBy(c => c.CategoryId)
                    .Select(g => new
                    {
                        id = g.Key,
                        name = g.First().Category.Name,
                        slug = g.First().Category.Slug,
                        courses = _mapper.Map<IEnumerable<CourseDTO>>(g.Take(8))
                    })
                    .ToList<object>();
            }
            else
            {
                // --------------------------------------------------
                // Fallback nếu recommendedCourses rỗng
                // --------------------------------------------------

                var allCourses = await _uow.Courses.Query()
                    .Include(c => c.Category)
                    .ToListAsync(); // load vào memory

                var topCategories = allCourses
                    .GroupBy(c => c.CategoryId)
                    .OrderByDescending(g => g.Count())
                    .Take(3)
                    .ToList();

                recommendedCategories = topCategories
                    .Select(g => new
                    {
                        id = g.Key,
                        name = g.First().Category.Name,
                        slug = g.First().Category.Slug,
                        courses = _mapper.Map<IEnumerable<CourseDTO>>(g.Take(8))
                    })
                    .ToList<object>();
            }


            // --------------------------------------------------
            // 3) New Arrival Courses (fallback thêm dữ liệu)
            // --------------------------------------------------
            var newArrivals = await _uow.Courses.Query()
                .Include(c => c.Category)
                .Include(c => c.Shop)
                .OrderByDescending(c => c.Id)
                .Take(10)
                .ToListAsync();

            var newArrivalsDto = _mapper.Map<IEnumerable<CourseDTO>>(newArrivals);


            // --------------------------------------------------
            // 4) Popular Categories (nếu cần thêm section)
            // --------------------------------------------------
            var popularCategories = await _uow.Courses.Query()
                .Include(c => c.Category)
                .GroupBy(c => c.CategoryId)
                .Select(g => new
                {
                    id = g.Key,
                    name = g.First().Category.Name,
                    slug = g.First().Category.Slug,
                    count = g.Count()
                })
                .OrderByDescending(x => x.count)
                .Take(5)
                .ToListAsync();


            // --------------------------------------------------
            // FINAL RESPONSE
            // --------------------------------------------------
            return new
            {
                recommendedCourses = recommendedCoursesDto,
                recommendedCategories = recommendedCategories,
                newArrivalCourses = newArrivalsDto,
                popularCategories = popularCategories
            };
        }

    }
}
