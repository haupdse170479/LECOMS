using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Order
{
    public class PreviewCheckoutResultDTO
    {
        public List<PreviewShopOrderDTO> Orders { get; set; } = new();

        public decimal TotalAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal DiscountApplied { get; set; }

        public string? VoucherCodeUsed { get; set; }
        public int ServiceTypeId { get; set; }

        // Echo address (optional - giúp FE khỏi giữ state rời rạc)
        public string ShipToName { get; set; } = "";
        public string ShipToPhone { get; set; } = "";
        public string ShipToAddress { get; set; } = "";

        public int ToProvinceId { get; set; }
        public string ToProvinceName { get; set; } = "";
        public int ToDistrictId { get; set; }
        public string ToDistrictName { get; set; } = "";
        public string ToWardCode { get; set; } = "";
        public string ToWardName { get; set; } = "";
        public string? Note { get; set; }
    }

    public class PreviewShopOrderDTO
    {
        public string PreviewOrderId { get; set; } = ""; // temp id để map voucher breakdown
        public int ShopId { get; set; }
        public string ShopName { get; set; } = "";

        public decimal Subtotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }

        public int TotalWeight { get; set; }
        public string? EstimatedDeliveryText { get; set; }

        public List<PreviewOrderItemDTO> Items { get; set; } = new();
    }

    public class PreviewOrderItemDTO
    {
        public string ProductId { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string? ProductImage { get; set; }

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal => UnitPrice * Quantity;
    }
}
