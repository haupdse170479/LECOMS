using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Shop
{
    public class GHNConnectionStatusDTO
    {
        /// <summary>
        /// Shop đã cấu hình GHN Token và ShopId chưa
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// GHN Shop ID (nếu đã connect)
        /// </summary>
        public string? GHNShopId { get; set; }

        /// <summary>
        /// Thời gian kết nối (có thể dùng CreatedAt hoặc thêm field mới)
        /// </summary>
        public DateTime? ConnectedAt { get; set; }

        /// <summary>
        /// Message hướng dẫn user
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}
